using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
            Assert.Throws<ArgumentNullException>(() => new BackgroundWorkerSink(null, 10000, false, false, null));
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
        public async Task GivenDefaultConfig_WhenRequestsExceedCapacity_DoesNotBlock()
        {
            var batchTiming = Stopwatch.StartNew();
            using (var sink = new BackgroundWorkerSink(_logger, 1, blockWhenFull: false /*default*/, flushOnFatal: false /*default*/))
            {
                // Cause a delay when emitting to the inner sink, allowing us to easily fill the queue to capacity
                // while the first event is being propagated
                var acceptInterval = TimeSpan.FromMilliseconds(500);
                _innerSink.DelayEmit = acceptInterval;
                var tenSecondsWorth = 10_000 / acceptInterval.TotalMilliseconds + 1;
                for (int i = 0; i < tenSecondsWorth; i++)
                {
                    var emissionTiming = Stopwatch.StartNew();
                    sink.Emit(CreateEvent());
                    emissionTiming.Stop();

                    // Should not block the caller when the queue is full
                    Assert.InRange(emissionTiming.ElapsedMilliseconds, 0, 200);
                }

                // Allow at least one to propagate
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                Assert.NotEqual(0, ((IAsyncLogEventSinkInspector)sink).DroppedMessagesCount);
            }
            // Sanity check the overall timing
            batchTiming.Stop();
            // Need to add a significant fudge factor as AppVeyor build can result in `await` taking quite some time
            Assert.InRange(batchTiming.ElapsedMilliseconds, 950, 2050);
        }

        [Fact]
        public async Task GivenDefaultConfig_WhenFatalEmitted_DoesNotFlush()
        {
            var batchTiming = Stopwatch.StartNew();
            using (var sink = new BackgroundWorkerSink(_logger, 1, blockWhenFull: false /*default*/, flushOnFatal: false /*default*/))
            {
                // Cause a delay when emitting to the inner sink, allowing us to easily fill the queue to capacity
                // while the first event is being propagated
                var acceptInterval = TimeSpan.FromMilliseconds(500);
                _innerSink.DelayEmit = acceptInterval;
                var tenSecondsWorth = 10_000 / acceptInterval.TotalMilliseconds + 1;
                for (int i = 0; i < tenSecondsWorth; i++)
                {
                    var emissionTiming = Stopwatch.StartNew();
                    sink.Emit(CreateEvent(LogEventLevel.Fatal));
                    emissionTiming.Stop();

                    // Should not block the caller when the queue is full
                    Assert.InRange(emissionTiming.ElapsedMilliseconds, 0, 200);
                }

                // Allow at least one to propagate
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                Assert.NotEqual(0, ((IAsyncLogEventSinkInspector)sink).DroppedMessagesCount);
            }
            // Sanity check the overall timing
            batchTiming.Stop();
            // Need to add a significant fudge factor as AppVeyor build can result in `await` taking quite some time
            Assert.InRange(batchTiming.ElapsedMilliseconds, 950, 2050);
        }

        [Fact]
        public async Task GivenDefaultConfig_WhenRequestsExceedCapacity_ThenDropsEventsAndRecovers()
        {
            using (var sink = new BackgroundWorkerSink(_logger, 1, blockWhenFull: false /*default*/, flushOnFatal: false /*default*/))
            {
                var acceptInterval = TimeSpan.FromMilliseconds(200);
                _innerSink.DelayEmit = acceptInterval;

                for (int i = 0; i < 2; i++)
                {
                    sink.Emit(CreateEvent());
                    sink.Emit(CreateEvent());
                    await Task.Delay(acceptInterval);
                    sink.Emit(CreateEvent());
                }
                // Wait for the buffer and propagation to complete
                await Task.Delay(TimeSpan.FromSeconds(1));
                // Now verify things are back to normal; emit an event...
                var finalEvent = CreateEvent();
                sink.Emit(finalEvent);
                // ... give adequate time for it to be guaranteed to have percolated through
                await Task.Delay(TimeSpan.FromSeconds(1));

                // At least one of the preceding events should not have made it through
                var propagatedExcludingFinal =
                    from e in _innerSink.Events
                    where !Object.ReferenceEquals(finalEvent, e)
                    select e;
                Assert.InRange(2, 2 * 3 / 2 - 1, propagatedExcludingFinal.Count());
                // Final event should have made it through
                Assert.Contains(_innerSink.Events, x => Object.ReferenceEquals(finalEvent, x));
                Assert.NotEqual(0, ((IAsyncLogEventSinkInspector)sink).DroppedMessagesCount);
            }
        }

        [Fact]
        public async Task GivenConfiguredToBlock_WhenQueueFilled_ThenBlocks()
        {
            using (var sink = new BackgroundWorkerSink(_logger, 1, blockWhenFull: true, flushOnFatal: false))
            {
                // Cause a delay when emitting to the inner sink, allowing us to fill the queue to capacity
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
                Assert.Equal(0, ((IAsyncLogEventSinkInspector)sink).DroppedMessagesCount);
            }
        }

        [Fact]
        public async Task GivenConfiguredToFlush_WhenQueueFilled_ThenFlushes()
        {
            using (var sink = new BackgroundWorkerSink(_logger, 3, blockWhenFull: false, flushOnFatal: true))
            {
                // Cause a delay when emitting to the inner sink, allowing us to queue events
                _innerSink.DelayEmit = TimeSpan.FromMilliseconds(100);

                var events = new List<LogEvent>
                {
                    CreateEvent(LogEventLevel.Debug),
                    CreateEvent(LogEventLevel.Verbose),
                    CreateEvent(LogEventLevel.Fatal)
                };

                events.ForEach(e =>sink.Emit(e));

                await Task.Delay(TimeSpan.FromSeconds(100));

                // All events should be flushed
                Assert.Equal(3, _innerSink.Events.Count);
            }
        }

        [Fact]
        public void MonitorParameterAffordsSinkInspectorSuitableForHealthChecking()
        {
            var collector = new MemorySink { DelayEmit = TimeSpan.FromSeconds(2) };
            // 2 spaces in queue; 1 would make the second log entry eligible for dropping if consumer does not activate instantaneously
            var bufferSize = 2;
            var monitor = new DummyMonitor();
            using (var logger = new LoggerConfiguration()
                .WriteTo.Async(w => w.Sink(collector), bufferSize: 2, monitor: monitor)
                .CreateLogger())
            {
                // Construction of BackgroundWorkerSink triggers StartMonitoring
                var inspector = monitor.Inspector;
                Assert.Equal(bufferSize, inspector.BufferSize);
                Assert.Equal(0, inspector.Count);
                Assert.Equal(0, inspector.DroppedMessagesCount);
                logger.Information("Something to freeze the processing for 2s");
                // Can be taken from queue either instantanously or be awaiting consumer to take
                Assert.InRange(inspector.Count, 0, 1);
                Assert.Equal(0, inspector.DroppedMessagesCount);
                logger.Information("Something that will sit in the queue");
                Assert.InRange(inspector.Count, 1, 2);
                logger.Information("Something that will probably also sit in the queue (but could get dropped if first message has still not been picked up)");
                Assert.InRange(inspector.Count, 1, 2);
                logger.Information("Something that will get dropped unless we get preempted for 2s during our execution");
                const string droppedMessage = "Something that will definitely get dropped";
                logger.Information(droppedMessage);
                Assert.InRange(inspector.Count, 1, 2);
                // Unless we are put to sleep for a Rip Van Winkle period, either:
                // a) the BackgroundWorker will be emitting the item [and incurring the 2s delay we established], leaving a single item in the buffer
                // or b) neither will have been picked out of the buffer yet.
                Assert.InRange(inspector.Count, 1, 2);
                Assert.Equal(bufferSize, inspector.BufferSize);
                Assert.DoesNotContain(collector.Events, x => x.MessageTemplate.Text == droppedMessage);
                // Because messages wait 2 seconds, the only real way to get one into the buffer is with a debugger breakpoint or a sleep
                Assert.InRange(collector.Events.Count, 0, 3);
            }
            // Dispose should trigger a StopMonitoring call
            Assert.Null(monitor.Inspector);
        }

        private BackgroundWorkerSink CreateSinkWithDefaultOptions()
        {
            return new BackgroundWorkerSink(_logger, 10000, false, false);
        }

        private static LogEvent CreateEvent(LogEventLevel level = LogEventLevel.Error)
        {
            return new LogEvent(DateTimeOffset.MaxValue, level, null,
                new MessageTemplate("amessage", Enumerable.Empty<MessageTemplateToken>()),
                Enumerable.Empty<LogEventProperty>());
        }
    }
}