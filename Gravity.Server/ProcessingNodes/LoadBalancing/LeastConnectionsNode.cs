using System.Linq;
using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.LoadBalancing
{
    internal class LeastConnectionsNode: LoadBalancerNode
    {
        public override Task ProcessRequest(IOwinContext context, ILog log)
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

            output.IncrementConnectionCount();
            var startTime = output.TrafficAnalytics.BeginRequest();

            return output.Node.ProcessRequest(context, log).ContinueWith(t =>
            {
                output.TrafficAnalytics.EndRequest(startTime);
                output.DecrementConnectionCount();
            });
        }
    }
}