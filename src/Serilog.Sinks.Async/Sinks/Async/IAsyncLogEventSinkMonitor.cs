namespace Serilog.Sinks.Async
{
    /// <summary>
    /// Defines a mechanism for the Async Sink to provide buffer metadata to facilitate integration into system health checking.
    /// </summary>
    /// <remarks>If the instance implements <see cref="System.IDisposable"/>, it will be <c>Dispose()</c>d at then time the Sink is.</remarks>
    public interface IAsyncLogEventSinkMonitor
    {
        /// <summary>
        /// Invoked by Sink to supply the buffer state hook to the monitor.
        /// </summary>
        /// <param name="state">The Async Sink's state information interface.</param>
        void MonitorState(IAsyncLogEventSinkState state);
    }
}