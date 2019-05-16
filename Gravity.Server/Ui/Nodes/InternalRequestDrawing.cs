using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class InternalRequestDrawing: NodeDrawing
    {
        public InternalRequestDrawing(
            DrawingElement drawing, 
            InternalNode internalRequest) 
            : base(drawing, "Internal", 2, internalRequest.Name)
        {
            SetCssClass( internalRequest.Disabled ? "disabled" : "internal");
        }
    }
}