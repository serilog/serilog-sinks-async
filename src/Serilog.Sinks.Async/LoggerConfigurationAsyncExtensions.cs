using System;
using Serilog.Configuration;
using Serilog.Sinks.Async;

namespace Serilog
{
    /// <summary>
    /// Extends <see cref="LoggerConfiguration"/> with methods for configuring asynchronous logging.
    /// </summary>
    public static class LoggerConfigurationAsyncExtensions
    {
        /// <summary>
        /// Configure a sink to be invoked asynchronously, on a background worker thread.
        /// Accepts a reference to a <paramref name="monitor"/> that will be supplied the internal state interface for health monitoring purposes.
        /// </summary>
        /// <param name="loggerSinkConfiguration">The <see cref="LoggerSinkConfiguration"/> being configured.</param>
        /// <param name="configure">An action that configures the wrapped sink.</param>
        /// <param name="bufferSize">The size of the concurrent queue used to feed the background worker thread. If
        /// the thread is unable to process events quickly enough and the queue is filled, depending on
        /// <paramref name="blockWhenFull"/> the queue will block or subsequent events will be dropped until
        /// room is made in the queue.</param>
        /// <param name="blockWhenFull">Block when the queue is full, instead of dropping events.</param>
        /// <param name="monitor">Monitor to supply buffer information to.</param>
        /// <returns>A <see cref="LoggerConfiguration"/> allowing configuration to continue.</returns>
        public static LoggerConfiguration Async(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            Action<LoggerSinkConfiguration> configure,
            int bufferSize = 10000,
            bool blockWhenFull = false,
            IAsyncLogEventSinkMonitor? monitor = null)
        {
            var wrapper = LoggerSinkConfiguration.Wrap(
                wrappedSink => new BackgroundWorkerSink(wrappedSink, bufferSize, blockWhenFull, monitor),
                configure);
            return loggerSinkConfiguration.Sink(wrapper);
        }
    }
}