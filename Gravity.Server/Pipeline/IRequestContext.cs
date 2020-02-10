using Gravity.Server.Interfaces;
using System;
using System.Collections.Generic;

namespace Gravity.Server.Pipeline
{
    /// <summary>
    /// Represents an HTTP request and the corresponding HTTP response
    /// </summary>
    public interface IRequestContext: IDisposable
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

        /// <summary>
        /// Provides a place to store arbitrary state associated with the request
        /// </summary>
        IDictionary<string, object> Environment { get; }
    }
}