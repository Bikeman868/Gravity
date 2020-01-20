using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.Pipeline
{
    internal class OwinRequestContext: IRequestContext
    {
        public OwinRequestContext(
            IOwinContext owinContext,
            ILogFactory logFactory)
        {
            _log = logFactory.Create(this);
            _incoming = new IncommingMessageWrapper(owinContext);
            _outgoing = new OutgoingMessageWrapper(owinContext);
        }

        public void Dispose()
        {
            _log?.Dispose();
        }

        private readonly ILog _log;
        ILog IRequestContext.Log => _log;

        private IIncomingMessage _incoming;
        IIncomingMessage IRequestContext.Incoming => _incoming;

        private readonly IOutgoingMessage _outgoing;
        IOutgoingMessage IRequestContext.Outgoing => _outgoing;

        private class IncommingMessageWrapper : IIncomingMessage
        {
            private readonly IOwinContext _owinContext;

            public IncommingMessageWrapper(IOwinContext owinContext)
            {
                _owinContext = owinContext;

                _scheme = string.Equals(_owinContext.Request.Scheme, "https", StringComparison.OrdinalIgnoreCase) ? Scheme.Https : Scheme.Http;

                var hostHeader = _owinContext.Request.Host.Value;
                var colonPos = hostHeader.IndexOf(';');
                _domainName = colonPos < 0 ? hostHeader : hostHeader.Substring(0, colonPos);

            }

            string IIncomingMessage.Method 
            { 
                get => _owinContext.Request.Method; 
                set => _owinContext.Request.Method = value; 
            }

            private Scheme _scheme;
            Scheme IIncomingMessage.Scheme
            {
                get => _scheme;
                set
                {
                    _scheme = value;
                    _owinContext.Request.Scheme = value.ToString().ToLower();
                }
            }

            private string _domainName;
            string IIncomingMessage.DomainName
            {
                get => _domainName;
                set
                {
                    _domainName = value;
                    var port = ((IIncomingMessage)this).DestinationPort;
                    if (port == 80)
                        _owinContext.Request.Host = new HostString(value);
                    else
                        _owinContext.Request.Host = new HostString(value + ":" + port);
                }
            }

            PathString IIncomingMessage.Path
            {
                get => _owinContext.Request.Path;
                set => _owinContext.Request.Path = value;
            }

            QueryString IIncomingMessage.Query
            {
                get => _owinContext.Request.QueryString;
                set => _owinContext.Request.QueryString = value;
            }

            IPAddress IIncomingMessage.SourceAddress
            {
                get => IPAddress.Parse(_owinContext.Request.RemoteIpAddress);
                set => _owinContext.Request.RemoteIpAddress = value.ToString();
            }

            ushort IIncomingMessage.SourcePort
            {
                get => _owinContext.Request.RemotePort.HasValue ? (ushort)_owinContext.Request.RemotePort : (ushort)9999;
                set => _owinContext.Request.RemotePort = value;
            }

            IPAddress IIncomingMessage.DestinationAddress
            {
                get => IPAddress.Parse(_owinContext.Request.LocalIpAddress);
                set => _owinContext.Request.LocalIpAddress = value.ToString();
            }

            ushort IIncomingMessage.DestinationPort
            {
                get => _owinContext.Request.LocalPort.HasValue ? (ushort)_owinContext.Request.LocalPort : (ushort)80;
                set => _owinContext.Request.LocalPort = value;
            }

            IDictionary<string, string[]> IMessage.Headers => _owinContext.Request.Headers;

            IList<Action<IRequestContext>> IMessage.OnSendHeaders { get; } = new List<Action<IRequestContext>>();

            int? IMessage.ContentLength
            {
                get
                {
                    var header = _owinContext.Request.Headers["Content-Length"];
                    if (header == null) return null;
                    if (!int.TryParse(header, out var contentLength))
                        throw new Exception($"Content-Length header '{header}' is not an integer");
                    return contentLength;
                }
                set => _owinContext.Request.Headers["Content-Length"] = value.ToString();
            }

            Stream IMessage.Content
            {
                get => _owinContext.Request.Body;
                set => _owinContext.Request.Body = value;
            }
        }

        private class OutgoingMessageWrapper : IOutgoingMessage
        {
            private readonly IOwinContext _owinContext;

            public OutgoingMessageWrapper(IOwinContext owinContext)
            {
                _owinContext = owinContext;

                var contentLengthHeader = _owinContext.Response.Headers["Content-Length"];
                if (contentLengthHeader != null)
                {
                    if (!int.TryParse(contentLengthHeader, out var contentLength))
                        throw new Exception($"Content-Length header '{contentLengthHeader}' is not an integer");
                    _contentLength = contentLength;
                }
            }

            ushort IOutgoingMessage.StatusCode
            {
                get => (ushort)_owinContext.Response.StatusCode;
                set => _owinContext.Response.StatusCode = value;
            }

            string IOutgoingMessage.ReasonPhrase
            {
                get => _owinContext.Response.ReasonPhrase;
                set => _owinContext.Response.ReasonPhrase = value;
            }

            IDictionary<string, string[]> IMessage.Headers => _owinContext.Response.Headers;

            IList<Action<IRequestContext>> IMessage.OnSendHeaders { get; } = new List<Action<IRequestContext>>();

            private int? _contentLength;

            int? IMessage.ContentLength
            {
                get => _contentLength;
                set
                {
                    _contentLength = value;
                    _owinContext.Response.Headers["Content-Length"] = value.ToString();
                }
            }

            Stream IMessage.Content
            {
                get => _owinContext.Response.Body;
                set => _owinContext.Response.Body = value;
            }
        }
    }
}