using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Gravity.Server.Configuration;
using Gravity.Server.Interfaces;
using Gravity.Server.Utility;
using Microsoft.Owin;
using OwinFramework.Interfaces.Builder;
using Urchin.Client.Interfaces;

namespace Gravity.Server.Pipeline
{
    internal class RequestListener: IRequestListener
    {
        private static readonly object _lock = new object();

        private readonly INodeGraph _nodeGraph;
        private readonly ILogFactory _logFactory;

        private ListenerConfiguration _currentConfiguration;
        private ListenerConfiguration _newConfiguration;
        private readonly IDisposable _configurationRegistration;

        private Thread _trafficUpdateThread;

        public RequestListener(
            IConfigurationStore configuration,
            INodeGraph nodeGraph,
            ILogFactory logFactory)
        {
            _nodeGraph = nodeGraph;
            _logFactory = logFactory;

            _configurationRegistration = configuration.Register<ListenerConfiguration>(
                "/gravity/listener",
                c =>
                {
                    Trace.WriteLine("[CONFIG] New listener configuration");

                    _newConfiguration = c.Sanitize();

                    if (ReferenceEquals(_currentConfiguration, null))
                    {
                        Trace.WriteLine("[CONFIG] There is no current listener configuration, the new configuration will be adopted immediately");
                        _currentConfiguration = _newConfiguration;
                    }
                });

            _trafficUpdateThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        Thread.Sleep(3000);

                        var endpoints = _currentConfiguration.Endpoints;
                        if (endpoints != null)
                        {
                            foreach (var endpoint in endpoints)
                                endpoint.ProcessingNode?.TrafficAnalytics.Recalculate();
                        }

                        if (!ReferenceEquals(_currentConfiguration, _newConfiguration))
                        {
                            endpoints = _newConfiguration.Endpoints;
                            if (endpoints != null)
                            {
                                var offline = false;
                                foreach (var endpoint in endpoints)
                                {
                                    endpoint.ProcessingNode = new NodeOutput
                                    {
                                        Name = endpoint.NodeName,
                                        Node = _nodeGraph.NodeByName(endpoint.NodeName),
                                    };


                                    if (endpoint.ProcessingNode.Node == null)
                                    {
                                        Trace.WriteLine($"[CONFIG] Listener endpoint {endpoint.Name ?? endpoint.IpAddress} has no node attached in the new configuration");
                                        offline = true;
                                    }
                                    else
                                    {
                                        if (endpoint.ProcessingNode.Node.Offline)
                                        {
                                            Trace.WriteLine($"[CONFIG] Listener endpoint {endpoint.Name ?? endpoint.IpAddress} processing node is not ready to accept traffic yet");
                                            offline = true;
                                        }
                                    }
                                }

                                if (!offline)
                                {
                                    Trace.WriteLine("[CONFIG] Bringing new listener configuration online");
                                    _currentConfiguration = _newConfiguration;
                                }
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

        ListenerEndpointConfiguration[] IRequestListener.Endpoints { get { return _currentConfiguration.Endpoints; } }

        Task IRequestListener.ProcessRequest(IOwinContext owinContext, Func<Task> next)
        {
            var configuration = _currentConfiguration;
            if (configuration.Disabled) return next();

            var localIp = owinContext.Request.LocalIpAddress;
            var localPort = owinContext.Request.LocalPort;

            var endpoints = _currentConfiguration.Endpoints;
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

                    if (output.Offline) continue;

                    var requestContext = (IRequestContext)new OwinRequestContext(owinContext, _logFactory);
                    requestContext.Log?.Log(LogType.Request, LogLevel.Standard, () => 
                        $"Starting new request to {requestContext.Incoming.Method} {requestContext.Incoming.Scheme.ToString().ToLower()}://{requestContext.Incoming.DomainName}{requestContext.Incoming.Path}{requestContext.Incoming.Query}");

                    var trafficAnalyticInfo = output.TrafficAnalytics.BeginRequest();

                    var task = output.Node.ProcessRequest(requestContext);

                    if (task == null)
                    {
                        requestContext.Log?.Log(LogType.Request, LogLevel.Standard, () => $"Completed request with transfer to Owin pipeline");
                        requestContext.Dispose();
                        return next();
                    }

                    return task.ContinueWith(t =>
                    {
                        output.TrafficAnalytics.EndRequest(trafficAnalyticInfo);
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