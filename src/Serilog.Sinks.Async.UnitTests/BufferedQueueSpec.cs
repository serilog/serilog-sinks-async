using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Serilog.Sinks.Async.UnitTests
{
    public class BufferedQueueSpec
    {
        public interface IRanAction<in TMessage>
        {
            void Ran(TMessage message);
        }

        [TestClass]
        public class GivenAContext
        {
            private BufferedQueue<TestMessage> _queue;
            private Mock<IRanAction<TestMessage>> _ranner;

            [TestInitialize]
            public void Initialize()
            {
                _ranner = new Mock<IRanAction<TestMessage>>();
                _queue = new BufferedQueue<TestMessage>(5);
            }

            [TestMethod, TestCategory("Unit")]
            public async Task WhenProduceBatchOfOne_ThenReturnsProduced()
            {
                await _queue.ProduceAsync(new TestMessage());

                Assert.AreEqual(1, _queue.Count);
            }

            [TestMethod, TestCategory("Unit")]
            public void WhenProduceBatch_ThenReturnsProduced()
            {
                var messages = new List<TestMessage>
                {
                    new TestMessage(),
                    new TestMessage(),
                    new TestMessage()
                };

                messages.ForEach(async msg => await _queue.ProduceAsync(msg));

                Assert.AreEqual(3, _queue.Count);
            }

            [TestMethod, TestCategory("Unit")]
            public async Task WhenConsumeAsyncAndNoMessages_ThenConsumesNone()
            {
                var consumer = _queue.ConsumeAsync(msg => _ranner.Object.Ran(msg));
                _queue.Complete();
                await consumer;

                _ranner.Verify(r => r.Ran(It.IsAny<TestMessage>()), Times.Never);
            }

            [TestMethod, TestCategory("Unit")]
            public async Task WhenConsumeAsyncAndActionThrows_ThenConsumesAllProduced()
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

                messages.ForEach(async msg => await _queue.ProduceAsync(msg));
                var consumer = _queue.ConsumeAsync(msg => _ranner.Object.Ran(msg));
                _queue.Complete();

                await Task.WhenAll(consumer, _queue.IsComplete);

                messages.ForEach(msg => { _ranner.Verify(r => r.Ran(msg), Times.Once); });
            }

            [TestMethod, TestCategory("Unit")]
            public async Task WhenConsumeAsyncWithProducedBatchOfOne_ThenConsumesAllProduced()
            {
                var message = new TestMessage();

                await _queue.ProduceAsync(message);
                var consumer = _queue.ConsumeAsync(msg => _ranner.Object.Ran(msg));
                _queue.Complete();

                await Task.WhenAll(consumer, _queue.IsComplete);

                _ranner.Verify(r => r.Ran(message), Times.Once);
            }

            [TestMethod, TestCategory("Unit")]
            public async Task WhenConsumeAsyncWithProducedBatchSmallerThanBuffer_ThenConsumesAllProduced()
            {
                var messages = CreateMessages(3);

                messages.ForEach(async msg => await _queue.ProduceAsync(msg));
                var consumer = _queue.ConsumeAsync(msg => _ranner.Object.Ran(msg));
                _queue.Complete();

                await Task.WhenAll(consumer, _queue.IsComplete);

                messages.ForEach(msg => { _ranner.Verify(r => r.Ran(msg), Times.Once); });
            }

            [TestMethod, TestCategory("Unit")]
            public async Task WhenConsumeAsyncWithProducedBatchSizeOfBuffer_ThenConsumesAllProduced()
            {
                var messages = CreateMessages(_queue.Size);

                messages.ForEach(async msg => await _queue.ProduceAsync(msg));
                var consumer = _queue.ConsumeAsync(msg => _ranner.Object.Ran(msg));
                _queue.Complete();

                await Task.WhenAll(consumer, _queue.IsComplete);

                messages.ForEach(msg => { _ranner.Verify(r => r.Ran(msg), Times.Once); });
            }

            [TestMethod, TestCategory("Unit")]
            public async Task WhenConsumeAsyncWithProducedBatchLargerThanBuffer_ThenConsumesAllProduced()
            {
                var messages = CreateMessages(_queue.Size + 1);

                messages.ForEach(async msg => await _queue.ProduceAsync(msg));
                var consumer = _queue.ConsumeAsync(msg => _ranner.Object.Ran(msg));
                _queue.Complete();

                await Task.WhenAll(consumer, _queue.IsComplete);

                messages.ForEach(msg => { _ranner.Verify(r => r.Ran(msg), Times.Once); });
            }

            private static List<TestMessage> CreateMessages(int count)
            {
                var messages = new List<TestMessage>();

                for (var counter = 0; counter < count; counter++)
                {
                    messages.Add(new TestMessage());
                }

                return messages;
            }
        }

        public class TestMessage
        {
        }
    }
}