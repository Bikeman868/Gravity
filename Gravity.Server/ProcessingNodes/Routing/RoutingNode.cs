using System;
using System.Linq;
using System.Threading.Tasks;
using Gravity.Server.Configuration;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Routing
{
    internal class RoutingNode: INode
    {
        public string Name { get; set; }
        public bool Disabled { get; set; }
        public RouterOutputConfiguration[] Outputs { get; set; }
        public bool Available { get; private set; }

        private readonly IExpressionParser _expressionParser;

        private Output[] _outputs;

        public RoutingNode(
            IExpressionParser expressionParser)
        {
            _expressionParser = expressionParser;
        }

        public void Dispose()
        {
        }

        void INode.Bind(INodeGraph nodeGraph)
        {
            _outputs = Outputs.Select(o =>
                {
                    var output = new Output
                    {
                        RuleLogic = o.RuleLogic,
                        Node = nodeGraph.NodeByName(o.RouteTo),
                    };

                    if (o.Rules != null && o.Rules.Length > 0)
                    {
                        output.Rules = o.Rules
                            .Select(r =>
                            {
                                var equals = r.Condition.IndexOf('=');
                                if (equals < 0)
                                    throw new Exception("Routing expression must contain 'expr = expr'. You have '" +
                                                        r.Condition + "'");

                                var leftSide = r.Condition.Substring(0, equals);
                                var rightSide = r.Condition.Substring(equals + 1);

                                return new Rule
                                {
                                    Expression1 = _expressionParser.Parse<string>(leftSide),
                                    Expression2 = _expressionParser.Parse<string>(rightSide),
                                };
                            })
                            .ToArray();
                    }
                    return output;
                })
            .ToArray();
        }

        void INode.UpdateAvailability()
        {
            if (Disabled || _outputs == null)
            {
                Available = false;
                return;
            }

            for (var i = 0; i < _outputs.Length; i++)
            {
                if (_outputs[i].Node != null && _outputs[i].Node.Available)
                {
                    Available = true;
                    return;
                }
            }

            Available = false;
        }

        Task INode.ProcessRequest(IOwinContext context)
        {
            for (var i = 0; i < Outputs.Length; i++)
            {
                var output = _outputs[i];
                if (output.IsMatch(context))
                    return output.Node.ProcessRequest(context);
            }

            context.Response.StatusCode = 404;
            context.Response.ReasonPhrase = "No routes in " + Name + " matches the request";
            return context.Response.WriteAsync(string.Empty);
        }

        private class Output
        {
            public Rule[] Rules;
            public RuleLogic RuleLogic;
            public INode Node;

            public bool IsMatch(IOwinContext context)
            {
                if (Rules == null)
                    return true;

                switch (RuleLogic)
                {
                    case RuleLogic.All:
                        return Rules.All(r => r.IsMatch(context));
                    case RuleLogic.None:
                        return Rules.All(r => !r.IsMatch(context));
                    case RuleLogic.Any:
                        return Rules.Any(r => r.IsMatch(context));
                }
                throw new Exception("Routing node does not understand RuleLogic=" + RuleLogic);
            }
        }

        private class Rule
        {
            public IExpression<string> Expression1;
            public IExpression<string> Expression2;

            public bool IsMatch(IOwinContext context)
            {
                return string.Equals(
                    Expression1.Evaluate(context),
                    Expression2.Evaluate(context),
                    StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}