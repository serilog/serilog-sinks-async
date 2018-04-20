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
        public async Task GivenDefaultConfig_WhenRequestsExceedCapacity_DoesNotBlock()
        {
            var batchTiming = Stopwatch.StartNew();
            using (var sink = new BackgroundWorkerSink(_logger, 1, blockWhenFull: false /*default*/))
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
            }
            // Sanity check the overall timing
            batchTiming.Stop();
            // Need to add a significant fudge factor as AppVeyor build can result in `await` taking quite some time
            Assert.InRange(batchTiming.ElapsedMilliseconds, 950, 2050);
        }

        [Fact]
        public async Task GivenDefaultConfig_WhenRequestsExceedCapacity_ThenDropsEventsAndRecovers()
        {
            using (var sink = new BackgroundWorkerSink(_logger, 1, blockWhenFull: false /*default*/))
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
            }
        }

        [Fact]
        public async Task GivenConfiguredToBlock_WhenQueueFilled_ThenBlocks()
        {
            using (var sink = new BackgroundWorkerSink(_logger, 1, blockWhenFull: true))
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

        [Fact]
        public async Task InspectorOutParameterAffordsHealthMonitoringHook()
        {
            var collector = new MemorySink { DelayEmit = TimeSpan.FromSeconds(2) };
            // 2 spaces in queue; 1 would make the second log entry eligible for dropping if consumer does not activate instantaneously
            var bufferSize = 2;
            using (var logger = new LoggerConfiguration()
                .WriteTo.Async(w => w.Sink(collector), bufferSize: 2, inspector: out IQueueState inspector)
                .CreateLogger())
            {
                Assert.Equal(bufferSize, inspector.BufferSize);
                Assert.Equal(0, inspector.Count);
                logger.Information("Something to freeze the processing for 2s");
                await Task.Delay(TimeSpan.FromMilliseconds(200));
                // Can be taken from queue either instantanously or be awaiting consumer to take
                Assert.InRange(inspector.Count, 0, 1);
                logger.Information("Something that will sit in the queue");
                // Unless we are put to sleep for a Rip Van Winkle period, either:
                // a) the BackgroundWorker will be emitting the item [and incurring the 2s delay we established], leaving a single item in the buffer
                // or b) neither will have been picked out of the buffer yet.
                await Task.Delay(TimeSpan.FromMilliseconds(200));
                Assert.InRange(inspector.Count, 1, 2);
                Assert.Equal(bufferSize, inspector.BufferSize);
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