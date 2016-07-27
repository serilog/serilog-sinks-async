using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.Async.PerformanceTests
{
    public class SilentSink : ILogEventSink
    {
        public void Emit(LogEvent logEvent)
        {
        }
    }
}