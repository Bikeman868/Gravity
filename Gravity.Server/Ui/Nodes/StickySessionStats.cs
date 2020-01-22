using Gravity.Server.Configuration;
using Gravity.Server.Ui.Shapes;
using Gravity.Server.ProcessingNodes.LoadBalancing;

namespace Gravity.Server.Ui.Nodes
{
    internal class StickySessionStats: LoadBalancerStats
    {
        public StickySessionStats(
            DrawingElement drawing,
            StickySessionNode stickySession,
            DashboardConfiguration dashboardConfiguration,
            DashboardConfiguration.NodeConfiguration nodeConfiguration)
            : base(
            drawing, 
            stickySession,
            dashboardConfiguration,
            new[] { new StickySessionTile(drawing, stickySession, nodeConfiguration, dashboardConfiguration.TrafficIndicator) })
        {
        }
    }
}