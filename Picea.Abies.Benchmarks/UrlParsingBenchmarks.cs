using BenchmarkDotNet.Attributes;

namespace Picea.Abies.Benchmarks;

[MemoryDiagnoser]
[MarkdownExporter]
public class UrlParsingBenchmarks
{
    private string[]? _testUrls;

    [GlobalSetup]
    public void Setup()
    {
        for (var i = 0; i < 1000; i++)
        {
            _testUrls = new string[1000];
            for (var j = 0; j < 1000; j++)
            {
                _testUrls[j] = UrlGenerator.GenerateRandomUrl();
            }
        }
    }

    [Benchmark]
    public void ParseUrls()
    {
        if (_testUrls is null)
        {
            throw new InvalidOperationException("Test URLs have not been initialized.");
        }
        foreach (var url in _testUrls)
        {
            Navigation.ParseUrl(url);
            // Perform operations if necessary
        }
    }
}
