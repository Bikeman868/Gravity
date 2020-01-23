using System.Collections.Generic;
using Gravity.Server.Configuration;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class ListenerTile: NodeTile
    {
        private readonly DrawingElement _drawing;
        private readonly ListenerEndpointConfiguration _listener;
        private readonly double[] _trafficIndicatorThresholds;

        public ListenerTile(
            DrawingElement drawing, 
            ListenerEndpointConfiguration listener,
            TrafficIndicatorConfiguration trafficIndicatorConfiguration)
            : base(
                drawing,
                listener?.Title ?? "Listener ", 
                "listener", 
                listener.Disabled)
        {
            _drawing = drawing;
            _listener = listener;
            _trafficIndicatorThresholds = trafficIndicatorConfiguration.Thresholds;

            var details = new List<string>();

            if (listener.IpAddress == "*")
                details.Add("Listen on all IP addresses");
            else if (!string.IsNullOrEmpty(listener.IpAddress))
                details.Add("Listen on " + listener.IpAddress);

            if (listener.PortNumber == 0)
                details.Add("Bind to all ports");
            else
                details.Add("Bind to port " + listener.PortNumber);

            details.Add("Send to node " + listener.NodeName);

            if (listener.ProcessingNode != null)
            {
                details.Add(listener.ProcessingNode.TrafficAnalytics.LifetimeRequestCount + " requests");
                details.Add(listener.ProcessingNode.TrafficAnalytics.RequestTime.TotalMilliseconds.ToString("n2") + " ms");
                details.Add(listener.ProcessingNode.TrafficAnalytics.RequestsPerMinute.ToString("n2") + " /min");
            }

            AddDetails(details, null, listener.Disabled ? "disabled" : string.Empty);
        }

        public override void AddLines(IDictionary<string, NodeTile> nodeDrawings)
        {
            NodeTile nodeDrawing;
            if (nodeDrawings.TryGetValue(_listener.NodeName, out nodeDrawing))
            {
                var css = "connection_unknown";

                if (!_listener.Disabled && _listener.ProcessingNode != null)
                {
                    var requestsPerMinute = _listener.ProcessingNode.TrafficAnalytics.RequestsPerMinute;
                    if (requestsPerMinute < _trafficIndicatorThresholds[0]) css = "connection_none";
                    else if (requestsPerMinute < _trafficIndicatorThresholds[1]) css = "connection_light";
                    else if (requestsPerMinute < _trafficIndicatorThresholds[2]) css = "connection_medium";
                    else if (requestsPerMinute < _trafficIndicatorThresholds[3]) css = "connection_heavy";
                }

                _drawing.AddChild(new ConnectedLineDrawing(TopRightSideConnection, nodeDrawing.TopLeftSideConnection)
                {
                    CssClass = css
                });
            }
        }
    }
}