using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Shapes;
using System.Collections.Generic;

namespace Gravity.Server.Ui.Nodes
{
    internal class RoundRobbinDrawing: NodeDrawing
    {
        public RoundRobbinDrawing(
            DrawingElement drawing, 
            RoundRobinBalancer roundRobbin) 
            : base(drawing, "Round robin " + roundRobbin.Name)
        {
            CssClass = "round_robin";

            var details = new List<string>();

            if (roundRobbin.Disabled) 
                details.Add("Disabled");

            if (roundRobbin.Outputs != null)
            {
                details.Add(string.Join(" -> ", roundRobbin.Outputs));
            }

            AddDetails(details);
        }
    }
}