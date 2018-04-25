namespace Serilog.Sinks.Async
{
    /// <summary>
    /// Defines a mechanism for the Async Sink to afford Health Checks a buffer metadata inspection mechanism.
    /// </summary>
    public interface IAsyncLogEventSinkMonitor
    {
        /// <summary>
        /// Invoked by Sink to supply the inspector to the monitor.
        /// </summary>
        /// <param name="inspector">The Async Sink's inspector.</param>
        void StartMonitoring(IAsyncLogEventSinkInspector inspector);

        /// <summary>
        /// Invoked by Sink to indicate that it is being Disposed.
        /// </summary>
        /// <param name="inspector">The Async Sink's inspector.</param>
        void StopMonitoring(IAsyncLogEventSinkInspector inspector);
    }
}