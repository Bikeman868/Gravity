using System;
using System.Linq;
using System.Threading.Tasks;
using Gravity.Server.Configuration;
using Gravity.Server.Interfaces;
using Gravity.Server.Utility;
using Gravity.Server.Pipeline;

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
                    ConditionLogic = outputConfiguration.ConditionLogic,
                };

                if (outputConfiguration.Conditions != null && outputConfiguration.Conditions.Length > 0)
                {
                    _routes[i].Rules = outputConfiguration.Conditions
                        .Select<RouterConditionConfiguration,  Rule>(r =>
                        {
                            var equals = r.Condition.IndexOf('=');
                            if (equals < 1 || equals > r.Condition.Length - 2)
                                throw new Exception("Routing expression must contain 'expr = expr'. You have '" +
                                                    r.Condition + "'");

                            var prefix = r.Condition[equals - 1];
                            var isPrefixed = "<>!~".Contains(prefix);

                            var leftSide = r.Condition.Substring(0, isPrefixed ? equals - 1 : equals);
                            var rightSide = r.Condition.Substring(equals + 1);

                            if (isPrefixed)
                            {
                                switch (prefix)
                                {
                                    case '~':
                                        return new ContainsRule
                                        {
                                            Expression1 = _expressionParser.Parse<string>(leftSide),
                                            Expression2 = _expressionParser.Parse<string>(rightSide),
                                        };
                                    case '!':
                                        return new NotEqualsRule
                                        {
                                            Expression1 = _expressionParser.Parse<string>(leftSide),
                                            Expression2 = _expressionParser.Parse<string>(rightSide),
                                        };
                                    case '<':
                                        return new StartsWithRule
                                        {
                                            Expression1 = _expressionParser.Parse<string>(leftSide),
                                            Expression2 = _expressionParser.Parse<string>(rightSide),
                                        };
                                    case '>':
                                        return new EndsWithRule
                                        {
                                            Expression1 = _expressionParser.Parse<string>(leftSide),
                                            Expression2 = _expressionParser.Parse<string>(rightSide),
                                        };
                                }
                            }
                            return new EqualsRule
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

        Task INode.ProcessRequest(IRequestContext context)
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
                        context.Log?.Log(LogType.Logic, LogLevel.Standard, () => $"Router '{Name}' sending the request to node '{node.Name}'");

                        var startTime = output.TrafficAnalytics.BeginRequest();
                        var task = node.ProcessRequest(context);

                        if (task == null)
                        {
                            output.TrafficAnalytics.EndRequest(startTime);
                            return null;
                        }

                        return task.ContinueWith(t =>
                        {
                            output.TrafficAnalytics.EndRequest(startTime);
                        });
                    }
                }
            }

            context.Log?.Log(LogType.Logic, LogLevel.Standard, () => $"Router '{Name}' has no rules that match the request, returning 404 response");

            return Task.Run(() =>
            {
                context.Outgoing.StatusCode = 404;
                context.Outgoing.ReasonPhrase = "No routes in '" + Name + "' match the request";
                context.Outgoing.SendHeaders(context);
            });
        }

        private class Route
        {
            public Rule[] Rules;
            public ConditionLogic ConditionLogic;

            public bool IsMatch(IRequestContext context)
            {
                if (Rules == null)
                    return true;

                switch (ConditionLogic)
                {
                    case ConditionLogic.All:
                        return Rules.All(r => r.IsMatch(context));
                    case ConditionLogic.None:
                        return Rules.All(r => !r.IsMatch(context));
                    case ConditionLogic.Any:
                        return Rules.Any(r => r.IsMatch(context));
                }
                throw new Exception("Routing node does not understand ConditionLogic=" + ConditionLogic);
            }
        }

        private abstract class Rule
        {
            public abstract bool IsMatch(IRequestContext context);
        }

        private class EqualsRule : Rule
        {
            public IExpression<string> Expression1;
            public IExpression<string> Expression2;

            public override bool IsMatch(IRequestContext context)
            {
                var value1 = Expression1.Evaluate(context);
                var value2 = Expression2.Evaluate(context);

                if (string.IsNullOrEmpty(value1)) return string.IsNullOrEmpty(value2);
                if (string.IsNullOrEmpty(value2)) return false;

                return string.Equals(value1, value2, StringComparison.OrdinalIgnoreCase);
            }
        }

        private class NotEqualsRule: Rule
        {
            public IExpression<string> Expression1;
            public IExpression<string> Expression2;

            public override bool IsMatch(IRequestContext context)
            {
                var value1 = Expression1.Evaluate(context);
                var value2 = Expression2.Evaluate(context);

                if (string.IsNullOrEmpty(value1)) return !string.IsNullOrEmpty(value2);
                if (string.IsNullOrEmpty(value2)) return true;

                return !string.Equals(value1, value2, StringComparison.OrdinalIgnoreCase);
            }
        }

        private class ContainsRule: Rule
        {
            public IExpression<string> Expression1;
            public IExpression<string> Expression2;

            public override bool IsMatch(IRequestContext context)
            {
                var value1 = Expression1.Evaluate(context);
                if (string.IsNullOrEmpty(value1)) return false;

                var value2 = Expression2.Evaluate(context);
                if (string.IsNullOrEmpty(value2)) return false;

                return value1.IndexOf(value2, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        private class StartsWithRule: Rule
        {
            public IExpression<string> Expression1;
            public IExpression<string> Expression2;

            public override bool IsMatch(IRequestContext context)
            {
                var value1 = Expression1.Evaluate(context);
                if (string.IsNullOrEmpty(value1)) return false;

                var value2 = Expression2.Evaluate(context);
                if (string.IsNullOrEmpty(value2)) return false;

                return value1.StartsWith(value2, StringComparison.OrdinalIgnoreCase);
            }
        }

        private class EndsWithRule: Rule
        {
            public IExpression<string> Expression1;
            public IExpression<string> Expression2;

            public override bool IsMatch(IRequestContext context)
            {
                var value1 = Expression1.Evaluate(context);
                if (string.IsNullOrEmpty(value1)) return false;

                var value2 = Expression2.Evaluate(context);
                if (string.IsNullOrEmpty(value2)) return false;

                return value1.EndsWith(value2, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}