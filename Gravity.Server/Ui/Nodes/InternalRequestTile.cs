using Gravity.Server.Configuration;
using Gravity.Server.ProcessingNodes.SpecialPurpose;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class InternalRequestTile: NodeTile
    {
        public InternalRequestTile(
            DrawingElement drawing, 
            InternalNode internalRequest,
            DashboardConfiguration.NodeConfiguration nodeConfiguration) 
            : base(
                drawing, 
                nodeConfiguration?.Title ?? "Internal", 
                "public", 
                internalRequest.Offline, 
                2, 
                internalRequest.Name)
        {
            LinkUrl = "/ui/node?name=" + internalRequest.Name;
        }
    }
}