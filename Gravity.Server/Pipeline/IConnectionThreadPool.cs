using Gravity.Server.ProcessingNodes.Server;
using System;
using System.Threading.Tasks;

namespace Gravity.Server.Pipeline
{
    internal interface IConnectionThreadPool
    {
        Task<bool> ProcessTransaction(Connection connection,
            IRequestContext context,
            TimeSpan responseTimeout,
            int readTimeoutMs,
            bool reuseConnections);

    }
}