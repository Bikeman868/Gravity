using System.Xml.Linq;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions
{
    public interface IAction: IRuleElement
    {
        IAction Initialize(XElement configuration);

        /// <summary>
        /// Performs the redirection, rewrite or whatever action is required
        /// when the rule matches the incomming request
        /// </summary>
        void PerformAction(
            IOwinContext context, 
            IRuleResult ruleResult, 
            out bool stopProcessing, 
            out bool endRequest);
    }
}
