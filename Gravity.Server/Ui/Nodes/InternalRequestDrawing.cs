using Gravity.Server.ProcessingNodes.SpecialPurpose;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class InternalRequestDrawing: NodeDrawing
    {
        public InternalRequestDrawing(
            DrawingElement drawing, 
            InternalNode internalRequest) 
            : base(drawing, "Internal", "internal", internalRequest.Offline, 2, internalRequest.Name)
        {
        }
    }
}