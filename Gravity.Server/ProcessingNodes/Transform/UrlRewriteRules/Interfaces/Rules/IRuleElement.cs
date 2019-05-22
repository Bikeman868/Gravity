using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules
{
    public interface IRuleElement
    {
        string ToString(IRequestInfo requestInfo);
    }
}
