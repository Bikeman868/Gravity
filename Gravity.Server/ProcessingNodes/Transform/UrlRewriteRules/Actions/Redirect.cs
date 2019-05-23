using System;
using System.Linq;
using System.Xml.Linq;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Rules;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Actions
{
    internal class Redirect : Action, IRedirectAction
    {
        private Action<IRuleExecutionContext, string> _redirectAction;
        private string _code;

        public override void PerformAction(
            IRuleExecutionContext requestInfo,
            IRuleResult ruleResult,
            out bool stopProcessing,
            out bool endRequest)
        {
            stopProcessing = _stopProcessing;
            endRequest = _endRequest;
        }

        public IRedirectAction Initialize(XElement configuration, bool stopProcessing, bool endRequest)
        {
            _stopProcessing = stopProcessing;
            _endRequest = endRequest;

            var redirectTypeAttribute = configuration.Attributes().FirstOrDefault(a => a.Name.LocalName.ToLower() == "redirecttype");
            _code = redirectTypeAttribute == null ? "307" : redirectTypeAttribute.Value;

            switch (_code.ToLower())
            {
                case "permanent":
                case "301":
                    _code = "301";
                    //_redirectAction = (ri, url) => ri.Context.Response.RedirectPermanent(url);
                    break;
                case "found":
                case "302":
                    _code = "302";
                    _redirectAction = (ri, url) =>
                    {
                        ri.Context.Response.Redirect(url);
                        ri.Context.Response.StatusCode = 302;
                    };
                    break;
                case "seeother":
                case "see other":
                case "303":
                    _code = "303";
                    _redirectAction = (ri, url) =>
                    {
                        ri.Context.Response.Redirect(url);
                        ri.Context.Response.StatusCode = 303;
                    };
                    break;
                case "temporary":
                case "307":
                    _code = "307";
                    _redirectAction = (ri, url) => ri.Context.Response.Redirect(url);
                    break;
                default:
                    throw new Exception(
                        "Unknown redirectType=\"" 
                        + _code 
                        + "\". Supported values are permanent, found, seeOther, temporary, 301, 302, 303 and 307");
            }
            base.Initialize(configuration);
            return this;
        }

        public override string ToString()
        {
            return "Redirect to new URL with " + _code + " code";
        }

        public override string ToString(IRuleExecutionContext requestInfo)
        {
            return "redirect to '" + requestInfo.NewUrlString + "' with " + _code + " code";
        }
    }
}
