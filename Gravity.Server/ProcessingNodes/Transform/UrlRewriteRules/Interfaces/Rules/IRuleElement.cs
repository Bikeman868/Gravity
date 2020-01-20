using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules
{
    internal interface IRuleElement
    {
        string ToString(IRuleExecutionContext requestInfo);
    }
}
