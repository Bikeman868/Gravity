using System;

namespace Gravity.Server.ProcessingNodes.Server
{
    internal class Response
    {
        public int StatusCode;
        public string ReasonPhrase;
        public Tuple<string, string>[] Headers;
        public byte[] Content;
    }
}