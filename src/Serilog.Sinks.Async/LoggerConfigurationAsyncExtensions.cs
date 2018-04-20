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
        /// <returns>A <see cref="LoggerConfiguration"/> allowing configuration to continue.</returns>
        public static LoggerConfiguration Async(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            Action<LoggerSinkConfiguration> configure,
            int bufferSize = 10000,
            bool blockWhenFull = false)
        {
            return LoggerSinkConfiguration.Wrap(
                loggerSinkConfiguration,
                wrappedSink => new BackgroundWorkerSink(wrappedSink, bufferSize, blockWhenFull),
                configure);
        }

        /// <summary>
        /// Configure a sink to be invoked asynchronously, on a background worker thread.
        /// Provides an <paramref name="inspector"/> that can be used to check the live state of the buffer for health monitoring purposes.
        /// </summary>
        /// <param name="loggerSinkConfiguration">The <see cref="LoggerSinkConfiguration"/> being configured.</param>
        /// <param name="configure">An action that configures the wrapped sink.</param>
        /// <param name="bufferSize">The size of the concurrent queue used to feed the background worker thread. If
        /// the thread is unable to process events quickly enough and the queue is filled, depending on
        /// <paramref name="blockWhenFull"/> the queue will block or subsequent events will be dropped until
        /// room is made in the queue.</param>
        /// <param name="blockWhenFull">Block when the queue is full, instead of dropping events.</param>
        /// <param name="inspector">Provides a way to inspect the state of the queue for health monitoring purposes.</param>
        /// <returns>A <see cref="LoggerConfiguration"/> allowing configuration to continue.</returns>
        public static LoggerConfiguration Async(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            Action<LoggerSinkConfiguration> configure,
            out IQueueState inspector,
            int bufferSize = 10000,
            bool blockWhenFull = false)
        {
            // Cannot assign directly to the out param from within the lambda, so we need a temp
            IQueueState stateLens = null;
            var result = LoggerSinkConfiguration.Wrap(
                loggerSinkConfiguration,
                wrappedSink =>
                {
                    var sink = new BackgroundWorkerSink(wrappedSink, bufferSize, blockWhenFull);
                    stateLens = sink;
                    return sink;
                },
                configure);
            inspector = stateLens;
            return result;
        }
    }

    /// <summary>
    /// Provides a way to inspect the current state of Async wrapper's ingestion queue.
    /// </summary>
    public interface IQueueState
    {
        /// <summary>
        /// Count of items currently awaiting ingestion.
        /// </summary>
        /// <exception cref="T:System.ObjectDisposedException">The Sink has been disposed.</exception>
        int Count { get; }

        /// <summary>
        /// Maximum number of items permitted to be held in the buffer awaiting ingestion.
        /// </summary>
        /// <exception cref="T:System.ObjectDisposedException">The Sink has been disposed.</exception>
        int BufferSize { get; }
    }
}
