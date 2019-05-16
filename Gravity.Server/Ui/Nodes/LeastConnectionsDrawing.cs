using System.Collections.Generic;
using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class LeastConnectionsDrawing: NodeDrawing
    {
        private readonly DrawingElement _drawing;
        private readonly LeastConnectionsNode _leastConnections;

        public LeastConnectionsDrawing(
            DrawingElement drawing, 
            LeastConnectionsNode leastConnections) 
            : base(drawing, "Least busy", 2, leastConnections.Name)
        {
            _drawing = drawing;
            _leastConnections = leastConnections;
            
            SetCssClass(leastConnections.Disabled ? "disabled" : "least_connections");

            var details = new List<string>();

            if (leastConnections.Outputs != null)
                details.Add("To: " + string.Join(", ", leastConnections.Outputs));

            AddDetails(details);
        }

        public override void AddLines(IDictionary<string, NodeDrawing> nodeDrawings)
        {
            foreach (var output in _leastConnections.Outputs)
            {
                NodeDrawing nodeDrawing;
                if (nodeDrawings.TryGetValue(output, out nodeDrawing))
                {
                    _drawing.AddChild(new ConnectedLineDrawing(TopRightSideConnection, nodeDrawing.TopLeftSideConnection)
                    {
                        CssClass = _leastConnections.Disabled ? "connection_disabled" : "connection_light"
                    });
                }
            }
        }
    }
}