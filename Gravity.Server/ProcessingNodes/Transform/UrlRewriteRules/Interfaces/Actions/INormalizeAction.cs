namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions
{
    public enum NormalizeAction {  None, Add, Remove }

    public interface INormalizeAction: IAction
    {
        INormalizeAction Initialize(
            NormalizeAction leadingSeparator, 
            NormalizeAction trailingSeparator);
    }
}
