# Serilog.Sinks.Async [![Build status](https://ci.appveyor.com/api/projects/status/gvk0wl7aows14spn?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-async) [![NuGet](https://img.shields.io/nuget/v/Serilog.Sinks.Async.svg)](https://www.nuget.org/packages/Serilog.Sinks.Async) [![Join the chat at https://gitter.im/serilog/serilog](https://img.shields.io/gitter/room/serilog/serilog.svg)](https://gitter.im/serilog/serilog)

An asynchronous wrapper for other [Serilog](https://serilog.net) sinks. Use this sink to reduce the overhead of logging calls by delegating work to a background thread. This is especially suited to non-batching sinks like the [File](https://github.com/serilog/serilog-sinks-file) and [RollingFile](https://github.com/serilog/serilog-sinks-rollingfile) sinks that may be affected by I/O bottlenecks.

**Note:** many of the network-based sinks (_CouchDB_, _Elasticsearch_, _MongoDB_, _Seq_, _Splunk_...) already perform asychronous batching natively and do not benefit from this wrapper.

### Getting started

Install from [NuGet](https://nuget.org/packages/serilog.sinks.async):

```powershell
Install-Package Serilog.Sinks.Async
```

Assuming you have already installed the target sink, such as the file sink, move the wrapped sink's configuration within a `WriteTo.Async()` statement:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Async(a => a.File("logs/myapp.log"))
    // Other logger configuration
    .CreateLogger()
    
Log.Information("This will be written to disk on the worker thread");

// At application shutdown
Log.CloseAndFlush();
```

The wrapped sink (`File` in this case) will be invoked on a worker thread while your application's thread gets on with more important stuff.

Because the memory buffer may contain events that have not yet been written to the target sink, it is important to call `Log.CloseAndFlush()` or `Logger.Dispose()` when the application exits.

### Buffering

The default memory buffer feeding the worker thread is capped to 10,000 items, after which arriving events will be dropped. To increase or decrease this limit, specify it when configuring the async sink.

```csharp
    // Reduce the buffer to 500 events
    .WriteTo.Async(a => a.File("logs/myapp.log"), bufferSize: 500)
```

### Blocking

Warning: For the same reason one typically does not want exceptions from logging to leak into the execution path, one typically does not want a logger to be able to have the side-efect of actually interrupting application processing until the log propagation has been unblocked.

When the buffer size limit is reached, the default behavior is to drop any further attempted writes until the queue abates, reporting each such failure to the `Serilog.Debugging.SelfLog`. To replace this with a blocking behaviour, set `blockWhenFull` to `true`.

```csharp
    // Wait for any queued event to be accepted by the `File` log before allowing the calling thread
    // to resume its application work after a logging call when there are 10,000 LogEvents waiting
    .WriteTo.Async(a => a.File("logs/myapp.log"), blockWhenFull: true)
```

### XML `<appSettings>` and JSON configuration

Using [Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration) JSON:

```json
{
  "Serilog": {
    "WriteTo": [{
      "Name": "Async",
      "Args": {
        "configure": [{
          "Name": "Console"
        }]
      }
    }]
  }
}
```

XML configuration support has not yet been added for this wrapper.

### About this sink

This sink was created following this conversation thread: https://github.com/serilog/serilog/issues/809.
