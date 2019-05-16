using System.Collections.Generic;
using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class CorsDrawing: NodeDrawing
    {
        private readonly DrawingElement _drawing;
        private readonly CorsNode _corsNode;

        public CorsDrawing(
            DrawingElement drawing, 
            CorsNode corsNode) 
            : base(drawing, "CORS/CORB", 2, corsNode.Name)
        {
            _drawing = drawing;
            _corsNode = corsNode;

            SetCssClass(corsNode.Disabled ? "disabled" : "cors");

            var details = new List<string>();

            details.Add("For " + (corsNode.WebsiteOrigin ?? string.Empty));
            details.Add("Allow " + (corsNode.AllowedOrigins ?? string.Empty));

            if (!string.IsNullOrEmpty(corsNode.AllowedMethods))
                details.Add("Allow " + corsNode.AllowedMethods);

            if (!string.IsNullOrEmpty(corsNode.AllowedHeaders))
                details.Add("Allow " + corsNode.AllowedHeaders);

            if (corsNode.AllowCredentials)
                details.Add("Allow credentials");

            if (!string.IsNullOrEmpty(corsNode.ExposedHeaders))
                details.Add("Expose " + corsNode.ExposedHeaders);

            AddDetails(details);
        }

        public override void AddLines(IDictionary<string, NodeDrawing> nodeDrawings)
        {
            if (string.IsNullOrEmpty(_corsNode.OutputNode))
                return;

            NodeDrawing nodeDrawing;
            if (nodeDrawings.TryGetValue(_corsNode.OutputNode, out nodeDrawing))
            {
                _drawing.AddChild(new ConnectedLineDrawing(TopRightSideConnection, nodeDrawing.TopLeftSideConnection)
                {
                    CssClass = _corsNode.Disabled ? "connection_disabled" : "connection_light"
                });
            }
        }
    }
}