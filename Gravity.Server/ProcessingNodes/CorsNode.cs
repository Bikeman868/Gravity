using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes
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

        public void Dispose()
        {
        }

        void INode.Bind(INodeGraph nodeGraph)
        {
        }

        Task INode.ProcessRequest(IOwinContext context)
        {
            return null;
        }
    }
}