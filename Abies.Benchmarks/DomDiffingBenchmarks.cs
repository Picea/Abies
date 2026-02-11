using Abies.DOM;
using BenchmarkDotNet.Attributes;
using static Abies.Html.Attributes;
using static Abies.Html.Elements;

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

/// <summary>
/// Benchmarks specifically for keyed diffing operations.
/// Simulates the js-framework-benchmark swap scenario.
/// </summary>
[MemoryDiagnoser]
[MarkdownExporter]
[JsonExporterAttribute.Full]
public class KeyedDiffingBenchmarks
{
    private Node _swap1kOld = null!;
    private Node _swap1kNew = null!;
    private Node _reverse1kOld = null!;
    private Node _reverse1kNew = null!;
    private Node _shuffle1kOld = null!;
    private Node _shuffle1kNew = null!;

    [GlobalSetup]
    public void Setup()
    {
        SetupSwap1k();
        SetupReverse1k();
        SetupShuffle1k();
    }

    private void SetupSwap1k()
    {
        // Create 1000 rows, then swap positions 1 and 998 (like js-framework-benchmark)
        var oldRows = new Node[1000];
        var newRows = new Node[1000];

        for (int i = 0; i < 1000; i++)
        {
            oldRows[i] = CreateRow(i, false);
            newRows[i] = CreateRow(i, false);
        }

        // Swap positions 1 and 998
        (newRows[1], newRows[998]) = (newRows[998], newRows[1]);

        _swap1kOld = tbody([id("tbody")], oldRows);
        _swap1kNew = tbody([id("tbody")], newRows);
    }

    private void SetupReverse1k()
    {
        // Create 1000 rows, then reverse the order
        var oldRows = new Node[1000];
        var newRows = new Node[1000];

        for (int i = 0; i < 1000; i++)
        {
            oldRows[i] = CreateRow(i, false);
            newRows[999 - i] = CreateRow(i, false);
        }

        _reverse1kOld = tbody([id("tbody")], oldRows);
        _reverse1kNew = tbody([id("tbody")], newRows);
    }

    private void SetupShuffle1k()
    {
        // Create 1000 rows, then shuffle them
        var oldRows = new Node[1000];
        var newRows = new Node[1000];
        var indices = Enumerable.Range(0, 1000).ToArray();
        var rng = new Random(42); // Fixed seed for reproducibility

        for (int i = 0; i < 1000; i++)
        {
            oldRows[i] = CreateRow(i, false);
        }

        // Fisher-Yates shuffle
        for (int i = indices.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        for (int i = 0; i < 1000; i++)
        {
            newRows[i] = CreateRow(indices[i], false);
        }

        _shuffle1kOld = tbody([id("tbody")], oldRows);
        _shuffle1kNew = tbody([id("tbody")], newRows);
    }

    private static Node CreateRow(int rowId, bool isSelected) =>
        tr([class_(isSelected ? "danger" : ""), id($"row-{rowId}")],
        [
            td([class_("col-md-1")], [text(rowId.ToString())]),
            td([class_("col-md-4")],
            [
                a([class_("lbl")], [text($"Label {rowId}")])
            ]),
            td([class_("col-md-1")],
            [
                a([],
                [
                    span([class_("glyphicon glyphicon-remove")], [])
                ])
            ]),
            td([class_("col-md-6")], [])
        ]);

    /// <summary>
    /// Benchmark swap of 2 rows in 1000 (js-framework-benchmark 05_swap1k).
    /// Tests keyed diffing with LIS optimization.
    /// </summary>
    [Benchmark(Baseline = true)]
    public void Swap1k()
    {
        Operations.Diff(_swap1kOld, _swap1kNew);
    }

    /// <summary>
    /// Benchmark reverse of 1000 rows.
    /// Worst case for keyed diffing - maximum reordering.
    /// </summary>
    [Benchmark]
    public void Reverse1k()
    {
        Operations.Diff(_reverse1kOld, _reverse1kNew);
    }

    /// <summary>
    /// Benchmark random shuffle of 1000 rows.
    /// Tests keyed diffing with random reordering.
    /// </summary>
    [Benchmark]
    public void Shuffle1k()
    {
        Operations.Diff(_shuffle1kOld, _shuffle1kNew);
    }
}

/// <summary>
/// Benchmarks for dictionary operations with string vs int keys.
/// Used to measure the overhead of string hashing in keyed diffing.
/// </summary>
[MemoryDiagnoser]
[MarkdownExporter]
public class DictionaryKeyBenchmarks
{
    private const int ElementCount = 1000;

    private string[] _stringKeys = null!;
    private int[] _intKeys = null!;
    private Dictionary<string, int> _stringDict = null!;
    private Dictionary<int, int> _intDict = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create keys like those used in js-framework-benchmark
        _stringKeys = new string[ElementCount];
        _intKeys = new int[ElementCount];

        for (int i = 0; i < ElementCount; i++)
        {
            _stringKeys[i] = $"lazy-row-{i}";
            _intKeys[i] = i;
        }

        // Pre-populate dictionaries for lookup benchmarks
        _stringDict = new Dictionary<string, int>(ElementCount);
        _intDict = new Dictionary<int, int>(ElementCount);

        for (int i = 0; i < ElementCount; i++)
        {
            _stringDict[_stringKeys[i]] = i;
            _intDict[_intKeys[i]] = i;
        }
    }

    /// <summary>
    /// Building a dictionary with string keys (current approach).
    /// </summary>
    [Benchmark(Baseline = true)]
    public Dictionary<string, int> BuildStringKeyDict()
    {
        var dict = new Dictionary<string, int>(ElementCount);
        for (int i = 0; i < ElementCount; i++)
        {
            dict[_stringKeys[i]] = i;
        }
        return dict;
    }

    /// <summary>
    /// Building a dictionary with int keys (optimized approach).
    /// </summary>
    [Benchmark]
    public Dictionary<int, int> BuildIntKeyDict()
    {
        var dict = new Dictionary<int, int>(ElementCount);
        for (int i = 0; i < ElementCount; i++)
        {
            dict[_intKeys[i]] = i;
        }
        return dict;
    }

    /// <summary>
    /// Lookup in dictionary with string keys.
    /// </summary>
    [Benchmark]
    public int LookupStringKeys()
    {
        int sum = 0;
        for (int i = 0; i < ElementCount; i++)
        {
            sum += _stringDict[_stringKeys[i]];
        }
        return sum;
    }

    /// <summary>
    /// Lookup in dictionary with int keys.
    /// </summary>
    [Benchmark]
    public int LookupIntKeys()
    {
        int sum = 0;
        for (int i = 0; i < ElementCount; i++)
        {
            sum += _intDict[_intKeys[i]];
        }
        return sum;
    }
}
