using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions
{
    public interface IInsertAction: IAction
    {
        IInsertAction Initialize(Scope scope, string scopeIndex, IValueGetter valueGetter);
    }
}
