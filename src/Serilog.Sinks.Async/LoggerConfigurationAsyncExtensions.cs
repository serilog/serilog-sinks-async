using System;
using System.ComponentModel;
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
        /// </summary>
        /// <param name="loggerSinkConfiguration">The <see cref="LoggerSinkConfiguration"/> being configured.</param>
        /// <param name="configure">An action that configures the wrapped sink.</param>
        /// <param name="bufferSize">The size of the concurrent queue used to feed the background worker thread. If
        /// the thread is unable to process events quickly enough and the queue is filled, subsequent events will be
        /// dropped until room is made in the queue.</param>
        /// <returns>A <see cref="LoggerConfiguration"/> allowing configuration to continue.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static LoggerConfiguration Async(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            Action<LoggerSinkConfiguration> configure,
            int bufferSize)
        {
            return loggerSinkConfiguration.Async(configure, bufferSize, false);
        }

        /// <summary>
        /// Configure a sink to be invoked asynchronously, on a background worker thread.
        /// </summary>
        /// <param name="loggerSinkConfiguration">The <see cref="LoggerSinkConfiguration"/> being configured.</param>
        /// <param name="configure">An action that configures the wrapped sink.</param>
        /// <param name="bufferSize">The size of the concurrent queue used to feed the background worker thread. If
        /// the thread is unable to process events quickly enough and the queue is filled, depending on
        /// <paramref name="blockWhenFull"/> the queue will block or subsequent events will be dropped until
        /// room is made in the queue.</param>
        /// <param name="blockWhenFull">Block when the queue is full, instead of dropping events.</param>
        /// <param name="monitorIntervalSeconds">Interval between invocations of <paramref name="monitor"/>.</param>
        /// <param name="monitor">Callback to facilitate health checking the internal queue. Frequency is controlled by <paramref name="monitorIntervalSeconds"/>.</param>
        /// <returns>A <see cref="LoggerConfiguration"/> allowing configuration to continue.</returns>
        public static LoggerConfiguration Async(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            Action<LoggerSinkConfiguration> configure,
            int bufferSize = 10000,
            bool blockWhenFull = false,
            int monitorIntervalSeconds = 10,
            Action<System.Collections.Concurrent.BlockingCollection<Events.LogEvent>> monitor = null)
        {
            return LoggerSinkConfiguration.Wrap(
                loggerSinkConfiguration,
                wrappedSink => new BackgroundWorkerSink(wrappedSink, bufferSize, blockWhenFull, monitorIntervalSeconds, monitor),
                configure);
        }

    }
}
