using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Serilog.Sinks.Async
{
    sealed class BackgroundWorkerSink : ILogEventSink, IDisposable
    {
        readonly ILogEventSink _pipeline;
        readonly bool _blockWhenFull;
        readonly BlockingCollection<LogEvent> _queue;
        readonly Task _worker;
#if! NETSTANDARD_NO_TIMER
        readonly Timer _monitorCallbackInvocationTimer;
#endif
        public BackgroundWorkerSink(
            ILogEventSink pipeline, int bufferCapacity,
            bool blockWhenFull,
            int monitorIntervalSeconds = 0, Action<BlockingCollection<LogEvent>> monitor = null)
        {
            if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));
            if (bufferCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(bufferCapacity));
            if (monitorIntervalSeconds < 0) throw new ArgumentOutOfRangeException(nameof(monitorIntervalSeconds));
            _pipeline = pipeline;
            _blockWhenFull = blockWhenFull;
            _queue = new BlockingCollection<LogEvent>(bufferCapacity);
            _worker = Task.Factory.StartNew(Pump, CancellationToken.None, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            if (monitor != null)
            {
                if (monitorIntervalSeconds < 1) throw new ArgumentOutOfRangeException(nameof(monitorIntervalSeconds), "must be >=1");
#if! NETSTANDARD_NO_TIMER
                var interval = TimeSpan.FromSeconds(monitorIntervalSeconds);
                _monitorCallbackInvocationTimer = new Timer(queue => monitor((BlockingCollection<LogEvent>)queue), _queue, interval, interval);
#else
                throw new PlatformNotSupportedException($"Please use a platform supporting .netstandard1.2 or later to avail of the ${nameof(monitor)} facility.");
#endif
            }
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
                        SelfLog.WriteLine("{0} unable to enqueue, capacity {1}", typeof(BackgroundWorkerSink), _queue.BoundedCapacity);
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

#if! NETSTANDARD_NO_TIMER
            // Only stop monitoring when we've actually completed flushing
            _monitorCallbackInvocationTimer?.Dispose();
#endif

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
