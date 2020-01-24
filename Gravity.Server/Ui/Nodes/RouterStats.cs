using System;
using System.Collections.Generic;
using System.Linq;
using Gravity.Server.Configuration;
using Gravity.Server.ProcessingNodes.Routing;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class RouterStats: NodeStats
    {
        public RouterStats(
            DrawingElement drawing, 
            RoutingNode router,
            DashboardConfiguration dashboardConfiguration)
            : base(drawing)
        {
            var nodeConfiguration = FindNodeConfiguration(dashboardConfiguration, router.Name);

            var topSection = AddSection();
            var methodsSection = AddSection();
            var statusCodesSection = AddSection();

            topSection.AddChild(new RouterTile(drawing, router, nodeConfiguration, dashboardConfiguration.TrafficIndicator));

            var requestRateData = new Tuple<string, float>[router.OutputNodes.Length];
            for (var i = 0; i < router.OutputNodes.Length; i++)
            {
                var nodeName = router.OutputNodes[i].Name;
                var nodeTitle = FindNodeTitle(dashboardConfiguration, nodeName);

                requestRateData[i] = new Tuple<string, float>(
                    nodeTitle,
                    (float)router.OutputNodes[i].TrafficAnalytics.RequestsPerMinute);
            }

            topSection.AddChild(CreatePieChart("Request Rate", "/min", requestRateData, TotalHandling.Sum));

            var requestTimeData = new Tuple<string, float>[router.OutputNodes.Length];
            for (var i = 0; i < router.OutputNodes.Length; i++)
            {
                var nodeName = router.OutputNodes[i].Name;
                var nodeTitle = FindNodeTitle(dashboardConfiguration, nodeName);

                requestTimeData[i] = new Tuple<string, float>(
                    nodeTitle,
                    (float)router.OutputNodes[i].TrafficAnalytics.RequestTime.TotalMilliseconds);
            }

            topSection.AddChild(CreatePieChart("Request Time", "ms", requestTimeData, TotalHandling.Maximum));

            for (var i = 0; i < router.OutputNodes.Length; i++)
            {
                var nodeName = router.OutputNodes[i].Name;
                var nodeTitle = FindNodeTitle(dashboardConfiguration, nodeName);

                var methodsPerMinute = router.OutputNodes[i].TrafficAnalytics.MethodsPerMinute;
                var statusCodesPerMinute = router.OutputNodes[i].TrafficAnalytics.StatusCodesPerMinute;

                Tuple<string, float>[] methodData;
                lock (methodsPerMinute)
                {
                    var methods = methodsPerMinute.Keys.ToList();
                    methodData = new Tuple<string, float>[methods.Count];

                    for (var j = 0; j < methods.Count; j++)
                        methodData[j] = new Tuple<string, float>(methods[j], (float)methodsPerMinute[methods[j]]);
                }
                methodsSection.AddChild(CreatePieChart(nodeTitle + " Methods", "/min", methodData, TotalHandling.Sum));

                Tuple<string, float>[] statusCodeData;
                lock (statusCodesPerMinute)
                {
                    var statusCodes = statusCodesPerMinute.Keys.ToList();
                    statusCodeData = new Tuple<string, float>[statusCodes.Count];

                    for (var j = 0; j < statusCodes.Count; j++)
                        statusCodeData[j] = new Tuple<string, float>(statusCodes[j].ToString(), (float)statusCodesPerMinute[statusCodes[j]]);
                }
                statusCodesSection.AddChild(CreatePieChart(nodeTitle + " Status", "/min", statusCodeData, TotalHandling.Sum));
            }
        }
    }
}