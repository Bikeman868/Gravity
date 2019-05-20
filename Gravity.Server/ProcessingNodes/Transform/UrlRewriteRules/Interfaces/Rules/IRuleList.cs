using System.Collections.Generic;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules
{
    public interface IRuleList : IAction
    {
        IRuleList Initialize(
            string name,
            IList<IRule> rules = null,
            bool stopProcessing = false);

        IRuleList Add(IRule rule);

        string Name { get; }
        IRuleListResult Evaluate(IOwinContext context);
    }
}
