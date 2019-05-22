using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Actions
{
    internal class AbortRequest: Action, IAbortAction
    {
        public IAbortAction Initialize()
        {
            return this;
        }

        public override void PerformAction(
            IRequestInfo requestInfo,
            IRuleResult ruleResult,
            out bool stopProcessing,
            out bool endRequest)
        {
            stopProcessing = true;
            endRequest = true;
        }

        public override string ToString()
        {
            return "Abort the request";
        }

        public override string ToString(IRequestInfo requestInfo)
        {
            return "abort the request";
        }
    }
}
