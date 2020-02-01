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
            int readTimeoutMs,
            bool reuseConnection)
        {
            var stream = new RequestStream(_bufferPool).Start(connection, context, responseTimeout, readTimeoutMs, reuseConnection);
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
                var sleepCounter = 0;
                var iterationsPerSleep = configuration.IterationsPerSleep;

                threads[i] = new Thread(() =>
                {
                    var version = _threadVersion;
                    while (true)
                    {
                        // If we always Sleep(1) then we can never execute more than 1000 iterations/sec
                        // If we always Sleep(0) then the CPU graph sits at 100%
                        sleepCounter = (sleepCounter + 1) % iterationsPerSleep;
                        Thread.Sleep(sleepCounter == 0 ? 1 : 0);

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
                threads[i].Name = $"Connection pool #{threadId}";
                threads[i].IsBackground = true;
                threads[i].Priority = ThreadPriority.Normal;
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
                    _requestStreams.Delete(streamListElement);

                streamListElement = streamListElement.Next;
            }
        }

        private class Configuration
        {
            /// <summary>
            /// The number of threads that will service the pool of state machines
            /// for all of the active request streams. A good valuw would be 2x the
            /// number of vCPU in the machine. Note that each thread has a 1MB stack
            /// so 1000 threads consumes 1GB of memory just for stacks.
            /// </summary>
            [JsonProperty("threadCount")]
            public int ThreadCount { get; set; }

            /// <summary>
            /// When set to 1, the threads sleep on every iteration of the
            /// state machine meaning that the connection pool can never execute
            /// more than 1000 iterations/sec on each thread. Setting this value 
            /// to 2 doubles this to 2000/sec etc.
            /// </summary>
            [JsonProperty("iterationsPerSleep")]
            public int IterationsPerSleep { get; set; }

            public Configuration()
            {
                ThreadCount = 8;
                IterationsPerSleep = 10;
            }
        }
    }
}