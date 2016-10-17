# Serilog.Sinks.Async [![Build status](https://ci.appveyor.com/api/projects/status/gvk0wl7aows14spn?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-async) [![NuGet](https://img.shields.io/nuget/vpre/Serilog.Sinks.Async.svg?maxAge=2592000)](https://www.nuget.org/packages/Serilog.Sinks.Async) [![Join the chat at https://gitter.im/serilog/serilog](https://img.shields.io/gitter/room/serilog/serilog.svg)](https://gitter.im/serilog/serilog)

An asynchronous wrapper for other [Serilog](https://serilog.net) sinks. Use this sink to reduce the overhead of logging calls by delegating work to a background thread. This is especially suited to non-batching sinks like the [File](https://github.com/serilog/serilog-sinks-file) and [RollingFile](https://github.com/serilog-serilog-sinks-rollingfile) sinks that may be affected by I/O bottlenecks.

**Note:** many of the network-based sinks (_CouchDB_, _Elasticsearch_, _MongoDB_, _Seq_, _Splunk_...) already perform asychronous batching natively and do not benefit from this wrapper.

### Getting started

Install from [NuGet](https://nuget.org/packages/serilog.sinks.async):

```powershell
Install-Package Serilog.Sinks.Async
```

Assuming you have already installed the target sink, such as the rolling file sink, move the wrapped sink's configuration within a `WriteTo.Async()` statement:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Async(a => a.RollingFile("logs/myapp-{Date}.txt"))
    // Other logger configuration
    .CreateLogger()
    
Log.Information("This will be written to disk on the worker thread");

// At application shutdown
Log.CloseAndFlush();
```

The wrapped sink (`RollingFile` in this case) will be invoked on a worker thread while your application's thread gets on with more important stuff.

Because the memory buffer may contain events that have not yet been written to the target sink, it is important to call `Log.CloseAndFlush()` or `Logger.Dispose()` when the application exits.

### Buffering

This sink uses a separate worker thread to write to your sink, freeing up the calling thread to run in your app without having to wait.

The default memory buffer feeding the worker thread is capped to 10,000 items, after which arriving events will be dropped. To increase or decrease this limit, specify it when configuring the async sink.

```csharp
    // Reduce the buffer to 500 events
    .WriteTo.Async(a => a.RollingFile("logs/myapp-{Date}.txt"), 500)
```

### XML `<appSettings>` and JSON configuration

XML and JSON configuration support has not yet been added for this wrapper.

### About this sink

This sink was created following this conversation thread: https://github.com/serilog/serilog/issues/809.
