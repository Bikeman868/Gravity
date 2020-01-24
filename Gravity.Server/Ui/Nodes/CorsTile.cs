﻿using System.Collections.Generic;
using Gravity.Server.Configuration;
using Gravity.Server.ProcessingNodes.SpecialPurpose;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class CorsTile: NodeTile
    {
        private readonly DrawingElement _drawing;
        private readonly CorsNode _corsNode;

        public CorsTile(
            DrawingElement drawing, 
            CorsNode corsNode,
            DashboardConfiguration.NodeConfiguration nodeConfiguration) 
            : base(
                drawing,
                nodeConfiguration?.Title ?? "CORS/CORB", 
                "cors", 
                corsNode.Offline, 
                2, 
                corsNode.Name)
        {
            _drawing = drawing;
            _corsNode = corsNode;

            LinkUrl = "/ui/node?name=" + corsNode.Name;

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

            AddDetails(details, null, _corsNode.Offline ? "disabled" : string.Empty);
        }

        public override void AddLines(IDictionary<string, NodeTile> nodeDrawings)
        {
            if (string.IsNullOrEmpty(_corsNode.OutputNode))
                return;

            NodeTile nodeDrawing;
            if (nodeDrawings.TryGetValue(_corsNode.OutputNode, out nodeDrawing))
            {
                _drawing.AddChild(new ConnectedLineDrawing(TopRightSideConnection, nodeDrawing.TopLeftSideConnection)
                {
                    CssClass = _corsNode.Offline ? "connection_none" : "connection_unknown"
                });
            }
        }
    }
}