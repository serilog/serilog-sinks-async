using System;
using Serilog.Configuration;
using Serilog.Events;

namespace Serilog.Sinks.Async
{
    public static class LoggerConfigurationExtensions
    {
        public static LoggerConfiguration Async(this LoggerSinkConfiguration configuration,
            Action<LoggerSinkConfiguration> sinkConfiguration, int bufferSize = 0)
        {
            var sublogger = new LoggerConfiguration();

            sinkConfiguration(sublogger.WriteTo);

            var wrapper = new BufferedQueueSink(sublogger.CreateLogger(), bufferSize);

            return configuration.Sink(wrapper);
        }

        public static LoggerConfiguration Async2(this LoggerSinkConfiguration configuration,
            Action<LoggerSinkConfiguration> sinkConfiguration, int bufferSize = 10000)
        {
            var sublogger = new LoggerConfiguration();
            sublogger.MinimumLevel.Is(LevelAlias.Minimum);

            sinkConfiguration(sublogger.WriteTo);

            var wrapper = new AsyncWorkerSink(sublogger.CreateLogger(), bufferSize);

            return configuration.Sink(wrapper);
        }
    }
}