using Gravity.Server.Ui.Shapes;
using Gravity.Server.ProcessingNodes.LoadBalancing;

namespace Gravity.Server.Ui.Nodes
{
    internal class RoundRobinDrawing: LoadBalancerDrawing
    {
        public RoundRobinDrawing(
            DrawingElement drawing, 
            RoundRobinNode roundRobin,
            double[] trafficIndicatorThresholds)
            : base(
            drawing, 
            roundRobin, 
            trafficIndicatorThresholds, 
            "Round robin", 
            "round_robin",
            null,
            false,
            false,
            true)
        {
        }
    }
}