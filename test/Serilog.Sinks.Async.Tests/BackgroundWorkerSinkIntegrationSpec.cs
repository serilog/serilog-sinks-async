using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Serilog.Events;
using Serilog.Sinks.Async.Tests.Support;
using Serilog.Core;
using Xunit;
using System.Linq;

namespace Serilog.Sinks.Async.Tests
{
    public class BackgroundWorkerSinkIntegrationSpec
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
                Loop.For(counter =>
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
                }, count);
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
            Debug.WriteLine("{0:h:mm:ss tt} Retrieving {1} events", DateTime.Now, count);

            Loop.Retry(() => sink.Events, events => events != null && events.Count >= count, TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(30));

            return sink.Events.ToList();
        }

        public class GivenNoBufferQueueAndNoDelays : SinkSpecBase
        {
            public GivenNoBufferQueueAndNoDelays()
                : base(false, false)
            {
            }
        }

        public class GivenBufferQueueAndNoDelays : SinkSpecBase
        {
            public GivenBufferQueueAndNoDelays()
                : base(true, false)
            {
            }
        }

        public class GivenNoBufferQueueAndDelays : SinkSpecBase
        {
            public GivenNoBufferQueueAndDelays()
                : base(false, true)
            {
            }
        }

        public class GivenBufferQueueAndDelays : SinkSpecBase
        {
            public GivenBufferQueueAndDelays()
                : base(true, true)
            {
            }
        }

        public abstract class SinkSpecBase : IDisposable
        {
            private bool _delayCreation;
            private Logger _logger;
            private MemorySink _memorySink;

            protected SinkSpecBase(bool useBufferedQueue, bool delayCreation)
            {
                _delayCreation = delayCreation;

                _memorySink = new MemorySink();

                if (useBufferedQueue)
                {
                    _logger = new LoggerConfiguration()
                        .WriteTo.Async(a => a.Sink(_memorySink))
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

            public void Dispose()
            {
                _logger.Dispose();
                Debug.WriteLine("{0:h:mm:ss tt} Ended test", DateTime.Now);
            }

            [Fact]
            public void WhenAuditSingle_ThenQueued()
            {
                CreateAudits(_logger, 1, _delayCreation);

                var result = RetrieveEvents(_memorySink, 1);

                Assert.Single(result);
            }

            [Fact]
            public void WhenAuditTen_ThenQueued()
            {
                CreateAudits(_logger, 10, _delayCreation);

                var result = RetrieveEvents(_memorySink, 10);

                Assert.Equal(10, result.Count);
            }

            [Fact]
            public void WhenAuditHundred_ThenQueued()
            {
                CreateAudits(_logger, 100, _delayCreation);

                var result = RetrieveEvents(_memorySink, 100);

                Assert.Equal(100, result.Count);
            }

            [Fact]
            public void WhenAuditFiveHundred_ThenQueued()
            {
                CreateAudits(_logger, 500, _delayCreation);

                var result = RetrieveEvents(_memorySink, 500);

                Assert.Equal(500, result.Count);
            }
        }
    }
}