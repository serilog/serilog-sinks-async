namespace Serilog.Sinks.Async
{
    /// <summary>
    /// Provides a way to inspect the state of Async wrapper's ingestion queue.
    /// </summary>
    public interface IAsyncLogEventSinkInspector
    {
        /// <summary>
        /// Configured maximum number of items permitted to be held in the buffer awaiting ingestion.
        /// </summary>
        /// <exception cref="T:System.ObjectDisposedException">The Sink has been disposed.</exception>
        int BufferSize { get; }

        /// <summary>
        /// Current moment-in-time Count of items currently awaiting ingestion.
        /// </summary>
        /// <exception cref="T:System.ObjectDisposedException">The Sink has been disposed.</exception>
        int Count { get; }

        /// <summary>
        /// Accumulated number of messages dropped due to breaches of <see cref="BufferSize"/> limit.
        /// </summary>
        long DroppedMessagesCount { get; }
    }
}