using System;
using BenchmarkDotNet.Running;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Serilog.Sinks.Async.PerformanceTests
{
    [TestClass]
    public class Benchmarks
    {
        [TestMethod]
        public void Benchmark()
        {
            BenchmarkRunner.Run<ThroughputBenchmark>();
        }
    }
}
