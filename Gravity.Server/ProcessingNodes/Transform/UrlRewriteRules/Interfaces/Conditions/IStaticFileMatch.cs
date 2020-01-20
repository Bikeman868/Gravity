namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions
{
    internal interface IStaticFileMatch : ICondition
    {
        IStaticFileMatch Initialize(
            IValueGetter valueGetter,
            bool isDirectory,
            bool inverted = false);
    }
}
