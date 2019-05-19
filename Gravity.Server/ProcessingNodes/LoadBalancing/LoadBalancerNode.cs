using System;
using System.Linq;
using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Gravity.Server.Utility;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.LoadBalancing
{
    internal abstract class LoadBalancerNode: INode
    {
        public string Name { get; set; }
        public string[] Outputs { get; set; }
        public bool Disabled { get; set; }
        public bool Offline { get; private set; }
        public NodeOutput[] OutputNodes;

        private DateTime _nextTrafficUpdate;

        public virtual void Dispose()
        {
        }

        void INode.Bind(INodeGraph nodeGraph)
        {
            OutputNodes = Outputs.Select(name => new NodeOutput
            {
                Name = name,
                Node = nodeGraph.NodeByName(name),
            }).ToArray();
        }

        void INode.UpdateStatus()
        {
            var nodes = OutputNodes;
            var offline = true;

            if (nodes != null)
            {
                if (!Disabled)
                {
                    for (var i = 0; i < nodes.Length; i++)
                    {
                        var node = nodes[i];
                        node.Disabled = node.Node == null || node.Node.Offline;

                        if (!node.Disabled)
                            offline = false;
                    }
                }

                var now = DateTime.UtcNow;
                if (now > _nextTrafficUpdate)
                {
                    _nextTrafficUpdate = now.AddSeconds(3);
                    for (var i = 0; i < nodes.Length; i++)
                    {
                        var node = nodes[i];
                        node.TrafficAnalytics.Recalculate();
                    }
                }
            }

            Offline = offline;
        }

        public abstract Task ProcessRequest(IOwinContext context);
    }
}