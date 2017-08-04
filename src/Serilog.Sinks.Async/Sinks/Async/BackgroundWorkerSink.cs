using System;
using System.Collections.Concurrent;
using System.Threading;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using System.Threading.Tasks;

namespace Serilog.Sinks.Async
{
    sealed class BackgroundWorkerSink : ILogEventSink, IDisposable
    {
        readonly ILogEventSink _pipeline;
        readonly int _bufferCapacity;
        readonly bool _blockWhenFull;
        readonly BlockingCollection<LogEvent> _queue;
        readonly Task _worker;

        public BackgroundWorkerSink(ILogEventSink pipeline, int bufferCapacity, bool blockWhenFull)
        {
            if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));
            if (bufferCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(bufferCapacity));
            _pipeline = pipeline;
            _bufferCapacity = bufferCapacity;
            _blockWhenFull = blockWhenFull;
            _queue = new BlockingCollection<LogEvent>(_bufferCapacity);
            _worker = Task.Factory.StartNew(Pump, CancellationToken.None, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public void Emit(LogEvent logEvent)
        {
            if (this._queue.IsAddingCompleted)
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
                        SelfLog.WriteLine("{0} unable to enqueue, capacity {1}", typeof(BackgroundWorkerSink), _bufferCapacity);
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
    }
}
