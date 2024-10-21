using System;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.Async.Tests.Support;

class NotImplementedSink: ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        throw new NotImplementedException();
    }
}