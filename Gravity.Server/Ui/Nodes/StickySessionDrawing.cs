using System.Collections.Generic;
using Gravity.Server.Configuration;
using Gravity.Server.ProcessingNodes.LoadBalancing;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class StickySessionDrawing: LoadBalancerDrawing
    {
        public StickySessionDrawing(
            DrawingElement drawing, 
            StickySessionNode stickySession,
            DashboardConfiguration.NodeConfiguration nodeConfiguration,
            TrafficIndicatorConfiguration trafficIndicatorConfiguration)
            : base(
            drawing, 
            stickySession,
            trafficIndicatorConfiguration,
            nodeConfiguration?.Title ?? "Sticky session", 
            "sticky_session",
            new List<string>
            {
                "Cookie: " + stickySession.SessionCookie,
                "Lifetime: " + stickySession.SessionDuration
            },
            true,
            true,
            true)
        {
        }
    }
}