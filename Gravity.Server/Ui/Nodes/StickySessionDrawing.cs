using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class StickySessionDrawing: NodeDrawing
    {
        public StickySessionDrawing(
            DrawingElement drawing, 
            StickySessionBalancer stickySession) 
            : base(drawing, "Sticky session", 2, stickySession.Name)
        {
            CssClass = stickySession.Disabled ? "disabled" : "sticky_session";
        }
    }
}