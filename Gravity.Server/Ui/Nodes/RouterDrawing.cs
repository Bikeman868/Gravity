using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Gravity.Server.Configuration;
using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class RouterDrawing: NodeDrawing
    {
        private readonly DrawingElement _drawing;
        private readonly RoutingNode _router;

        private RouterOutputDrawing[] _outputDrawings;

        public RouterDrawing(
            DrawingElement drawing, 
            RoutingNode router) 
            : base(drawing, "Router", 2, router.Name)
        {
            _drawing = drawing;
            _router = router;
            CssClass = router.Disabled ? "disabled" : "router";

            var details = new List<string>();

            if (router.Outputs != null)
            {
                _outputDrawings = router.Outputs
                    .Select(o => new RouterOutputDrawing(drawing, o, o.RouteTo))
                    .ToArray();

                foreach (var outputDrawing in _outputDrawings)
                    AddChild(outputDrawing);
            }

            AddDetails(details);
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
                        CssClass = "connection_heavy"
                    });
                }
            }
        }

        private class RouterOutputDrawing : NodeDrawing
        {
            public RouterOutputDrawing(
                DrawingElement drawing,
                RouterOutputConfiguration routerOutput,
                string label)
                : base(drawing, "Output", 3, label)
            {
                CssClass = "router_output";

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

                AddDetails(details);
            }
        }
    }
}