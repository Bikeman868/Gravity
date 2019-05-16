using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gravity.Server.Configuration;
using Gravity.Server.DataStructures;
using Gravity.Server.Interfaces;
using Microsoft.Owin;
using OwinFramework.Builder;
using OwinFramework.Interfaces.Builder;
using OwinFramework.InterfacesV1.Capability;
using OwinFramework.InterfacesV1.Middleware;
using OwinFramework.MiddlewareHelpers.Traceable;

namespace Gravity.Server.Middleware
{
    internal class ListenerMiddleware:
        IMiddleware<IRequestRewriter>,
        IConfigurable,
        ITraceable
    {
        private readonly INodeGraph _nodeList;
        private readonly IList<IDependency> _dependencies = new List<IDependency>();
        IList<IDependency> IMiddleware.Dependencies { get { return _dependencies; } }

        string IMiddleware.Name { get; set; }
        public Action<IOwinContext, Func<string>> Trace { get; set; }

        private readonly TraceFilter _traceFilter;

        public ListenerMiddleware(
            IConfiguration configuration,
            INodeGraph nodeList)
        {
            _nodeList = nodeList;
            _traceFilter = new TraceFilter(configuration, this);

            ConfigurationChanged(new ListenerConfiguration());
            this.RunFirst();
        }

        public Task Invoke(IOwinContext context, Func<Task> next)
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

                    _traceFilter.Trace(context, TraceLevel.Information, 
                        () => "request matches listener endpoint " + i + ", forwarding to Node " + endpoint.NodeName);

                    var output = endpoint.ProcessingNode;

                    if (output == null)
                    {
                        output = new NodeOutput
                        {
                            Name = endpoint.NodeName,
                            Node = _nodeList.NodeByName(endpoint.NodeName),
                        };
                        endpoint.ProcessingNode = output;
                    }
                    else
                    {
                        output.Node = _nodeList.NodeByName(endpoint.NodeName);
                    }

                    if (output.Node == null)
                    {
                        _traceFilter.Trace(context, TraceLevel.Error,
                            () => "there is no processing node configured with the name '" + output.Name + "'");

                        context.Response.StatusCode = 500;
                        context.Response.ReasonPhrase = "Listener endpoint configuration error";
                        return context.Response.WriteAsync(string.Empty);
                    }

                    if (output.Disabled)
                    {
                        _traceFilter.Trace(context, TraceLevel.Debug,
                            () => "listener output '" + output.Name + "' is disabled");

                        context.Response.StatusCode = 503;
                        context.Response.ReasonPhrase = "Listener endpoint disabled";
                        return context.Response.WriteAsync(string.Empty);
                    }

                    output.IncrementRequestCount();
                    return output.Node.ProcessRequest(context) ?? next();
                }
            }

            context.Response.StatusCode = 500;
            context.Response.ReasonPhrase = "No matching endpoints";
            return context.Response.WriteAsync(string.Empty);
        }

        #region IConfigurable

        private IDisposable _configurationRegistration;
        private ListenerConfiguration _configuration;

        public void Configure(IConfiguration configuration, string path)
        {
            _configurationRegistration = configuration.Register(
                path,
                ConfigurationChanged,
                new ListenerConfiguration());
        }

        private void ConfigurationChanged(ListenerConfiguration configuration)
        {
            _configuration = configuration.Sanitize();
        }

        #endregion
    }
}