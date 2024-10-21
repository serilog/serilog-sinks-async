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
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Serilog.Sinks.Async;

sealed class BackgroundWorkerSink : ILogEventSink, IAsyncLogEventSinkInspector, IDisposable, ISetLoggingFailureListener
{
    readonly ILogEventSink _wrappedSink;
    readonly bool _blockWhenFull;
    readonly BlockingCollection<LogEvent> _queue;
    readonly Task _worker;
    readonly IAsyncLogEventSinkMonitor? _monitor;
    
    // By contract, set only during initialization, so updates are not synchronized.
    ILoggingFailureListener _failureListener = SelfLog.FailureListener;

    long _droppedMessages;

    public BackgroundWorkerSink(ILogEventSink wrappedSink, int bufferCapacity, bool blockWhenFull, IAsyncLogEventSinkMonitor? monitor)
    {
        if (bufferCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(bufferCapacity));
        _wrappedSink = wrappedSink ?? throw new ArgumentNullException(nameof(wrappedSink));
        _blockWhenFull = blockWhenFull;
        _queue = new BlockingCollection<LogEvent>(bufferCapacity);
        _worker = Task.Factory.StartNew(Pump, CancellationToken.None, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        _monitor = monitor;
        monitor?.StartMonitoring(this);
    }

    public void Emit(LogEvent logEvent)
    {
        if (_queue.IsAddingCompleted)
        {
            _failureListener.OnLoggingFailed(this, LoggingFailureKind.Final, "the sink has been disposed", [logEvent], null);
            return;
        }

        try
        {
            if (_blockWhenFull)
            {
                _queue.Add(logEvent);
            }
            else
            {
                if (!_queue.TryAdd(logEvent))
                {
                    Interlocked.Increment(ref _droppedMessages);
                    _failureListener.OnLoggingFailed(this, LoggingFailureKind.Permanent, $"unable to enqueue, capacity {_queue.BoundedCapacity}", [logEvent], null);
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            // Thrown in the event of a race condition when we try to add another event after
            // CompleteAdding has been called
            _failureListener.OnLoggingFailed(this, LoggingFailureKind.Final, "the sink has been disposed", [logEvent], ex);
        }
    }

    public void Dispose()
    {
        // Prevent any more events from being added
        _queue.CompleteAdding();

        // Allow queued events to be flushed
        _worker.Wait();

        (_wrappedSink as IDisposable)?.Dispose();

        _monitor?.StopMonitoring(this);
    }

    void Pump()
    {
        try
        {
            foreach (var next in _queue.GetConsumingEnumerable())
            {
                try
                {
                    _wrappedSink.Emit(next);
                }
                catch (Exception ex)
                {
                    _failureListener.OnLoggingFailed(this, LoggingFailureKind.Permanent, "failed to emit event to wrapped sink", [next], ex);
                }
            }
        }
        catch (Exception fatal)
        {
            _failureListener.OnLoggingFailed(this, LoggingFailureKind.Final, "fatal error in worker thread", null, fatal);
        }
    }

    int IAsyncLogEventSinkInspector.BufferSize => _queue.BoundedCapacity;

    int IAsyncLogEventSinkInspector.Count => _queue.Count;

    long IAsyncLogEventSinkInspector.DroppedMessagesCount => _droppedMessages;
    
    public void SetFailureListener(ILoggingFailureListener failureListener)
    {
        _failureListener = failureListener ?? throw new ArgumentNullException(nameof(failureListener));
    }
}