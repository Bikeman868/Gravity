using System.IO;
using System.Text;
using System.Threading.Tasks;
using Gravity.Server.Configuration;
using Gravity.Server.Interfaces;
using Microsoft.Owin;
using OwinFramework.Interfaces.Utility;

namespace Gravity.Server.ProcessingNodes.Transform
{
    internal class TransformNode: INode
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IFactory _factory;

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
            IHostingEnvironment hostingEnvironment,
            IFactory factory)
        {
            _hostingEnvironment = hostingEnvironment;
            _factory = factory;
        }

        public void Dispose()
        {
        }

        void INode.Bind(INodeGraph nodeGraph)
        {
            _nextNode = nodeGraph.NodeByName(OutputNode);

            if (!string.IsNullOrEmpty(RequestScript))
            {
                using (var stream = new MemoryStream())
                {
                    var writer = new StreamWriter(stream);
                    writer.Write(RequestScript);
                    writer.Flush();
                    stream.Position = 0;

                    ParseRequestStream(stream, writer.Encoding);
                }
            }
            else if (!string.IsNullOrEmpty(RequestScriptFile))
            {
                var filePath = _hostingEnvironment.MapPath(RequestScriptFile);
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    ParseRequestStream(stream, Encoding.UTF8);
                }
            }

            if (!string.IsNullOrEmpty(ResponseScript))
            {
                using (var stream = new MemoryStream())
                {
                    var writer = new StreamWriter(stream);
                    writer.Write(ResponseScript);
                    writer.Flush();
                    stream.Position = 0;

                    ParseResponseStream(stream, writer.Encoding);
                }
            }
            else if (!string.IsNullOrEmpty(ResponseScriptFile))
            {
                var filePath = _hostingEnvironment.MapPath(ResponseScriptFile);
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    ParseResponseStream(stream, Encoding.UTF8);
                }
            }
        }

        private void ParseRequestStream(Stream stream, Encoding encoding)
        {
            switch (ScriptLanguage)
            {
                case ScriptLanguage.UrlRewriteModule:
                    var rewriteRuleParser = (IScriptParser)(new UrlRewriteRules.Parser(_factory));
                    _requestTransform = rewriteRuleParser.ParseRequestScript(stream, encoding);
                    break;
            }
        }

        private void ParseResponseStream(Stream stream, Encoding encoding)
        {
            switch (ScriptLanguage)
            {
                case ScriptLanguage.UrlRewriteModule:
                    var rewriteRuleParser = (IScriptParser)(new UrlRewriteRules.Parser(_factory));
                    _responseTransform = rewriteRuleParser.ParseResponseScript(stream, encoding);
                    break;
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