using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Actions
{
    /// <summary>
    /// Truncates the path of the URL to specified number of elements
    /// </summary>
    internal class None : Action, IDoNothingAction
    {
        public IDoNothingAction Initialize()
        {
            return this;
        }

        public override void PerformAction(
            IRuleExecutionContext requestInfo,
            IRuleResult ruleResult,
            out bool stopProcessing,
            out bool endRequest)
        {
            stopProcessing = _stopProcessing;
            endRequest = _endRequest;
        }

        public override string ToString()
        {
            return "Do nothing";
        }

        public override string ToString(IRuleExecutionContext request)
        {
            return "do nothing";
        }
    }
}
