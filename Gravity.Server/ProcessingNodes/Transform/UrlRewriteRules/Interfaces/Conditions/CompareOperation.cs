namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions
{
    public enum CompareOperation
    {
        StartsWith,
        EndsWith,
        Contains,
        Equals,
        MatchWildcard,
        MatchRegex,
        Greater,
        Less
    }
}
