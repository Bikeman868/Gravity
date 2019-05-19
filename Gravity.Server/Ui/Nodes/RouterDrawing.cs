﻿using System.Collections.Generic;
using System.Linq;
using Gravity.Server.Configuration;
using Gravity.Server.ProcessingNodes.Routing;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class RouterDrawing: NodeDrawing
    {
        private readonly DrawingElement _drawing;
        private readonly RoutingNode _router;
        private readonly RouterOutputDrawing[] _outputDrawings;

        public RouterDrawing(
            DrawingElement drawing, 
            RoutingNode router) 
            : base(drawing, "Router", "router", router.Offline, 2, router.Name)
        {
            _drawing = drawing;
            _router = router;

            var details = new List<string>();

            if (router.Outputs != null)
            {
                _outputDrawings = router.Outputs
                    .Select(o => new RouterOutputDrawing(drawing, o, o.RouteTo, router.Offline))
                    .ToArray();

                foreach (var outputDrawing in _outputDrawings)
                    AddChild(outputDrawing);
            }

            AddDetails(details, null, router.Offline ? "disabled" : string.Empty);
        }

        public override void AddLines(IDictionary<string, NodeDrawing> nodeDrawings)
        {
            if (_router.Outputs == null) return;

            for (var i = 0; i < _router.Outputs.Length; i++)
            {
                var output = _router.Outputs[i];
                var outputDrawing = _outputDrawings[i];

                NodeDrawing nodeDrawing;
                if (nodeDrawings.TryGetValue(output.RouteTo, out nodeDrawing))
                {
                    _drawing.AddChild(new ConnectedLineDrawing(outputDrawing.TopRightSideConnection, nodeDrawing.TopLeftSideConnection)
                    {
                        CssClass = _router.Offline ? "connection_none" : "connection_unknown"
                    });
                }
            }
        }

        private class RouterOutputDrawing : NodeDrawing
        {
            public RouterOutputDrawing(
                DrawingElement drawing,
                RouterOutputConfiguration routerOutput,
                string label,
                bool disabled)
                : base(drawing, "Output", "router_output", disabled, 3, label)
            {
                var details = new List<string>();

                if (routerOutput.Rules != null && routerOutput.Rules.Length > 0)
                {
                    details.Add("If " + routerOutput.RuleLogic.ToString().ToLower());

                    foreach (var rule in routerOutput.Rules)
                    {
                        if (!rule.Disabled)
                        {
                            if (rule.Negate)
                                details.Add("Not " + rule.Condition);
                            else
                                details.Add(rule.Condition);
                        }
                    }
                }

                AddDetails(details, null, disabled ? "disabled" : string.Empty);
            }
        }
    }
}