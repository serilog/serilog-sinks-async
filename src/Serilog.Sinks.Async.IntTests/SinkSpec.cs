using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace Serilog.Sinks.Async.IntTests
{
    public class SinkSpec
    {
        /// <summary>
        ///     If <see cref="withDelay" />, then adds a 1sec delay before every fifth element created
        /// </summary>
        private static void CreateAudits(ILogger logger, int count, bool withDelay)
        {
            var delay = TimeSpan.FromMilliseconds(1000);
            var sw = new Stopwatch();
            sw.Start();
            Debug.WriteLine("{0:h:mm:ss tt} Start: Writing {1} audits", DateTime.Now, count);
            try
            {
                var delayCount = 0;
                for (var counter = 0; counter < count; counter++)
                {
                    if (withDelay
                        && counter > 0
                        && counter%5 == 0)
                    {
                        delayCount++;
                        Debug.WriteLine("{0:h:mm:ss tt} Delay ({1}) after {2}th write, for {3:0.###}secs", DateTime.Now,
                            delayCount, counter,
                            delay.TotalSeconds);
                        Thread.Sleep(delay);
                    }
                    logger.Information("{$Counter}", counter);
                }
            }
            finally
            {
                sw.Stop();
                Debug.WriteLine("{0:h:mm:ss tt}   End: Writing {1} audits, taking {2:0.###}", DateTime.Now, count,
                    sw.Elapsed.TotalSeconds);
            }
        }

        private static List<LogEvent> RetrieveEvents(MemorySink sink, int count)
        {
            Debug.WriteLine("{0:h:mm:ss tt} Retrieving events", DateTime.Now);

            var evts = new List<LogEvent>();
            var result = TaskExtensions.Retry(() =>
            {
                evts.AddRange(sink.LogEvents);
                return evts;
            }, events => events != null && events.Count == count, TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(30));

            return result.Result;
        }

        [TestClass]
        public class GivenNoBufferQueueAndNoDelays : SinkSpecBase
        {
            [TestInitialize]
            public void Initialize()
            {
                base.Initialize(false, false);
            }

            [TestCleanup]
            public override void Cleanup()
            {
                base.Cleanup();
            }
        }

        [TestClass]
        public class GivenBufferQueueAndNoDelays : SinkSpecBase
        {
            [TestInitialize]
            public void Initialize()
            {
                base.Initialize(true, false);
            }

            [TestCleanup]
            public override void Cleanup()
            {
                base.Cleanup();
            }
        }

        [TestClass]
        public class GivenNoBufferQueueAndDelays : SinkSpecBase
        {
            [TestInitialize]
            public void Initialize()
            {
                base.Initialize(false, true);
            }

            [TestCleanup]
            public override void Cleanup()
            {
                base.Cleanup();
            }
        }

        [TestClass]
        public class GivenBufferQueueAndDelays : SinkSpecBase
        {
            [TestInitialize]
            public void Initialize()
            {
                base.Initialize(true, true);
            }

            [TestCleanup]
            public override void Cleanup()
            {
                base.Cleanup();
            }
        }

        public class SinkSpecBase
        {
            private bool _delayCreation;
            private ILogger _logger;
            private MemorySink _memorySink;

            protected void Initialize(bool useBufferedQueue, bool delayCreation)
            {
                _delayCreation = delayCreation;

                _memorySink = new MemorySink();

                if (useBufferedQueue)
                {
                    _logger = new LoggerConfiguration()
                        .WriteTo.Sink(new BufferedQueueSink(_memorySink))
                        .CreateLogger();
                }
                else
                {
                    _logger = new LoggerConfiguration()
                        .WriteTo.Sink(_memorySink)
                        .CreateLogger();
                }

                Debug.WriteLine("{0:h:mm:ss tt} Started test", DateTime.Now);
            }

            public virtual void Cleanup()
            {
                Debug.WriteLine("{0:h:mm:ss tt} Ended test", DateTime.Now);
            }

            [TestMethod, TestCategory("Integration.Perf")]
            public void WhenAuditSingle_ThenQueued()
            {
                CreateAudits(_logger, 1, _delayCreation);

                var result = RetrieveEvents(_memorySink, 1);

                Assert.AreEqual(1, result.Count);
            }

            [TestMethod, TestCategory("Integration.Perf")]
            public void WhenAuditTen_ThenQueued()
            {
                CreateAudits(_logger, 10, _delayCreation);

                var result = RetrieveEvents(_memorySink, 10);

                Assert.AreEqual(10, result.Count);
            }

            [TestMethod, TestCategory("Integration.Perf")]
            public void WhenAuditHundred_ThenQueued()
            {
                CreateAudits(_logger, 100, _delayCreation);

                var result = RetrieveEvents(_memorySink, 100);

                Assert.AreEqual(100, result.Count);
            }

            [TestMethod, TestCategory("Integration.Perf")]
            public void WhenAuditFiveHundred_ThenQueued()
            {
                CreateAudits(_logger, 500, _delayCreation);

                var result = RetrieveEvents(_memorySink, 500);

                Assert.AreEqual(500, result.Count);
            }
        }
    }
}