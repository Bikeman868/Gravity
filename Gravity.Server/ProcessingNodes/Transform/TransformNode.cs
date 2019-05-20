using System.Threading.Tasks;
using Gravity.Server.Configuration;
using Gravity.Server.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules;
using Microsoft.Owin;
using OwinFramework.Interfaces.Utility;

namespace Gravity.Server.ProcessingNodes.Transform
{
    internal class TransformNode: INode
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public string Name { get; set; }
        public bool Disabled { get; set; }
        public string OutputNode { get; set; }
        public ScriptLanguage ScriptLanguage { get; set; }
        public string RequestScript { get; set; }
        public string ResponseScript { get; set; }
        public string RequestScriptFile { get; set; }
        public string ResponseScriptFile { get; set; }
        public bool Offline { get; private set; }

        private INode _nextNode;
        private IRequestTransform _requestTransform;
        private IResponseTransform _responseTransform;

        public TransformNode(
            IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public void Dispose()
        {
        }

        void INode.Bind(INodeGraph nodeGraph)
        {
            _nextNode = nodeGraph.NodeByName(OutputNode);
            var rewriteRuleParser = (IScriptParser)(new Parser());

            if (!string.IsNullOrEmpty(RequestScript))
            {
                switch (ScriptLanguage)
                {
                    //case ScriptLanguage.UrlRewriteModule:
                    //    _requestTransform = rewriteRuleParser.ParseRequestScript(RequestScript);
                    //    break;
                }
            }

            if (!string.IsNullOrEmpty(ResponseScript))
            {
                switch (ScriptLanguage)
                {
                    //case ScriptLanguage.UrlRewriteModule:
                    //    _responseTransform = rewriteRuleParser.ParseResponseScript(ResponseScript);
                    //    break;
                }
            }
        }

        void INode.UpdateStatus()
        {
            if (Disabled || _nextNode == null)
                Offline = true;
            else
                Offline = _nextNode.Offline;
        }

        Task INode.ProcessRequest(IOwinContext context)
        {
            if (_nextNode == null)
            {
                context.Response.StatusCode = 503;
                context.Response.ReasonPhrase = "Transform " + Name + " has no downstream";
                return context.Response.WriteAsync(string.Empty);
            }

            if (!Disabled && _requestTransform != null)
            {
                _requestTransform.Transform(context);
            }

            if (Disabled || _responseTransform == null)
                return _nextNode.ProcessRequest(context);

            var wrappedContext = _responseTransform.WrapOriginalRequest(context);
            return _nextNode.ProcessRequest(wrappedContext).ContinueWith(t =>
            {
                _responseTransform.Transform(context, wrappedContext);
            });
        }
    }
}