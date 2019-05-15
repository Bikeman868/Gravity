using System.Collections.Generic;
using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class StickySessionDrawing: NodeDrawing
    {
        private readonly DrawingElement _drawing;
        private readonly StickySessionBalancer _stickySession;

        public StickySessionDrawing(
            DrawingElement drawing, 
            StickySessionBalancer stickySession) 
            : base(drawing, "Sticky session", 2, stickySession.Name)
        {
            _drawing = drawing;
            _stickySession = stickySession;
            
            SetCssClass(stickySession.Disabled ? "disabled" : "sticky_session");
        }

        public override void AddLines(IDictionary<string, NodeDrawing> nodeDrawings)
        {
            foreach (var output in _stickySession.Outputs)
            {
                NodeDrawing nodeDrawing;
                if (nodeDrawings.TryGetValue(output, out nodeDrawing))
                {
                    _drawing.AddChild(new ConnectedLineDrawing(TopRightSideConnection, nodeDrawing.TopLeftSideConnection)
                    {
                        CssClass = "connection_disabled"
                    });
                }
            }
        }
    }
}