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
        public RouterDrawing(
            DrawingElement drawing, 
            RoutingNode router) 
            : base(drawing, "Router " + router.Name)
        {
            CssClass = "router";

            var details = new List<string>();

            if (router.Disabled) 
                details.Add("Disabled");

            if (router.Outputs != null)
            {
                foreach (var output in router.Outputs)
                    AddChild(new RouterOutputDrawing(drawing, output));
            }

            AddDetails(details);
        }

        private class RouterOutputDrawing : NodeDrawing
        {
            public RouterOutputDrawing(
                DrawingElement drawing,
                RouterOutputConfiguration routerOutput)
                : base(drawing, "Output to " + routerOutput.RouteTo)
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