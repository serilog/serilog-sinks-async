using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.Async.Tests.Support;

class CollectingFailureListener: ILoggingFailureListener
{
    readonly object _sync = new();
    readonly List<LogEvent> _events = [];
    readonly List<Exception> _exceptions = [];
    
    public IReadOnlyList<LogEvent> Events
    {
        get
        {
            lock (_sync)
                return _events.ToList();
        }
    }
    public IReadOnlyList<Exception> Exceptions
    {
        get
        {
            lock (_sync)
                return _exceptions.ToList();
        }
    }

    public void OnLoggingFailed(object sender, LoggingFailureKind kind, string message, IReadOnlyCollection<LogEvent> events,
        Exception exception)
    {
        lock (_sync)
        {
            if (exception != null)
                _exceptions.Add(exception);
            
            foreach (var logEvent in events ?? [])
            {
                _events.Add(logEvent);
            }
        }
    }
}