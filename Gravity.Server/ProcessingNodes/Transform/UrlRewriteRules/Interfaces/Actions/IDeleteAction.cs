using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions
{
    internal interface IDeleteAction: IAction
    {
        IDeleteAction Initialize(Scope scope, string scopeIndex = null);
    }
}
