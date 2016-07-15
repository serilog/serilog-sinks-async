using System;
using Serilog.Configuration;

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
    }
}