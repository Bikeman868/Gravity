using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Gravity.Server.ProcessingNodes.Server;
using Newtonsoft.Json;
using OwinFramework.Utility.Containers;
using Urchin.Client.Interfaces;

namespace Gravity.Server.Pipeline
{
    internal class ConnectionThreadPool: IConnectionThreadPool
    {
        private readonly IDisposable _configRegistration;
        private readonly LinkedList<RequestStream> _requestStreams;
        private readonly IBufferPool _bufferPool;

        private Thread[] _threads;
        private int _threadVersion;

        public ConnectionThreadPool(
            IConfigurationStore configurationStore,
            IBufferPool bufferPool)
        {
            _bufferPool = bufferPool;
            _requestStreams = new LinkedList<RequestStream>();

            _configRegistration = configurationStore.Register(
                "/gravity/connectionThreadPool", 
                ConfigurationChanged,
                new Configuration());
        }

        Task<bool> IConnectionThreadPool.ProcessTransaction(
            Connection connection,
            IRequestContext context, 
            TimeSpan responseTimeout, 
            int readTimeoutMs)
        {
            var stream = new RequestStream(_bufferPool).Start(connection, context, responseTimeout, readTimeoutMs);
            _requestStreams.Append(stream);
            return stream.Task;
        }

        private void ConfigurationChanged(Configuration configuration)
        {
            if (configuration.ThreadCount < 1)
                configuration.ThreadCount = 1;

            var threads = new Thread[configuration.ThreadCount];

            for (var i = 0; i < configuration.ThreadCount; i++)
            {
                var threadId = i + 1;
                var counter = 0;

                threads[i] = new Thread(() =>
                {
                    var version = _threadVersion;
                    while (true)
                    {
                        // If we always Sleep(1) then we can never execute more than 1000 iterations/sec
                        // If we always Sleep(0) then the CPU graph sits at 100%
                        counter = (counter + 1) % 50;
                        Thread.Sleep(counter == 0 ? 1 : 0);

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
            var streamListElement = _requestStreams.FirstElementOrDefault();
            if (streamListElement == null)
            {
                Thread.Sleep(50);
                return;
            }

            while (streamListElement != null)
            {
                var stream = streamListElement.Data;
                if (!stream.NextStep())
                {
                    _requestStreams.Delete(streamListElement);
                    stream.Dispose();
                }

                streamListElement = streamListElement.Next;
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