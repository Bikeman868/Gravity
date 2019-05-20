using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions
{
    public interface IReplaceAction: IAction
    {
        IReplaceAction Initialize(Scope scope, string scopeIndex, IValueGetter valueGetter);
    }
}
