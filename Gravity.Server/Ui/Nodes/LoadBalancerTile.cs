using Gravity.Server.Ui.Shapes;
using System.Collections.Generic;
using Gravity.Server.Configuration;
using Gravity.Server.ProcessingNodes.LoadBalancing;
using Gravity.Server.Pipeline;

namespace Gravity.Server.Ui.Nodes
{
    internal class LoadBalancerTile: NodeTile
    {
        private readonly DrawingElement _drawing;
        private readonly LoadBalancerNode _loadBalancer;
        private readonly OutputDrawing[] _outputDrawings;
        private readonly double[] _trafficIndicatorThresholds;

        public LoadBalancerTile(
            DrawingElement drawing, 
            LoadBalancerNode loadBalancer,
            TrafficIndicatorConfiguration trafficIndicatorConfiguration,
            string title,
            string cssClass,
            List<string> details,
            bool showConnections,
            bool showSessions,
            bool showTraffic)
            : base(drawing, title, cssClass, loadBalancer.Offline, 2, loadBalancer.Name)
        {
            _drawing = drawing;
            _loadBalancer = loadBalancer;
            _trafficIndicatorThresholds = trafficIndicatorConfiguration.Thresholds;

            LinkUrl = "/ui/node?name=" + loadBalancer.Name;

            if (details != null)
                AddDetails(details, null, loadBalancer.Offline ? "offline" : string.Empty);

            if (loadBalancer.Offline)
                Title.CssClass += " disabled";

            if (loadBalancer.Outputs != null)
            {
                _outputDrawings = new OutputDrawing[loadBalancer.Outputs.Length];

                for (var i = 0; i < loadBalancer.Outputs.Length; i++)
                {
                    var outputNodeName = loadBalancer.Outputs[i];
                    var output = loadBalancer.OutputNodes[i];
                    _outputDrawings[i] = new OutputDrawing(
                        drawing, 
                        outputNodeName,
                        "Server",
                        output, 
                        cssClass + "_output",
                        showConnections,
                        showSessions,
                        showTraffic);
                }

                foreach (var outputDrawing in _outputDrawings)
                    AddChild(outputDrawing);
            }
        }

        public override void AddLines(IDictionary<string, NodeTile> nodeDrawings)
        {
            if (_loadBalancer.Outputs == null) return;

            for (var i = 0; i < _loadBalancer.Outputs.Length; i++)
            {
                var outputNodeName = _loadBalancer.Outputs[i];
                var outputNode = _loadBalancer.OutputNodes[i];
                var outputDrawing = _outputDrawings[i];

                NodeTile nodeDrawing;
                if (nodeDrawings.TryGetValue(outputNodeName, out nodeDrawing))
                {
                    var css = "connection_none";

                    if (!outputNode.Offline)
                    {
                        var requestsPerMinute = outputNode.TrafficAnalytics.RequestsPerMinute;
                        if (requestsPerMinute < _trafficIndicatorThresholds[0]) css = "connection_none";
                        else if (requestsPerMinute < _trafficIndicatorThresholds[1]) css = "connection_light";
                        else if (requestsPerMinute < _trafficIndicatorThresholds[2]) css = "connection_medium";
                        else if (requestsPerMinute < _trafficIndicatorThresholds[3]) css = "connection_heavy";
                    }

                    _drawing.AddChild(new ConnectedLineDrawing(outputDrawing.TopRightSideConnection, nodeDrawing.TopLeftSideConnection)
                    {
                        CssClass = css
                    });
                }
            }
        }

        private class OutputDrawing : NodeTile
        {
            public OutputDrawing(
                DrawingElement drawing,
                string label,
                string title,
                NodeOutput output,
                string cssClass,
                bool showConnections,
                bool showSessions,
                bool showTraffic)
                : base(drawing, title ?? "Output", cssClass, output == null || output.Offline, 3, label)
            {
                if (output != null)
                {
                    var details = new List<string>();

                    if (showTraffic)
                    {
                        details.Add(output.TrafficAnalytics.LifetimeRequestCount + " requests");
                        details.Add(output.TrafficAnalytics.RequestTime.TotalMilliseconds.ToString("n2") + "ms");
                        details.Add(output.TrafficAnalytics.RequestsPerMinute.ToString("n2") + "/min");
                    }

                    if (showConnections)
                        details.Add(output.ConnectionCount + " connections");

                    if (showSessions)
                        details.Add(output.SessionCount + " sessions");

                    if (details.Count > 0)
                        AddDetails(details, null, output.Offline ? "disabled" : string.Empty);
                }
            }
        }
    }
}