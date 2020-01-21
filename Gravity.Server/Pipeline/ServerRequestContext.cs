using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Gravity.Server.Interfaces;
using Gravity.Server.Utility;
using Microsoft.Owin;

namespace Gravity.Server.Pipeline
{
    internal class ServerRequestContext : IRequestContext
    {
        private readonly ILog _log;
        ILog IRequestContext.Log => _log;

        private readonly IIncomingMessage _incoming;
        IIncomingMessage IRequestContext.Incoming => _incoming;

        private readonly IOutgoingMessage _outgoing;
        IOutgoingMessage IRequestContext.Outgoing => _outgoing;

        IDictionary<string, object> IRequestContext.Environment { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// This version is used to forward an incoming request to a server
        /// </summary>
        public ServerRequestContext(
            IRequestContext requestContext,
            IPAddress serverIpAddress,
            ushort serverPort,
            Scheme scheme,
            string domainName)
        {
            _log = requestContext.Log;

            _incoming = new IncomingMessage
            {
                Headers = requestContext.Incoming.Headers,
                OnSendHeaders = requestContext.Incoming.OnSendHeaders,
                Content = requestContext.Incoming.Content,
                ContentLength = requestContext.Incoming.ContentLength,

                Method = requestContext.Incoming.Method,
                Scheme = scheme,
                DomainName = domainName,
                Path = requestContext.Incoming.Path,
                Query = requestContext.Incoming.Query,
                SourceAddress = requestContext.Incoming.SourceAddress,
                SourcePort = requestContext.Incoming.SourcePort,

                DestinationAddress = serverIpAddress,
                DestinationPort = serverPort
            };

            _outgoing = requestContext.Outgoing;
        }

        /// <summary>
        /// This is used for requests to the server that originate internally
        /// </summary>
        public ServerRequestContext(
            ILog log,
            IPAddress address,
            ushort port,
            Scheme scheme,
            string domainName,
            String method,
            PathString path,
            QueryString query)
        {
            _log = log;

            _incoming = new IncomingMessage
            {
                Headers = new DefaultDictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Host", new[]{ domainName + ":" + port } }
                },
                OnSendHeaders = new List<Action<IRequestContext>>(),

                Method = method,
                Scheme = scheme,
                DomainName = domainName,
                Path = path,
                Query = query,

                DestinationAddress = address,
                DestinationPort = port
            };

            _outgoing = new OutgoingMessage
            {
                Headers = new DefaultDictionary<string, string[]>(StringComparer.OrdinalIgnoreCase),
                Content = new MemoryStream()
            };
        }

        void IDisposable.Dispose()
        {
        }

        private class Message : IMessage
        {
            public IDictionary<string, string[]> Headers { get; set; }
            public IList<Action<IRequestContext>> OnSendHeaders { get; set; }

            public int? ContentLength { get; set; }
            public Stream Content { get; set; }
        }

        private class IncomingMessage : Message, IIncomingMessage
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

        private class OutgoingMessage : Message, IOutgoingMessage
        {
            ushort IOutgoingMessage.StatusCode { get; set; }
            string IOutgoingMessage.ReasonPhrase { get; set; }
        }
    }
}