# Serilog.Sinks.Async [![Build status](https://ci.appveyor.com/api/projects/status/rostpmo2gq08ecag?svg=true)](https://ci.appveyor.com/project/mindkin/serilog-sinks-async)
An async Serilog sink

Use this buffered async delegating sink to reduce the time it takes to write your log events to a sink.
This sink can work with any `IEventLogSink`. Especially suited to sinks that are either slow to write or wait on I/O (like databases, files systems etc). This sink uses a separate thread pool thread to execute your sink, freeing up the calling thread to run in your app.

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

If you think your code is logging faster than your sink can handle, then the buffer is going to grow in memory.
Set a maximum size of the buffer so that your machine memory is not filled up. 
Buffered logevents are then (async) postponed until your sink catches up.

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Async(x => x.Sink(new YourSink), 500) //Max number of logevents to buffer in memory
    // Other logger configurationg
    .CreateLogger()
```
