using Abies.Benchmarks;
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(DomDiffingBenchmarks).Assembly).Run(args);
