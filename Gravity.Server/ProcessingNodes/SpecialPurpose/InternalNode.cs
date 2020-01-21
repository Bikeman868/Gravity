using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;

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

        Task INode.ProcessRequest(IRequestContext context)
        {
            context.Log?.Log(LogType.Logic, LogLevel.Standard, () => $"Internal node '{Name}' passing the request back to the Owin pipeline for other middleware to handle");

            // returning null here will make the listener middleware chain the
            // next middleware in the OWIN pipeline
            return null;
        }
    }
}