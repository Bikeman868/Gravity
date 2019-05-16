using System.Collections.Generic;
using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class ServerDrawing: NodeDrawing
    {
        public ServerDrawing(
            DrawingElement drawing, 
            ServerNode server) 
            : base(drawing, "Server", 2, server.Name)
        {
            SetCssClass(server.Disabled ? "disabled" : (server.Healthy ? "server_healthy" : "server_unhealthy"));

            var details = new List<string>();

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
        }
    }
}