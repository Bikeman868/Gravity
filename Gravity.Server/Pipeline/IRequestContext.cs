using Gravity.Server.Interfaces;
using System;

namespace Gravity.Server.Pipeline
{
    /// <summary>
    /// Represents an HTTP request and the corresponding HTTP response
    /// </summary>
    internal interface IRequestContext: IDisposable
    {
        /// <summary>
        /// Logging is specific to the reqest context
        /// </summary>
        ILog Log { get; }

        /// <summary>
        /// The incomming request to send to the back-end server
        /// </summary>
        IIncomingMessage Incoming { get; }

        /// <summary>
        /// The outgoing reply from the back-end server
        /// </summary>
        IOutgoingMessage Outgoing { get; }
    }
}