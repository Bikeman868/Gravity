using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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
            : base(drawing, "Listener " + listener.IpAddress + ":" + listener.PortNumber)
        {
            _drawing = drawing;
            _listener = listener;
            CssClass = listener.Disabled ? "disabled" : "listener";

            var details = new List<string>();

            details.Add("Send to node " + listener.NodeName);

            if (listener.ProcessingNode != null)
                details.Add(listener.ProcessingNode.RequestCount + " requests");

            AddDetails(details);
        }

        public override void AddLines(IDictionary<string, NodeDrawing> nodeDrawings)
        {
            NodeDrawing nodeDrawing;
            if (nodeDrawings.TryGetValue(_listener.NodeName, out nodeDrawing))
            {
                _drawing.AddChild(new ConnectedLineDrawing(TopRightSideConnection, nodeDrawing.TopLeftSideConnection)
                {
                    CssClass = "connection_light"
                });
            }
        }
    }
}