namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions
{
    public interface IActionList: IAction
    {
        IActionList Initialize(bool stopProcessing = false, bool endRequest = false);
        IActionList Add(IAction action);
    }
}
