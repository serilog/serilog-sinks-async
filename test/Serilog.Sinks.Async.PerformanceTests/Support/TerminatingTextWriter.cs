using System;
using System.IO;
using System.Text;

namespace Serilog.Sinks.Async.PerformanceTests.Support;

public class TerminatingTextWriter : TextWriter
{
    public override Encoding Encoding { get; } = Encoding.ASCII;

    public override void Write(char value)
    {
        Console.WriteLine("SelfLog triggered");
        Environment.Exit(1);
    }
}