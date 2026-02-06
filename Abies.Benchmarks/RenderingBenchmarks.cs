using Abies.DOM;
using Abies.Html;
using BenchmarkDotNet.Attributes;
using static Abies.Html.Attributes;
using static Abies.Html.Elements;
using static Abies.Html.Events;

namespace Abies.Benchmarks;

/// <summary>
/// Comprehensive benchmarks for Virtual DOM rendering to HTML.
/// Measures memory allocations and throughput of the Render.Html method.
/// </summary>
/// <remarks>
/// These benchmarks establish baselines for:
/// - StringBuilder pooling effectiveness
/// - HTML encoding performance (HtmlEncode fast-path potential)
/// - Event handler serialization overhead
/// - Small, medium, and large DOM tree rendering
/// 
/// Quality gates should alert when:
/// - Memory allocations increase by >10%
/// - Throughput decreases by >5%
/// </remarks>
[MemoryDiagnoser]
[MarkdownExporter]
[JsonExporterAttribute.Full]
[JsonExporterAttribute.FullCompressed]
public class RenderingBenchmarks
{
    // Baseline message for event handlers
    private record TestMessage : Message;
    private static readonly Message _testMessage = new TestMessage();

    // Simple element (no encoding needed)
    private Node _simpleElement = null!;

    // Element requiring HTML encoding
    private Node _encodingRequiredElement = null!;

    // Element with event handlers
    private Node _elementWithHandlers = null!;

    // Small page (5-10 elements)
    private Node _smallPage = null!;

    // Medium page (50 elements)
    private Node _mediumPage = null!;

    // Large page (200+ elements, simulating article list)
    private Node _largePage = null!;

    // Deep nesting (10 levels)
    private Node _deeplyNested = null!;

    // Wide tree (50 siblings)
    private Node _wideTree = null!;

    // Form with many inputs
    private Node _complexForm = null!;

    [GlobalSetup]
    public void Setup()
    {
        SetupSimpleElement();
        SetupEncodingRequiredElement();
        SetupElementWithHandlers();
        SetupSmallPage();
        SetupMediumPage();
        SetupLargePage();
        SetupDeeplyNested();
        SetupWideTree();
        SetupComplexForm();
    }

    private void SetupSimpleElement()
    {
        // No special characters - fast path for encoding
        _simpleElement = div(
            [id("simple"), class_("container")],
            [
                span([class_("label")], [text("Hello World")])
            ]);
    }

    private void SetupEncodingRequiredElement()
    {
        // Contains characters that need HTML encoding: < > & " '
        _encodingRequiredElement = div(
            [id("encoded"), class_("container")],
            [
                span([class_("code")], [text("if (x < 10 && y > 5) { return \"value\"; }")]),
                span([class_("special")], [text("Tom & Jerry's <Adventure>")]),
                p([title("Quote: \"Hello\"")], [text("Special chars: < > & \" '")])
            ]);
    }

    private void SetupElementWithHandlers()
    {
        // Multiple event handlers - tests GUID allocation
        _elementWithHandlers = div(
            [id("interactive"), class_("form-group")],
            [
                input([
                    type("text"),
                    placeholder("Enter name"),
                    oninput(_testMessage),
                    onblur(_testMessage),
                    onfocus(_testMessage)
                ]),
                button([
                    type("submit"),
                    onclick(_testMessage),
                    onmouseenter(_testMessage),
                    onmouseleave(_testMessage)
                ], [text("Submit")])
            ]);
    }

    private void SetupSmallPage()
    {
        _smallPage = div([id("app"), class_("container")],
        [
            header([id("header"), class_("nav-header")],
            [
                h1([], [text("My Application")]),
                nav([class_("main-nav")],
                [
                    a([href("/"), class_("nav-link")], [text("Home")]),
                    a([href("/about"), class_("nav-link")], [text("About")])
                ])
            ]),
            main([id("content")],
            [
                p([], [text("Welcome to the application.")])
            ])
        ]);
    }

