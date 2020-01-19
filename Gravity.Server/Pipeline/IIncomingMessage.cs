﻿using Microsoft.Owin;
using System.Net;

namespace Gravity.Server.Pipeline
{
    internal interface IIncomingMessage : IMessage
    {
        /// <summary>
        /// For example "GET"
        /// </summary>
        string Method { get; set; }

        /// <summary>
        /// For example http or https
        /// </summary>
        Protocol Protocol { get; set; }

        /// <summary>
        /// For example "mydomain.com"
        /// </summary>
        string DomainName { get; set; }

        /// <summary>
        /// For example "/content/page1"
        /// </summary>
        PathString Path { get; set; }

        /// <summary>
        /// For example "?order=ascending"
        /// </summary>
        QueryString Query { get; set; }

        /// <summary>
        /// For example "143.54.67.3"
        /// </summary>
        IPAddress SourceAddress { get; set; }

        /// <summary>
        /// For example 5387
        /// </summary>
        ushort SourcePort { get; set; }

        /// <summary>
        /// For example "143.54.67.3"
        /// </summary>
        IPAddress DestinationAddress { get; set; }

        /// <summary>
        /// For example 443
        /// </summary>
        ushort DestinationPort { get; set; }
    }
}