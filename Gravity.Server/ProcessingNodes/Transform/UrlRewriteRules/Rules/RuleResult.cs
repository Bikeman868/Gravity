using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;
using Ioc.Modules;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Rules
{
    internal class RuleResult : IRuleResult
    {
        public bool StopProcessing { get; set; }
        public bool EndRequest { get; set; }
        public bool IsDynamic { get; set; }

        private IPropertyBag _properties;
        public IPropertyBag Properties
        {
            get 
            {
                if (_properties == null) _properties = new PropertyBag();
                return _properties;
            }
        }

    }
}
