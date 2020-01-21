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

        private readonly IDictionary<string, NodeOutput> _sessionNodes;
        private readonly List<Tuple<string, DateTime>> _sessionExpiry;
        private readonly Thread _cleanupThread;

        public StickySessionNode()
        {
            SessionDuration = TimeSpan.FromHours(1);

            _sessionNodes = new DefaultDictionary<string, NodeOutput>(StringComparer.OrdinalIgnoreCase);
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
                context.Log?.Log(LogType.Logic, LogLevel.Important, () => $"Sticky session load balancer '{Name}' is disabled by configuration");

                return Task.Run(() =>
                {
                    context.Outgoing.StatusCode = 503;
                    context.Outgoing.ReasonPhrase = "Balancer " + Name + " is disabled";
                    context.Outgoing.SendHeaders(context);
                });
            }

            if (string.IsNullOrEmpty(SessionCookie))
            {
                context.Log?.Log(LogType.Logic, LogLevel.Important, () => $"Sticky session load balancer '{Name}' has no cookie name configured");

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
                context.Log?.Log(LogType.Logic, LogLevel.Detailed, () => $"No session cookie in incoming request");

                var output = OutputNodes
                    .Where(o => !o.Disabled && o.Node != null)
                    .OrderBy(o => o.ConnectionCount)
                    .ThenBy(o => o.SessionCount)
                    .FirstOrDefault();

                if (output == null)
                {
                    context.Log?.Log(LogType.Logic, LogLevel.Important, () => $"Sticky session load balancer '{Name}' has no enabled outputs");

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
                    context.Log?.Log(LogType.Logic, LogLevel.Detailed, () => $"Sticky session load balancer '{Name}' looking for a Set-Cookie header in the response");

                    if (ctx.Outgoing.Headers.TryGetValue("Set-Cookie", out var setCookieHeaders))
                    {
                        var setSession = setCookieHeaders.FirstOrDefault(c => c.StartsWith(SessionCookie + "="));
                        if (setSession != null)
                        {
                            context.Log?.Log(LogType.Logic, LogLevel.Detailed, () => $"A session cookie was found, this caller will be sticky to output '{output.Name}'");

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

                context.Log?.Log(LogType.Logic, LogLevel.Standard, () => $"Sticky session load balancer '{Name}' routing request to '{output.Name}'");

                var task = output.Node.ProcessRequest(context);

                if (task == null)
                {
                    output.TrafficAnalytics.EndRequest(startTime);
                    return null;
                }

                return task.ContinueWith(t =>
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
                context.Log?.Log(LogType.Logic, LogLevel.Standard, () => $"No session found for session id {sessionId}");

                sessionOutput = OutputNodes
                    .Where(o => !o.Disabled && o.Node != null)
                    .OrderBy(o => o.ConnectionCount)
                    .ThenBy(o => o.SessionCount)
                    .FirstOrDefault();

                if (sessionOutput == null)
                {
                    context.Log?.Log(LogType.Logic, LogLevel.Important, () => $"Sticky session load balancer '{Name}' has no enabled outputs");

                    return Task.Run(() =>
                    {
                        context.Outgoing.StatusCode = 503;
                        context.Outgoing.ReasonPhrase = "Balancer " + Name + " has no enabled outputs";
                        context.Outgoing.SendHeaders(context);
                    });
                }

                context.Log?.Log(LogType.Logic, LogLevel.Standard, () => $"Sticking this session to least connected output '{sessionOutput.Name}'");

                sessionOutput.IncrementSessionCount();
                lock (_sessionNodes) _sessionNodes[sessionId] = sessionOutput;
                lock (_sessionExpiry) _sessionExpiry.Add(new Tuple<string, DateTime>(sessionId, DateTime.UtcNow + SessionDuration));
            }

            if (sessionOutput.Disabled)
            {
                context.Log?.Log(LogType.Logic, LogLevel.Important, () => $"The sticky output '{sessionOutput.Name}' for load balancer '{Name}' for session id {sessionId} is disabled");

                return Task.Run(() =>
                {
                    context.Outgoing.StatusCode = 503;
                    context.Outgoing.ReasonPhrase = "Balancer " + Name + " sticky output is down";
                    context.Outgoing.SendHeaders(context);
                });
            }

            context.Log?.Log(LogType.Logic, LogLevel.Standard, () => $"Sticky session load balancer '{Name}' routing request to '{sessionOutput.Name}'");

            startTime = sessionOutput.TrafficAnalytics.BeginRequest();
            sessionOutput.IncrementConnectionCount();

            var sessionTask = sessionOutput.Node.ProcessRequest(context);

            if (sessionTask == null)
            {
                sessionOutput.TrafficAnalytics.EndRequest(startTime);
                return null;
            }

            return sessionTask.ContinueWith(t =>
            {
                sessionOutput.TrafficAnalytics.EndRequest(startTime);
                sessionOutput.DecrementConnectionCount();
            });
        }
    }
}