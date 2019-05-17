using System.Linq;
using System.Threading.Tasks;
using Gravity.Server.DataStructures;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes
{
    internal class LeastConnectionsNode: INode
    {
        public string Name { get; set; }
        public string[] Outputs { get; set; }
        public bool Disabled { get; set; }

        public NodeOutput[] OutputNodes;

        public void Dispose()
        {
        }

        void INode.Bind(INodeGraph nodeGraph)
        {
            OutputNodes = Outputs.Select(name => new NodeOutput
            {
                Name = name,
                Node = nodeGraph.NodeByName(name),
            }).ToArray();
        }

        Task INode.ProcessRequest(IOwinContext context)
        {
            if (Disabled)
            {
                context.Response.StatusCode = 503;
                context.Response.ReasonPhrase = "Balancer " + Name + " is disabled";
                return context.Response.WriteAsync(string.Empty);
            }

            var output = OutputNodes
                .Where(o => !o.Disabled && o.Node != null)
                .OrderBy(o => o.ConnectionCount)
                .FirstOrDefault();

            if (output == null)
            {
                context.Response.StatusCode = 503;
                context.Response.ReasonPhrase = "Balancer " + Name + " has no enabled outputs";
                return context.Response.WriteAsync(string.Empty);
            }

            output.IncrementRequestCount();
            output.IncrementConnectionCount();

            return output.Node.ProcessRequest(context).ContinueWith(t =>
            {
                output.DecrementConnectionCount();
            });
        }
    }
}