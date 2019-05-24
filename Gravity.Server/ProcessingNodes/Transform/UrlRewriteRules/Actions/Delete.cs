﻿using System;
using System.Linq;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Actions
{
    internal class Delete : Action, IDeleteAction
    {
        private Scope _scope;
        private string _scopeIndex;
        private int _scopeIndexValue;

        public IDeleteAction Initialize(Scope scope, string scopeIndex)
        {
            _scope = scope;
            _scopeIndex = scopeIndex;

            if (string.IsNullOrEmpty(scopeIndex))
            {
                switch (scope)
                {
                    case Scope.Header:
                        throw new Exception("When deleting a request header you must specify the name of the header to delete");
                    case Scope.Parameter:
                        _scope = Scope.QueryString;
                        break;
                    case Scope.PathElement:
                        _scope = Scope.Path;
                        break;
                }
            }
            else
            {
                if (!int.TryParse(scopeIndex, out _scopeIndexValue))
                {
                    if (scope == Scope.PathElement) _scope = Scope.Path;
                }
            }

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
                case Scope.Url:
                    requestInfo.NewUrlString = "/";
                    break;
                case Scope.Host:
                    requestInfo.NewHost = string.Empty;
                    break;
                case Scope.Path:
                    requestInfo.NewPathString = "/";
                    break;
                case Scope.QueryString:
                    requestInfo.NewParametersString = string.Empty;
                    break;
                case Scope.Header:
                    requestInfo.SetHeader(_scopeIndex, null);
                    break;
                case Scope.Parameter:
                    requestInfo.NewParameters.Remove(_scopeIndex);
                    requestInfo.ParametersChanged();
                    break;
                case Scope.PathElement:
                {
                    if (_scopeIndexValue == 0 || requestInfo.NewPathString == "/")
                    {
                        requestInfo.NewPathString = "/";
                    }
                    else
                    {
                        var count = requestInfo.NewPath.Count;
                        if (string.IsNullOrEmpty(requestInfo.NewPath[count - 1])) count--;
                        var indexToRemove = _scopeIndexValue < 0
                            ? count + _scopeIndexValue
                            : _scopeIndexValue;
                        if (indexToRemove > 0 && indexToRemove < count)
                        {
                            requestInfo.NewPath.RemoveAt(indexToRemove);
                            requestInfo.PathChanged();
                        }
                    }
                    break;
                }
                case Scope.HostElement:
                {
                    if (_scopeIndexValue == 0 || string.IsNullOrEmpty(requestInfo.NewHost))
                    {
                        requestInfo.NewHost = string.Empty;
                    }
                    else
                    {
                        var hostElements = requestInfo.NewHost.Split('.');
                        var count = hostElements.Length;
                        if (string.IsNullOrEmpty(hostElements[count - 1])) count--;
                        var indexToRemove = _scopeIndexValue < 0
                            ? count + _scopeIndexValue
                            : _scopeIndexValue - 1;
                        if (indexToRemove >= 0 && indexToRemove < count)
                        {
                            var elementList = hostElements.ToList();
                            elementList.RemoveAt(indexToRemove);
                            requestInfo.NewHost = string.Join(".", elementList);
                        }
                    }
                    break;
                }
                case Scope.ServerVariable:
                    requestInfo.SetServerVariable(_scopeIndex, null);
                    break;
            }

            stopProcessing = _stopProcessing;
            endRequest = _endRequest;
        }

        public override string ToString()
        {
            var text = "Delete " + _scope;
            if (!string.IsNullOrEmpty(_scopeIndex))
                text += "[" + _scopeIndex + "]";
            return text;
        }

        public override string ToString(IRuleExecutionContext request)
        {
            var text = "delete " + _scope;
            if (!string.IsNullOrEmpty(_scopeIndex))
                text += "[" + _scopeIndex + "]";
            return text;
        }
    }
}
