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
                context.Log?.Log(LogType.Logic, LogLevel.Important, () => $"Least connected load balancer '{Name}' is disabled");

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
                context.Log?.Log(LogType.Logic, LogLevel.Important, () => $"Least connected load balancer '{Name}' has no enabled outputs");

                return Task.Run(() =>
                {
                    context.Outgoing.StatusCode = 503;
                    context.Outgoing.ReasonPhrase = "Balancer " + Name + " has no enabled outputs";
                    context.Outgoing.SendHeaders(context);
                });
            }

            context.Log?.Log(LogType.Step, LogLevel.Standard, () => $"Least connected load balancer '{Name}' routing request to '{output.Name}'");

            output.IncrementConnectionCount();
            var trafficAnalyticInfo = output.TrafficAnalytics.BeginRequest();

            var task = output.Node.ProcessRequest(context);

            if (task == null)
            {
                output.TrafficAnalytics.EndRequest(trafficAnalyticInfo);
                return null;
            }

            return task.ContinueWith(t =>
                {
                    output.TrafficAnalytics.EndRequest(trafficAnalyticInfo);
                    output.DecrementConnectionCount();
                });
        }
    }
}