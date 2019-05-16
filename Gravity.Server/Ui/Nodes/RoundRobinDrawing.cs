using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Shapes;
using System.Collections.Generic;

namespace Gravity.Server.Ui.Nodes
{
    internal class RoundRobbinDrawing: NodeDrawing
    {
        private readonly DrawingElement _drawing;
        private readonly RoundRobinNode _roundRobbin;

        public RoundRobbinDrawing(
            DrawingElement drawing, 
            RoundRobinNode roundRobbin) 
            : base(drawing, "Round robin", 2, roundRobbin.Name)
        {
            _drawing = drawing;
            _roundRobbin = roundRobbin;
            
            SetCssClass(roundRobbin.Disabled ? "disabled" : "round_robin");

            var details = new List<string>();

            if (roundRobbin.Outputs != null)
            {
                details.Add("To: " + string.Join(" -> ", roundRobbin.Outputs));
            }

            AddDetails(details);
        }

        public override void AddLines(IDictionary<string, NodeDrawing> nodeDrawings)
        {
            foreach (var output in _roundRobbin.Outputs)
            {
                NodeDrawing nodeDrawing;
                if (nodeDrawings.TryGetValue(output, out nodeDrawing))
                {
                    _drawing.AddChild(new ConnectedLineDrawing(TopRightSideConnection, nodeDrawing.TopLeftSideConnection)
                    {
                        CssClass = _roundRobbin.Disabled ? "connection_disabled" : "connection_light"
                    });
                }
            }
        }
    }
}