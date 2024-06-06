// Copyright © Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Serilog.Configuration;
using Serilog.Sinks.Async;

namespace Serilog;

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