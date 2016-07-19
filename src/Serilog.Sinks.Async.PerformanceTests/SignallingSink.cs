using System;
using System.Threading;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.Async.PerformanceTests
{
    internal class SignallingSink : ILogEventSink
    {
        private readonly int _expectedCount;
        private readonly ManualResetEvent _wh;
        private int _current;

        public SignallingSink(int expectedCount)
        {
            _expectedCount = expectedCount;
            _wh = new ManualResetEvent(false);
        }

        public void Emit(LogEvent logEvent)
        {
            if (Interlocked.Increment(ref _current) == _expectedCount)
                _wh.Set();
        }

        public void Reset()
        {
            _wh.Reset();
            _current = 0;
        }

        public void Wait()
        {
            if (!_wh.WaitOne(60000))
                throw new TimeoutException("Event was not signaled within 60s.");
        }
    }
}