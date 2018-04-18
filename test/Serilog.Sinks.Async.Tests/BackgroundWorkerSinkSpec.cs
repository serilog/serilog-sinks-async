using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.Async.Tests.Support;
using Xunit;

namespace Serilog.Sinks.Async.Tests
{
    public class BackgroundWorkerSinkSpec
    {
        readonly Logger _logger;
        readonly MemorySink _innerSink;

        public BackgroundWorkerSinkSpec()
        {
            _innerSink = new MemorySink();
            _logger = new LoggerConfiguration().WriteTo.Sink(_innerSink).CreateLogger();
        }

        [Fact]
        public void WhenCtorWithNullSink_ThenThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new BackgroundWorkerSink(null, 10000, false));
        }

        [Fact]
        public async Task WhenEmitSingle_ThenRelaysToInnerSink()
        {
            using (var sink = this.CreateSinkWithDefaultOptions())
            {
                var logEvent = CreateEvent();

                sink.Emit(logEvent);

                await Task.Delay(TimeSpan.FromSeconds(3));

                Assert.Single(_innerSink.Events);
            }
        }

        [Fact]
        public async Task WhenInnerEmitThrows_ThenContinuesRelaysToInnerSink()
        {
            using (var sink = this.CreateSinkWithDefaultOptions())
            {
                _innerSink.ThrowAfterCollecting = true;

                var events = new List<LogEvent>
                {
                    CreateEvent(),
                    CreateEvent(),
                    CreateEvent()
                };
                events.ForEach(e => sink.Emit(e));

                await Task.Delay(TimeSpan.FromSeconds(3));

                Assert.Equal(3, _innerSink.Events.Count);
            }
        }

        [Fact]
        public async Task WhenEmitMultipleTimes_ThenRelaysToInnerSink()
        {
            using (var sink = this.CreateSinkWithDefaultOptions())
            {
                var events = new List<LogEvent>
                {
                    CreateEvent(),
                    CreateEvent(),
                    CreateEvent()
                };
                events.ForEach(e => { sink.Emit(e); });

                await Task.Delay(TimeSpan.FromSeconds(3));

                Assert.Equal(3, _innerSink.Events.Count);
            }
        }

        [Fact]
        public async Task WhenQueueFull_ThenDropsEvents()
        {
            using (var sink = new BackgroundWorkerSink(_logger, 1, false))
            {
                // Cause a delay when emmitting to the inner sink, allowing us to fill the queue to capacity
                // after the first event is popped
                _innerSink.DelayEmit = TimeSpan.FromMilliseconds(300);

                var events = new List<LogEvent>
                {
                    CreateEvent(),
                    CreateEvent(),
                    CreateEvent(),
                    CreateEvent(),
                    CreateEvent()
                };
                events.ForEach(e =>
                {
                    var sw = Stopwatch.StartNew();
                    sink.Emit(e);
                    sw.Stop();

                    Assert.True(sw.ElapsedMilliseconds < 200, "Should not block the caller when the queue is full");
                });

                // If we *weren't* dropped events, the delay in the inner sink would mean the 5 events would take
                // at least 15 seconds to process
                await Task.Delay(TimeSpan.FromSeconds(2));

                // Events should be dropped
                Assert.Equal(2, _innerSink.Events.Count);
            }
        }

        [Fact]
        public async Task WhenQueueFull_ThenBlocks()
        {
            using (var sink = new BackgroundWorkerSink(_logger, 1, true))
            {
                // Cause a delay when emmitting to the inner sink, allowing us to fill the queue to capacity
                // after the first event is popped
                _innerSink.DelayEmit = TimeSpan.FromMilliseconds(300);

                var events = new List<LogEvent>
                {
                    CreateEvent(),
                    CreateEvent(),
                    CreateEvent()
                };

                int i = 0;
                events.ForEach(e =>
                {
                    var sw = Stopwatch.StartNew();
                    sink.Emit(e);
                    sw.Stop();

                    // Emit should return immediately the first time, since the queue is not yet full. On
                    // subsequent calls, the queue should be full, so we should be blocked
                    if (i > 0)
                    {
                        Assert.True(sw.ElapsedMilliseconds > 200, "Should block the caller when the queue is full");
                    }
                });

                await Task.Delay(TimeSpan.FromSeconds(2));

                // No events should be dropped
                Assert.Equal(3, _innerSink.Events.Count);
            }
        }

        private BackgroundWorkerSink CreateSinkWithDefaultOptions()
        {
            return new BackgroundWorkerSink(_logger, 10000, false);
        }

        private static LogEvent CreateEvent()
        {
            return new LogEvent(DateTimeOffset.MaxValue, LogEventLevel.Error, null,
                new MessageTemplate("amessage", Enumerable.Empty<MessageTemplateToken>()),
                Enumerable.Empty<LogEventProperty>());
        }
    }
}