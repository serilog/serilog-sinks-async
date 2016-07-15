using System.Collections.Concurrent;
using System.Collections.Generic;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.Async.IntTests
{
    internal class MemorySink : ILogEventSink
    {
        private ConcurrentQueue<LogEvent> _events = new ConcurrentQueue<LogEvent>();

        public IEnumerable<LogEvent> LogEvents
        {
            get { return _events.ToArray(); }
        }

        public void Emit(LogEvent logEvent)
        {
            _events.Enqueue(logEvent);
        }

        public void Clear()
        {
            _events = new ConcurrentQueue<LogEvent>();
        }
    }
}