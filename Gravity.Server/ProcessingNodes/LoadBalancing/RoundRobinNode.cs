using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.LoadBalancing
{
    internal class RoundRobinNode: LoadBalancerNode
    {
        private int _next;

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

            var outputs = OutputNodes;

            if (outputs == null || outputs.Length == 0)
            {
                return Task.Run(() =>
                {
                    context.Outgoing.StatusCode = 503;
                    context.Outgoing.ReasonPhrase = "Balancer " + Name + " has no outputs";
                    context.Outgoing.SendHeaders(context);
                });
            }

            var enabledOutputs = outputs.Where(o => !o.Disabled && o.Node != null).ToList();

            if (enabledOutputs.Count == 0)
            {
                return Task.Run(() =>
                {
                    context.Outgoing.StatusCode = 503;
                    context.Outgoing.ReasonPhrase = "Balancer " + Name + " outputs are all disabled";
                    context.Outgoing.SendHeaders(context);
                });
            }

            var index = Interlocked.Increment(ref _next) % enabledOutputs.Count;
            var output = enabledOutputs[index];

            var startTime = output.TrafficAnalytics.BeginRequest();
            return output.Node.ProcessRequest(context)
                .ContinueWith(nodeTask =>
                {
                    output.TrafficAnalytics.EndRequest(startTime);
                });
        }
    }
}