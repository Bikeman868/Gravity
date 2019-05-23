using System.IO;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Operations;

namespace UrlRewrite.Operations
{
    internal class LowerCaseOperation : ILowerCaseOperation
    {
        public ILowerCaseOperation Initialize()
        {
            return this;
        }

        public string Execute(string value)
        {
            return ReferenceEquals(value, null) ? string.Empty : value.ToLower();
        }

        public string ToString(IRuleExecutionContext requestInfo)
        {
            return "ToLower()";
        }

        public override string ToString()
        {
            return "ToLower()";
        }
    }
}
