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
                ConfigureInternalPageNodes(configuration, nodes);
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

            _current = instance;
        }

        private void ConfigureInternalPageNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            if (configuration.InternalPageNodes != null)
            {
                foreach (var internalPageConfiguration in configuration.InternalPageNodes)
                {
                    var node = new InternalPage
                    {
                        Name = internalPageConfiguration.Name,
                        Disabled = internalPageConfiguration.Disabled,
                    };
                    internalPageConfiguration.Node = node;
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
                    var node = new Response
                    {
                        Name = responseNodeConfiguration.Name,
                        Disabled = responseNodeConfiguration.Disabled,
                        StatusCode = responseNodeConfiguration.StatusCode,
                        ReasonPhrase = responseNodeConfiguration.ReasonPhrase ?? "OK",
                        Content = responseNodeConfiguration.Content ?? string.Empty,
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
                    var node = new RoundRobinBalancer
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
                    var node = new ServerEndpoint
                    {
                        Name = serverNodeConfiguration.Name,
                        Disabled = serverNodeConfiguration.Disabled,
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
                    var node = new StickySessionBalancer
                    {
                        Name = stickySessionNodeConfiguration.Name,
                        Disabled = stickySessionNodeConfiguration.Disabled,
                        //Outputs = stickySessionNodeConfiguration.Outputs,
                        //SesionCookie = stickySessionNodeConfiguration.SesionCookie,
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
                    var node = new StickySessionBalancer
                    {
                        Name = transformNodeConfiguration.Name,
                        Disabled = transformNodeConfiguration.Disabled,
                        //Script = stickySessionNodeConfiguration.Script,
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
        }
    }
}
