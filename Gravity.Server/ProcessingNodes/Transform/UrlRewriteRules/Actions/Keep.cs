using System;
using System.Collections.Generic;
using System.Linq;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Actions
{
    internal class Keep : Action, IKeepAction
    {
        private Scope _scope;
        private List<string> _scopeIndex;
        private List<int> _scopeIndexValue;

        public IKeepAction Initialize(Scope scope, string scopeIndex = null)
        {
            _scope = scope;

            if (scopeIndex == null)
            {
                _scopeIndex = null;
                _scopeIndexValue = null;
                throw new Exception("The keep action requires a comma separated list of the scope indexes to keep");
            }

            switch (scope)
            {
                case Scope.Header:
                case Scope.Parameter:
                case Scope.PathElement:
                case Scope.ServerVariable:
                    break;
                case Scope.Path:
                    _scope = Scope.PathElement;
                    break;
                case Scope.Host:
                    _scope = Scope.HostElement;
                    break;
                case Scope.QueryString:
                    _scope = Scope.Parameter;
                    break;
                default:
                    throw new Exception("You can not have a scope of " + scope + " with the <keep> action");
            }

            _scopeIndex = scopeIndex
                .ToLower()
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => s .Length > 0)
                .ToList();

            _scopeIndexValue = _scopeIndex
                .Select(
                    s =>
                    {
                        int i;
                        return int.TryParse(s, out i) ? (int?)i : (int?) null;
                    })
                .Where( i => i.HasValue)
                .Select( i => i.Value)
                .OrderBy(i => i)
                .ToList();

            if (_scopeIndexValue.Count == 0 || _scopeIndexValue[0] != 0)
                _scopeIndexValue.Insert(0, 0);

            return this;
        }

        public override void PerformAction(
            IRuleExecutionContext requestInfo,
            IRuleResult ruleResult,
            out bool stopProcessing,
            out bool endRequest)
        {
            switch (_scope)
            {
                case Scope.Header:
                    foreach (var header in requestInfo.GetHeaderNames())
                    {
                        if (!_scopeIndex.Contains(header.ToLower()))
                            requestInfo.SetHeader(header, null);
                    }
                    break;
                case Scope.Parameter:
                    var parameters = new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase);
                    foreach (var parameterName in _scopeIndex)
                    {
                        IList<string> parameterValue;
                        if (requestInfo.NewParameters.TryGetValue(parameterName, out parameterValue))
                        {
                            if (parameterValue != null && parameterValue.Count > 0)
                                parameters[parameterName] = parameterValue;
                        }
                    }
                    requestInfo.NewParameters = parameters;
                    break;
                case Scope.PathElement:
                    // Note that _scopeIndexValue is sorted into ascending order and always includes 0
                    var newPath = _scopeIndexValue
                        .Where(i => i >= 0 && i < requestInfo.NewPath.Count)
                        .Select(index => requestInfo.NewPath[index])
                        .ToList();
                    if (newPath.Count < 2) newPath.Add(string.Empty);
                    requestInfo.NewPath = newPath;
                    break;
                case Scope.HostElement:
                    // TODO: but only if this makes any sense
                    break;
            }

            stopProcessing = _stopProcessing;
            endRequest = _endRequest;
        }

        public override string ToString()
        {
            var text = "Keep " + _scope;
            if (_scopeIndex != null)
                text += "[" + string.Join(",", _scopeIndex) + "]";
            return text;
        }

        public override string ToString(IRuleExecutionContext request)
        {
            var text = "keep " + _scope;
            if (_scopeIndex != null)
                text += "[" + string.Join(",", _scopeIndex) + "]";
            return text;
        }
    }
}
