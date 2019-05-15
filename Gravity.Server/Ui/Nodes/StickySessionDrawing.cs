using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class StickySessionDrawing: NodeDrawing
    {
        public StickySessionDrawing(
            DrawingElement drawing, 
            StickySessionBalancer stickySession) 
            : base(drawing, "Sticky session " + stickySession.Name)
        {
            CssClass = "sticky_session";
        }
    }
}