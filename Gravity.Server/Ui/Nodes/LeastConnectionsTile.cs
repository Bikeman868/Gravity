using Gravity.Server.Configuration;
using Gravity.Server.ProcessingNodes.LoadBalancing;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class LeastConnectionsTile: LoadBalancerTile
    {
        public LeastConnectionsTile(
            DrawingElement drawing, 
            LeastConnectionsNode leastConnections,
            DashboardConfiguration.NodeConfiguration nodeConfiguration,
            TrafficIndicatorConfiguration trafficIndicatorConfiguration)
            : base(
            drawing, 
            leastConnections,
            trafficIndicatorConfiguration,
            nodeConfiguration?.Title ?? "Least connections", 
            "least_connections", 
            null,
            true,
            false,
            true)
        {
        }
    }
}