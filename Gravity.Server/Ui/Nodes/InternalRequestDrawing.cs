using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class InternalRequestDrawing: NodeDrawing
    {
        public InternalRequestDrawing(
            DrawingElement drawing, 
            InternalPage internalRequest) 
            : base(drawing, "Internal request " + internalRequest.Name)
        {
            CssClass = "internal";
        }
    }
}