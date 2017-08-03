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
        volatile bool _disposed;
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
            // The disposed check is racy, but only ensures we don't prevent flush from
            // completing by pushing more events.
            if (_disposed)
                return;

            if (!this._blockWhenFull)
            {
                if (!_queue.TryAdd(logEvent))
                    SelfLog.WriteLine("{0} unable to enqueue, capacity {1}", typeof(BackgroundWorkerSink), _bufferCapacity);
            }
            else
            {
                this._queue.Add(logEvent);
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _queue.CompleteAdding();
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
