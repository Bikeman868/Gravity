using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.SpecialPurpose
{
    internal class ResponseNode: INode
    {
        public string Name { get; set; }
        public bool Disabled { get; set; }
        public int StatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public string Content { get; set; }
        public string ContentFile { get; set; }
        public string[] HeaderNames { get; set; }
        public string[] HeaderValues { get; set; }

        public void Dispose()
        {
        }

        void INode.Bind(INodeGraph nodeGraph)
        {
            if (!string.IsNullOrWhiteSpace(ContentFile))
            {
                // TODO: Load the file
            }
        }

        Task INode.ProcessRequest(IOwinContext context)
        {
            context.Response.StatusCode = StatusCode;
            context.Response.ReasonPhrase = ReasonPhrase;

            if (HeaderNames != null)
            {
                for (var i = 0; i < HeaderNames.Length; i++) 
                    context.Response.Headers[HeaderNames[i]] = HeaderValues[i];
            }

            return context.Response.WriteAsync(Content);
        }
    }
}