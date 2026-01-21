using System.Runtime.Versioning;
using Abies.Benchmarks;
using BenchmarkDotNet.Running;

[SupportedOSPlatform("browser")]
internal class Program
{

    private static void Main(string[] args)
    {
        
        //BenchmarkRunner.Run<UrlParsingBenchmarks>();
        BenchmarkRunner.Run<DomDiffingBenchmarks>();
    }
}