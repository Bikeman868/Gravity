using System;
using System.Collections.Generic;
using System.Linq;
using Gravity.Server.Configuration;
using Gravity.Server.ProcessingNodes.Logging;
using Gravity.Server.ProcessingNodes.Transform;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class CustomLogTile: NodeTile
    {
        private readonly DrawingElement _drawing;
        private readonly CustomLogNode _customLog;

        public CustomLogTile(
            DrawingElement drawing, 
            CustomLogNode customLog,
            DashboardConfiguration.NodeConfiguration nodeConfiguration) 
            : base(
                drawing,
                nodeConfiguration?.Title ?? "Custom log",
                "logging", 
                customLog.Offline, 
                2, 
                customLog.Name)
        {
            _drawing = drawing;
            _customLog = customLog;

            LinkUrl = "/ui/node?name=" + customLog.Name;

            var details = new List<string>
            {
                $"For {string.Join(", ", customLog.Methods)} requests",
                $"For {string.Join(", ", customLog.StatusCodes)} result codes",
                $"Create log files in {customLog.Directory}\\{customLog.FileNamePrefix}",
                $"Keep files for {customLog.MaximumLogFileAge}",
                $"Include {customLog.ContentType} in the log",
                $"Multiple lines {customLog.Detailed}"
            };
            AddDetails(details, null, customLog.Offline ? "disabled" : string.Empty);
        }

        public override void AddLines(IDictionary<string, NodeTile> nodeDrawings)
        {
            if (string.IsNullOrEmpty(_customLog.OutputNode))
                return;

            if (nodeDrawings.TryGetValue(_customLog.OutputNode, out var nodeDrawing))
            {
                _drawing.AddChild(new ConnectedLineDrawing(TopRightSideConnection, nodeDrawing.TopLeftSideConnection)
                {
                    CssClass = _customLog.Offline ? "connection_none" : "connection_unknown"
                });
            }
        }
    }
}