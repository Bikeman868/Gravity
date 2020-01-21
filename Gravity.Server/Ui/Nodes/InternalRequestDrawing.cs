using Gravity.Server.Configuration;
using Gravity.Server.ProcessingNodes.SpecialPurpose;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class InternalRequestDrawing: NodeDrawing
    {
        public InternalRequestDrawing(
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
        }
    }
}