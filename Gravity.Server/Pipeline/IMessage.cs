using System;
using System.Collections.Generic;
using System.IO;

namespace Gravity.Server.Pipeline
{
    internal interface IMessage
    {
        /// <summary>
        /// For example
        ///    Host: mydomain.com:443
        ///    Content-Length: 6573
        ///    Content-Encoding: gzip
        /// </summary>
        IDictionary<string, string> Headers { get; }

        /// <summary>
        /// Add handlers to this list. They will be called just before
        /// the headers are sent and before starting to transmit the
        /// content
        /// </summary>
        IList<Action<IRequestContext>> OnSendHeaders { get; }

        /// <summary>
        /// The number of bytes in the content stream if known
        /// </summary>
        int? ContentLength { get; set; }

        /// <summary>
        /// A stream of content. Note that this can be never ending
        /// in the case of a music streaming service for example
        /// </summary>
        Stream Content { get; set; }
    }
}