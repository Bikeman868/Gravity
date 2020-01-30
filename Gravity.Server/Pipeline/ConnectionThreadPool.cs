using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Gravity.Server.ProcessingNodes.Server;
using Newtonsoft.Json;
using OwinFramework.Utility.Containers;
using Urchin.Client.Interfaces;

namespace Gravity.Server.Pipeline
{
    internal class ConnectionThreadPool: IConnectionThreadPool
    {
        private readonly IDisposable _configRegistration;
        private readonly LinkedList<ActiveConnection> _activeConnections;

        private Thread[] _threads;
        private int _threadVersion;

        public ConnectionThreadPool(
            IConfigurationStore configurationStore)
        {
            _activeConnections = new LinkedList<ActiveConnection>();

            _configRegistration = configurationStore.Register(
                "/gravity/connectionThreadPool", 
                ConfigurationChanged,
                new Configuration());
        }

        Task IConnectionThreadPool.AddConnection(Func<bool> processIncoming, Func<bool> processOutgoing)
        {
            var activeConnection = new ActiveConnection(processIncoming, processOutgoing);
            _activeConnections.Append(activeConnection);
            return activeConnection.Task;
        }

        private void ConfigurationChanged(Configuration configuration)
        {
            if (configuration.ThreadCount < 1)
                configuration.ThreadCount = 1;

            var threads = new Thread[configuration.ThreadCount];

            for (var i = 0; i < configuration.ThreadCount; i++)
            {
                var threadId = i + 1;
                threads[i] = new Thread(() =>
                {
                    var version = _threadVersion;
                    while (true)
                    {
                        try
                        {
                            if (version != _threadVersion)
                            {
                                Trace.WriteLine($"Connection pool thread #{threadId} is old version and will exit");
                                return;
                            }

                            ThreadLoop();
                        }
                        catch (ThreadAbortException)
                        {
                            Trace.WriteLine($"Connection pool thread #{threadId} received an abort request");
                            return;
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Connection pool thread #{threadId} caught exception {ex.Message}");
                        }
                    }
                });
                threads[i].Name = $"Connection pool #{i+1}";
                threads[i].IsBackground = true;
                threads[i].Priority = ThreadPriority.AboveNormal;
            }

            _threadVersion++;

            foreach (var thread in threads)
                thread.Start();

            if (_threads != null)
            {
                foreach (var thread in _threads)
                    thread.Abort();
            }

            _threads = threads;
        }

        private void ThreadLoop()
        {
            var connection = _activeConnections.FirstElementOrDefault();
            if (connection == null)
            {
                Thread.Sleep(50);
                return;
            }

            while (connection != null)
            {
                if (connection.Data.Continue())
                {
                    connection.Data.Dispose();
                    _activeConnections.Delete(connection);
                }

                connection = connection.Next;
            }
        }

        private class Configuration
        {
            [JsonProperty("threadCount")]
            public int ThreadCount { get; set; }

            public Configuration()
            {
                ThreadCount = 8;
            }
        }
    }
}