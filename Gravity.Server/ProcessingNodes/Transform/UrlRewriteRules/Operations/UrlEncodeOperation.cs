using System.IO;
using System.Web;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Operations;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Operations
{
    internal class UrlEncodeOperation : IUrlEncodeOperation
    {
        public IUrlEncodeOperation Initialize()
        {
            return this;
        }

        public string Execute(string value)
        {
            return ReferenceEquals(value, null) ? string.Empty : HttpUtility.UrlEncode(value);
        }

        public string ToString(IRequestInfo requestInfo)
        {
            return "UrlEncode()";
        }

        public override string ToString()
        {
            return "UrlEncode()";
        }
    }
}
