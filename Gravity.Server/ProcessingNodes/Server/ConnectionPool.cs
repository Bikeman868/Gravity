using System;
using System.Collections.Generic;
using System.Net;
using Gravity.Server.Interfaces;

namespace Gravity.Server.ProcessingNodes.Server
{
    internal class ConnectionPool: IDisposable
    {
        private readonly IPEndPoint _endpoint;
        private readonly string _hostName;
        private readonly string _protocol;
        private readonly TimeSpan _connectionTimeout;
        private readonly Queue<Connection> _pool;

        public ConnectionPool(
            IPEndPoint endpoint,
            string hostName,
            string protocol,
            TimeSpan connectionTimeout)
        {
            _endpoint = endpoint;
            _hostName = hostName;
            _protocol = protocol;
            _connectionTimeout = connectionTimeout;
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

        public Connection GetConnection(ILog log, TimeSpan responseTimeout, TimeSpan readTimeout)
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
                                return connection.Initialize(responseTimeout, readTimeout);
                            }

                            log?.Log(LogType.Pooling, LogLevel.Superficial, () => "The connection dequeued from the pool has been idle too long and will be disposed");
                            connection.Dispose();
                        }
                        else
                        {
                            log?.Log(LogType.Pooling, LogLevel.Superficial, () => "The connection dequeued from the pool was not connected and will be disposed");
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
            return new Connection(log, _endpoint, _hostName, _protocol, _connectionTimeout).Initialize(responseTimeout, readTimeout);
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