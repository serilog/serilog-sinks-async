using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Serilog.Sinks.Async
{
    sealed class BackgroundWorkerSink : ILogEventSink, IQueueState, IDisposable
    {
        readonly ILogEventSink _pipeline;
        readonly bool _blockWhenFull;
        readonly BlockingCollection<LogEvent> _queue;
        readonly Task _worker;

        long _droppedMessages;

        public BackgroundWorkerSink(ILogEventSink pipeline, int bufferCapacity, bool blockWhenFull)
        {
            if (bufferCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(bufferCapacity));
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _blockWhenFull = blockWhenFull;
            _queue = new BlockingCollection<LogEvent>(bufferCapacity);
            _worker = Task.Factory.StartNew(Pump, CancellationToken.None, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
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

            (_pipeline as IDisposable)?.Dispose();
        }

        void Pump()
        {
            try
            {
                foreach (var next in _queue.GetConsumingEnumerable())
                {
                    _pipeline.Emit(next);
                }
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("{0} fatal error in worker thread: {1}", typeof(BackgroundWorkerSink), ex);
            }
        }

        int IQueueState.Count => _queue.Count;

        int IQueueState.BufferSize => _queue.BoundedCapacity;

        long IQueueState.DroppedMessagesCount => _droppedMessages;
    }
}
