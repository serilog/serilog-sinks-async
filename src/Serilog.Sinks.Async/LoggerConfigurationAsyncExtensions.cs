using System;
using Serilog.Configuration;
using Serilog.Events;
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
        public static LoggerConfiguration Async(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            Action<LoggerSinkConfiguration> configure,
            int bufferSize = 10000)
        {
            var sublogger = new LoggerConfiguration();
            sublogger.MinimumLevel.Is(LevelAlias.Minimum);

            configure(sublogger.WriteTo);

            var wrapper = new BackgroundWorkerSink(sublogger.CreateLogger(), bufferSize);

            return loggerSinkConfiguration.Sink(wrapper);
        }
    }
}
