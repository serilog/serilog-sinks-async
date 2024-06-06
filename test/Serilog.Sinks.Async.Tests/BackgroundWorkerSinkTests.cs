using Serilog.Sinks.Async.Tests.Support;
using Xunit;

namespace Serilog.Sinks.Async.Tests;

public class BackgroundWorkerSinkTests
{
    [Fact]
    public void EventsArePassedToInnerSink()
    {
            var collector = new MemorySink();

            using (var log = new LoggerConfiguration()
                .WriteTo.Async(w => w.Sink(collector))
                .CreateLogger())
            {
                log.Information("Hello, async world!");
                log.Information("Hello again!");
            }

            Assert.Equal(2, collector.Events.Count);
        }

    [Fact]
    public void DisposeCompletesWithoutWorkPerformed()
    {
            var collector = new MemorySink();

            using (new LoggerConfiguration()
                .WriteTo.Async(w => w.Sink(collector))
                .CreateLogger())
            {
            }

            Assert.Empty(collector.Events);
        }

    [Fact]
    public void CtorAndDisposeInformMonitor()
    {
            var collector = new MemorySink();
            var monitor = new DummyMonitor();

            using (new LoggerConfiguration()
                .WriteTo.Async(w => w.Sink(collector), monitor: monitor)
                .CreateLogger())
            {
                Assert.NotNull(monitor.Inspector);
            }

            Assert.Null(monitor.Inspector);
        }
}