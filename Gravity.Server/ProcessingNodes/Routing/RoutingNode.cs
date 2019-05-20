using System;
using System.Linq;
using System.Threading.Tasks;
using Gravity.Server.Configuration;
using Gravity.Server.Interfaces;
using Gravity.Server.Utility;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Routing
{
    internal class RoutingNode: INode
    {
        public string Name { get; set; }
        public bool Disabled { get; set; }
        public RouterOutputConfiguration[] Outputs { get; set; }
        public bool Offline { get; private set; }
        public NodeOutput[] OutputNodes;

        private readonly IExpressionParser _expressionParser;

        private Route[] _routes;
        private DateTime _nextTrafficUpdate;

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
            _routes = new Route[Outputs.Length];
            OutputNodes = new NodeOutput[Outputs.Length];

            for (var i = 0; i < Outputs.Length; i++)
            {
                var outputConfiguration = Outputs[i];

                OutputNodes[i] = new NodeOutput
                {
                    Name = outputConfiguration.RouteTo,
                    Node = nodeGraph.NodeByName(outputConfiguration.RouteTo),
                };

                _routes[i] = new Route
                {
                    RuleLogic = outputConfiguration.RuleLogic,
                };

                if (outputConfiguration.Rules != null && outputConfiguration.Rules.Length > 0)
                {
                    _routes[i].Rules = outputConfiguration.Rules
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
            }
        }

        void INode.UpdateStatus()
        {
            var nodes = OutputNodes;
            var routes = _routes;
            var offline = true;

            if (routes != null && nodes != null)
            {

                if (!Disabled)
                {
                    for (var i = 0; i < routes.Length; i++)
                    {
                        if (OutputNodes[i].Node != null && !OutputNodes[i].Node.Offline)
                        {
                            offline = false;
                            break;
                        }
                    }
                }

                var now = DateTime.UtcNow;
                if (now > _nextTrafficUpdate)
                {
                    _nextTrafficUpdate = now.AddSeconds(3);
                    for (var i = 0; i < nodes.Length; i++)
                    {
                        var node = nodes[i];
                        node.TrafficAnalytics.Recalculate();
                    }
                }
            }

            Offline = offline;
        }

        Task INode.ProcessRequest(IOwinContext context)
        {
            for (var i = 0; i < _routes.Length; i++)
            {
                var route = _routes[i];
                if (route.IsMatch(context))
                {
                    var output = OutputNodes[i];
                    var node = output.Node;
                    if (node != null)
                    {
                        var startTime = output.TrafficAnalytics.BeginRequest();
                        var task = node.ProcessRequest(context);
                        if (task == null)
                            return null;
                        return task.ContinueWith(t =>
                            {
                                output.TrafficAnalytics.EndRequest(startTime);
                            });
                    }
                }
            }

            context.Response.StatusCode = 404;
            context.Response.ReasonPhrase = "No routes in " + Name + " matches the request";
            return context.Response.WriteAsync(string.Empty);
        }

        private class Route
        {
            public Rule[] Rules;
            public RuleLogic RuleLogic;

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