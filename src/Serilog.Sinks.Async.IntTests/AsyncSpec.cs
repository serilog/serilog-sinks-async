using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Serilog.Sinks.Async.IntTests
{
    [TestClass]
    public class AsyncSpec
    {
        private ILogger _logger;
        private MemorySink _memorySink;

        [TestInitialize]
        public void Initialize()
        {
            _memorySink = new MemorySink();

            _logger = new LoggerConfiguration()
                .WriteTo.Async(x => x.Sink(_memorySink), 500)
                .CreateLogger();
        }

        [TestMethod, TestCategory("Integration")]
        public void WhenLog_ThenSunk()
        {
            _logger.Information("{Message}", "amessage");

            var result = Loop.Retry(() => _memorySink.LogEvents,
                events => events.Count() == 1,
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));

            Assert.AreEqual(1, result.Result.Count());
            Assert.AreEqual("\"amessage\"", result.Result.First().Properties["Message"].ToString());
        }
    }
}