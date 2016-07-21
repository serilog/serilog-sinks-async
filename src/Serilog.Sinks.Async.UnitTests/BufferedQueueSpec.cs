using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Serilog.Sinks.Async.UnitTests
{
    public class BufferedQueueSpec
    {
        private static List<TestMessage> CreateMessages(int count)
        {
            var messages = new List<TestMessage>();

            Loop.For(() => { messages.Add(new TestMessage()); }, count);

            return messages;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public interface IRanAction<in TMessage>
        {
            void Ran(TMessage message);
        }

        [TestClass]
        public class GivenAnAction
        {
            private const int BufferSize = 5;
            private BufferedQueue<TestMessage> _queue;
            private Mock<IRanAction<TestMessage>> _ranner;

            [TestInitialize]
            public void Initialize()
            {
                _ranner = new Mock<IRanAction<TestMessage>>();
                _queue = new BufferedQueue<TestMessage>(BufferSize, _ranner.Object.Ran);
            }

            [TestCleanup]
            public void Cleanup()
            {
                if (_queue != null)
                {
                    _queue.Complete();
                }
            }

            [TestMethod, TestCategory("Unit")]
            public void WhenProduceNoMessages_ThenConsumesNone()
            {
                _ranner.Verify(r => r.Ran(It.IsAny<TestMessage>()), Times.Never);
            }

            [TestMethod, TestCategory("Unit")]
            public async Task WhenProduceAndActionThrows_ThenConsumesAllProduced()
            {
                var messages = new List<TestMessage>
                {
                    new TestMessage(),
                    new TestMessage(),
                    new TestMessage()
                };
                var exception = new Exception();
                _ranner.Setup(r => r.Ran(It.IsAny<TestMessage>()))
                    .Throws(exception);

                await Task.WhenAll(messages.Select(msg => _queue.ProduceAsync(msg)));
                _queue.Complete();

                await _queue.IsComplete;

                messages.ForEach(msg => { _ranner.Verify(r => r.Ran(msg), Times.Once); });
            }

            [TestMethod, TestCategory("Unit")]
            public async Task WhenProduceBatchOfOne_ThenConsumesAllProduced()
            {
                var message = new TestMessage();

                await _queue.ProduceAsync(message);
                _queue.Complete();

                await _queue.IsComplete;

                _ranner.Verify(r => r.Ran(message), Times.Once);
            }

            [TestMethod, TestCategory("Unit")]
            public async Task WhenProducedBatchSmallerThanBuffer_ThenConsumesAllProduced()
            {
                var messages = CreateMessages(BufferSize - 1);

                await Task.WhenAll(messages.Select(msg => _queue.ProduceAsync(msg)));
                _queue.Complete();

                await _queue.IsComplete;

                messages.ForEach(msg => { _ranner.Verify(r => r.Ran(msg), Times.Once); });
            }

            [TestMethod, TestCategory("Unit")]
            public async Task WhenProducedBatchSizeOfBuffer_ThenConsumesAllProduced()
            {
                var messages = CreateMessages(BufferSize);

                await Task.WhenAll(messages.Select(msg => _queue.ProduceAsync(msg)));
                _queue.Complete();

                await _queue.IsComplete;

                messages.ForEach(msg => { _ranner.Verify(r => r.Ran(msg), Times.Once); });
            }

            [TestMethod, TestCategory("Unit")]
            public async Task WhenProducedBatchLargerThanBuffer_ThenConsumesAllProduced()
            {
                var messages = CreateMessages(BufferSize + 1);

                await Task.WhenAll(messages.Select(msg => _queue.ProduceAsync(msg)));
                _queue.Complete();

                await _queue.IsComplete;

                messages.ForEach(msg => { _ranner.Verify(r => r.Ran(msg), Times.Once); });
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public class TestMessage
        {
        }
    }
}