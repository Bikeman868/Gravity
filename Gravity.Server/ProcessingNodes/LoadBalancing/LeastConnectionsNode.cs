using System.Linq;
using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.LoadBalancing
{
    internal class LeastConnectionsNode: LoadBalancerNode
    {
        public override Task ProcessRequest(IRequestContext context)
        {
            if (Disabled)
            {
                return Task.Run(() => 
                {
                    context.Outgoing.StatusCode = 503;
                    context.Outgoing.ReasonPhrase = "Balancer " + Name + " is disabled";
                    context.Outgoing.SendHeaders(context);
                });
            }

            var output = OutputNodes
                .Where(o => !o.Disabled && o.Node != null)
                .OrderBy(o => o.ConnectionCount)
                .FirstOrDefault();

            if (output == null)
            {
                return Task.Run(() =>
                {
                    context.Outgoing.StatusCode = 503;
                    context.Outgoing.ReasonPhrase = "Balancer " + Name + " has no enabled outputs";
                    context.Outgoing.SendHeaders(context);
                });
            }

            output.IncrementConnectionCount();
            var startTime = output.TrafficAnalytics.BeginRequest();

            return output.Node.ProcessRequest(context)
                .ContinueWith(t =>
                {
                    output.TrafficAnalytics.EndRequest(startTime);
                    output.DecrementConnectionCount();
                });
        }
    }
}