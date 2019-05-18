using System.Collections.Generic;
using Gravity.Server.DataStructures;
using Gravity.Server.ProcessingNodes;
using Gravity.Server.ProcessingNodes.Server;
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
            SetCssClass(server.Disabled ? "disabled" : (server.Healthy == false ? "server_unhealthy" : "server_healthy" ));
            
            var ipAddresses = server.IpAddresses;

            var details = new List<string>();

            if (server.Healthy.HasValue)
            {
                if (server.Healthy.Value)
                {
                    details.Add("Health check passed");
                }
                else
                {
                    details.Add("Health check failed");
                    AddUnhealthyReason(server.UnhealthyReason, details);
                }
            }

            details.Add("Host " + (server.Host ?? string.Empty));
            details.Add("Port " + server.Port);
            details.Add("Connection timeout " + server.ConnectionTimeout);
            details.Add("Response timeout " + server.ResponseTimeout);
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

        private static void AddUnhealthyReason(string unhealthyReason, List<string> details)
        {
            details.Add("Health check failed");

            while (unhealthyReason.Length > 50)
            {
                var i = unhealthyReason.IndexOf(' ', 45);
                if (i < 0)
                {
                    details.Add(unhealthyReason.Substring(0, 50));
                    unhealthyReason = unhealthyReason.Substring(50);
                }
                else
                {
                    details.Add(unhealthyReason.Substring(0, i));
                    unhealthyReason = unhealthyReason.Substring(i + 1);
                }
            }

            if (unhealthyReason.Length > 0)
                details.Add(unhealthyReason);
        }

        private class IpAddressDrawing : NodeDrawing
        {
            public IpAddressDrawing(
                DrawingElement drawing,
                string label,
                ServerIpAddress ipAddress)
                : base(drawing, ipAddress.Address.ToString(), 3)
            {
                CssClass = ipAddress.Healthy == false ? "server_ip_address_unhealthy": "server_ip_address_healthy";

                var details = new List<string>();

                if (ipAddress.Healthy.HasValue)
                {
                    if (ipAddress.Healthy.Value)
                    {
                        details.Add("Health check passed");
                    }
                    else
                    {
                        AddUnhealthyReason(ipAddress.UnhealthyReason, details);
                    }
                }
                details.Add(ipAddress.RequestCount + " requests");
                details.Add(ipAddress.ConnectionCount + " connections");

                AddDetails(details);
            }
        }
    }
}