using System.Collections.Generic;
using System.Linq;
using Gravity.Server.ProcessingNodes;
using Gravity.Server.ProcessingNodes.LoadBalancing;
using Gravity.Server.Ui.Shapes;
using Gravity.Server.Utility;

namespace Gravity.Server.Ui.Nodes
{
    internal class StickySessionDrawing: NodeDrawing
    {
        private readonly DrawingElement _drawing;
        private readonly StickySessionNode _stickySession;
        private readonly OutputDrawing[] _outputDrawings;

        public StickySessionDrawing(
            DrawingElement drawing, 
            StickySessionNode stickySession) 
            : base(drawing, "Sticky session", "sticky_session", stickySession.Offline, 2, stickySession.Name)
        {
            _drawing = drawing;
            _stickySession = stickySession;
            
            var details = new List<string>();

            details.Add("Cookie: " + stickySession.SessionCookie);
            details.Add("Lifetime: " + stickySession.SessionDuration);

            AddDetails(details, null, stickySession.Offline ? "disabled" : string.Empty);

            if (stickySession.Outputs != null)
            {
                _outputDrawings = new OutputDrawing[stickySession.Outputs.Length];

                for (var i = 0; i < stickySession.Outputs.Length; i++)
                {
                    var outputNodeName = stickySession.Outputs[i];
                    var output = stickySession.OutputNodes[i];
                    _outputDrawings[i] = new OutputDrawing(drawing, outputNodeName, output);
                }

                foreach (var outputDrawing in _outputDrawings)
                    AddChild(outputDrawing);
            }
        }

        public override void AddLines(IDictionary<string, NodeDrawing> nodeDrawings)
        {
            if (_stickySession.Outputs == null) return;

            for (var i = 0; i < _stickySession.Outputs.Length; i++)
            {
                var output = _stickySession.Outputs[i];
                var outputDrawing = _outputDrawings[i];

                NodeDrawing nodeDrawing;
                if (nodeDrawings.TryGetValue(output, out nodeDrawing))
                {
                    _drawing.AddChild(new ConnectedLineDrawing(outputDrawing.TopRightSideConnection, nodeDrawing.TopLeftSideConnection)
                    {
                        CssClass = _stickySession.Disabled ? "connection_disabled" : "connection_light"
                    });
                }
            }
        }

        private class OutputDrawing : NodeDrawing
        {
            public OutputDrawing(
                DrawingElement drawing,
                string label,
                NodeOutput output)
                : base(drawing, "Output", "sticky_session_output", output == null || output.Disabled, 3, label)
            {
                if (output != null)
                {
                    var details = new List<string>();

                    details.Add(output.SessionCount + " sessions");
                    details.Add(output.TrafficAnalytics.LifetimeRequestCount + " requests");
                    details.Add(output.ConnectionCount + " connections");

                    AddDetails(details, null, output.Disabled ? "disabled" : string.Empty);
                }
            }
        }
    }
}