using System;
using System.Collections.Generic;
using System.Linq;
using Gravity.Server.Configuration;
using Gravity.Server.ProcessingNodes.Server;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class ServerStats: NodeStats
    {
        public ServerStats(
            DrawingElement drawing, 
            ServerNode server,
            DashboardConfiguration dashboardConfiguration)
            : base(drawing)
        {
            var nodeConfiguration = FindNodeConfiguration(dashboardConfiguration, server.Name);

            var topSection = AddSection();
            var bottomSection = AddSection();

            topSection.AddChild(new ServerTile(drawing, server, nodeConfiguration));

            if (server.IpAddresses == null) return;

            var requestRateData = new Tuple<string, float>[server.IpAddresses.Length];
            for (var i = 0; i < server.IpAddresses.Length; i++)
            {
                var ipAddress = server.IpAddresses[i].Address.ToString();

                requestRateData[i] = new Tuple<string, float>(
                    ipAddress,
                    (float)server.IpAddresses[i].TrafficAnalytics.RequestsPerMinute);
            }

            bottomSection.AddChild(CreatePieChart("Request Rate", "/min", requestRateData, TotalHandling.Sum, "rate_piechart"));

            var requestTimeData = new Tuple<string, float>[server.IpAddresses.Length];
            for (var i = 0; i < server.IpAddresses.Length; i++)
            {
                var ipAddress = server.IpAddresses[i].Address.ToString();

                requestTimeData[i] = new Tuple<string, float>(
                    ipAddress,
                    (float)server.IpAddresses[i].TrafficAnalytics.RequestTime.TotalMilliseconds);
            }

            bottomSection.AddChild(CreatePieChart("Request Time", "ms", requestTimeData, TotalHandling.Maximum, "time_piechart"));

            for (var i = 0; i < server.IpAddresses.Length; i++)
            {
                var ipAddress = server.IpAddresses[i].Address.ToString();
                var methodsPerMinute = server.IpAddresses[i].TrafficAnalytics.MethodsPerMinute;

                Tuple<string, float>[] methodData;
                    
                lock (methodsPerMinute)
                {
                    var methods = methodsPerMinute.Keys.ToList();
                    methodData = new Tuple<string, float>[methods.Count];

                    for (var j = 0; j < methods.Count; j++)
                        methodData[j] = new Tuple<string, float>(methods[j], (float)methodsPerMinute[methods[j]]);
                }

                bottomSection.AddChild(CreatePieChart(ipAddress + " Methods", "/min", methodData, TotalHandling.Sum, "method_piechart"));
            }

            for (var i = 0; i < server.IpAddresses.Length; i++)
            {
                var ipAddress = server.IpAddresses[i].Address.ToString();
                var statusCodesPerMinute = server.IpAddresses[i].TrafficAnalytics.StatusCodesPerMinute;

                Tuple<string, float>[] statusCodeData;

                lock (statusCodesPerMinute)
                {
                    var statusCodes = statusCodesPerMinute.Keys.ToList();
                    statusCodeData = new Tuple<string, float>[statusCodes.Count];

                    for (var j = 0; j < statusCodes.Count; j++)
                        statusCodeData[j] = new Tuple<string, float>(statusCodes[j].ToString(), (float)statusCodesPerMinute[statusCodes[j]]);
                }

                bottomSection.AddChild(CreatePieChart(ipAddress + " Status", "/min", statusCodeData, TotalHandling.Sum, "status_piechart"));
            }

        }
    }
}