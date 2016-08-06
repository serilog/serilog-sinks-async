using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.Async.Tests;
using Serilog.Sinks.Async.Tests.Support;
using Xunit;

namespace Serilog.Sinks.Async.Tests
{
    public class BackgroundWorkerSinkSpec : IDisposable
    {
        readonly MemorySink _innerSink;
        readonly BackgroundWorkerSink _sink;

        public BackgroundWorkerSinkSpec()
        {
            _innerSink = new MemorySink();
            var logger = new LoggerConfiguration().WriteTo.Sink(_innerSink).CreateLogger();
            _sink = new BackgroundWorkerSink(logger, 10000);
        }

        public void Dispose()
        {
            _sink.Dispose();
        }

        [Fact]
        public void WhenCtorWithNullSink_ThenThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new BackgroundWorkerSink(null, 10000));
        }

        [Fact]
        public async Task WhenEmitSingle_ThenRelaysToInnerSink()
        {
            var logEvent = CreateEvent();
            _sink.Emit(logEvent);

            await Task.Delay(TimeSpan.FromSeconds(3));

            Assert.Equal(1, _innerSink.Events.Count);
        }

        [Fact]
        public async Task WhenInnerEmitThrows_ThenContinuesRelaysToInnerSink()
        {
            _innerSink.ThrowAfterCollecting = true;

            var events = new List<LogEvent>
            {
                CreateEvent(),
                CreateEvent(),
                CreateEvent()
            };
            events.ForEach(e => _sink.Emit(e));

            await Task.Delay(TimeSpan.FromSeconds(3));

            Assert.Equal(3, _innerSink.Events.Count);
        }

        [Fact]
        public async Task WhenEmitMultipleTimes_ThenRelaysToInnerSink()
        {
            var events = new List<LogEvent>
            {
                CreateEvent(),
                CreateEvent(),
                CreateEvent()
            };

            events.ForEach(e => { _sink.Emit(e); });

            await Task.Delay(TimeSpan.FromSeconds(3));

            Assert.Equal(3, _innerSink.Events.Count);
        }

        private static LogEvent CreateEvent()
        {
            return new LogEvent(DateTimeOffset.MaxValue, LogEventLevel.Error, null,
                new MessageTemplate("amessage", Enumerable.Empty<MessageTemplateToken>()),
                Enumerable.Empty<LogEventProperty>());
        }
    }
}