using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Serilog.Sinks.Async
{
    /// <summary>
    ///     Provides a continous buffered producer/consumer queue (of specified buffer size) of the specified
    ///     <see cref="TMessage" />.
    ///     The producer thread will be async awaited for the consumer, when the number of messages reaches the specified
    ///     buffer size, to prevent filling up memory.
    /// </summary>
    public class BufferedQueue<TMessage>
    {
        private const int DefaultQueueSize = 50;

        private readonly BufferBlock<TMessage> _queue;

        public BufferedQueue()
            : this(0)
        {
        }

        public BufferedQueue(int size)
        {
            Size = ((size > 0) ? size : DefaultQueueSize);
            _queue = new BufferBlock<TMessage>(new DataflowBlockOptions
            {
                BoundedCapacity = Size
            });
        }

        public Task IsComplete
        {
            get { return _queue.Completion; }
        }

        public int Count
        {
            get { return _queue.Count; }
        }

        public int Size { get; private set; }

        public async Task ProduceAsync(TMessage message)
        {
            await _queue.SendAsync(message);
        }

        public async Task ConsumeAsync(Action<TMessage> action)
        {
            await ConsumeAsync(action, CancellationToken.None);
        }

        public async Task ConsumeAsync(Action<TMessage> action, CancellationToken cancellation)
        {
            while (await _queue.OutputAvailableAsync(cancellation))
            {
                var message = await _queue.ReceiveAsync(cancellation);

                try
                {
                    action(message);
                }
                catch (Exception)
                {
                    //Log  and Ignore exception and continue
                }
            }
        }

        public void Complete()
        {
            _queue.Complete();
        }
    }
}