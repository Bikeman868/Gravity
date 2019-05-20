namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions
{
    public interface ITruncateAction: IAction
    {
        ITruncateAction Initialize(int maximumDepth);
    }
}
