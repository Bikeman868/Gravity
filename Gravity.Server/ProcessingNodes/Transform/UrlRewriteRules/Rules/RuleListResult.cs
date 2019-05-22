using System.Collections.Generic;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Rules
{
    internal class RuleListResult : IRuleListResult
    {
        public bool StopProcessing { get; set; }
        public bool EndRequest { get; set; }
        public bool IsDynamic { get; set; }
        public List<IRuleResult> RuleResults { get; set; }
    }
}
