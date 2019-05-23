using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Operations;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions
{
    public interface IValueGetter
    {
        IValueGetter Initialize(Scope scope, string scopeIndex = null, IOperation operation = null);
        IValueGetter Initialize(Scope scope, int scopeIndex, IOperation operation = null);

        string GetString(IRuleExecutionContext requestInfo, IRuleResult ruleResult);
        int GetInt(IRuleExecutionContext requestInfo, IRuleResult ruleResult, int defaultValue);

        string ToString(IRuleExecutionContext requestInfo);
    }
}
