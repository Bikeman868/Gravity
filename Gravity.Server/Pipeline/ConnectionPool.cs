using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;

namespace Gravity.Server.ProcessingNodes.Server
{
    internal class ConnectionPool: IDisposable
    {
        private readonly IPEndPoint _endpoint;
        private readonly string _domainName;
        private readonly ushort _portNumber;
        private readonly Scheme _scheme;
        private readonly TimeSpan _connectionTimeout;
        private readonly Queue<Connection> _pool;
        private readonly IBufferPool _bufferPool;

        public ConnectionPool(
            IBufferPool bufferPool,
            IPEndPoint endpoint,
            string domainName,
            Scheme scheme,
            ushort portNumber,
            TimeSpan connectionTimeout)
        {
            _endpoint = endpoint;
            _domainName = domainName;
            _scheme = scheme;
            _portNumber = portNumber;
            _connectionTimeout = connectionTimeout;
            _bufferPool = bufferPool;
            _pool = new Queue<Connection>();
        }

        public void Dispose()
        {
            lock (_pool)
            {
                while (_pool.Count > 0)
                    _pool.Dequeue().Dispose();
            }
        }

        public Task<Connection> GetConnection(ILog log, TimeSpan responseTimeout, int readTimeoutMs)
        {
            while (true)
            {
                lock (_pool)
                {
                    if (_pool.Count > 0)
                    {
                        log?.Log(LogType.Pooling, LogLevel.Detailed, () => $"Connection pool contains {_pool.Count} connections");

                        var connection = _pool.Dequeue();
                        if (connection.IsConnected)
                        {
                            if (!connection.IsStale)
                            {
                                log?.Log(LogType.Pooling, LogLevel.Detailed, () => "Reusing the connection dequeued from the pool");
                                return Task.FromResult(connection);
                            }

                            log?.Log(LogType.Pooling, LogLevel.Important, () => "The connection dequeued from the pool has been idle too long and will be disposed");
                            connection.Dispose();
                        }
                        else
                        {
                            log?.Log(LogType.Pooling, LogLevel.Important, () => "The connection dequeued from the pool was not connected and will be disposed");
                            connection.Dispose();
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            log?.Log(LogType.Pooling, LogLevel.Detailed, () => "The connection pool is empty, creating a new connection");
            var newConnection = new Connection(_bufferPool, _endpoint, _domainName, _scheme, _connectionTimeout);
            return newConnection.Connect(log)
                .ContinueWith(connectTask => 
                {
                    if (connectTask.IsFaulted)
                    {
                        log?.Log(LogType.Pooling, LogLevel.Important, () => $"Connection to {_endpoint} failed with {connectTask.Exception.Message}");
                        throw new Exception("Failed to connect to " + _endpoint, connectTask.Exception);
                    }

                    if (connectTask.IsCanceled)
                    {
                        log?.Log(LogType.Pooling, LogLevel.Important, () => $"Timeot connecting to {_endpoint}");
                        throw new Exception("Timeout connecting to " + _endpoint);
                    }

                    return newConnection;
                });
        }

        public void ReuseConnection(ILog log, Connection connection)
        {
            if (connection.IsConnected)
            {
                log?.Log(LogType.Pooling, LogLevel.Detailed, () => "The connection is still connected and can be reused");

                lock (_pool)
                {
                    if (_pool.Count < 500)
                    {
                        _pool.Enqueue(connection);
                        return;
                    }
                }
                log?.Log(LogType.Pooling, LogLevel.Detailed, () => "The connection pool is full");
            }
            else
            {
                log?.Log(LogType.Pooling, LogLevel.Detailed, () => "This connection is no longer connected and will not be reused");
            }

            log?.Log(LogType.Pooling, LogLevel.Detailed, () => "Disposing of the connection");
            connection.Dispose();
        }
    }
}