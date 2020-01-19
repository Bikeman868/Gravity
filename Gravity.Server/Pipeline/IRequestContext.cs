using Gravity.Server.Interfaces;

namespace Gravity.Server.Pipeline
{
    internal interface IRequestContext
    {
        ILog Log { get; }
        IIncomingMessage Incoming { get; set; }
        IOutgoingMessage Outgoing { get; set; }
    }
}