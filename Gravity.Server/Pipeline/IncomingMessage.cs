using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.Owin;

namespace Gravity.Server.Pipeline
{
    internal class IncomingMessage: Message, IIncomingMessage
    {
        public string Method { get; set; }
        public Scheme Scheme { get; set; }
        public string DomainName { get; set; }
        public PathString Path { get; set; }
        public QueryString Query { get; set; }

        public IPAddress SourceAddress { get; set; }
        public ushort SourcePort { get; set; }

        public IPAddress DestinationAddress { get; set; }
        public ushort DestinationPort { get; set; }

    }
}