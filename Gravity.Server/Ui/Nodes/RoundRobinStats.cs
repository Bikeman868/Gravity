using Gravity.Server.Configuration;
using Gravity.Server.Ui.Shapes;
using Gravity.Server.ProcessingNodes.LoadBalancing;

namespace Gravity.Server.Ui.Nodes
{
    internal class RoundRobinStats: LoadBalancerStats
    {
        public RoundRobinStats(
            DrawingElement drawing,
            RoundRobinNode roundRobin,
            DashboardConfiguration dashboardConfiguration,
            DashboardConfiguration.NodeConfiguration nodeConfiguration)
            : base(
            drawing, 
            roundRobin,
            dashboardConfiguration,
            new[] { new RoundRobinTile(drawing, roundRobin, nodeConfiguration, dashboardConfiguration.TrafficIndicator) })
        {
        }
    }
}