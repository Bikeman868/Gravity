using Gravity.Server.Configuration;
using Gravity.Server.Ui.Shapes;
using Gravity.Server.ProcessingNodes.LoadBalancing;

namespace Gravity.Server.Ui.Nodes
{
    internal class RoundRobinDrawing: LoadBalancerDrawing
    {
        public RoundRobinDrawing(
            DrawingElement drawing, 
            RoundRobinNode roundRobin,
            DashboardConfiguration.NodeConfiguration nodeConfiguration,
            TrafficIndicatorConfiguration trafficIndicatorConfiguration)
            : base(
            drawing, 
            roundRobin,
            trafficIndicatorConfiguration,
            nodeConfiguration?.Title ?? "Round robin", 
            "round_robin",
            null,
            false,
            false,
            true)
        {
        }
    }
}