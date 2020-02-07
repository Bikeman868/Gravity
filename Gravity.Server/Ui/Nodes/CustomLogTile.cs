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
                $"Path {customLog.Directory}{customLog.FileNamePrefix}*.txt",
                $"Keep for {customLog.MaximumLogFileAge}",
                $"Format {customLog.ContentType}",
            };

            details.Add(customLog.Detailed ? "Multiple lines" : "Single line");

            if (customLog.Methods != null && customLog.Methods.Length > 0)
                details.AddRange(customLog.Methods.Select(m => "Log " + m + " requests"));
            else
                details.Add("Log all requests");

            if (customLog.IncludeStatusCodes != null && customLog.IncludeStatusCodes.Length > 0)
                details.AddRange(customLog.IncludeStatusCodes.Select(s => "Log " + s + " responses"));
            else if (customLog.ExcludeStatusCodes != null && customLog.ExcludeStatusCodes.Length > 0)
                details.AddRange(customLog.ExcludeStatusCodes.Select(s => "Do not log " + s + " responses"));
            else
                details.Add("Log all responses");

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