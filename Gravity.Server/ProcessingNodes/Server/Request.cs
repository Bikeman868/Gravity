using System;
using System.Net;

namespace Gravity.Server.ProcessingNodes.Server
{
    internal class Request
    {
        public IPAddress IpAddress;
        public int PortNumber;
        public string Method;
        public string PathAndQuery;
        public Tuple<string, string>[] Headers;
        public byte[] Content;
    }
}