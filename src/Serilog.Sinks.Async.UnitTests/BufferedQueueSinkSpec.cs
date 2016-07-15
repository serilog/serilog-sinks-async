using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.Async.UnitTests
{
    public class BufferedQueueSinkSpec
    {
        [TestClass]
        public class GivenAContext
        {
            private Mock<ILogEventSink> _innerSink;
            private BufferedQueueSink _sink;

            [TestInitialize]
            public void Initialize()
            {
                _innerSink = new Mock<ILogEventSink>();
                _innerSink.Setup(s => s.Emit(It.IsAny<LogEvent>()))
                    .Callback((LogEvent le) => { });
                _sink = new BufferedQueueSink(_innerSink.Object);
            }

            [TestMethod, TestCategory("Unit")]
            [ExpectedException(typeof (ArgumentNullException))]
            public void WhenCtorWithNullSink_ThenThrows()
            {
                new BufferedQueueSink(null);
            }

            [TestMethod, TestCategory("Unit")]
            public void WhenEmitAndNotStarted_ThenConsumerStarted()
            {
                _sink.Emit(CreateEvent());

                Assert.IsTrue(_sink.ConsumerStarted);
            }

            [TestMethod, TestCategory("Unit")]
            public async Task WhenEmitSingle_ThenRelaysToInnerSink()
            {
                var logEvent = CreateEvent();
                _sink.Emit(logEvent);

                await Task.Delay(TimeSpan.FromSeconds(5));

                _innerSink.Verify(s => s.Emit(logEvent), Times.Once);
            }

            [TestMethod, TestCategory("Unit")]
            public async Task WhenInnerEmitThrows_ThenContinuesRelaysToInnerSink()
            {
                var exception = new Exception();
                _innerSink.Setup(s => s.Emit(It.IsAny<LogEvent>()))
                    .Throws(exception);

                var events = new List<LogEvent>
                {
                    CreateEvent(),
                    CreateEvent(),
                    CreateEvent()
                };
                events.ForEach(e => _sink.Emit(e));

                await Task.Delay(TimeSpan.FromSeconds(5));

                events.ForEach(e => _innerSink.Verify(s => s.Emit(e), Times.Once));
            }

            [TestMethod, TestCategory("Unit")]
            public async Task WhenEmitMultipleTimes_ThenRelaysToInnerSink()
            {
                var events = new List<LogEvent>
                {
                    CreateEvent(),
                    CreateEvent(),
                    CreateEvent()
                };

                events.ForEach(e => { _sink.Emit(e); });

                await Task.Delay(TimeSpan.FromSeconds(5));

                _innerSink.Verify(s => s.Emit(It.IsAny<LogEvent>()), Times.Exactly(3));
                events.ForEach(e => _innerSink.Verify(s => s.Emit(e), Times.Once));
            }

            private static LogEvent CreateEvent()
            {
                return new LogEvent(DateTimeOffset.MaxValue, LogEventLevel.Verbose, null,
                    new MessageTemplate("amessage", Enumerable.Empty<MessageTemplateToken>()),
                    Enumerable.Empty<LogEventProperty>());
            }
        }

        public class TestMessage
        {
        }

        public class TestSink : ILogEventSink
        {
            public void Emit(LogEvent logEvent)
            {
            }
        }
    }
}