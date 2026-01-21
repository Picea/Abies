using BenchmarkDotNet.Attributes;
using Abies;
using Abies.DOM;
using Abies.Html;
using System.Runtime.Versioning;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;

namespace Abies.Benchmarks;

[MemoryDiagnoser]
[MarkdownExporter]
[SupportedOSPlatform("browser")]
public class DomDiffingBenchmarks
{
    private Node _oldElement = new Node("");
    private Node _newElement = new Node("");

    [GlobalSetup]
    public void Setup()
    {
        // Create the old element
        _oldElement = div(
        [
            id("container"),
            href("https://www.example.com")
        ],
        [
            button([type("button")],
            [text("Click Me")])
        ]);

        // Create the new element with some changes
        _newElement = div(
        [
            id("container"),
            href("https://www.changed.com") // Changed href attribute
        ],
        [
            button(
            [
                type("button")
            ],
            [
                text("Click Here") // Changed text
            ]),
            a(
            [
                href("https://www.newlink.com")
            ],
            [
                text("New Link")
            ])
        ]);
    }

    [Benchmark]
    public void TestDomDiffing()
    {
        

        for(var i = 0; i < 1000; i++)
        {
            Operations.Diff(_oldElement, _newElement);

        }
    }
}
