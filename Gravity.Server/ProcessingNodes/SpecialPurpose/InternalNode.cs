using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.SpecialPurpose
{
    internal class InternalNode: INode
    {
        public string Name { get; set; }
        public bool Disabled { get; set; }
        public bool Offline { get { return Disabled; } }

        public void Dispose()
        {
        }

        void INode.Bind(INodeGraph nodeGraph)
        {
        }

        void INode.UpdateStatus()
        {
        }

        Task INode.ProcessRequest(IOwinContext context)
        {
            // returning null here will make the listener middleware chain the
            // next middleware in the OWIN pipeline
            return null;
        }
    }
}