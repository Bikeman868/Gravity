using System;
using System.Threading;
using System.Threading.Tasks;
using Gravity.Server.Configuration;
using Gravity.Server.Interfaces;
using Gravity.Server.Utility;
using Microsoft.Owin;
using OwinFramework.Interfaces.Builder;

namespace Gravity.Server.ProcessingNodes
{
    internal class RequestListener: IRequestListener
    {
        private static readonly object _lock = new object();

        private readonly INodeGraph _nodeGraph;
        private readonly ILogFactory _logFactory;

        private ListenerConfiguration _configuration;
        private readonly IDisposable _configurationRegistration;

        private Thread _trafficUpdateThread;

        public RequestListener(
            IConfiguration configuration,
            INodeGraph nodeGraph,
            ILogFactory logFactory)
        {
            _nodeGraph = nodeGraph;
            _logFactory = logFactory;

            _configurationRegistration = configuration.Register(
                "/gravity/listener",
                c => _configuration = c.Sanitize(),
                new ListenerConfiguration());

            _trafficUpdateThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        Thread.Sleep(3000);
                        var endpoints = _configuration.Endpoints;
                        if (endpoints != null)
                        {
                            foreach (var endpoint in endpoints)
                            {
                                if (endpoint.ProcessingNode != null)
                                    endpoint.ProcessingNode.TrafficAnalytics.Recalculate();
                            }
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    catch
                    {
                    }
                }
            })
            {
                IsBackground = true,
                Priority = ThreadPriority.Lowest
            };

            _trafficUpdateThread.Start();
        }

        ListenerEndpointConfiguration[] IRequestListener.Endpoints { get { return _configuration.Endpoints; } }

        Task IRequestListener.ProcessRequest(IOwinContext context, Func<Task> next)
        {
            var configuration = _configuration;
            if (configuration.Disabled) return next();

            var localIp = context.Request.LocalIpAddress;
            var localPort = context.Request.LocalPort;

            var endpoints = _configuration.Endpoints;
            for (var i = 0; i < endpoints.Length; i++)
            {
                var endpoint = endpoints[i];
                if (endpoint.Disabled) continue;

                if (endpoint.IpAddress == "*" || endpoint.IpAddress == localIp)
                {
                    if (localPort.HasValue && endpoint.PortNumber != 0 &&
                        localPort.Value != endpoint.PortNumber) continue;

                    var output = endpoint.ProcessingNode;

                    if (output == null)
                    {
                        output = new NodeOutput
                        {
                            Name = endpoint.NodeName,
                            Node = _nodeGraph.NodeByName(endpoint.NodeName),
                        };
                        endpoint.ProcessingNode = output;
                    }
                    else
                    {
                        output.Node = _nodeGraph.NodeByName(endpoint.NodeName);
                    }

                    if (output.Node == null)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ReasonPhrase = "Listener endpoint configuration error";
                        return context.Response.WriteAsync(string.Empty);
                    }

                    if (output.Disabled)
                    {
                        context.Response.StatusCode = 503;
                        context.Response.ReasonPhrase = "Listener endpoint disabled";
                        return context.Response.WriteAsync(string.Empty);
                    }

                    using (var log = _logFactory.Create(context))
                    {
                        var startTime = output.TrafficAnalytics.BeginRequest();
#if DEBUG
                        lock (_lock)
#endif
                        {
                            var task = output.Node.ProcessRequest(context, log);

                            if (task == null)
                                return next();

                            return task.ContinueWith(t => output.TrafficAnalytics.EndRequest(startTime));
                        }
                    }
                }
            }

            context.Response.StatusCode = 500;
            context.Response.ReasonPhrase = "No matching endpoints";
            return context.Response.WriteAsync(string.Empty);
        }
    }
}