using System;
using System.Collections.Generic;
using System.Linq;
using Gravity.Server.Configuration;
using Gravity.Server.ProcessingNodes.LoadBalancing;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class LoadBalancerStats: NodeStats
    {
        public LoadBalancerStats(
            DrawingElement drawing, 
            LoadBalancerNode loadBalancer,
            DashboardConfiguration dashboardConfiguration,
            DrawingElement[] topSectionElements)
            : base(drawing)
        {
            var topSection = AddSection();
            var bottomSection = AddSection();

            foreach(var element in topSectionElements)
                topSection.AddChild(element);

            var requestRateData = new Tuple<string, float>[loadBalancer.OutputNodes.Length];
            for (var i = 0; i < loadBalancer.OutputNodes.Length; i++)
            {
                var nodeName = loadBalancer.OutputNodes[i].Name;
                var nodeTitle = FindNodeTitle(dashboardConfiguration, nodeName);

                requestRateData[i] = new Tuple<string, float>(
                    nodeTitle,
                    (float)loadBalancer.OutputNodes[i].TrafficAnalytics.RequestsPerMinute);
            }

            bottomSection.AddChild(CreatePieChart("Request Rate", "/min", requestRateData, TotalHandling.Sum));

            var requestTimeData = new Tuple<string, float>[loadBalancer.OutputNodes.Length];
            for (var i = 0; i < loadBalancer.OutputNodes.Length; i++)
            {
                var nodeName = loadBalancer.OutputNodes[i].Name;
                var nodeTitle = FindNodeTitle(dashboardConfiguration, nodeName);

                requestTimeData[i] = new Tuple<string, float>(
                    nodeTitle,
                    (float)loadBalancer.OutputNodes[i].TrafficAnalytics.RequestTime.TotalMilliseconds);
            }

            bottomSection.AddChild(CreatePieChart("Request Time", "ms", requestTimeData, TotalHandling.Maximum));
        }
    }
}