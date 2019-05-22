using System.Xml.Linq;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions
{
    public interface ICondition : IRuleElement
    {
        ICondition Initialize(XElement configuration, IValueGetter valueGetter);

        /// <summary>
        /// Tests a request to see if it meets this condition
        /// </summary>
        bool Test(IRequestInfo requestInfo, IRuleResult ruleResult = null);
    }
}
