using Serilog.Events;
using Serilog.Core;
using System.Collections.Concurrent;
using System;

namespace Serilog.Sinks.Async.Tests.Support
{
    public class MemorySink : ILogEventSink
    {
        public ConcurrentBag<LogEvent> Events { get; } = new ConcurrentBag<LogEvent>();
        public bool ThrowAfterCollecting { get; set; }

        public void Emit(LogEvent logEvent)
        {
            Events.Add(logEvent);

            if (ThrowAfterCollecting)
                throw new Exception($"Exception requested through {nameof(ThrowAfterCollecting)}.");
        }
    }
}