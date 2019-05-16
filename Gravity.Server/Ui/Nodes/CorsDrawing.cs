using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Gravity.Server.Configuration;
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
            : base(drawing, "CORS", 2, corsNode.Name)
        {
            _drawing = drawing;
            _corsNode = corsNode;

            SetCssClass(corsNode.Disabled ? "disabled" : "cors");
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