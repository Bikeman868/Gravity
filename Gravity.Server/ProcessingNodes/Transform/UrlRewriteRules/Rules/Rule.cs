using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Rules
{
    internal class Rule : IRule
    {
        private string _name;
        private ICondition _condition;
        private bool _stopProcessing;
        private bool _isDynamic;
        private List<IAction> _actions;

        public IRule Initialize(
            string name, 
            ICondition condition, 
            IAction action, 
            bool stopProcessing,
            bool isDynamic)
        {
            _name = name;
            _condition = condition;
            _actions = action == null ? null : new List<IAction> { action };
            _stopProcessing = stopProcessing;
            _isDynamic = isDynamic;

            return this;
        }

        public string Name { get { return _name; } }

        public IRuleResult Evaluate(IRequestInfo requestInfo)
        {
            var result = new RuleResult();

            var conditionIsTrue = true;
            if (_condition != null)
            {
                conditionIsTrue = _condition.Test(requestInfo, result);
            }

            if (conditionIsTrue)
            {
                result.StopProcessing = _stopProcessing;
                result.IsDynamic = _isDynamic;

                if (_actions != null)
                {
                    foreach (var action in _actions)
                    {
                        bool stopProcessing;
                        bool endRequest;
                        action.PerformAction(requestInfo, result, out stopProcessing, out endRequest);

                        if (endRequest)
                            result.EndRequest = true;

                        if (stopProcessing)
                        {
                            result.StopProcessing = true;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public override string ToString()
        {
            return "rule '" + _name + "'";
        }

        public void Initialize(XElement configuration)
        {
        }

        public string ToString(IRequestInfo requestInfo)
        {
            return ToString();
        }
    }
}
