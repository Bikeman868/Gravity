using System;
using System.Linq;
using System.Threading.Tasks;
using Gravity.Server.Configuration;
using Gravity.Server.Interfaces;
using Gravity.Server.Utility;
using Gravity.Server.Pipeline;
using System.Collections.Generic;

namespace Gravity.Server.ProcessingNodes.Routing
{
    internal class RoutingNode: ProcessingNode
    {
        public RouterOutputConfiguration[] Outputs { get; set; }
        public NodeOutput[] OutputNodes;

        private readonly IExpressionParser _expressionParser;

        private GroupRule[] _outputRule;
        private DateTime _nextTrafficUpdate;

        public RoutingNode(
            IExpressionParser expressionParser)
        {
            _expressionParser = expressionParser;
        }

        public override void Bind(INodeGraph nodeGraph)
        {
            _outputRule = new GroupRule[Outputs.Length];
            OutputNodes = new NodeOutput[Outputs.Length];

            for (var i = 0; i < Outputs.Length; i++)
            {
                var outputConfiguration = Outputs[i];

                OutputNodes[i] = new NodeOutput
                {
                    Name = outputConfiguration.RouteTo,
                    Node = nodeGraph.NodeByName(outputConfiguration.RouteTo),
                };

                _outputRule[i] = new GroupRule(outputConfiguration, _expressionParser);
            }
        }

        public override void UpdateStatus()
        {
            var nodes = OutputNodes;
            var routes = _outputRule;
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

        public override Task ProcessRequest(IRequestContext context)
        {
            for (var i = 0; i < _outputRule.Length; i++)
            {
                if (!_outputRule[i].IsMatch(context))
                {
                    context.Log?.Log(LogType.Step, LogLevel.VeryDetailed, () => $"Router '{Name}' matches the request but has no output node");
                    continue;
                }

                var output = OutputNodes[i];
                var node = output.Node;

                if (node == null)
                {
                    context.Log?.Log(LogType.Step, LogLevel.Detailed, () => $"Router '{Name}' output {i+1} matches the request but is not connected to a node");
                }
                else
                {
                    context.Log?.Log(LogType.Step, LogLevel.Standard, () => $"Router '{Name}' output {i + 1} matches the request, sending the request to node '{node.Name}'");

                    var trafficAnalyticInfo = output.TrafficAnalytics.BeginRequest();
                    trafficAnalyticInfo.Method = context.Incoming.Method;
                    var task = node.ProcessRequest(context);

                    if (task == null)
                    {
                        output.TrafficAnalytics.EndRequest(trafficAnalyticInfo);
                        return null;
                    }

                    return task.ContinueWith(t =>
                    {
                        trafficAnalyticInfo.StatusCode = context.Outgoing.StatusCode;
                        output.TrafficAnalytics.EndRequest(trafficAnalyticInfo);
                    });
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

        private abstract class Rule
        {
            public abstract bool IsMatch(IRequestContext context);
        }

        private class GroupRule: Rule
        {
            public Rule[] Rules;
            public ConditionLogic ConditionLogic;

            public GroupRule(
                RouterGroupConfiguration configuration, 
                IExpressionParser expressionParser)
            {
                if (configuration.Disabled) return;

                ConditionLogic = configuration.ConditionLogic;
                var rules = new List<Rule>();

                if (configuration.Conditions != null)
                {
                    rules.AddRange(configuration.Conditions.Select<RouterConditionConfiguration, Rule>(r =>
                        {
                            var equals = r.Condition.IndexOf('=');
                            if (equals < 1 || equals > r.Condition.Length - 2)
                                throw new Exception("Routing expression must contain 'expr = expr'. You have '" + r.Condition + "'");

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
                                            Expression1 = expressionParser.Parse<string>(leftSide),
                                            Expression2 = expressionParser.Parse<string>(rightSide),
                                        };
                                    case '!':
                                        return new NotEqualsRule
                                        {
                                            Expression1 = expressionParser.Parse<string>(leftSide),
                                            Expression2 = expressionParser.Parse<string>(rightSide),
                                        };
                                    case '<':
                                        return new StartsWithRule
                                        {
                                            Expression1 = expressionParser.Parse<string>(leftSide),
                                            Expression2 = expressionParser.Parse<string>(rightSide),
                                        };
                                    case '>':
                                        return new EndsWithRule
                                        {
                                            Expression1 = expressionParser.Parse<string>(leftSide),
                                            Expression2 = expressionParser.Parse<string>(rightSide),
                                        };
                                }
                            }
                            return new EqualsRule
                            {
                                Expression1 = expressionParser.Parse<string>(leftSide),
                                Expression2 = expressionParser.Parse<string>(rightSide),
                            };
                        }));
                }

                if (configuration.Groups != null)
                {
                    rules.AddRange(
                        configuration.Groups
                        .Where(g => !g.Disabled)
                        .Select(g => new GroupRule(g, expressionParser)));
                }

                Rules = rules.ToArray();
            }

            public override bool IsMatch(IRequestContext context)
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