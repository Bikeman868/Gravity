﻿using System;
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
            var bottomSection = AddSection();

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

            bottomSection.AddChild(CreatePieChart("Request Rate", "/min", requestRateData, TotalHandling.Sum));

            var requestTimeData = new Tuple<string, float>[router.OutputNodes.Length];
            for (var i = 0; i < router.OutputNodes.Length; i++)
            {
                var nodeName = router.OutputNodes[i].Name;
                var nodeTitle = FindNodeTitle(dashboardConfiguration, nodeName);

                requestTimeData[i] = new Tuple<string, float>(
                    nodeTitle,
                    (float)router.OutputNodes[i].TrafficAnalytics.RequestTime.TotalMilliseconds);
            }

            bottomSection.AddChild(CreatePieChart("Request Time", "ms", requestTimeData, TotalHandling.Maximum));
        }
    }
}