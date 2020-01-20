using System;
using System.Collections.Generic;
using System.IO;

namespace Gravity.Server.Pipeline
{
    internal class OutgoingMessage : Message, IOutgoingMessage
    {
        public ushort StatusCode { get; set; }
        public string ReasonPhrase { get; set; }
    }
}