namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions
{
    internal interface ITruncateAction: IAction
    {
        ITruncateAction Initialize(int maximumDepth);
    }
}
