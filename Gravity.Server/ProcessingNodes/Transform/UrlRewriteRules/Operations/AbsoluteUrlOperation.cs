using System.IO;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Operations;

namespace UrlRewrite.Operations
{
    internal class AbsoluteUrlOperation : IAbsoluteUrlOperation
    {
        public IAbsoluteUrlOperation Initialize()
        {
            return this;
        }

        public string Execute(string value)
        {
            if (ReferenceEquals(value, null) || value.Length == 0) return "/";
            if (value[0] == '/' || value.Contains("://")) return value;
            return "/" + value;
        }

        public string ToString(IRequestInfo requestInfo)
        {
            return "ToAbsoluteUrl()";
        }

        public override string ToString()
        {
            return "ToAbsoluteUrl()";
        }
    }
}
