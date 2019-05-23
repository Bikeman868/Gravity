using System.Collections.Generic;
using System.Xml.Linq;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Rules
{
    internal class RuleList : IRuleList, IAction
    {
        private string _name;
        private bool _stopProcessing;

        private IList<IRule> _rules;

        public IRuleList Initialize(
            string name, 
            IList<IRule> rules,
            bool stopProcessing)
        {
            _name = name;
            _rules = rules;
            _stopProcessing = stopProcessing;
            return this;
        }

        public IRuleList Add(IRule rule)
        {
            if (_rules == null) _rules = new List<IRule>();
            _rules.Add(rule);
            return this;
        }

        public string Name
        {
            get { return _name; }
        }

        public IRuleListResult Evaluate(IRuleExecutionContext requestInfo)
        {
            var ruleListResult = new RuleListResult();

            if (_rules != null && _rules.Count > 0)
            {
                ruleListResult.RuleResults = new List<IRuleResult>();

                foreach (var rule in _rules)
                {
                    var ruleResult = rule.Evaluate(requestInfo);
                    ruleListResult.RuleResults.Add(ruleResult);

                    if (ruleResult.EndRequest) ruleListResult.EndRequest = true;
                    if (ruleResult.IsDynamic) ruleListResult.IsDynamic = true;
                    if (ruleResult.StopProcessing)
                    {
                        ruleListResult.StopProcessing = _stopProcessing;
                        break;
                    }
                }
            }
            return ruleListResult;
        }

        public override string ToString()
        {
            var count = _rules == null ? 0 : _rules.Count;
            return "list of " + count + " rules '" + _name + "'";
        }

        public IAction Initialize(XElement configuration)
        {
            return this;
        }

        public string ToString(IRuleExecutionContext requestInfo)
        {
            return ToString();
        }

        void IAction.PerformAction(
            IRuleExecutionContext requestInfo, 
            IRuleResult ruleResult, 
            out bool stopProcessing, 
            out bool endRequest)
        {
            var result = Evaluate(requestInfo);
            stopProcessing = result.StopProcessing;
            endRequest = result.EndRequest;
        }
    }
}
