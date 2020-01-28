using System.Collections.Generic;
using Gravity.Server.Configuration;
using Gravity.Server.ProcessingNodes.Server;
using Gravity.Server.Ui.Shapes;
using Gravity.Server.Utility;

namespace Gravity.Server.Ui.Nodes
{
    internal class ServerTile: NodeTile
    {
        private readonly IpAddressDrawing[] _ipAddressDrawings;

        public ServerTile(
            DrawingElement drawing, 
            ServerNode server,
            DashboardConfiguration.NodeConfiguration nodeConfiguration) 
            : base(
                drawing,
                nodeConfiguration?.Title ?? "Server", 
                server.Healthy == false ? "server_unhealthy" : "server_healthy", 
                server.Disabled, 
                2, 
                server.Name)
        {
            var ipAddresses = server.IpAddresses;

            LinkUrl = "/ui/node?name=" + server.Name;

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

            details.Add("Host " + (server.DomainName ?? string.Empty));
            details.Add("Port " + (server.Port.HasValue ? server.Port.Value.ToString() :  "pass-through"));
            details.Add("Connection timeout " + server.ConnectionTimeout + (server.ReuseConnections ? " then reuse" : ""));
            details.Add("Max connections " + server.MaximumConnectionCount);
            details.Add("Response timeout " + server.ResponseTimeout);
            details.Add("Read timeout " + server.ReadTimeoutMs + "ms");
            details.Add("Health check " + 
                server.HealthCheckMethod + (server.HealthCheckPort == 443 ? " https": " http") +"://" + 
                (server.HealthCheckHost ?? server.DomainName) + 
                ((server.HealthCheckPort == 80 || server.HealthCheckPort == 443) ? "" : ":" + server.HealthCheckPort) + 
                server.HealthCheckPath + " returns " + string.Join(" or ", server.HealthCheckCodes));

            AddDetails(details, null, server.Disabled ? "disabled" : string.Empty);

            if (ipAddresses != null)
            {
                _ipAddressDrawings = new IpAddressDrawing[ipAddresses.Length];

                for (var i = 0; i < ipAddresses.Length; i++)
                {
                    var ipAddress = ipAddresses[i];
                    _ipAddressDrawings[i] = new IpAddressDrawing(drawing, ipAddress);
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

        private class IpAddressDrawing : NodeTile
        {
            public IpAddressDrawing(
                DrawingElement drawing,
                ServerIpAddress ipAddress)
                : base(
                    drawing, 
                    ipAddress.Address.ToString(), 
                    ipAddress.Healthy == false ? "server_ip_address_unhealthy": "server_ip_address_healthy", 
                    false, 
                    3)
            {
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
                details.Add(ipAddress.TrafficAnalytics.LifetimeRequestCount + " requests");
                details.Add(ipAddress.TrafficAnalytics.RequestTime.TotalMilliseconds.ToString("n1") + " ms");
                details.Add(ipAddress.TrafficAnalytics.RequestsPerMinute.ToString("n1") + " /min");
                details.Add(ipAddress.ConnectionCount + " connections");

                AddDetails(details);
            }
        }
    }
}