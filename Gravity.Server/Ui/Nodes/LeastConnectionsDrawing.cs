using Gravity.Server.ProcessingNodes.LoadBalancing;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class LeastConnectionsDrawing: LoadBalancerDrawing
    {
        public LeastConnectionsDrawing(
            DrawingElement drawing, 
            LeastConnectionsNode leastConnections,
            double[] trafficIndicatorThresholds)
            : base(
            drawing, 
            leastConnections,
            trafficIndicatorThresholds, 
            "Least connections", 
            "least_connections", 
            null,
            true,
            false,
            true)
        {
        }
    }
}