using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;

namespace Gravity.Server.Pipeline
{
    internal class ConnectionException: ApplicationException
    {
        public Connection Connection;

        public ConnectionException(Connection connection, string message, Exception innerException = null):
            base("Connection " + message, innerException)
        {
            Connection = connection;
        }
    }

    internal enum ConnectionState
    {
        New,
        Connected,
        Busy,
        Old,
        Disconnected,
        Pending
    }

    internal class Connection : IDisposable
    {
        private readonly IConnectionThreadPool _connectionThreadPool;
        private readonly TimeSpan _maximumIdleTime = TimeSpan.FromMinutes(5);
        private readonly IBufferPool _bufferPool;
        private readonly IPEndPoint _endpoint;
        private readonly string _domainName;
        private readonly TimeSpan _connectionTimeout;
        private readonly Scheme _scheme;

        private TcpClient _tcpClient;
        private Stream _stream;
        private DateTime _lastUsedUtc;

        public Connection(
            IBufferPool bufferPool,
            IConnectionThreadPool connectionThreadPool,
            IPEndPoint endpoint,
            string domainName,
            Scheme scheme,
            TimeSpan connectionTimeout)
        {
            _bufferPool = bufferPool;
            _connectionThreadPool = connectionThreadPool;

            _endpoint = endpoint;
            _domainName = domainName;
            _scheme = scheme;
            _connectionTimeout = connectionTimeout;

            State = ConnectionState.New;
        }

        public void Dispose()
        {
            _stream?.Close();
            _tcpClient?.Close();
            State = ConnectionState.Disconnected;
        }

        public Task ConnectAsync(ILog log)
        {
            log?.Log(LogType.TcpIp, LogLevel.Standard,
                () =>
                    $"Opening a new Tcp connection to {_scheme.ToString().ToLower()}://{_domainName}:{_endpoint.Port} at {_endpoint.Address}");

            _tcpClient = new TcpClient
            {
                ReceiveTimeout = 0,
                SendTimeout = 0
            };

            return _tcpClient.ConnectAsync(_endpoint.Address, _endpoint.Port)
                .ContinueWith(connectTask =>
                {
                    if (connectTask.IsFaulted)
                    {
                        log?.Log(LogType.Exception, LogLevel.Important,
                            () => $"Failed to connect. {connectTask.Exception?.Message}");
                        throw new ConnectionException(this, "Exception in TcpClient", connectTask.Exception);
                    }

                    if (connectTask.IsCanceled)
                    {
                        log?.Log(LogType.Exception, LogLevel.Important,
                            () => $"Failed to connect within {_connectionTimeout}");
                        throw new ConnectionException(this, "TcpClient connection was cancelled");
                    }

                    State = ConnectionState.Connected;

                    _stream = _tcpClient.GetStream();

                    if (_scheme == Scheme.Https)
                    {
                        log?.Log(LogType.TcpIp, LogLevel.Standard, () => "Wrapping Tcp connection in SSL stream");
                        var sslStream = new SslStream(_stream);

                        _stream = sslStream;

                        log?.Log(LogType.TcpIp, LogLevel.Standard,
                            () => $"Authenticating server's SSL certificate for {_domainName}");
                        sslStream.AuthenticateAsClient(_domainName);
                        log?.Log(LogType.TcpIp, LogLevel.Detailed,
                            () => $"The server's SSL certificate is valid for {_domainName}");
                    }

                    _lastUsedUtc = DateTime.UtcNow;
                });
        }

        public ConnectionState State { get; private set; }

        public Stream Stream => _stream;

        public TimeSpan MaximumIdleTime => _maximumIdleTime;

        public bool IsAvailable
        {
            get
            {
                switch (State)
                {
                    case ConnectionState.New:
                    case ConnectionState.Old:
                        return true;
                    case ConnectionState.Connected:
                        if (DateTime.UtcNow - _lastUsedUtc > _maximumIdleTime)
                            State = ConnectionState.Old;
                        return true;
                    default:
                        return false;
                }
            }
        }

        public void BeginTransaction(ILog log)
        {
            State = ConnectionState.Busy;
        }

        public void EndTransaction(ILog log, bool success)
        {
            _lastUsedUtc = DateTime.UtcNow;

            if (success)
            {
                log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"Successful completion of transaction on {_scheme.ToString().ToLower()}://{_domainName}:{_endpoint.Port} at {_endpoint.Address}. The connection can be reused for another transaction");
                State = ConnectionState.Connected;
            }
            else
            {
                log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"Failed transaction on {_scheme.ToString().ToLower()}://{_domainName}:{_endpoint.Port} at {_endpoint.Address}. The connection might be unstable and will not be reused");
                Dispose();
            }
        }

        public int ReceiveTimeoutMs
        {
            get => _tcpClient.ReceiveTimeout;
            set => _tcpClient.ReceiveTimeout = value;
        }
    }
}