    private void SetupMediumPage()
    {
        // Simulates a typical dashboard page
        var cards = new Node[10];
        for (int i = 0; i < 10; i++)
        {
            cards[i] = div([class_("card")],
            [
                div([class_("card-header")],
                [
                    h3([], [text($"Card {i + 1}")])
                ]),
                div([class_("card-body")],
                [
                    p([], [text($"This is the content for card {i + 1}.")]),
                    button([class_("btn"), onclick(_testMessage)], [text("Action")])
                ])
            ]);
        }

        _mediumPage = div([id("dashboard"), class_("container")],
        [
            header([class_("dashboard-header")],
            [
                h1([], [text("Dashboard")]),
                div([class_("user-info")],
                [
                    span([], [text("Welcome, User")]),
                    button([onclick(_testMessage)], [text("Logout")])
                ])
            ]),
            div([class_("card-grid")], cards)
        ]);
    }

    private void SetupLargePage()
    {
        // Simulates article list (like Conduit home page with 20 articles)
        var articles = new Node[20];
        for (int i = 0; i < 20; i++)
        {
            var tags = new Node[5];
            for (int t = 0; t < 5; t++)
            {
                tags[t] = span([class_("tag")], [text($"tag-{t}")]);
            }

            articles[i] = div([class_("article-preview")],
            [
                div([class_("article-meta")],
                [
                    a([href($"/profile/user{i}")],
                    [
                        img([src($"https://api.example.com/avatars/{i}")])
                    ]),
                    div([class_("info")],
                    [
                        a([href($"/profile/user{i}"), class_("author")], [text($"User {i}")]),
                        span([class_("date")], [text($"January {i + 1}, 2026")])
                    ]),
                    button([class_("btn btn-outline-primary btn-sm"), onclick(_testMessage)],
                    [
                        Elements.i([class_("ion-heart")], []),
                        text($" {i * 10}")
                    ])
                ]),
                a([href($"/article/slug-{i}"), class_("preview-link")],
                [
                    h1([], [text($"Article Title {i + 1}")]),
                    p([], [text($"This is the preview text for article {i + 1}. It contains a brief summary of the article content...")]),
                    span([], [text("Read more...")])
                ]),
                div([class_("tag-list")], tags)
            ]);
        }

        // Add pagination
        var pageLinks = new Node[10];
        for (int p = 0; p < 10; p++)
        {
            pageLinks[p] = li([class_("page-item")],
            [
                a([class_("page-link"), href($"?page={p + 1}"), onclick(_testMessage)], [text($"{p + 1}")])
            ]);
        }

        _largePage = div([class_("home-page")],
        [
            div([class_("banner")],
            [
                div([class_("container")],
                [
                    h1([class_("logo-font")], [text("conduit")]),
                    p([], [text("A place to share your knowledge.")])
                ])
            ]),
            div([class_("container page")],
            [
                div([class_("row")],
                [
                    div([class_("col-md-9")],
                    [
                        div([class_("feed-toggle")],
                        [
                            ul([class_("nav nav-pills outline-active")],
                            [
                                li([class_("nav-item")],
                                [
                                    a([class_("nav-link"), onclick(_testMessage)], [text("Your Feed")])
                                ]),
                                li([class_("nav-item")],
                                [
                                    a([class_("nav-link active")], [text("Global Feed")])
                                ])
                            ])
                        ]),
                        div([class_("article-list")], articles),
                        ul([class_("pagination")], pageLinks)
                    ])
                ])
            ])
        ]);
    }

    private void SetupDeeplyNested()
    {
        // 10 levels of nesting
        Node current = text("Deeply nested content");
        for (int i = 9; i >= 0; i--)
        {
            current = div([class_($"level-{i}")], [current]);
        }
        _deeplyNested = current;
    }

    private void SetupWideTree()
    {
        // 50 siblings at same level
        var children = new Node[50];
        for (int i = 0; i < 50; i++)
        {
            children[i] = span([class_($"item-{i}")], [text($"Item {i}")]);
        }
        _wideTree = div([id("wide-container")], children);
    }

