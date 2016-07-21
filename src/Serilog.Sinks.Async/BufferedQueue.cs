using System;
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
        private readonly ActionBlock<TMessage> _executor;
        private readonly BufferBlock<TMessage> _queue;

        public BufferedQueue(int size)
        {
            Size = ((size > 0) ? size : DefaultQueueSize);
            _queue = new BufferBlock<TMessage>(new DataflowBlockOptions
            {
                BoundedCapacity = Size
            });
        }

        public BufferedQueue(int size, Action<TMessage> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            Size = ((size > 0) ? size : DefaultQueueSize);
            _queue = new BufferBlock<TMessage>(new DataflowBlockOptions
            {
                BoundedCapacity = Size
            });
            _executor = new ActionBlock<TMessage>(message => { ExecuteAction(action, message); });
            _queue.LinkTo(_executor, new DataflowLinkOptions
            {
                PropagateCompletion = true
            });
        }

        public Task IsComplete
        {
            get { return _executor != null ? _executor.Completion : _queue.Completion; }
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

        private static void ExecuteAction(Action<TMessage> action, TMessage message)
        {
            try
            {
                action(message);
            }
            catch (Exception)
            {
                //Log and Ignore exception and continue
            }
        }

        public void Complete()
        {
            _queue.Complete();
        }
    }
}