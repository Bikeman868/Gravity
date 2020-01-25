﻿using System;
using System.Collections.Generic;
using System.Linq;
using Gravity.Server.Configuration;
using Gravity.Server.ProcessingNodes.Logging;
using Gravity.Server.ProcessingNodes.Transform;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class ChangeLogFilterTile: NodeTile
    {
        private readonly DrawingElement _drawing;
        private readonly ChangeLogFilterNode _changeLogFilter;

        public ChangeLogFilterTile(
            DrawingElement drawing, 
            ChangeLogFilterNode changeLogFilter,
            DashboardConfiguration.NodeConfiguration nodeConfiguration) 
            : base(
                drawing,
                nodeConfiguration?.Title ?? "Log level",
                "logging", 
                changeLogFilter.Offline, 
                2, 
                changeLogFilter.Name)
        {
            _drawing = drawing;
            _changeLogFilter = changeLogFilter;

            LinkUrl = "/ui/node?name=" + changeLogFilter.Name;

            var details = new List<string>();
            if (changeLogFilter.MaximumLogLevel == 0)
            {
                details.Add("Disable logging");
            }
            else
            {
                details.Add($"Log level {changeLogFilter.MaximumLogLevel}");
                if (changeLogFilter.LogTypes == null || changeLogFilter.LogTypes.Length == 0)
                    details.Add("All message types");
                else
                    details.AddRange(changeLogFilter.LogTypes.Select(t => "Log " + t.ToString()));
            }
            AddDetails(details, null, changeLogFilter.Offline ? "disabled" : string.Empty);
        }

        public override void AddLines(IDictionary<string, NodeTile> nodeDrawings)
        {
            if (string.IsNullOrEmpty(_changeLogFilter.OutputNode))
                return;

            if (nodeDrawings.TryGetValue(_changeLogFilter.OutputNode, out var nodeDrawing))
            {
                _drawing.AddChild(new ConnectedLineDrawing(TopRightSideConnection, nodeDrawing.TopLeftSideConnection)
                {
                    CssClass = _changeLogFilter.Offline ? "connection_none" : "connection_unknown"
                });
            }
        }
    }
}