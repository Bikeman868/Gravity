using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Gravity.Server.Pipeline
{
    internal interface IOutgoingMessage: IMessage
    {
        /// <summary>
        /// For example 404 for not found
        /// </summary>
        ushort StatusCode { get; set; }

        // For example "Not found"
        string ReasonPhrase { get; set; }
    }
}