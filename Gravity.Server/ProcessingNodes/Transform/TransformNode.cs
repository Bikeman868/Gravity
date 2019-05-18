using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Transform
{
    internal class TransformNode: INode
    {
        public string Name { get; set; }
        public bool Disabled { get; set; }
        public string OutputNode { get; set; }
        public string RequestScript { get; set; }
        public string ResponseScript { get; set; }

        private INode _nextNode;
        private IRequestTransform _requestTransform;
        private IResponseTransform _responseTransform;

        public void Dispose()
        {
        }

        void INode.Bind(INodeGraph nodeGraph)
        {
            _nextNode = nodeGraph.NodeByName(OutputNode);
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