using System;
using System.Collections.Generic;
using System.Net;

namespace Gravity.Server.ProcessingNodes.Server
{
    internal class ConnectionPool: IDisposable
    {
        private readonly IPEndPoint _endpoint;
        private readonly string _hostName;
        private readonly string _protocol;
        private readonly TimeSpan _connectionTimeout;
        private readonly TimeSpan _responseTimeout;
        private readonly Queue<Connection> _pool;

        public ConnectionPool(
            IPEndPoint endpoint,
            string hostName,
            string protocol,
            TimeSpan connectionTimeout, 
            TimeSpan responseTimeout)
        {
            _endpoint = endpoint;
            _hostName = hostName;
            _protocol = protocol;
            _connectionTimeout = connectionTimeout;
            _responseTimeout = responseTimeout;
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

        public Connection GetConnection()
        {
            while (true)
            {
                lock (_pool)
                {
                    if (_pool.Count > 0)
                    {
                        var connection = _pool.Dequeue();
                        if (connection.IsConnected)
                            return connection;

                        connection.Dispose();
                    }
                    else break;
                }
            }
            return new Connection(_endpoint, _hostName, _protocol, _connectionTimeout, _responseTimeout);
        }

        public void ReuseConnection(Connection connection)
        {
            if (connection.IsConnected)
            {
                lock (_pool)
                {
                    if (_pool.Count < 500)
                    {
                        _pool.Enqueue(connection);
                        return;
                    }
                }
            }
            connection.Dispose();
        }
    }
}