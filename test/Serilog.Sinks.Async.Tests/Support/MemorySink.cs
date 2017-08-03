using Serilog.Events;
using Serilog.Core;
using System.Collections.Concurrent;
using System;
using System.Threading.Tasks;

namespace Serilog.Sinks.Async.Tests.Support
{
    public class MemorySink : ILogEventSink
    {
        public ConcurrentBag<LogEvent> Events { get; } = new ConcurrentBag<LogEvent>();
        public bool ThrowAfterCollecting { get; set; }
        public bool DelayEmit { get; set; }

        public void Emit(LogEvent logEvent)
        {
            if (DelayEmit)
                Task.Delay(TimeSpan.FromSeconds(3)).Wait();

            Events.Add(logEvent);

            if (ThrowAfterCollecting)
                throw new Exception($"Exception requested through {nameof(ThrowAfterCollecting)}.");
        }
    }
}