using System;
using System.Threading.Tasks;

namespace Gravity.Server.Pipeline
{
    internal interface IConnectionThreadPool
    {
        Task AddConnection(Func<bool> processIncoming, Func<bool> processOutgoing);
    }
}