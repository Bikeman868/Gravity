using System;
using System.Threading;
using System.Threading.Tasks;
using Gravity.Server.Configuration;
using Gravity.Server.Interfaces;
using Gravity.Server.Utility;
using Microsoft.Owin;
using OwinFramework.Interfaces.Builder;

namespace Gravity.Server.Pipeline
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

        Task IRequestListener.ProcessRequest(IOwinContext owinContext, Func<Task> next)
        {
            var configuration = _configuration;
            if (configuration.Disabled) return next();

            var localIp = owinContext.Request.LocalIpAddress;
            var localPort = owinContext.Request.LocalPort;

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
                        return Task.Run(() =>
                        {
                            owinContext.Response.StatusCode = 500;
                            owinContext.Response.ReasonPhrase = "Listener endpoint configuration error";
                        });
                    }

                    if (output.Disabled) continue;

                    var requestContext = (IRequestContext)new OwinRequestContext(owinContext, _logFactory);
                    requestContext.Log?.Log(LogType.Request, LogLevel.Standard, () => 
                        $"Starting new request to {requestContext.Incoming.Method} {requestContext.Incoming.Scheme.ToString().ToLower()}://{requestContext.Incoming.DomainName}{requestContext.Incoming.Path}{requestContext.Incoming.Query}");

                    var startTime = output.TrafficAnalytics.BeginRequest();

                    var task = output.Node.ProcessRequest(requestContext);

                    if (task == null)
                    {
                        requestContext.Log?.Log(LogType.Request, LogLevel.Standard, () => $"Completed request with transfer to Owin pipeline");
                        requestContext.Dispose();
                        return next();
                    }

                    return task.ContinueWith(t =>
                    {
                        output.TrafficAnalytics.EndRequest(startTime);
                        requestContext.Log?.Log(LogType.Request, LogLevel.Standard, () => $"Completed request with {requestContext.Outgoing.StatusCode} response");
                        requestContext.Dispose();
                    });
                }
            }

            return Task.Run(() =>
            {
                owinContext.Response.StatusCode = 500;
                owinContext.Response.ReasonPhrase = "No matching endpoints are enabled";
            });
        }
    }
}