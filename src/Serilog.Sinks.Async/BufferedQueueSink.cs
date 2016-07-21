using System;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.Async
{
    public class BufferedQueueSink : ILogEventSink
    {
        private readonly int _bufferSize;
        private readonly ILogEventSink _sink;
        private BufferedQueue<LogEvent> _queue;

        public BufferedQueueSink(ILogEventSink sink)
            : this(sink, 0)
        {
        }

        public BufferedQueueSink(ILogEventSink sink, int bufferSize)
        {
            if (sink == null)
            {
                throw new ArgumentNullException("sink");
            }

            _sink = sink;
            _bufferSize = bufferSize;
        }

        public bool ConsumerStarted { get; private set; }

        public async void Emit(LogEvent logEvent)
        {
            EnsureConsumerStarted();

            await _queue.ProduceAsync(logEvent);
        }

        private void EnsureConsumerStarted()
        {
            if (ConsumerStarted)
            {
                return;
            }
            ConsumerStarted = true;

            _queue = new BufferedQueue<LogEvent>(_bufferSize, logEvent =>
            {
                try
                {
                    _sink.Emit(logEvent);
                }
                catch (Exception)
                {
                    //Log and Ignore exception and continue
                }
            });
        }
    }
}