using System.IO;
using System.Xml.Linq;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Actions
{
    public abstract class Action : IAction
    {
        protected bool _stopProcessing;
        protected bool _endRequest;

        public abstract void PerformAction(
            IRequestInfo requestInfo, 
            IRuleResult ruleResult, 
            out bool stopProcessing,
            out bool endRequest);

        public abstract string ToString(IRequestInfo requestInfo);

        public virtual IAction Initialize(XElement configuration)
        {
            return this;
        }
    }
}
