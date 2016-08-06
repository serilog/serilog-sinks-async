# Serilog.Sinks.Async [![Build status](https://ci.appveyor.com/api/projects/status/gvk0wl7aows14spn?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-async) [![NuGet](https://img.shields.io/nuget/vpre/Serilog.Sinks.Async.svg?maxAge=2592000)](https://www.nuget.org/packages/Serilog.Sinks.Async)

Use this buffered, async, delegating, sink to reduce the time it takes for your app to write your log events to your sinks. This sink can work with any `ILogEventSink` you use.

Especially suited to non-batching sinks that are either slow to write or have I/O bottlenecks (like http, databases, file writes etc.). 

This sink uses a separate worker thread to write to your sink, freeing up the calling thread to run in your app without having to wait.

Install from NuGet:

```powershell
Install-Package Serilog.Sinks.Async -Pre
```

Add this sink to your pipeline:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Async(x => x.YourSink())
    // Other logger configuration
    .CreateLogger()
```

Now `YourSink` will write messages using a worker thread while your applicatoin thread gets on with more important stuff.

The default memory buffer feeding the worker thread is capped to 10,000 items, after which arriving events will be dropped. To increase or decrease this limit, specify it when configuring the async sink.

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Async(x => x.YourSink(), 500) // Max number of events to buffer in memory
    // Other logger configurationg
    .CreateLogger()
```

## About this sink

This sink was created following this conversation thread: https://github.com/serilog/serilog/issues/809
