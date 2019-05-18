using System.Collections.Generic;
using Gravity.Server.ProcessingNodes;
using Gravity.Server.ProcessingNodes.Transform;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class TransformDrawing: NodeDrawing
    {
        private readonly DrawingElement _drawing;
        private readonly TransformNode _transform;

        public TransformDrawing(
            DrawingElement drawing, 
            TransformNode transform) 
            : base(drawing, "Transform", 2, transform.Name)
        {
            _drawing = drawing;
            _transform = transform;

            SetCssClass(transform.Disabled ? "disabled" : "transform");
        }

        public override void AddLines(IDictionary<string, NodeDrawing> nodeDrawings)
        {
            if (string.IsNullOrEmpty(_transform.OutputNode))
                return;

            NodeDrawing nodeDrawing;
            if (nodeDrawings.TryGetValue(_transform.OutputNode, out nodeDrawing))
            {
                _drawing.AddChild(new ConnectedLineDrawing(TopRightSideConnection, nodeDrawing.TopLeftSideConnection)
                {
                    CssClass = _transform.Disabled ? "connection_disabled" : "connection_light"
                });
            }
        }
    }
}