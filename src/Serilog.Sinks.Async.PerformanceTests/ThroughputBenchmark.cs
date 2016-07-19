using System;
using BenchmarkDotNet.Attributes;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.Async.PerformanceTests
{
    public class ThroughputBenchmark
    {
        private const int Count = 10000;

        private readonly LogEvent _evt = new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null,
            new MessageTemplate(new[] {new TextToken("Hello")}), new LogEventProperty[0]);

        private readonly SignallingSink _signal;
        private readonly ILogger _syncLogger, _asyncLogger;

        public ThroughputBenchmark()
        {
            _signal = new SignallingSink(Count);

            _syncLogger = new LoggerConfiguration()
                .WriteTo.Sink(_signal)
                .CreateLogger();

            _asyncLogger = new LoggerConfiguration()
                .WriteTo.Async(a => a.Sink(_signal))
                .CreateLogger();
        }

        [Setup]
        public void Reset()
        {
            _signal.Reset();
        }

        [Benchmark(Baseline = true)]
        public void Sync()
        {
            for (var i = 0; i < Count; ++i)
            {
                _syncLogger.Write(_evt);
            }

            // Will complete immediately, but makes the comparison fairer.
            _signal.Wait();
        }

        [Benchmark]
        public void Async()
        {
            for (var i = 0; i < Count; ++i)
            {
                _asyncLogger.Write(_evt);
            }

            _signal.Wait();
        }
    }
}