using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Serilog.Sinks.Async
{
    sealed class BackgroundWorkerSink : ILogEventSink, IAsyncLogEventSinkInspector, IDisposable
    {
        readonly ILogEventSink _wrappedSink;
        readonly bool _blockWhenFull;
        readonly BlockingCollection<LogEvent> _queue;
        readonly Task _worker;
        readonly IAsyncLogEventSinkMonitor? _monitor;

        long _droppedMessages;

        public BackgroundWorkerSink(ILogEventSink wrappedSink, int bufferCapacity, bool blockWhenFull, IAsyncLogEventSinkMonitor? monitor = null)
        {
            if (bufferCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(bufferCapacity));
            _wrappedSink = wrappedSink ?? throw new ArgumentNullException(nameof(wrappedSink));
            _blockWhenFull = blockWhenFull;
            _queue = new BlockingCollection<LogEvent>(bufferCapacity);
            _worker = Task.Factory.StartNew(Pump, CancellationToken.None, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            _monitor = monitor;
            monitor?.StartMonitoring(this);
        }

        public void Emit(LogEvent logEvent)
        {
            if (_queue.IsAddingCompleted)
                return;

            try
            {
                if (_blockWhenFull)
                {
                    _queue.Add(logEvent);
                }
                else
                {
                    if (!_queue.TryAdd(logEvent))
                    {
                        Interlocked.Increment(ref _droppedMessages);
                        SelfLog.WriteLine("{0} unable to enqueue, capacity {1}", typeof(BackgroundWorkerSink), _queue.BoundedCapacity);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Thrown in the event of a race condition when we try to add another event after
                // CompleteAdding has been called
            }
        }

        public void Dispose()
        {
            // Prevent any more events from being added
            _queue.CompleteAdding();

            // Allow queued events to be flushed
            _worker.Wait();

            (_wrappedSink as IDisposable)?.Dispose();

            _monitor?.StopMonitoring(this);
        }

        void Pump()
        {
            try
            {
                foreach (var next in _queue.GetConsumingEnumerable())
                {
                    try
                    {
                        _wrappedSink.Emit(next);
                    }
                    catch (Exception ex)
                    {
                        SelfLog.WriteLine("{0} failed to emit event to wrapped sink: {1}", typeof(BackgroundWorkerSink), ex);
                    }
                }
            }
            catch (Exception fatal)
            {
                SelfLog.WriteLine("{0} fatal error in worker thread: {1}", typeof(BackgroundWorkerSink), fatal);
            }
        }

        int IAsyncLogEventSinkInspector.BufferSize => _queue.BoundedCapacity;

        int IAsyncLogEventSinkInspector.Count => _queue.Count;

        long IAsyncLogEventSinkInspector.DroppedMessagesCount => _droppedMessages;
    }
}