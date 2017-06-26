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
        readonly Logger _pipeline;
        readonly int _bufferCapacity;
        volatile bool _disposed;
        readonly CancellationTokenSource _cancel = new CancellationTokenSource();
        readonly BlockingCollection<LogEvent> _queue;
        readonly Task _worker;

        public BackgroundWorkerSink(Logger pipeline, int bufferCapacity)
        {
            if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));
            if (bufferCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(bufferCapacity));
            _pipeline = pipeline;
            _bufferCapacity = bufferCapacity;
            _queue = new BlockingCollection<LogEvent>(_bufferCapacity);
            _worker = Task.Factory.StartNew(Pump, CancellationToken.None, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public void Emit(LogEvent logEvent)
        {
            // The disposed check is racy, but only ensures we don't prevent flush from
            // completing by pushing more events.
            if (!_disposed && !_queue.TryAdd(logEvent))
                SelfLog.WriteLine("{0} unable to enqueue, capacity {1}", typeof(BackgroundWorkerSink), _bufferCapacity);
        }

        public void Dispose()
        {
            _disposed = true;
            _cancel.Cancel();
            _worker.Wait();            
            _pipeline.Dispose();
            // _cancel not disposed, because it will make _cancel.Cancel() non-idempotent
        }

        void Pump()
        {
            try
            {
                try
                {
                    while (true)
                    {
                        var next = _queue.Take(_cancel.Token);
                        _pipeline.Write(next);
                    }
                }
                catch (OperationCanceledException)
                {
                    LogEvent next;
                    while (_queue.TryTake(out next))
                        _pipeline.Write(next);
                }
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("{0} fatal error in worker thread: {1}", typeof(BackgroundWorkerSink), ex);
            }
        }
    }
}
