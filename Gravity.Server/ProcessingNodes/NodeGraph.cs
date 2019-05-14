using System;
using System.Linq;
using Gravity.Server.Interfaces;

namespace Gravity.Server.ProcessingNodes
{
    internal class NodeGraph: INodeGraph
    {
        private INodeGraph _current;

        public void Configure()
        {
            var instance = new Instance();

            instance.Nodes = new INode[]
            {
                new RoundRobinBalancer { Name = "A", Outputs = new[] { "C", "D", "E" } }, 
                new InternalPageNode { Name = "B" },
                new ServerEndpoint { Name = "C" }, 
                new ServerEndpoint { Name = "D" }, 
                new ServerEndpoint { Name = "E" }, 
            };

            foreach (var node in instance.Nodes) 
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

            void INodeGraph.Configure()
            {
            }
        }
    }
}
