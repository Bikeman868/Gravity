using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Gravity.Server.Configuration;
using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class TransformDrawing: NodeDrawing
    {
        private readonly DrawingElement _drawing;
        private readonly Transform _transform;

        public TransformDrawing(
            DrawingElement drawing, 
            Transform transform) 
            : base(drawing, "Transform", 2, transform.Name)
        {
            _drawing = drawing;
            _transform = transform;

            SetCssClass(transform.Disabled ? "disabled" : "transform");
        }

        public override void AddLines(IDictionary<string, NodeDrawing> nodeDrawings)
        {
            NodeDrawing nodeDrawing;
            if (nodeDrawings.TryGetValue(_transform.OutputNode, out nodeDrawing))
            {
                _drawing.AddChild(new ConnectedLineDrawing(TopRightSideConnection, nodeDrawing.TopLeftSideConnection)
                {
                    CssClass = "connection_light"
                });
            }
        }
    }
}