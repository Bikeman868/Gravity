﻿using System.Linq;
using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Shapes;
using System.Collections.Generic;
using Gravity.Server.DataStructures;

namespace Gravity.Server.Ui.Nodes
{
    internal class RoundRobbinDrawing: NodeDrawing
    {
        private readonly DrawingElement _drawing;
        private readonly RoundRobinNode _roundRobbin;
        private readonly OutputDrawing[] _outputDrawings;

        public RoundRobbinDrawing(
            DrawingElement drawing, 
            RoundRobinNode roundRobbin) 
            : base(drawing, "Round robin", 2, roundRobbin.Name)
        {
            _drawing = drawing;
            _roundRobbin = roundRobbin;

            SetCssClass(roundRobbin.Disabled ? "disabled" : "round_robbin");

            if (roundRobbin.Outputs != null)
            {
                _outputDrawings = new OutputDrawing[roundRobbin.Outputs.Length];

                for (var i = 0; i < roundRobbin.Outputs.Length; i++)
                {
                    var outputNodeName = roundRobbin.Outputs[i];
                    var output = roundRobbin.OutputNodes[i];
                    _outputDrawings[i] = new OutputDrawing(drawing, outputNodeName, output);
                }

                foreach (var outputDrawing in _outputDrawings)
                    AddChild(outputDrawing);
            }
        }

        public override void AddLines(IDictionary<string, NodeDrawing> nodeDrawings)
        {
            if (_roundRobbin.Outputs == null) return;

            for (var i = 0; i < _roundRobbin.Outputs.Length; i++)
            {
                var outputNodeName = _roundRobbin.Outputs[i];
                var outputDrawing = _outputDrawings[i];

                NodeDrawing nodeDrawing;
                if (nodeDrawings.TryGetValue(outputNodeName, out nodeDrawing))
                {
                    _drawing.AddChild(new ConnectedLineDrawing(outputDrawing.TopRightSideConnection, nodeDrawing.TopLeftSideConnection)
                    {
                        CssClass = _roundRobbin.Disabled ? "connection_disabled" : "connection_light"
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
                : base(drawing, "Output", 3, label)
            {
                CssClass = "round_robbin_output";

                var details = new List<string>();

                if (output != null)
                {
                    details.Add(output.RequestCount + " requests");
                    details.Add(output.ConnectionCount + " connections");
                }

                AddDetails(details);
            }
        }
    }
}