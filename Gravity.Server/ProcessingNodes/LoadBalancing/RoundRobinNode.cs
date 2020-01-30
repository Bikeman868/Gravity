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

        public override Task ProcessRequestAsync(IRequestContext context)
        {
            if (Disabled)
            {
                context.Log?.Log(LogType.Logic, LogLevel.Important, () => $"Round-robbin load balancer '{Name}' is disabled by configuration");

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
                context.Log?.Log(LogType.Logic, LogLevel.Important, () => $"Round-robbin load balancer '{Name}' has no outputs defined");

                return Task.Run(() =>
                {
                    context.Outgoing.StatusCode = 503;
                    context.Outgoing.ReasonPhrase = "Balancer " + Name + " has no outputs";
                    context.Outgoing.SendHeaders(context);
                });
            }

            var enabledOutputs = outputs.Where(o => !o.Offline && o.Node != null).ToList();

            if (enabledOutputs.Count == 0)
            {
                context.Log?.Log(LogType.Logic, LogLevel.Important, () => $"Round-robbin load balancer '{Name}' has no enabled outputs");

                return Task.Run(() =>
                {
                    context.Outgoing.StatusCode = 503;
                    context.Outgoing.ReasonPhrase = "Balancer " + Name + " outputs are all disabled";
                    context.Outgoing.SendHeaders(context);
                });
            }

            var index = Interlocked.Increment(ref _next) % enabledOutputs.Count;
            var output = enabledOutputs[index];

            context.Log?.Log(LogType.Step, LogLevel.Standard, () => $"Round-robbin load balancer '{Name}' routing request to '{output.Name}'");

            var trafficAnalyticInfo = output.TrafficAnalytics.BeginRequest();
            var task = output.Node.ProcessRequestAsync(context);

            if (task == null)
            {
                output.TrafficAnalytics.EndRequest(trafficAnalyticInfo);
                return null;
            }

            return task.ContinueWith(t =>
            {
                output.TrafficAnalytics.EndRequest(trafficAnalyticInfo);
            });
        }
    }
}