using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules
{
    public interface IRule : IRuleElement
    {
        IRule Initialize(
            string name,
            ICondition condition,
            IAction action,
            bool stopProcessing = false,
            bool isDynamic = false);

        string Name { get; }
        IRuleResult Evaluate(IRequestInfo requestInfo);
    }
}
