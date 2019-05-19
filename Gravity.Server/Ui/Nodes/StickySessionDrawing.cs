using System.Collections.Generic;
using Gravity.Server.ProcessingNodes.LoadBalancing;
using Gravity.Server.Ui.Shapes;
using Gravity.Server.Utility;

namespace Gravity.Server.Ui.Nodes
{
    internal class StickySessionDrawing: LoadBalancerDrawing
    {
        public StickySessionDrawing(
            DrawingElement drawing, 
            StickySessionNode stickySession,
            double[] trafficIndicatorThresholds)
            : base(
            drawing, 
            stickySession, 
            trafficIndicatorThresholds, 
            "Sticky session", 
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