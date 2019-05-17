using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes
{
    internal class TransformNode: INode
    {
        public string Name { get; set; }
        public bool Disabled { get; set; }
        public string OutputNode { get; set; }

        public void Dispose()
        {
        }

        void INode.Bind(INodeGraph nodeGraph)
        {
        }

        Task INode.ProcessRequest(IOwinContext context)
        {
            return null;
        }
    }
}