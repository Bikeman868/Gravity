using Gravity.Server.Configuration;
using Gravity.Server.Ui.Shapes;
using Gravity.Server.ProcessingNodes.LoadBalancing;

namespace Gravity.Server.Ui.Nodes
{
    internal class LeastConnectionsStats: LoadBalancerStats
    {
        public LeastConnectionsStats(
            DrawingElement drawing,
            LeastConnectionsNode leastConnections,
            DashboardConfiguration dashboardConfiguration,
            DashboardConfiguration.NodeConfiguration nodeConfiguration)
            : base(
            drawing, 
            leastConnections,
            dashboardConfiguration,
            new[] { new LeastConnectionsTile(drawing, leastConnections, nodeConfiguration, dashboardConfiguration.TrafficIndicator) })
        {
        }
    }
}