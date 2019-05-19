using System.Linq;
using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Shapes;
using System.Collections.Generic;
using Gravity.Server.ProcessingNodes.LoadBalancing;
using Gravity.Server.Utility;

namespace Gravity.Server.Ui.Nodes
{
    internal class RoundRobinDrawing: NodeDrawing
    {
        private readonly DrawingElement _drawing;
        private readonly RoundRobinNode _roundRobin;
        private readonly OutputDrawing[] _outputDrawings;

        public RoundRobinDrawing(
            DrawingElement drawing, 
            RoundRobinNode roundRobin) 
            : base(drawing, "Round robin", "round_robin", roundRobin.Offline, 2, roundRobin.Name)
        {
            _drawing = drawing;
            _roundRobin = roundRobin;

            if (roundRobin.Disabled)
                Title.CssClass += " disabled";

            if (roundRobin.Outputs != null)
            {
                _outputDrawings = new OutputDrawing[roundRobin.Outputs.Length];

                for (var i = 0; i < roundRobin.Outputs.Length; i++)
                {
                    var outputNodeName = roundRobin.Outputs[i];
                    var output = roundRobin.OutputNodes[i];
                    _outputDrawings[i] = new OutputDrawing(drawing, outputNodeName, output);
                }

                foreach (var outputDrawing in _outputDrawings)
                    AddChild(outputDrawing);
            }
        }

        public override void AddLines(IDictionary<string, NodeDrawing> nodeDrawings)
        {
            if (_roundRobin.Outputs == null) return;

            for (var i = 0; i < _roundRobin.Outputs.Length; i++)
            {
                var outputNodeName = _roundRobin.Outputs[i];
                var outputDrawing = _outputDrawings[i];

                NodeDrawing nodeDrawing;
                if (nodeDrawings.TryGetValue(outputNodeName, out nodeDrawing))
                {
                    _drawing.AddChild(new ConnectedLineDrawing(outputDrawing.TopRightSideConnection, nodeDrawing.TopLeftSideConnection)
                    {
                        CssClass = _roundRobin.Disabled ? "connection_disabled" : "connection_light"
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
                : base(drawing, "Output", "round_robin_output", output == null || output.Disabled, 3, label)
            {
                if (output != null)
                {
                    var details = new List<string>();

                    details.Add(output.TrafficAnalytics.LifetimeRequestCount + " requests");
                    details.Add(output.TrafficAnalytics.RequestTime.TotalMilliseconds.ToString("n2") + " ms");
                    details.Add(output.TrafficAnalytics.RequestsPerMinute.ToString("n2") + " /min");
                    details.Add(output.ConnectionCount + " connections");

                    AddDetails(details, null, output.Disabled ? "disabled" : string.Empty);
                }
            }
        }
    }
}