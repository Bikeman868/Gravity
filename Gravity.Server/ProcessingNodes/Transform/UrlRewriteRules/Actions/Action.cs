﻿using System.IO;
using System.Xml.Linq;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Actions
{
    internal abstract class Action : IAction
    {
        protected bool _stopProcessing;
        protected bool _endRequest;

        public abstract void PerformAction(
            IRuleExecutionContext requestInfo, 
            IRuleResult ruleResult, 
            out bool stopProcessing,
            out bool endRequest);

        public abstract string ToString(IRuleExecutionContext requestInfo);

        public virtual IAction Initialize(XElement configuration)
        {
            return this;
        }
    }
}
