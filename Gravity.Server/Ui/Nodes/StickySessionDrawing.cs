﻿using System.Collections.Generic;
using System.Linq;
using Gravity.Server.DataStructures;
using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Shapes;

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
            : base(drawing, "Sticky session", 2, stickySession.Name)
        {
            _drawing = drawing;
            _stickySession = stickySession;
            
            SetCssClass(stickySession.Disabled ? "disabled" : "sticky_session");

            var details = new List<string>();

            details.Add("Cookie: " + stickySession.SessionCookie);
            details.Add("Lifetime: " + stickySession.SessionDuration);

            AddDetails(details);

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
                : base(drawing, "Output", 3, label)
            {
                CssClass = "sticky_session_output";

                var details = new List<string>();

                if (output != null)
                {
                    details.Add(output.SessionCount + " sessions");
                    details.Add(output.RequestCount + " requests");
                    details.Add(output.ConnectionCount + " connections");
                }

                AddDetails(details);
            }
        }
    }
}