using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.LoadBalancing
{
    internal class RoundRobinNode: LoadBalancerNode
    {
        private int _next;

        public override Task ProcessRequest(IOwinContext context)
        {
            if (Disabled)
            {
                context.Response.StatusCode = 503;
                context.Response.ReasonPhrase = "Balancer " + Name + " is disabled";
                return context.Response.WriteAsync(string.Empty);
            }

            var outputs = OutputNodes;

            if (outputs == null || outputs.Length == 0)
            {
                context.Response.StatusCode = 503;
                context.Response.ReasonPhrase = "Balancer " + Name + " has no outputs";
                return context.Response.WriteAsync(string.Empty);
            }

            var enabledOutputs = outputs.Where(o => !o.Disabled && o.Node != null).ToList();

            if (enabledOutputs.Count == 0)
            {
                context.Response.StatusCode = 503;
                context.Response.ReasonPhrase = "All balancer " + Name + " outputs are disabled";
                return context.Response.WriteAsync(string.Empty);
            }

            var index = Interlocked.Increment(ref _next) % enabledOutputs.Count;
            var output = enabledOutputs[index];

            var startTime = output.TrafficAnalytics.BeginRequest();
            return output.Node.ProcessRequest(context).ContinueWith(t =>
            {
                output.TrafficAnalytics.EndRequest(startTime);
            });
        }
    }
}