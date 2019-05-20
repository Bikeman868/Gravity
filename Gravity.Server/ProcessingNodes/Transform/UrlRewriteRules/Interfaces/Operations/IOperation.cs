using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Operations
{
    public interface IOperation : IRuleElement
    {
        string Execute(string value);
    }
}
