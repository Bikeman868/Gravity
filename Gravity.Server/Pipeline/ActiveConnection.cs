using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gravity.Server.ProcessingNodes.Server
{
    /// <summary>
    /// Provides a wrapper around the incoming and outgoing data stream that
    /// allows a small pool of threads to manage a large number of concurrent
    /// TCP connections
    /// </summary>
    internal class ActiveConnection: IDisposable
    {
        private readonly TaskCompletionSource<bool> _taskCompletionSource;
        private readonly Func<bool> _processIncoming;
        private readonly Func<bool> _processOutgoing;
        private readonly AutoResetEvent _event;
        private bool _incomingComplete;
        private bool _outgoingComplete;

        public Task Task => _taskCompletionSource.Task;

        public ActiveConnection(Func<bool> processIncoming, Func<bool> processOutgoing)
        {
            _processIncoming = processIncoming;
            _processOutgoing = processOutgoing;

            _incomingComplete = processIncoming != null;
            _outgoingComplete = processOutgoing != null;

            _event = new AutoResetEvent(true);
            _taskCompletionSource = new TaskCompletionSource<bool>();
        }

        public void Dispose()
        {
            _event.Dispose();
        }

        /// <summary>
        /// Checks for anything to do on an active connection returning immediately
        /// so that the thread can check other active connections.
        /// </summary>
        /// <returns>Returns true if the request is complete</returns>
        public bool Continue()
        {
            // Return immediately if another thread is already servicing this request
            if (!_event.WaitOne(0))
                return false;

            try
            {
                if (_incomingComplete && _outgoingComplete)
                    return true;

                if (!_incomingComplete)
                    _incomingComplete = _processIncoming();

                if (!_outgoingComplete)
                    _outgoingComplete = _processOutgoing();

                if (_incomingComplete && _outgoingComplete)
                    _taskCompletionSource.SetResult(true);
            }
            finally
            {
                // Allow other threads to service this connection
                _event.Set();
            }

            return _incomingComplete && _outgoingComplete;
        }
    }
}