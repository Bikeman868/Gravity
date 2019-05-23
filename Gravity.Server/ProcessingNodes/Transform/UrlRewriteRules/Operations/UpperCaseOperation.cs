using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Operations;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Operations
{
    internal class UpperCaseOperation : IUpperCaseOperation
    {
        public IUpperCaseOperation Initialize()
        {
            return this;
        }

        public string Execute(string value)
        {
            return ReferenceEquals(value, null) ? string.Empty : value.ToUpper();
        }

        public string ToString(IRuleExecutionContext requestInfo)
        {
            return "ToUpper()";
        }

        public override string ToString()
        {
            return "ToUpper()";
        }
    }
}
