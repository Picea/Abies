using BenchmarkDotNet.Attributes;
using Abies.DOM;
using Abies.Html;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;

namespace Abies.Benchmarks;

/// <summary>
/// Comprehensive benchmarks for Virtual DOM diffing operations.
/// Results are exported to JSON for CI/CD integration with github-action-benchmark.
/// </summary>
/// <remarks>
/// These benchmarks measure:
/// - Attribute-only changes (minimal diff)
/// - Text content changes
/// - Node additions and removals
/// - Small, medium, and large DOM tree updates
/// </remarks>
[MemoryDiagnoser]
[MarkdownExporter]
[JsonExporterAttribute.Full]
[JsonExporterAttribute.FullCompressed]
public class DomDiffingBenchmarks
{
    // Small DOM scenarios
    private Node _smallOld = null!;
    private Node _smallNew = null!;
    
    // Medium DOM scenarios (10-20 nodes)
    private Node _mediumOld = null!;
    private Node _mediumNew = null!;
    
    // Large DOM scenarios (100+ nodes)
    private Node _largeOld = null!;
    private Node _largeNew = null!;
    
    // Attribute-only change scenario
    private Node _attrOld = null!;
    private Node _attrNew = null!;
    
    // Text-only change scenario
    private Node _textOld = null!;
    private Node _textNew = null!;
    
    // Node addition scenario
    private Node _addOld = null!;
    private Node _addNew = null!;
    
    // Node removal scenario
    private Node _removeOld = null!;
    private Node _removeNew = null!;

    [GlobalSetup]
    public void Setup()
    {
        SetupSmallDom();
        SetupMediumDom();
        SetupLargeDom();
        SetupAttributeOnlyChange();
        SetupTextOnlyChange();
        SetupNodeAddition();
        SetupNodeRemoval();
    }

    private void SetupSmallDom()
    {
        _smallOld = div(
        [
            id("container"),
            class_("main")
        ],
        [
            button([type("button")], [text("Click Me")])
        ]);

        _smallNew = div(
        [
            id("container"),
            class_("main updated")
        ],
        [
            button([type("submit")], [text("Submit")]),
            span([], [text("New element")])
        ]);
    }

    private void SetupMediumDom()
    {
        // Create a medium-sized DOM tree with 15 elements
        _mediumOld = div([id("app")],
        [
            header([id("header")],
            [
                nav([id("nav")],
                [
                    ul([id("menu")],
                    [
                        li([], [a([href("/home")], [text("Home")])]),
                        li([], [a([href("/about")], [text("About")])]),
                        li([], [a([href("/contact")], [text("Contact")])])
                    ])
                ])
            ]),
            main([id("main")],
            [
                article([id("article")],
                [
                    h1([], [text("Title")]),
                    p([id("intro")], [text("Introduction paragraph")]),
                    p([id("body")], [text("Body paragraph")])
                ])
            ]),
            footer([id("footer")],
            [
                p([], [text("Copyright 2025")])
            ])
        ]);

        _mediumNew = div([id("app")],
        [
            header([id("header")],
            [
                nav([id("nav")],
                [
                    ul([id("menu")],
                    [
                        li([], [a([href("/home")], [text("Home")])]),
                        li([], [a([href("/about")], [text("About Us")])]), // Changed text
                        li([], [a([href("/contact")], [text("Contact")])]),
                        li([], [a([href("/blog")], [text("Blog")])]) // Added item
                    ])
                ])
            ]),
            main([id("main")],
            [
                article([id("article")],
                [
                    h1([], [text("Updated Title")]), // Changed text
                    p([id("intro"), class_("highlight")], [text("Introduction paragraph")]), // Added class
                    p([id("body")], [text("Updated body paragraph")]) // Changed text
                ]),
                aside([id("sidebar")], [text("New sidebar")]) // Added element
            ]),
            footer([id("footer")],
            [
                p([], [text("Copyright 2026")]) // Changed text
            ])
        ]);
    }

