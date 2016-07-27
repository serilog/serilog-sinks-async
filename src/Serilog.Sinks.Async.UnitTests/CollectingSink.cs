using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.Async.UnitTests
{
    public class CollectingSink : ILogEventSink
    {
        public ConcurrentBag<LogEvent> Events { get; } = new ConcurrentBag<LogEvent>();

        public void Emit(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }
    }
}