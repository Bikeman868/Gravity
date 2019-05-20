using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions
{
    public interface IDeleteAction: IAction
    {
        IDeleteAction Initialize(Scope scope, string scopeIndex = null);
    }
}