    private void SetupLargeDom()
    {
        // Create a large DOM tree with 100+ elements (simulating a data table)
        var oldRows = new Node[50];
        var newRows = new Node[50];
        
        for (int i = 0; i < 50; i++)
        {
            oldRows[i] = tr([id($"row-{i}")],
            [
                td([], [text($"Cell {i}-1")]),
                td([], [text($"Cell {i}-2")]),
                td([], [text($"Cell {i}-3")])
            ]);
            
            // Every 5th row has changes
            if (i % 5 == 0)
            {
                newRows[i] = tr([id($"row-{i}"), class_("updated")],
                [
                    td([], [text($"Updated {i}-1")]),
                    td([], [text($"Updated {i}-2")]),
                    td([], [text($"Updated {i}-3")])
                ]);
            }
            else
            {
                newRows[i] = tr([id($"row-{i}")],
                [
                    td([], [text($"Cell {i}-1")]),
                    td([], [text($"Cell {i}-2")]),
                    td([], [text($"Cell {i}-3")])
                ]);
            }
        }
        
        _largeOld = table([id("data-table")],
        [
            thead([],
            [
                tr([],
                [
                    th([], [text("Column 1")]),
                    th([], [text("Column 2")]),
                    th([], [text("Column 3")])
                ])
            ]),
            tbody([], oldRows)
        ]);
        
        _largeNew = table([id("data-table"), class_("updated")],
        [
            thead([],
            [
                tr([],
                [
                    th([], [text("Column 1")]),
                    th([], [text("Column 2")]),
                    th([], [text("Column 3")]),
                    th([], [text("Column 4")]) // Added column header
                ])
            ]),
            tbody([], newRows)
        ]);
    }

    private void SetupAttributeOnlyChange()
    {
        _attrOld = div(
        [
            id("container"),
            class_("old-class"),
            style("color: red;")
        ],
        [
            span([id("child")], [text("Content")])
        ]);

        _attrNew = div(
        [
            id("container"),
            class_("new-class"),
            style("color: blue;"),
            title("New title")
        ],
        [
            span([id("child")], [text("Content")])
        ]);
    }

    private void SetupTextOnlyChange()
    {
        _textOld = div([id("content")],
        [
            h1([], [text("Original Title")]),
            p([], [text("Original paragraph content that will be changed.")]),
            span([], [text("Original span text")])
        ]);

        _textNew = div([id("content")],
        [
            h1([], [text("Updated Title")]),
            p([], [text("Updated paragraph content with new text.")]),
            span([], [text("Updated span text")])
        ]);
    }

    private void SetupNodeAddition()
    {
        _addOld = ul([id("list")],
        [
            li([], [text("Item 1")]),
            li([], [text("Item 2")])
        ]);

        _addNew = ul([id("list")],
        [
            li([], [text("Item 1")]),
            li([], [text("Item 2")]),
            li([], [text("Item 3")]),
            li([], [text("Item 4")]),
            li([], [text("Item 5")])
        ]);
    }

    private void SetupNodeRemoval()
    {
        _removeOld = ul([id("list")],
        [
            li([], [text("Item 1")]),
            li([], [text("Item 2")]),
            li([], [text("Item 3")]),
            li([], [text("Item 4")]),
            li([], [text("Item 5")])
        ]);

        _removeNew = ul([id("list")],
        [
            li([], [text("Item 1")]),
            li([], [text("Item 2")])
        ]);
    }

    /// <summary>
    /// Benchmark small DOM tree diffing (2-3 elements).
    /// Represents typical component updates.
    /// </summary>
    [Benchmark]
    public void SmallDomDiff()
    {
        Operations.Diff(_smallOld, _smallNew);
    }

    /// <summary>
    /// Benchmark medium DOM tree diffing (15-20 elements).
    /// Represents page section updates.
    /// </summary>
    [Benchmark]
    public void MediumDomDiff()
    {
        Operations.Diff(_mediumOld, _mediumNew);
    }

    /// <summary>
    /// Benchmark large DOM tree diffing (150+ elements).
    /// Represents data table updates - worst case scenario.
    /// </summary>
    [Benchmark]
    public void LargeDomDiff()
    {
        Operations.Diff(_largeOld, _largeNew);
    }

    /// <summary>
    /// Benchmark attribute-only changes (no structural changes).
    /// This is the fastest diff scenario.
    /// </summary>
    [Benchmark]
    public void AttributeOnlyDiff()
    {
        Operations.Diff(_attrOld, _attrNew);
    }

    /// <summary>
    /// Benchmark text content changes only.
    /// Common scenario for dynamic content updates.
    /// </summary>
    [Benchmark]
    public void TextOnlyDiff()
    {
        Operations.Diff(_textOld, _textNew);
    }

    /// <summary>
    /// Benchmark node additions to existing tree.
    /// Tests append performance.
    /// </summary>
    [Benchmark]
    public void NodeAdditionDiff()
    {
        Operations.Diff(_addOld, _addNew);
    }

    /// <summary>
    /// Benchmark node removals from existing tree.
    /// Tests removal detection performance.
    /// </summary>
    [Benchmark]
    public void NodeRemovalDiff()
    {
        Operations.Diff(_removeOld, _removeNew);
    }
}
