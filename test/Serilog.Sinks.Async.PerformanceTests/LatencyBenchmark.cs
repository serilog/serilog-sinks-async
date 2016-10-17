using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.Async.PerformanceTests.Support;

namespace Serilog.Sinks.Async.PerformanceTests
{
    public class LatencyBenchmark
    {
        private readonly LogEvent _evt = new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null,
            new MessageTemplate(new[] {new TextToken("Hello")}), new LogEventProperty[0]);

        private Logger _syncLogger, _asyncLogger, _fileLogger, _asyncFileLogger;

        static LatencyBenchmark()
        {
            SelfLog.Enable(new TerminatingTextWriter());
        }

        [Setup]
        public void Reset()
        {
            foreach (var logger in new[] { _syncLogger, _asyncLogger, _fileLogger, _asyncFileLogger})
            {
                logger?.Dispose();
            }

            foreach (var tmp in Directory.GetFiles(".", "*.tmplog"))
            {
                System.IO.File.Delete(tmp);
            }

            _syncLogger = new LoggerConfiguration()
                .WriteTo.Sink(new SilentSink())
                .CreateLogger();

            _asyncLogger = new LoggerConfiguration()
                .WriteTo.Async(a => a.Sink(new SilentSink()), 10000000)
                .CreateLogger();

            _fileLogger = new LoggerConfiguration()
                .WriteTo.File("sync-file.tmplog")
                .CreateLogger();

            _asyncFileLogger = new LoggerConfiguration()
                .WriteTo.Async(a => a.File("async-file.tmplog"), 10000000)
                .CreateLogger();
        }

        [Benchmark(Baseline = true)]
        public void Sync()
        {
            _syncLogger.Write(_evt);
        }

        [Benchmark]
        public void Async()
        {
            _asyncLogger.Write(_evt);
        }

        [Benchmark]
        public void File()
        {
            _fileLogger.Write(_evt);
        }

        [Benchmark]
        public void AsyncFile()
        {
            _asyncFileLogger.Write(_evt);
        }
    }
}
