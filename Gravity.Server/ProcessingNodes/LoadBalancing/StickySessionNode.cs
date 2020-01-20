using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;
using Gravity.Server.Utility;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.LoadBalancing
{
    internal class StickySessionNode: LoadBalancerNode
    {
        public string SessionCookie { get; set; }
        public TimeSpan SessionDuration { get; set; }

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
                                lock (_sessionNodes) _sessionNodes.Remove(sessionId);
                            }
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        return;
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

        public override void Dispose()
        {
            _cleanupThread.Abort();
            _cleanupThread.Join(TimeSpan.FromSeconds(10));
        }

        public override Task ProcessRequest(IRequestContext context)
        {
            if (Disabled)
            {
                return Task.Run(() =>
                {
                    context.Outgoing.StatusCode = 503;
                    context.Outgoing.ReasonPhrase = "Balancer " + Name + " is disabled";
                    context.Outgoing.SendHeaders(context);
                });
            }

            if (string.IsNullOrEmpty(SessionCookie))
            {
                return Task.Run(() =>
                {
                    context.Outgoing.StatusCode = 503;
                    context.Outgoing.ReasonPhrase = "Balancer " + Name + " has no session cookie configured";
                    context.Outgoing.SendHeaders(context);
                });
            }

            var cookies = context.Incoming.GetCookies();
            var sessionId = cookies.ContainsKey(SessionCookie) ? cookies[SessionCookie] : null;
            long startTime;

            if (string.IsNullOrEmpty(sessionId))
            {
                var output = OutputNodes
                    .Where(o => !o.Disabled && o.Node != null)
                    .OrderBy(o => o.ConnectionCount)
                    .ThenBy(o => o.SessionCount)
                    .FirstOrDefault();

                if (output == null)
                {
                    return Task.Run(() =>
                    {
                        context.Outgoing.StatusCode = 503;
                        context.Outgoing.ReasonPhrase = "Balancer " + Name + " has no enabled outputs";
                        context.Outgoing.SendHeaders(context);
                    });
                }

                startTime = output.TrafficAnalytics.BeginRequest();
                output.IncrementConnectionCount();

                context.Outgoing.OnSendHeaders.Add(ctx =>
                {
                    if (ctx.Outgoing.Headers.TryGetValue("Set-Cookie", out var setCookieHeaders))
                    {
                        var setSession = setCookieHeaders.FirstOrDefault(c => c.StartsWith(SessionCookie + "="));
                        if (setSession != null)
                        {
                            var start = SessionCookie.Length + 1;
                            var end = setSession.IndexOf(';', start);
                            if (end < 0) end = setSession.Length;
                            sessionId = setSession.Substring(start, end - start);

                            output.IncrementSessionCount();
                            lock (_sessionNodes) _sessionNodes[sessionId] = output;
                            lock (_sessionExpiry)
                                _sessionExpiry.Add(new Tuple<string, DateTime>(sessionId,
                                    DateTime.UtcNow + SessionDuration));
                        }
                    }
                });

                return output.Node.ProcessRequest(context)
                    .ContinueWith(nodeTask =>
                    {
                        output.TrafficAnalytics.EndRequest(startTime);
                        output.DecrementConnectionCount();

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
                    return Task.Run(() =>
                    {
                        context.Outgoing.StatusCode = 503;
                        context.Outgoing.ReasonPhrase = "Balancer " + Name + " has no enabled outputs";
                        context.Outgoing.SendHeaders(context);
                    });
                }

                sessionOutput.IncrementSessionCount();
                lock (_sessionNodes) _sessionNodes[sessionId] = sessionOutput;
                lock (_sessionExpiry) _sessionExpiry.Add(new Tuple<string, DateTime>(sessionId, DateTime.UtcNow + SessionDuration));
            }

            if (sessionOutput.Disabled)
            {
                return Task.Run(() =>
                {
                    context.Outgoing.StatusCode = 503;
                    context.Outgoing.ReasonPhrase = "Balancer " + Name + " sticky output is down";
                    context.Outgoing.SendHeaders(context);
                });
            }

            startTime = sessionOutput.TrafficAnalytics.BeginRequest();
            sessionOutput.IncrementConnectionCount();

            return sessionOutput.Node.ProcessRequest(context)
                .ContinueWith(t =>
                {
                    sessionOutput.TrafficAnalytics.EndRequest(startTime);
                    sessionOutput.DecrementConnectionCount();
                });
        }
    }
}