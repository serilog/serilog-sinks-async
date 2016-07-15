# Serilog.Sinks.Async [![Build status](https://ci.appveyor.com/api/projects/status/rostpmo2gq08ecag?svg=true)](https://ci.appveyor.com/project/mindkin/serilog-sinks-async)
An async Serilog sink

Use this buffered, async, delegating, sink to reduce the time it takes for your app to write your log events to your sinks. This sink can work with any `IEventLogSink` you use.

Especially suited to sinks that are either slow to write or have I/O bottlenecks (like http, databases, file writes etc.). 
This sink uses a separate thread pool thread to write to your sink, freeing up the calling thread to run in your app without having to wait. 

Utilizes the producer/consumer pattern (using the TPL `BufferBlock<T>` class), where the calling thread produces log events (on your main thread), and the consumer runs on a thread pool thread consuming log events and writing them to your sink.

Install from NuGet:

```powershell
Install-Package Serilog.Sinks.Async
```

Add this sink to your pipeline:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Async(x => x.Sink(new YourSink()))
    // Other logger configuration
    .CreateLogger()
```

Now `YourSink` will write messages using another [thread pool] thread while your logging thread gets on with more important stuff.

If you think your code is producing log events faster than your sink can consume and write them, then the buffer is going to grow in memory, until you run out!
Set a maximum size of the buffer so that your memory is not filled up. 
Buffered log events are then (async) postponed in your app thread until your sink catches up.

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Async(x => x.Sink(new YourSink), 500) //Max number of logevents to buffer in memory
    // Other logger configurationg
    .CreateLogger()
```

## About this Sink
This sink was created by this conversation thread: https://github.com/serilog/serilog/issues/809
