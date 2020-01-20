using System.Collections.Generic;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules
{
    internal interface IRuleListResult
    {
        bool StopProcessing { get; }
        bool EndRequest { get; }
        bool IsDynamic { get; }
        List<IRuleResult> RuleResults { get; }
    }
}
