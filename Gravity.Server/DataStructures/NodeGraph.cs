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
        private readonly IDisposable _configuration;
        private INodeGraph _current;

        public NodeGraph(
            IConfiguration configuration)
        {
            _configuration = configuration.Register(
                "/gravity/nodeGraph", 
                c => Configure(c.Sanitize()), 
                new NodeGraphConfiguration() );
        }

        private void Configure(NodeGraphConfiguration configuration)
        {
            var nodes = new List<INode>();

            if (configuration.ServerNodes != null)
            {
                foreach (var n in configuration.ServerNodes)
                {
                    nodes.Add(new ServerEndpoint
                    {
                        Name = n.Name
                    });
                }
            }

            if (configuration.RoundRobinNodes != null)
            {
                foreach (var n in configuration.RoundRobinNodes)
                {
                    nodes.Add(new RoundRobinBalancer
                    {
                        Name = n.Name, 
                        Outputs = n.Outputs
                    });
                }
            }

            var instance = new Instance
            {
                Nodes = nodes.ToArray()
            };

            foreach (var node in nodes) 
                node.Bind(instance);

            _current = instance;
        }

        INode INodeGraph.NodeByName(string name)
        {
            return _current.NodeByName(name);
        }

        private class Instance: INodeGraph
        {
            public INode[] Nodes;

            INode INodeGraph.NodeByName(string name)
            {
                return Nodes.FirstOrDefault(n => string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
