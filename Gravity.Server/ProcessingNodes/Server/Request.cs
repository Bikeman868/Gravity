using System;
using System.Net;

namespace Gravity.Server.ProcessingNodes.Server
{
    internal class Request
    {
        /// <summary>
        /// For example 'https'
        /// </summary>
        public string Protocol;

        /// <summary>
        /// For example 'microsoft.com'
        /// </summary>
        public string HostName;

        /// <summary>
        /// For example 145.65.97.2
        /// </summary>
        public IPAddress IpAddress;

        /// <summary>
        /// For example 443
        /// </summary>
        public int PortNumber;

        /// <summary>
        /// For example 'GET'
        /// </summary>
        public string Method;

        /// <summary>
        /// For example '/user/profile'
        /// </summary>
        public string PathAndQuery;

        /// <summary>
        /// For example 'Host=microsoft.com:443'
        /// </summary>
        public Tuple<string, string>[] Headers;

        /// <summary>
        /// The body of the message
        /// </summary>
        public byte[] Content;
    }
}