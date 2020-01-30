using System;
using System.Linq;
using System.Threading.Tasks;
using Gravity.Server.Configuration;
using Gravity.Server.Interfaces;
using Gravity.Server.Utility;
using Gravity.Server.Pipeline;
using System.Collections.Generic;
using System.Net;

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

        public override Task ProcessRequestAsync(IRequestContext context)
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
                    var task = node.ProcessRequestAsync(context);

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
            public bool Negate { get; set; }
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
                    rules.AddRange(configuration.Conditions
                        .Where(c => !c.Disabled)
                        .Select(c => CreateConditionRule(c, expressionParser)));
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
                        return Rules.All(r => r.Negate ? !r.IsMatch(context) : r.IsMatch(context));
                    case ConditionLogic.None:
                        return Rules.All(r => r.Negate ? r.IsMatch(context) : !r.IsMatch(context));
                    case ConditionLogic.Any:
                        return Rules.Any(r => r.Negate ? !r.IsMatch(context) : r.IsMatch(context));
                    case ConditionLogic.NotAll:
                        return Rules.Any(r => r.Negate ? r.IsMatch(context) : !r.IsMatch(context));
                }
                throw new Exception("Routing node does not understand ConditionLogic=" + ConditionLogic);
            }

            private Rule CreateConditionRule(
                RouterConditionConfiguration conditionConfiguration, 
                IExpressionParser expressionParser)
            {
                var equals = conditionConfiguration.Condition.IndexOf('=');
                if (equals < 1 || equals > conditionConfiguration.Condition.Length - 2)
                    throw new Exception($"Routing condition must contain 'expr = expr'. You have '{conditionConfiguration.Condition}'");

                var prefix = conditionConfiguration.Condition[equals - 1];
                var isPrefixed = "<>!~".Contains(prefix);

                var leftSide = conditionConfiguration.Condition.Substring(0, isPrefixed ? equals - 1 : equals);
                var rightSide = conditionConfiguration.Condition.Substring(equals + 1);

                var leftExpression = expressionParser.Parse<string>(leftSide);
                var rightExpression = expressionParser.Parse<string>(rightSide);

                if (leftExpression.BaseType == typeof(IPAddress))
                {
                    // Prefixes are not relevant to IP address comparisons
                    return new IPAddressEqualityRule
                    {
                        Negate = conditionConfiguration.Negate,
                        Expression1 = leftExpression.Cast<IPAddress>(),
                        Expression2 = rightExpression,
                    };
                }
                else // Use string comparison by default
                {
                    if (isPrefixed)
                    {
                        switch (prefix)
                        {
                            case '~':
                                return new StringContainsRule
                                {
                                    Negate = conditionConfiguration.Negate,
                                    Expression1 = leftExpression,
                                    Expression2 = rightExpression,
                                };
                            case '!':
                                return new StringInequalityRule
                                {
                                    Negate = conditionConfiguration.Negate,
                                    Expression1 = leftExpression,
                                    Expression2 = rightExpression,
                                };
                            case '<':
                                return new StringStartsWithRule
                                {
                                    Negate = conditionConfiguration.Negate,
                                    Expression1 = leftExpression,
                                    Expression2 = rightExpression,
                                };
                            case '>':
                                return new StringEndsWithRule
                                {
                                    Negate = conditionConfiguration.Negate,
                                    Expression1 = leftExpression,
                                    Expression2 = rightExpression,
                                };
                        }
                    }
                    return new StringEqualityRule
                    {
                        Negate = conditionConfiguration.Negate,
                        Expression1 = leftExpression,
                        Expression2 = rightExpression,
                    };
                }
            }
        }

        #region String comparison rules

        private class StringEqualityRule : Rule
        {
            public IExpression<string> Expression1 { get; set; }
            public IExpression<string> Expression2 { get; set; }

            public override bool IsMatch(IRequestContext context)
            {
                var value1 = Expression1.Evaluate(context);
                var value2 = Expression2.Evaluate(context);

                if (string.IsNullOrEmpty(value1)) return string.IsNullOrEmpty(value2);
                if (string.IsNullOrEmpty(value2)) return false;

                return string.Equals(value1, value2, StringComparison.OrdinalIgnoreCase);
            }
        }

        private class StringInequalityRule: Rule
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

        private class StringContainsRule: Rule
        {
            public IExpression<string> Expression1;
            public IExpression<string> Expression2;

            public override bool IsMatch(IRequestContext context)
            {
                if (Expression1.BaseType == typeof(IPAddress))
                {
                    var address1String = Expression1.Evaluate(context);
                    if (string.IsNullOrEmpty(address1String)) return false;

                    var address2String = Expression2.Evaluate(context);
                    if (string.IsNullOrEmpty(address2String)) return false;

                    if (!IPAddress.TryParse(address1String, out var address1)) return false;

                    IPAddress address2;
                    var mask = uint.MaxValue;

                    var cidrSeparator = address2String.IndexOf("/");
                    if (cidrSeparator < 0)
                    {
                        if (string.Equals("loopback", address2String, StringComparison.OrdinalIgnoreCase))
                            address2 = address1.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? IPAddress.Loopback : IPAddress.IPv6Loopback;
                        else if (!IPAddress.TryParse(address2String, out address2)) return false;
                    }
                    else
                    {
                        if (!IPAddress.TryParse(address2String.Substring(0, cidrSeparator), out address2)) return false;
                        if (!int.TryParse(address2String.Substring(cidrSeparator + 1), out var cidrBlock)) return false;
                        if (cidrBlock > 32 || cidrBlock < 4) return false;
                        unchecked { mask <<= 32 - cidrBlock; }
                    }

                    var address1Value = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(address1.GetAddressBytes(), 0));
                    var address2Value = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(address2.GetAddressBytes(), 0));

                    return (address1Value & mask) == address2Value;
                }

                var value1 = Expression1.Evaluate(context);
                if (string.IsNullOrEmpty(value1)) return false;

                var value2 = Expression2.Evaluate(context);
                if (string.IsNullOrEmpty(value2)) return false;

                return value1.IndexOf(value2, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        private class StringStartsWithRule: Rule
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

        private class StringEndsWithRule: Rule
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

        #endregion

        #region IP Address comparison rules

        private class IPAddressEqualityRule : Rule
        {
            private IExpression<string> _expression2;

            private IPAddressRange[] _expression2AddressRanges;

            public IExpression<IPAddress> Expression1 { get; set; }

            public IExpression<string> Expression2
            {
                get => _expression2;
                set
                {
                    _expression2 = value;

                    if (value.IsLiteral)
                    {
                        if (value.BaseType == typeof(string[]))
                        {
                            // The list of strings can come from a file, so will re-evaluate the literal from time to time
                            ReloadList();
                        }
                        else
                        {
                            var literalValue = value.Evaluate(null);
                            _expression2AddressRanges = new[] { IPAddressRange.Parse(literalValue) };
                        }
                    }
                }
            }

            public override bool IsMatch(IRequestContext context)
            {
                var address1 = Expression1.Evaluate(context);

                if (_expression2.IsLiteral)
                    return _expression2AddressRanges.Any(r => r.Contains(address1));

                var address2 = IPAddress.Parse(Expression2.Evaluate(context));

                return address1.Equals(address2);
            }

            private void ReloadList()
            {
                try
                {
                    var expression2Array = _expression2.Cast<string[]>();

                    _expression2AddressRanges = expression2Array.Evaluate(null)
                        .Select(addressRange => IPAddressRange.Parse(addressRange))
                        .ToArray();
                }
                finally
                {
                    Task
                        .Delay(TimeSpan.FromMinutes(1))
                        .ContinueWith(delayTask => 
                        {
                            if (!delayTask.IsCanceled) ReloadList();
                        });
                }
            }
        }

        #endregion
    }
}