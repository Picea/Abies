using BenchmarkDotNet.Attributes;
using Picea.Abies.DOM;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Events;

namespace Picea.Abies.Benchmarks;

[MemoryDiagnoser]
[MarkdownExporter]
[JsonExporterAttribute.Full]
[JsonExporterAttribute.FullCompressed]
public class HandlerRegistryBenchmarks
{
    private sealed record Click : Message;

    private static readonly Message _click = new Click();
    private readonly HandlerRegistry _registry = new();

    private Node[] _rows = null!;
    private Node _table = null!;

    [Params(1000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rows = new Node[RowCount];
        for (int i = 0; i < RowCount; i++)
        {
            _rows[i] = tr([id($"row-{i}")],
            [
                td([], [
                    a([href($"/article/{i}"), onclick(_click)], [text($"Article {i}")])
                ]),
                td([], [
                    button([type("button"), onclick(_click)], [text("Favorite")])
                ])
            ]);
        }

        _table = table([id("bench-table")], [tbody([id("tbody")], _rows)]);
    }

    [IterationSetup(Target = nameof(UnregisterHandlers_WholeTree))]
    public void SetupUnregisterIteration()
    {
        _registry.Clear();
        _registry.RegisterHandlers(_table);
    }

    [Benchmark]
    public void RegisterHandlers_WholeTree()
    {
        _registry.Clear();
        _registry.RegisterHandlers(_table);
    }

    [Benchmark]
    public void RegisterHandlers_SetChildrenHtmlStyle()
    {
        _registry.Clear();
        foreach (var row in _rows)
        {
            _registry.RegisterHandlers(row);
        }
    }

    [Benchmark]
    public void UnregisterHandlers_WholeTree()
    {
        _registry.UnregisterHandlers(_table);
    }
}
