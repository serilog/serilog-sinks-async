namespace Serilog.Sinks.Async;
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