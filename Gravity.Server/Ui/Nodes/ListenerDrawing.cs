using System.Collections.Generic;
using Gravity.Server.Configuration;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class ListenerDrawing: NodeDrawing
    {
        private readonly DrawingElement _drawing;
        private readonly ListenerEndpointConfiguration _listener;

        public ListenerDrawing(
            DrawingElement drawing, 
            ListenerEndpointConfiguration listener)
            : base(drawing, "Listener " + listener.IpAddress + ":" + listener.PortNumber, "listener", listener.Disabled)
        {
            _drawing = drawing;
            _listener = listener;

            var details = new List<string>();

            details.Add("Send to node " + listener.NodeName);

            if (listener.ProcessingNode != null)
            {
                details.Add(listener.ProcessingNode.TrafficAnalytics.LifetimeRequestCount + " requests");
                details.Add(listener.ProcessingNode.TrafficAnalytics.RequestTime.TotalMilliseconds.ToString("n2") + " ms");
                details.Add(listener.ProcessingNode.TrafficAnalytics.RequestsPerMinute.ToString("n2") + " /min");
            }

            AddDetails(details, null, listener.Disabled ? "disabled" : string.Empty);
        }

        public override void AddLines(IDictionary<string, NodeDrawing> nodeDrawings)
        {
            NodeDrawing nodeDrawing;
            if (nodeDrawings.TryGetValue(_listener.NodeName, out nodeDrawing))
            {
                _drawing.AddChild(new ConnectedLineDrawing(TopRightSideConnection, nodeDrawing.TopLeftSideConnection)
                {
                    CssClass = _listener.Disabled ? "connection_disabled" : "connection_light"
                });
            }
        }
    }
}