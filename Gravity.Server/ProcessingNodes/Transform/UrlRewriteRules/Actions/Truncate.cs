using System.Linq;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Actions
{
    /// <summary>
    /// Truncates the path of the URL to specified number of elements
    /// </summary>
    internal class Truncate : Action, ITruncateAction
    {
        private int _maximumDepth;

        public ITruncateAction Initialize(int maximumDepth)
        {
            _maximumDepth = maximumDepth;
            return this;
        }

        public override void PerformAction(
            IRequestInfo requestInfo,
            IRuleResult ruleResult,
            out bool stopProcessing,
            out bool endRequest)
        {
            if (requestInfo.NewPath != null && requestInfo.NewPath.Count > _maximumDepth)
                requestInfo.NewPath = requestInfo.NewPath.Take(_maximumDepth).ToList();

            stopProcessing = _stopProcessing;
            endRequest = _endRequest;
        }

        public override string ToString()
        {
            return "Truncate the URL path to a maximum depth of " + _maximumDepth;
        }

        public override string ToString(IRequestInfo request)
        {
            return "truncate the URL path to a maximum depth of " + _maximumDepth;
        }
    }
}
