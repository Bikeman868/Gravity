﻿using System.IO;
using System.Text;
using System.Threading.Tasks;
using Gravity.Server.Configuration;
using Gravity.Server.Interfaces;
using OwinFramework.Interfaces.Utility;
using Gravity.Server.Pipeline;

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
        public string Description { get; set; }
        public bool Offline { get; private set; }

        private INode _nextNode;
        private IRequestTransform _requestTransform;
        private IRequestTransform _responseTransform;

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

        Task INode.ProcessRequest(IRequestContext context)
        {
            if (_nextNode == null)
            {
                return Task.Run(() => 
                { 
                    context.Outgoing.StatusCode = 503;
                    context.Outgoing.ReasonPhrase = "Transform " + Name + " has no downstream";
                    context.Outgoing.SendHeaders(context);
                });
            }

            if (!Disabled)
            {
                if (_requestTransform != null)
                {
                    context.Log?.Log(LogType.Logic, LogLevel.Detailed, () => $"Transform node '{Name}' inserting request transform into the incoming pipe");
                    _requestTransform.Transform(context);
                }

                if (_responseTransform != null)
                {
                    context.Log?.Log(LogType.Logic, LogLevel.Detailed, () => $"Transform node '{Name}' inserting response transform into the outgoing pipe");
                    _responseTransform.Transform(context);
                }
            }

            context.Log?.Log(LogType.Step, LogLevel.Standard, () => $"Transform node '{Name}' forwarding to '{_nextNode.Name}'");

            return _nextNode.ProcessRequest(context);
        }
    }
}