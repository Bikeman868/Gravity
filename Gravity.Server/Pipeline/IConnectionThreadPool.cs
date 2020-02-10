using Gravity.Server.ProcessingNodes.Server;
using System;
using System.Threading.Tasks;

namespace Gravity.Server.Pipeline
{
    public interface IConnectionThreadPool
    {
        Task<bool> ProcessTransaction(Connection connection,
            IRequestContext context,
            TimeSpan responseTimeout,
            TimeSpan readTimeout,
            bool reuseConnections);

    }
}