using System;
using System.Collections.Generic;
using System.IO;

namespace Gravity.Server.Pipeline
{
    internal class Message : IMessage
    {
        public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();
        public IList<Action<IRequestContext>> OnSendHeaders { get; } = new List<Action<IRequestContext>>();

        public int? ContentLength { get; set; }
        public Stream Content { get; set; }
    }
}