    private void SetupComplexForm()
    {
        // Form with many interactive elements
        _complexForm = form([id("registration"), class_("form")],
        [
            fieldset([],
            [
                legend([], [text("Personal Information")]),
                div([class_("form-group")],
                [
                    label([for_("firstName")], [text("First Name")]),
                    input([
                        type("text"),
                        id("firstName"),
                        name("firstName"),
                        required(),
                        oninput(_testMessage),
                        onblur(_testMessage)
                    ])
                ]),
                div([class_("form-group")],
                [
                    label([for_("lastName")], [text("Last Name")]),
                    input([
                        type("text"),
                        id("lastName"),
                        name("lastName"),
                        required(),
                        oninput(_testMessage),
                        onblur(_testMessage)
                    ])
                ]),
                div([class_("form-group")],
                [
                    label([for_("email")], [text("Email")]),
                    input([
                        type("email"),
                        id("email"),
                        name("email"),
                        required(),
                        pattern("[a-z0-9._%+-]+@[a-z0-9.-]+\\.[a-z]{2,}$"),
                        oninput(_testMessage),
                        onblur(_testMessage)
                    ])
                ])
            ]),
            fieldset([],
            [
                legend([], [text("Account Settings")]),
                div([class_("form-group")],
                [
                    label([for_("password")], [text("Password")]),
                    input([
                        type("password"),
                        id("password"),
                        name("password"),
                        required(),
                        minlength("8"),
                        oninput(_testMessage)
                    ])
                ]),
                div([class_("form-group")],
                [
                    label([for_("confirmPassword")], [text("Confirm Password")]),
                    input([
                        type("password"),
                        id("confirmPassword"),
                        name("confirmPassword"),
                        required(),
                        oninput(_testMessage)
                    ])
                ]),
                div([class_("form-group")],
                [
                    label([],
                    [
                        input([type("checkbox"), name("newsletter"), onchange(_testMessage)]),
                        text(" Subscribe to newsletter")
                    ])
                ])
            ]),
            div([class_("form-actions")],
            [
                button([type("submit"), class_("btn btn-primary"), onclick(_testMessage)], [text("Register")]),
                button([type("reset"), class_("btn btn-secondary"), onclick(_testMessage)], [text("Clear")])
            ])
        ]);
    }

    // =============================================================================
    // BASELINE BENCHMARKS - Rendering
    // =============================================================================

    /// <summary>
    /// Baseline: Simple element without HTML encoding needs.
    /// This is the fast path - most common case.
    /// </summary>
    [Benchmark(Baseline = true)]
    public string RenderSimpleElement()
    {
        return Render.Html(_simpleElement);
    }

    /// <summary>
    /// Element with content requiring HTML encoding.
    /// Measures HtmlEncode overhead - target for SearchValues optimization.
    /// </summary>
    [Benchmark]
    public string RenderWithHtmlEncoding()
    {
        return Render.Html(_encodingRequiredElement);
    }

    /// <summary>
    /// Element with multiple event handlers.
    /// Measures GUID.NewGuid().ToString() allocation overhead.
    /// </summary>
    [Benchmark]
    public string RenderWithEventHandlers()
    {
        return Render.Html(_elementWithHandlers);
    }

    /// <summary>
    /// Small page rendering (5-10 elements).
    /// Typical component size.
    /// </summary>
    [Benchmark]
    public string RenderSmallPage()
    {
        return Render.Html(_smallPage);
    }

    /// <summary>
    /// Medium page rendering (50 elements).
    /// Typical dashboard view.
    /// </summary>
    [Benchmark]
    public string RenderMediumPage()
    {
        return Render.Html(_mediumPage);
    }

    /// <summary>
    /// Large page rendering (200+ elements).
    /// Worst case - full Conduit home page.
    /// </summary>
    [Benchmark]
    public string RenderLargePage()
    {
        return Render.Html(_largePage);
    }

    /// <summary>
    /// Deeply nested tree (10 levels).
    /// Tests recursive rendering overhead.
    /// </summary>
    [Benchmark]
    public string RenderDeeplyNested()
    {
        return Render.Html(_deeplyNested);
    }

    /// <summary>
    /// Wide tree (50 siblings).
    /// Tests iteration performance.
    /// </summary>
    [Benchmark]
    public string RenderWideTree()
    {
        return Render.Html(_wideTree);
    }

    /// <summary>
    /// Complex form with many interactive elements.
    /// Combined stress test for attributes, handlers, and structure.
    /// </summary>
    [Benchmark]
    public string RenderComplexForm()
    {
        return Render.Html(_complexForm);
    }
}
