using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Gravity.Server.Configuration;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;
using Gravity.Server.ProcessingNodes.LoadBalancing;
using Gravity.Server.ProcessingNodes.Routing;
using Gravity.Server.ProcessingNodes.Server;
using Gravity.Server.ProcessingNodes.SpecialPurpose;
using Gravity.Server.ProcessingNodes.Transform;
using Microsoft.Owin;
using OwinFramework.Interfaces.Builder;
using OwinFramework.Interfaces.Utility;

namespace Gravity.Server.Utility
{
    internal class NodeGraph: INodeGraph
    {
        private readonly IExpressionParser _expressionParser;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IFactory _factory;
        private readonly IBufferPool _bufferPool;
        private readonly IDisposable _configuration;

        private INodeGraph _current;
        private Thread _thread;

        public NodeGraph(
            IConfiguration configuration,
            IExpressionParser expressionParser,
            IHostingEnvironment hostingEnvironment,
            IFactory factory,
            IBufferPool bufferPool)
        {
            _expressionParser = expressionParser;
            _hostingEnvironment = hostingEnvironment;
            _factory = factory;
            _bufferPool = bufferPool;

            _configuration = configuration.Register(
                "/gravity/nodeGraph", 
                Configure, 
                new NodeGraphConfiguration());

            _thread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        Thread.Sleep(100);
                        var graph = _current;
                        if (graph != null)
                        {
                            var nodes = graph.GetNodes(n => n);
                            foreach (var node in nodes)
                            {
                                try
                                {
                                    node.UpdateStatus();
                                }
                                catch
                                {
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
                Name = "Update availability",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };

            _thread.Start();
        }

        public void Configure(NodeGraphConfiguration configuration)
        {
            try
            {
                configuration = configuration.Sanitize();
            }
            catch (Exception ex)
            {
                throw new Exception("There was a problem with sanitizing the configuration data", ex);
            }

            var nodes = new List<INode>();

            try
            { 
                ConfigureCorsNodes(configuration, nodes);
                ConfigureInternalPageNodes(configuration, nodes);
                ConfigureLeastConnectionsNodes(configuration, nodes);
                ConfigureResponseNodes(configuration, nodes);
                ConfigureRoundRobinNodes(configuration, nodes);
                ConfigureRouterNodes(configuration, nodes);
                ConfigureServerNodes(configuration, nodes);
                ConfigureStickySessionNodes(configuration, nodes);
                ConfigureTransformNodes(configuration, nodes);
            }
            catch (Exception ex)
            {
                throw new Exception("There was a problem re-configuring nodes", ex);
            }

            var instance = new NodeGraphInstance
            {
                Nodes = nodes.ToArray()
            };

            try
            {
                foreach (var node in nodes)
                    node.Bind(instance);
            }
            catch (Exception ex)
            {
                throw new Exception("There was a problem with binding nodes into a graph", ex);
            }

            var prior = _current as NodeGraphInstance;
            _current = instance;

            if (prior != null)
            {
                for (var i = 0; i < prior.Nodes.Length; i++)
                    prior.Nodes[i].Dispose();
            }
        }

        private void ConfigureCorsNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            if (configuration.CorsNodes != null)
            {
                foreach (var corsConfiguration in configuration.CorsNodes)
                {
                    var node = new CorsNode
                    {
                        Name = corsConfiguration.Name,
                        Disabled = corsConfiguration.Disabled,
                        OutputNode = corsConfiguration.OutputNode,
                        AllowCredentials = corsConfiguration.AllowCredentials,
                        AllowedMethods = corsConfiguration.AllowedMethods,
                        AllowedHeaders = corsConfiguration.AllowedHeaders,
                        AllowedOrigins = corsConfiguration.AllowedOrigins,
                        ExposedHeaders = corsConfiguration.ExposedHeaders,
                        WebsiteOrigin = corsConfiguration.WebsiteOrigin
                    };
                    corsConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        private void ConfigureInternalPageNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            if (configuration.InternalNodes != null)
            {
                foreach (var internalPageConfiguration in configuration.InternalNodes)
                {
                    var node = new InternalNode
                    {
                        Name = internalPageConfiguration.Name,
                        Disabled = internalPageConfiguration.Disabled,
                    };
                    internalPageConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        private void ConfigureLeastConnectionsNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            if (configuration.LeastConnectionsNodes != null)
            {
                foreach (var leastConnectionsConfiguration in configuration.LeastConnectionsNodes)
                {
                    var node = new LeastConnectionsNode
                    {
                        Name = leastConnectionsConfiguration.Name,
                        Disabled = leastConnectionsConfiguration.Disabled,
                        Outputs = leastConnectionsConfiguration.Outputs
                    };
                    leastConnectionsConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        private void ConfigureResponseNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            if (configuration.ResponseNodes != null)
            {
                foreach (var responseNodeConfiguration in configuration.ResponseNodes)
                {
                    var node = new ResponseNode
                    {
                        Name = responseNodeConfiguration.Name,
                        Disabled = responseNodeConfiguration.Disabled,
                        StatusCode = responseNodeConfiguration.StatusCode,
                        ReasonPhrase = responseNodeConfiguration.ReasonPhrase ?? "OK",
                        Content = responseNodeConfiguration.Content ?? string.Empty,
                        ContentFile = responseNodeConfiguration.ContentFile,
                    };
                    if (responseNodeConfiguration.Headers != null)
                    {
                        node.HeaderNames = responseNodeConfiguration.Headers.Select(h => h.HeaderName).ToArray();
                        node.HeaderValues = responseNodeConfiguration.Headers.Select(h => h.HeaderValue).ToArray();
                    }
                    responseNodeConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        private void ConfigureRoundRobinNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            if (configuration.RoundRobinNodes != null)
            {
                foreach (var roundRobinConfiguration in configuration.RoundRobinNodes)
                {
                    var node = new RoundRobinNode
                    {
                        Name = roundRobinConfiguration.Name,
                        Disabled = roundRobinConfiguration.Disabled,
                        Outputs = roundRobinConfiguration.Outputs
                    };
                    roundRobinConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        private void ConfigureRouterNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            if (configuration.RouterNodes != null)
            {
                foreach (var routerNodeConfiguration in configuration.RouterNodes)
                {
                    var node = new RoutingNode(_expressionParser)
                    {
                        Name = routerNodeConfiguration.Name,
                        Disabled = routerNodeConfiguration.Disabled,
                        Outputs = routerNodeConfiguration.Outputs
                    };
                    routerNodeConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        private void ConfigureServerNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            if (configuration.ServerNodes != null)
            {
                foreach (var serverNodeConfiguration in configuration.ServerNodes)
                {
                    var node = new ServerNode(_bufferPool)
                    {
                        Name = serverNodeConfiguration.Name,
                        Disabled = serverNodeConfiguration.Disabled,
                        DomainName = serverNodeConfiguration.Host,
                        Port = serverNodeConfiguration.Port,
                        ConnectionTimeout = serverNodeConfiguration.ConnectionTimeout,
                        ResponseTimeout = serverNodeConfiguration.ResponseTimeout,
                        ReadTimeoutMs = serverNodeConfiguration.ReadTimeoutMs,
                        ReuseConnections = serverNodeConfiguration.ReuseConnections,
                        DnsLookupInterval = serverNodeConfiguration.DnsLookupInterval,
                        RecalculateInterval = serverNodeConfiguration.RecalculateInterval,
                        HealthCheckPort = serverNodeConfiguration.HealthCheckPort,
                        HealthCheckHost = serverNodeConfiguration.HealthCheckHost,
                        HealthCheckPath = new PathString(serverNodeConfiguration.HealthCheckPath),
                        HealthCheckMethod = serverNodeConfiguration.HealthCheckMethod,
                        HealthCheckCodes = serverNodeConfiguration.HealthCheckCodes,
                        HealthCheckLog = serverNodeConfiguration.HealthCheckLog,
                        HealthCheckInterval = serverNodeConfiguration.HealthCheckInterval,
                    };
                    node.Initialize();
                    serverNodeConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        private void ConfigureStickySessionNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            if (configuration.StickySessionNodes != null)
            {
                foreach (var stickySessionNodeConfiguration in configuration.StickySessionNodes)
                {
                    var node = new StickySessionNode
                    {
                        Name = stickySessionNodeConfiguration.Name,
                        Disabled = stickySessionNodeConfiguration.Disabled,
                        Outputs = stickySessionNodeConfiguration.Outputs,
                        SessionCookie = stickySessionNodeConfiguration.SesionCookie,
                        SessionDuration = stickySessionNodeConfiguration.SessionDuration
                    };
                    stickySessionNodeConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        private void ConfigureTransformNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            Func<string[], string> joinScript = s =>
            {
                if (s == null || s.Length == 0) return null;
                return string.Join(Environment.NewLine, s);
            };

            if (configuration.TransformNodes != null)
            {
                foreach (var transformNodeConfiguration in configuration.TransformNodes)
                {
                    var node = new TransformNode(_hostingEnvironment, _factory)
                    {
                        Name = transformNodeConfiguration.Name,
                        Disabled = transformNodeConfiguration.Disabled,
                        OutputNode = transformNodeConfiguration.OutputNode,
                        Description = joinScript(transformNodeConfiguration.Description),
                        ScriptLanguage = transformNodeConfiguration.ScriptLanguage,
                        RequestScript = joinScript(transformNodeConfiguration.RequestScript),
                        ResponseScript = joinScript(transformNodeConfiguration.ResponseScript),
                        RequestScriptFile = transformNodeConfiguration.RequestScriptFile,
                        ResponseScriptFile = transformNodeConfiguration.ResponseScriptFile,
                    };
                    transformNodeConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        INode INodeGraph.NodeByName(string name)
        {
            return _current.NodeByName(name);
        }

        T[] INodeGraph.GetNodes<T>(Func<INode, T> map, Func<INode, bool> predicate)
        {
            return _current.GetNodes(map, predicate);
        }

        private class NodeGraphInstance: INodeGraph
        {
            public INode[] Nodes;

            INode INodeGraph.NodeByName(string name)
            {
                return Nodes.FirstOrDefault(n => string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase));
            }

            void INodeGraph.Configure(NodeGraphConfiguration configuration)
            {
            }

            T[] INodeGraph.GetNodes<T>(Func<INode, T> map, Func<INode, bool> predicate)
            {
                var enumeration = (IEnumerable<INode>)Nodes;
                if (predicate != null) enumeration = enumeration.Where(predicate);
                return enumeration.Select(map).ToArray();
            }
        }
    }
}
