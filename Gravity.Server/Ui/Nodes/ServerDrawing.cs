using System.Collections.Generic;
using Gravity.Server.DataStructures;
using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class ServerDrawing: NodeDrawing
    {
        private readonly IpAddressDrawing[] _ipAddressDrawings;

        public ServerDrawing(
            DrawingElement drawing, 
            ServerNode server) 
            : base(drawing, "Server", 2, server.Name)
        {
            SetCssClass(server.Disabled ? "disabled" : (server.Healthy ? "server_healthy" : "server_unhealthy"));
            
            var ipAddresses = server.IpAddresses;

            var details = new List<string>();

            if (!server.Healthy && (ipAddresses == null || ipAddresses.Length != 1))
            {
                details.Add("Health check failed");
                details.Add(server.UnhealthyReason);
            }

            details.Add("Host " + (server.Host ?? string.Empty));
            details.Add("Port " + server.Port);
            details.Add("Connection timeout " + server.ConnectionTimeout);
            details.Add("Request timeout " + server.RequestTimeout);
            details.Add("Health check " + 
                server.HealthCheckMethod + " http://" + 
                (server.HealthCheckHost ?? server.Host) + 
                (server.HealthCheckPort == 80 ? "" : ":" + server.HealthCheckPort) + 
                server.HealthCheckPath);

            AddDetails(details);

            if (ipAddresses != null)
            {
                _ipAddressDrawings = new IpAddressDrawing[ipAddresses.Length];

                for (var i = 0; i < ipAddresses.Length; i++)
                {
                    var ipAddress = ipAddresses[i];
                    _ipAddressDrawings[i] = new IpAddressDrawing(drawing, ipAddress.Address.ToString(), ipAddress);
                }

                foreach (var outputDrawing in _ipAddressDrawings)
                    AddChild(outputDrawing);
            }
        }

        private class IpAddressDrawing : NodeDrawing
        {
            public IpAddressDrawing(
                DrawingElement drawing,
                string label,
                ServerIpAddress ipAddress)
                : base(drawing, ipAddress.Address.ToString(), 3)
            {
                CssClass = ipAddress.Healthy ? "server_ip_address_healthy" : "server_ip_address_unhealthy";

                var details = new List<string>();

                if (ipAddress != null)
                {
                    if (!ipAddress.Healthy)
                    {
                        details.Add("Health check failed");
                        details.Add(ipAddress.UnhealthyReason);
                    }
                    details.Add(ipAddress.RequestCount + " requests");
                    details.Add(ipAddress.ConnectionCount + " connections");
                }

                AddDetails(details);
            }
        }
    }
}