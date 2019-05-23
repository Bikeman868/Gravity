using System.IO;
using System.Web;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Operations;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Operations
{
    internal class UrlDecodeOperation : IUrlDecodeOperation
    {
        public IUrlDecodeOperation Initialize()
        {
            return this;
        }

        public string Execute(string value)
        {
            return ReferenceEquals(value, null) ? string.Empty : HttpUtility.UrlDecode(value);
        }

        public string ToString(IRuleExecutionContext requestInfo)
        {
            return "UrlDecode()";
        }

        public override string ToString()
        {
            return "UrlDecode()";
        }
    }
}
