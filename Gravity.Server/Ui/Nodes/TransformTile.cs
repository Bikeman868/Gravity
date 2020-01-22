using System;
using System.Collections.Generic;
using System.Linq;
using Gravity.Server.Configuration;
using Gravity.Server.ProcessingNodes.Transform;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class TransformTile: NodeTile
    {
        private readonly DrawingElement _drawing;
        private readonly TransformNode _transform;

        public TransformTile(
            DrawingElement drawing, 
            TransformNode transform,
            DashboardConfiguration.NodeConfiguration nodeConfiguration) 
            : base(
                drawing,
                nodeConfiguration?.Title ?? "Transform",
                "transform", 
                transform.Offline, 
                2, 
                transform.Name)
        {
            _drawing = drawing;
            _transform = transform;

            if (!string.IsNullOrEmpty(transform.Description))
            {
                var details = transform.Description.Split('\n').ToList();
                AddDetails(details, null, transform.Offline ? "disabled" : string.Empty);
            }
        }

        public override void AddLines(IDictionary<string, NodeTile> nodeDrawings)
        {
            if (string.IsNullOrEmpty(_transform.OutputNode))
                return;

            NodeTile nodeDrawing;
            if (nodeDrawings.TryGetValue(_transform.OutputNode, out nodeDrawing))
            {
                _drawing.AddChild(new ConnectedLineDrawing(TopRightSideConnection, nodeDrawing.TopLeftSideConnection)
                {
                    CssClass = _transform.Offline ? "connection_none" : "connection_unknown"
                });
            }
        }
    }
}