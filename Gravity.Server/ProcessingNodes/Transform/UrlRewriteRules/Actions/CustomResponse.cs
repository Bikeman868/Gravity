using System.Xml.Linq;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Actions
{
    internal class CustomResponse : Action, ICustomResponse
    {
        private string _statusLine;
        private string _responseLine;

        public ICustomResponse Initialize(XElement configuration, bool stopProcessing, bool endRequest)
        {
            _stopProcessing = stopProcessing;
            _endRequest = endRequest;
            _statusLine = "HTTP/1.1 200 OK";
            _responseLine = "OK";

            if (configuration.HasAttributes)
            {
                foreach (var attribute in configuration.Attributes())
                {
                    switch (attribute.Name.LocalName.ToLower())
                    {
                        case "statusline":
                            _statusLine = attribute.Value;
                            break;
                        case "responseline":
                            _responseLine = attribute.Value;
                            break;
                    }
                }
            }
            base.Initialize(configuration);
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
            return "Return a custom response";
        }

        public override string ToString(IRuleExecutionContext requestInfo)
        {
            return "return a custom response";
        }
    }
}
