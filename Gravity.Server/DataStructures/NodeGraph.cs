using System;
using System.Collections.Generic;
using System.Linq;
using Gravity.Server.Configuration;
using Gravity.Server.Interfaces;
using Gravity.Server.ProcessingNodes;
using OwinFramework.Interfaces.Builder;

namespace Gravity.Server.DataStructures
{
    internal class NodeGraph: INodeGraph
    {
        private readonly IExpressionParser _expressionParser;
        private readonly IDisposable _configuration;
        private INodeGraph _current;

        public NodeGraph(
            IConfiguration configuration,
            IExpressionParser expressionParser)
        {
            _expressionParser = expressionParser;
            _configuration = configuration.Register(
                "/gravity/nodeGraph", 
                Configure, 
                new NodeGraphConfiguration());
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
                    var node = new ServerNode
                    {
                        Name = serverNodeConfiguration.Name,
                        Disabled = serverNodeConfiguration.Disabled,
                        Host = serverNodeConfiguration.Host,
                        Port = serverNodeConfiguration.Port,
                        ConnectionTimeout = serverNodeConfiguration.ConnectionTimeout,
                        RequestTimeout = serverNodeConfiguration.RequestTimeout,
                        HealthCheckPort = serverNodeConfiguration.HealthCheckPort,
                        HealthCheckHost = serverNodeConfiguration.HealthCheckHost,
                        HealthCheckPath = serverNodeConfiguration.HealthCheckPath,
                        HealthCheckMethod = serverNodeConfiguration.HealthCheckMethod,
                    };
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
            if (configuration.TransformNodes != null)
            {
                foreach (var transformNodeConfiguration in configuration.TransformNodes)
                {
                    var node = new TransformNode
                    {
                        Name = transformNodeConfiguration.Name,
                        Disabled = transformNodeConfiguration.Disabled,
                        OutputNode = transformNodeConfiguration.OutputNode,
                        //RequestScript = transformNodeConfiguration.RequestScript,
                        //ResponseScript = transformNodeConfiguration.ResponseScript,
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
