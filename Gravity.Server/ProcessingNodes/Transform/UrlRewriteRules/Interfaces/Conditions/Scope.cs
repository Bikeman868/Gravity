namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions
{
    public enum Scope
    {
        OriginalUrl,
        OriginalHost,
        OriginalPath,
        OriginalQueryString,
        OriginalPathElement,
        OriginalParameter,
        OriginalHeader,

        Url,
        Host,
        HostElement,
        Path,
        MatchPath,
        QueryString,
        PathElement,
        Parameter,
        Header,

        OriginalServerVariable,
        ServerVariable,
        Literal,

        ConditionGroup,
        MatchGroup
    }
}
