using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.SpecialPurpose
{
    internal class CorsNode: INode
    {
        public string Name { get; set; }
        public bool Disabled { get; set; }
        public string OutputNode { get; set; }
        public string WebsiteOrigin { get; set; }
        public string AllowedOrigins { get; set; }
        public string AllowedHeaders { get; set; }
        public string AllowedMethods { get; set; }
        public bool AllowCredentials { get; set; }
        public string ExposedHeaders { get; set; }
        public bool Available { get; private set; }

        private INode _nextNode;

        public void Dispose()
        {
        }

        void INode.Bind(INodeGraph nodeGraph)
        {
            _nextNode = nodeGraph.NodeByName(OutputNode);
        }

        void INode.UpdateAvailability()
        {
            if (Disabled || _nextNode == null)
            {
                Available = false;
                return;
            }

            Available = _nextNode.Available;
        }

        Task INode.ProcessRequest(IOwinContext context)
        {
            if (_nextNode == null)
            {
                context.Response.StatusCode = 503;
                context.Response.ReasonPhrase = "CORS node " + Name + " has no downstream";
                return context.Response.WriteAsync(string.Empty);
            }

            if (!Disabled)
            {
                // TODO: Preform CORS checks here
            }

            return _nextNode.ProcessRequest(context);
        }
    }
}