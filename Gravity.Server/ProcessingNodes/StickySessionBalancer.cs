using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Gravity.Server.DataStructures;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes
{
    public class StickySessionBalancer: INode
    {
        public string Name { get; set; }
        public string[] Outputs { get; set; }
        public bool Disabled { get; set; }

        private NodeOutput[] _outputs;

        void INode.Bind(INodeGraph nodeGraph)
        {
            _outputs = Outputs.Select(name => new NodeOutput
            {
                Name = name,
                Node = nodeGraph.NodeByName(name),
            }).ToArray();
        }

        Task INode.ProcessRequest(IOwinContext context)
        {
            return null;
        }
    }
}