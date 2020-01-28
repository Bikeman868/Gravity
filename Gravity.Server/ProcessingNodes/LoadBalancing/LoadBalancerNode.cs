using System;
using System.Linq;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;

namespace Gravity.Server.ProcessingNodes.LoadBalancing
{
    internal abstract class LoadBalancerNode : ProcessingNode
    {
        public string[] Outputs { get; set; }
        public NodeOutput[] OutputNodes;

        private DateTime _nextTrafficUpdate;

        public override void Bind(INodeGraph nodeGraph)
        {
            OutputNodes = Outputs.Select(name => new NodeOutput
            {
                Name = name,
                Node = nodeGraph.NodeByName(name),
            }).ToArray();
        }

        public override void UpdateStatus()
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
                        node.Offline = node.Node == null || node.Node.Offline;

                        if (!node.Offline)
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
    }
}