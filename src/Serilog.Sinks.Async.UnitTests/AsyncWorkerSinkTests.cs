using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.Async.UnitTests
{
    [TestClass]
    public class AsyncWorkerSinkTests
    {
        [TestMethod]
        public void EventsArePassedToInnerSink()
        {
            var collector = new CollectingSink();

            using (var log = new LoggerConfiguration()
                .WriteTo.Async2(w => w.Sink(collector))
                .CreateLogger())
            {
                log.Information("Hello, async world!");
                log.Information("Hello again!");
            }

            Assert.AreEqual(2, collector.Events.Count);
        }

        [TestMethod]
        public void DisposeCompletesWithoutWorkPerformed()
        {
            var collector = new CollectingSink();

            using (new LoggerConfiguration()
                .WriteTo.Async2(w => w.Sink(collector))
                .CreateLogger())
            {
            }

            Assert.AreEqual(0, collector.Events.Count);
        }
    }
}
