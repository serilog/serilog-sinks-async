using BenchmarkDotNet.Running;
using Xunit;

namespace Serilog.Sinks.Async.PerformanceTests;

public class Benchmarks
{
    [Fact]
    public void Benchmark()
    {
        BenchmarkRunner.Run<ThroughputBenchmark>();
        BenchmarkRunner.Run<LatencyBenchmark>();
    }
}