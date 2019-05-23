﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Conditions
{
    internal class ConditionList : IConditionList
    {
        private CombinationLogic _logic;
        private bool _trackAllCaptures;

        private List<ICondition> _conditions;
        private Func<IRuleExecutionContext, IRuleResult, bool> _testFunc;

        public IConditionList Initialize(CombinationLogic logic, bool trackAllCaptures = false)
        {
            _logic = logic;
            _trackAllCaptures = trackAllCaptures;

            switch (logic)
            {
                case CombinationLogic.MatchNone:
                    _testFunc = (ri, rr) => All(ri, rr, false);
                    break;
                case CombinationLogic.MatchAll:
                    _testFunc = (ri, rr) => All(ri, rr, true);
                    break;
                case CombinationLogic.MatchNotAny:
                    _testFunc = (ri, rr) => Any(ri, rr, false);
                    break;
                case CombinationLogic.MatchAny:
                    _testFunc = (ri, rr) => Any(ri, rr, true);
                    break;
                default:
                    _testFunc = (rq, rr) => false;
                    throw new NotImplementedException("Condition list does not know how to combine conditions using " + logic + " logic");
            }
            return this;
        }

        public IConditionList Add(ICondition condition)
        {
            if (_conditions == null) _conditions = new List<ICondition>();

            var conditionList = condition as ConditionList;
            if (conditionList == null 
                || conditionList._logic != _logic 
                || conditionList._trackAllCaptures != _trackAllCaptures)
                _conditions.Add(condition);
            else if (conditionList._conditions != null)
                _conditions.AddRange(conditionList._conditions);

            return this;
        }

        public ICondition Initialize(XElement configuration, IValueGetter valueGetter)
        {
            return this;
        }

        public bool Test(IRuleExecutionContext request, IRuleResult ruleResult)
        {
            if (_trackAllCaptures)
                ruleResult.Properties.Set(true, "trackAllCaptures");

            var result = _testFunc(request, ruleResult);
            
            if (_trackAllCaptures)
                ruleResult.Properties.Set(false, "trackAllCaptures");

            return result;
        }

        public override string ToString()
        {
            var count = _conditions == null ? 0 : _conditions.Count;
            return "list of " + count + " conditions" + (_trackAllCaptures ? " tracking all captures" : "");
        }

        public string ToString(IRuleExecutionContext requestInfo)
        {
            return ToString();
        }

        private bool All(IRuleExecutionContext request, IRuleResult ruleResult, bool expected)
        {
            if (_conditions == null || _conditions.Count == 0) return true;

            foreach (var condition in _conditions)
            {
                var isTrue = condition.Test(request, ruleResult);

                if (isTrue != expected)
                {
                    return false;
                }
            }

            return true;
        }

        private bool Any(IRuleExecutionContext request, IRuleResult ruleResult, bool expected)
        {
            if (_conditions == null || _conditions.Count == 0) return false;

            foreach (var condition in _conditions)
            {
                var isTrue = condition.Test(request, ruleResult);

                if (isTrue == expected)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
