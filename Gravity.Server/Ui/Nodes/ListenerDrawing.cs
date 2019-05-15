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
        public ListenerDrawing(
            DrawingElement page, 
            ListenerEndpointConfiguration listener)
            : base(page, "Listener " + listener.IpAddress + ":" + listener.PortNumber)
        {
            CssClass = listener.Disabled ? "disabled" : "listener";

            var details = new List<string>();

            details.Add("Send to node " + listener.NodeName);

            if (listener.ProcessingNode != null)
                details.Add(listener.ProcessingNode.RequestCount + " requests");

            AddDetails(details);
        }
    }
}