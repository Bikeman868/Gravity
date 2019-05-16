using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gravity.Server.DataStructures;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes
{
    internal class StickySessionNode: INode
    {
        public string Name { get; set; }
        public string[] Outputs { get; set; }
        public bool Disabled { get; set; }
        public string SessionCookie { get; set; }
        public TimeSpan SessionDuration { get; set; }

        public NodeOutput[] OutputNodes;

        private readonly Dictionary<string, NodeOutput> _sessionNodes;
        private readonly List<Tuple<string, DateTime>> _sessionExpiry;
        private readonly Thread _cleanupThread;

        public StickySessionNode()
        {
            SessionDuration = TimeSpan.FromHours(1);

            _sessionNodes = new Dictionary<string, NodeOutput>();
            _sessionExpiry = new List<Tuple<string, DateTime>>();

            _cleanupThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        Thread.Sleep(1000);
                        while (true)
                        {
                            var now = DateTime.UtcNow;
                            Tuple<string, DateTime> expiry;
                            lock (_sessionExpiry)
                            {
                                if (_sessionExpiry.Count == 0) break;
                                expiry = _sessionExpiry[0];
                                if (now < expiry.Item2) break;
                                _sessionExpiry.RemoveAt(0);
                            }
                            var sessionId = expiry.Item1;

                            NodeOutput output;
                            bool hasSession;
                            lock (_sessionNodes) hasSession = _sessionNodes.TryGetValue(sessionId, out output);

                            if (hasSession)
                            {
                                output.DecrementSessionCount();
                                lock(_sessionNodes) _sessionNodes.Remove(sessionId);
                            }
                        }
                    }
                    catch
                    { }
                }
            })
            {
                Name = "Sticky session cleanup",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };

            _cleanupThread.Start();
        }

        void INode.Bind(INodeGraph nodeGraph)
        {
            OutputNodes = Outputs.Select(name => new NodeOutput
            {
                Name = name,
                Node = nodeGraph.NodeByName(name),
            }).ToArray();
        }

        Task INode.ProcessRequest(IOwinContext context)
        {
            if (Disabled)
            {
                context.Response.StatusCode = 503;
                context.Response.ReasonPhrase = "Balancer " + Name + " is disabled";
                return context.Response.WriteAsync(string.Empty);
            }

            if (string.IsNullOrEmpty(SessionCookie))
            {
                context.Response.StatusCode = 503;
                context.Response.ReasonPhrase = "Balancer " + Name + " has no session cookie configured";
                return context.Response.WriteAsync(string.Empty);
            }

            var sessionId = context.Request.Cookies[SessionCookie];

            if (string.IsNullOrEmpty(sessionId))
            {
                var output = OutputNodes
                    .Where(o => !o.Disabled && o.Node != null)
                    .OrderBy(o => o.ConnectionCount)
                    .ThenBy(o => o.SessionCount)
                    .FirstOrDefault();

                if (output == null)
                {
                    context.Response.StatusCode = 503;
                    context.Response.ReasonPhrase = "Balancer " + Name + " has no enabled outputs";
                    return context.Response.WriteAsync(string.Empty);
                }

                output.IncrementRequestCount();
                output.IncrementConnectionCount();

                return output.Node.ProcessRequest(context).ContinueWith(t =>
                {
                    output.DecrementConnectionCount();

                    var setCookieHeaders = context.Response.Headers.FirstOrDefault(h => h.Key == "Set-Cookie");
                    if (setCookieHeaders.Value != null && setCookieHeaders.Value.Length > 0)
                    {
                        var setSession = setCookieHeaders.Value.FirstOrDefault(c => c.StartsWith(SessionCookie + "="));
                        if (setSession != null)
                        {
                            var start = SessionCookie.Length + 1;
                            var end = setSession.IndexOf(';', start);
                            if (end < 0) end = setSession.Length;
                            sessionId = setSession.Substring(start, end - start);

                            output.IncrementSessionCount();
                            lock (_sessionNodes) _sessionNodes[sessionId] = output;
                            lock (_sessionExpiry) _sessionExpiry.Add(new Tuple<string, DateTime>(sessionId, DateTime.UtcNow + SessionDuration));
                        }
                    }
                });
            }

            NodeOutput sessionOutput;
            bool hasSession;
            lock (_sessionNodes) hasSession = _sessionNodes.TryGetValue(sessionId, out sessionOutput);

            if (!hasSession)
            {
                sessionOutput = OutputNodes
                    .Where(o => !o.Disabled && o.Node != null)
                    .OrderBy(o => o.ConnectionCount)
                    .ThenBy(o => o.SessionCount)
                    .FirstOrDefault();

                if (sessionOutput == null)
                {
                    context.Response.StatusCode = 503;
                    context.Response.ReasonPhrase = "Balancer " + Name + " has no enabled outputs";
                    return context.Response.WriteAsync(string.Empty);
                }

                sessionOutput.IncrementSessionCount();
                lock (_sessionNodes) _sessionNodes[sessionId] = sessionOutput;
                lock (_sessionExpiry) _sessionExpiry.Add(new Tuple<string, DateTime>(sessionId, DateTime.UtcNow + SessionDuration));
            }

            if (sessionOutput.Disabled)
            {
                context.Response.StatusCode = 503;
                context.Response.ReasonPhrase = "Balancer " + Name + " sticky output is down";
                return context.Response.WriteAsync(string.Empty);
            }

            sessionOutput.IncrementRequestCount();
            sessionOutput.IncrementConnectionCount();

            return sessionOutput.Node.ProcessRequest(context).ContinueWith(t =>
            {
                sessionOutput.DecrementConnectionCount();
            });
        }
    }
}