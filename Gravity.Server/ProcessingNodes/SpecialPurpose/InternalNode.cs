using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;

namespace Gravity.Server.ProcessingNodes.SpecialPurpose
{
    internal class InternalNode: ProcessingNode
    {
        public override void UpdateStatus()
        {
            Offline = Disabled;
        }

        public override Task ProcessRequestAsync(IRequestContext context)
        {
            context.Log?.Log(LogType.Logic, LogLevel.Standard, () => $"Internal node '{Name}' passing the request back to the Owin pipeline for other middleware to handle");

            // returning null here will make the listener middleware chain the
            // next middleware in the OWIN pipeline
            return null;
        }
    }
}