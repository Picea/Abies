using Abies.DOM;
using BenchmarkDotNet.Attributes;
using static Abies.Html.Attributes;
using static Abies.Html.Elements;
using static Abies.Html.Events;

namespace Abies.Benchmarks;

/// <summary>
/// Benchmarks for application startup performance.
/// Measures the critical path from initialization to first render.
/// </summary>
/// <remarks>
/// These benchmarks simulate the startup sequence of an Abies application:
/// 1. Model initialization
/// 2. Initial view rendering
/// 3. HTML string generation
/// 
/// This does NOT measure:
/// - WASM runtime download time (network dependent)
/// - .NET runtime initialization (outside Abies control)
/// - Browser DOM parsing (outside Abies control)
/// 
/// Quality gates should alert when:
/// - Initial render time increases by >10%
/// - Memory allocations increase by >15%
/// 
/// Target: Full startup path should complete in <100ms on typical hardware
/// (excluding WASM download and .NET runtime initialization)
/// </remarks>
[MemoryDiagnoser]
[MarkdownExporter]
[JsonExporterAttribute.Full]
[JsonExporterAttribute.FullCompressed]
public class StartupBenchmarks
{
    // =============================================================================
    // Test Data - Simulates typical Abies application structures
    // =============================================================================

    /// <summary>
    /// Simple counter app model (minimal startup).
    /// </summary>
    public record CounterModel(int Count);

    /// <summary>
    /// Conduit-style app model (realistic startup).
    /// </summary>
    public record ConduitModel(
        List<Article> Articles,
        List<string> Tags,
        User? CurrentUser,
        bool IsLoading,
        int CurrentPage,
        int TotalPages
    );

    public record Article(int Id, string Title, string Description, string Author, DateTime CreatedAt, int FavoritesCount, List<string> Tags);
    public record User(string Username, string Email, string Image);

    /// <summary>
    /// Benchmark app model (js-framework-benchmark style).
    /// </summary>
    public record BenchmarkModel(List<BenchmarkRow> Data, int? Selected, int NextId);
    public record BenchmarkRow(int Id, string Label);

    // Test message for event handlers
    private record TestMessage : Message;
    private static readonly Message _testMessage = new TestMessage();

    // =============================================================================
    // MINIMAL APP - Counter
    // =============================================================================

    /// <summary>
    /// Initialize a minimal counter model.
    /// Simulates the simplest possible Abies app startup.
    /// </summary>
    [Benchmark(Baseline = true)]
    public CounterModel InitializeMinimalModel()
    {
        return new CounterModel(0);
    }

    /// <summary>
    /// Render a minimal counter view.
    /// </summary>
    [Benchmark]
    public string RenderMinimalView()
    {
        var model = new CounterModel(42);
        var view = CounterView(model);
        return Render.Html(view);
    }

    private static Node CounterView(CounterModel model) =>
        div([id("app"), class_("container")], [
            h1([], [text("Counter")]),
            p([class_("count")], [text($"Count: {model.Count}")]),
            button([id("increment"), onclick(_testMessage)], [text("+")]),
            button([id("decrement"), onclick(_testMessage)], [text("-")])
        ]);

    // =============================================================================
    // REALISTIC APP - Conduit Home Page
    // =============================================================================

    /// <summary>
    /// Initialize a Conduit-style model with empty data.
    /// Simulates real-world app initialization before data loads.
    /// </summary>
    [Benchmark]
    public ConduitModel InitializeConduitModel()
    {
        return new ConduitModel(
            Articles: new List<Article>(),
            Tags: new List<string>(),
            CurrentUser: null,
            IsLoading: true,
            CurrentPage: 1,
            TotalPages: 1
        );
    }

    /// <summary>
    /// Initialize a Conduit model with preloaded data.
    /// Simulates SSR or cached initial state.
    /// </summary>
    [Benchmark]
    public ConduitModel InitializeConduitModelWithData()
    {
        var articles = new List<Article>();
        for (int i = 0; i < 10; i++)
        {
            articles.Add(new Article(
                Id: i,
                Title: $"Article Title {i}",
                Description: $"This is the preview for article {i}",
                Author: $"author{i}",
                CreatedAt: DateTime.UtcNow.AddDays(-i),
                FavoritesCount: i * 10,
                Tags: new List<string> { "tag1", "tag2", $"tag{i}" }
            ));
        }

        var tags = new List<string> { "programming", "javascript", "webdev", "dotnet", "blazor" };

        return new ConduitModel(
            Articles: articles,
            Tags: tags,
            CurrentUser: new User("testuser", "test@example.com", "https://api.example.com/avatar.png"),
            IsLoading: false,
            CurrentPage: 1,
            TotalPages: 5
        );
    }

    /// <summary>
    /// Render Conduit loading state.
    /// The initial view before data loads.
    /// </summary>
    [Benchmark]
    public string RenderConduitLoadingView()
    {
        var model = new ConduitModel(
            Articles: new List<Article>(),
            Tags: new List<string>(),
            CurrentUser: null,
            IsLoading: true,
            CurrentPage: 1,
            TotalPages: 1
        );
        var view = ConduitView(model);
        return Render.Html(view);
    }

    /// <summary>
    /// Render Conduit with full data.
    /// The complete home page after data loads.
    /// </summary>
    [Benchmark]
    public string RenderConduitFullView()
    {
        var articles = new List<Article>();
        for (int i = 0; i < 10; i++)
        {
            articles.Add(new Article(
                Id: i,
                Title: $"How to build scalable web applications with .NET {i}",
                Description: $"Learn the best practices for building modern web applications using ASP.NET Core and Blazor WebAssembly in this comprehensive guide...",
                Author: $"johndoe{i}",
                CreatedAt: DateTime.UtcNow.AddDays(-i),
                FavoritesCount: i * 15 + 42,
                Tags: new List<string> { "dotnet", "webdev", $"tutorial{i}" }
            ));
        }

        var tags = new List<string> {
            "programming", "javascript", "webdev", "dotnet", "blazor",
            "react", "angular", "vue", "typescript", "csharp"
        };

        var model = new ConduitModel(
            Articles: articles,
            Tags: tags,
            CurrentUser: new User("johndoe", "john@example.com", "https://api.example.com/avatar.png"),
            IsLoading: false,
            CurrentPage: 1,
            TotalPages: 10
        );

        var view = ConduitView(model);
        return Render.Html(view);
    }

    private static Node ConduitView(ConduitModel model) =>
        div([class_("home-page")], [
            // Banner
            div([class_("banner")], [
                div([class_("container")], [
                    h1([class_("logo-font")], [text("conduit")]),
                    p([], [text("A place to share your knowledge.")])
                ])
            ]),
            // Main content
            div([class_("container page")], [
                div([class_("row")], [
                    // Article feed
                    div([class_("col-md-9")], [
                        // Feed toggle
                        div([class_("feed-toggle")], [
                            ul([class_("nav nav-pills outline-active")], [
                                li([class_("nav-item")], [
                                    a([class_("nav-link"), onclick(_testMessage)], [text("Your Feed")])
                                ]),
                                li([class_("nav-item")], [
                                    a([class_("nav-link active")], [text("Global Feed")])
                                ])
                            ])
                        ]),
                        // Articles or loading
                        model.IsLoading
                            ? div([class_("article-preview")], [text("Loading articles...")])
                            : div([class_("article-list")], model.Articles.ConvertAll(ArticlePreview).ToArray())
                    ]),
                    // Sidebar with tags
                    div([class_("col-md-3")], [
                        div([class_("sidebar")], [
                            p([], [text("Popular Tags")]),
                            model.Tags.Count == 0
                                ? text("Loading tags...")
                                : div([class_("tag-list")], model.Tags.ConvertAll(tag =>
                                    a([href(""), class_("tag-pill tag-default"), onclick(_testMessage)], [text(tag)])
                                ).ToArray())
                        ])
                    ])
                ])
            ])
        ]);

    private static Node ArticlePreview(Article article) =>
        div([class_("article-preview")], [
            div([class_("article-meta")], [
                a([href($"/profile/{article.Author}")], [
                    img([src("https://api.example.com/avatar.png")])
                ]),
                div([class_("info")], [
                    a([href($"/profile/{article.Author}"), class_("author")], [text(article.Author)]),
                    span([class_("date")], [text(article.CreatedAt.ToString("MMMM d, yyyy"))])
                ]),
                button([class_("btn btn-outline-primary btn-sm pull-xs-right"), onclick(_testMessage)], [
                    i([class_("ion-heart")], []),
                    text($" {article.FavoritesCount}")
                ])
            ]),
            a([href($"/article/{article.Id}"), class_("preview-link")], [
                h1([], [text(article.Title)]),
                p([], [text(article.Description)]),
                span([], [text("Read more...")])
            ]),
            ul([class_("tag-list")], article.Tags.ConvertAll(tag =>
                li([class_("tag-default tag-pill tag-outline")], [text(tag)])
            ).ToArray())
        ]);

    // =============================================================================
    // BENCHMARK APP - js-framework-benchmark style
    // =============================================================================

    /// <summary>
    /// Initialize benchmark model (empty state).
    /// </summary>
    [Benchmark]
    public BenchmarkModel InitializeBenchmarkModel()
    {
        return new BenchmarkModel(
            Data: new List<BenchmarkRow>(),
            Selected: null,
            NextId: 1
        );
    }

    /// <summary>
    /// Initialize benchmark model with 1000 rows.
    /// Tests initialization with large dataset.
    /// </summary>
    [Benchmark]
    public BenchmarkModel InitializeBenchmarkModelWith1kRows()
    {
        var adjectives = new[] { "pretty", "large", "big", "small", "tall", "short", "long", "handsome", "plain", "quaint", "clean", "elegant" };
        var colours = new[] { "red", "yellow", "blue", "green", "pink", "brown", "purple", "orange", "white", "black" };
        var nouns = new[] { "table", "chair", "house", "bbq", "desk", "car", "pony", "cookie", "sandwich", "burger", "pizza", "mouse", "keyboard" };

        var random = new Random(42); // Fixed seed for reproducibility
        var data = new List<BenchmarkRow>(1000);

        for (int i = 1; i <= 1000; i++)
        {
            var label = $"{adjectives[random.Next(adjectives.Length)]} {colours[random.Next(colours.Length)]} {nouns[random.Next(nouns.Length)]}";
            data.Add(new BenchmarkRow(i, label));
        }

        return new BenchmarkModel(Data: data, Selected: null, NextId: 1001);
    }

    /// <summary>
    /// Render benchmark empty state view.
    /// </summary>
    [Benchmark]
    public string RenderBenchmarkEmptyView()
    {
        var model = new BenchmarkModel(
            Data: new List<BenchmarkRow>(),
            Selected: null,
            NextId: 1
        );
        var view = BenchmarkView(model);
        return Render.Html(view);
    }

    /// <summary>
    /// Render benchmark view with 1000 rows.
    /// This is the js-framework-benchmark "create 1k rows" operation.
    /// </summary>
    [Benchmark]
    public string RenderBenchmarkWith1kRows()
    {
        var adjectives = new[] { "pretty", "large", "big", "small", "tall", "short", "long", "handsome", "plain", "quaint", "clean", "elegant" };
        var colours = new[] { "red", "yellow", "blue", "green", "pink", "brown", "purple", "orange", "white", "black" };
        var nouns = new[] { "table", "chair", "house", "bbq", "desk", "car", "pony", "cookie", "sandwich", "burger", "pizza", "mouse", "keyboard" };

        var random = new Random(42);
        var data = new List<BenchmarkRow>(1000);

        for (int i = 1; i <= 1000; i++)
        {
            var label = $"{adjectives[random.Next(adjectives.Length)]} {colours[random.Next(colours.Length)]} {nouns[random.Next(nouns.Length)]}";
            data.Add(new BenchmarkRow(i, label));
        }

        var model = new BenchmarkModel(Data: data, Selected: null, NextId: 1001);
        var view = BenchmarkView(model);
        return Render.Html(view);
    }

    private static Node BenchmarkView(BenchmarkModel model) =>
        div([class_("container")], [
            // Jumbotron with action buttons
            div([class_("jumbotron")], [
                div([class_("row")], [
                    div([class_("col-md-6")], [
                        h1([], [text("Abies-keyed")])
                    ]),
                    div([class_("col-md-6")], [
                        div([class_("row")], [
                            BenchmarkButton("run", "Create 1,000 rows"),
                            BenchmarkButton("runlots", "Create 10,000 rows"),
                            BenchmarkButton("add", "Append 1,000 rows"),
                            BenchmarkButton("update", "Update every 10th row"),
                            BenchmarkButton("clear", "Clear"),
                            BenchmarkButton("swaprows", "Swap Rows")
                        ])
                    ])
                ])
            ]),
            // Data table
            table([class_("table table-hover table-striped test-data")], [
                tbody([id("tbody")], model.Data.ConvertAll(row =>
                    RenderBenchmarkRowElement(row, model.Selected == row.Id)
                ).ToArray())
            ]),
            span([class_("preloadicon glyphicon glyphicon-remove"), ariaHidden("true")], [])
        ]);

    private static Node BenchmarkButton(string buttonId, string label) =>
        div([class_("col-sm-6 smallpad")], [
            button([
                type("button"),
                class_("btn btn-primary btn-block"),
                id(buttonId),
                onclick(_testMessage)
            ], [
                text(label)
            ])
        ]);

    private static Node RenderBenchmarkRowElement(BenchmarkRow row, bool isSelected) =>
        tr([class_(isSelected ? "danger" : "")], [
            td([class_("col-md-1")], [text(row.Id.ToString())]),
            td([class_("col-md-4")], [
                a([class_("lbl"), onclick(_testMessage)], [text(row.Label)])
            ]),
            td([class_("col-md-1")], [
                a([onclick(_testMessage)], [
                    span([class_("glyphicon glyphicon-remove"), ariaHidden("true")], [])
                ])
            ]),
            td([class_("col-md-6")], [])
        ]);

    // =============================================================================
    // FULL STARTUP PATH - Combined measurements
    // =============================================================================

    /// <summary>
    /// Full startup path for minimal app.
    /// Initialize + Render in one operation.
    /// </summary>
    [Benchmark]
    public string FullStartupPathMinimal()
    {
        var model = new CounterModel(0);
        var view = CounterView(model);
        return Render.Html(view);
    }

    /// <summary>
    /// Full startup path for Conduit app (loading state).
    /// Initialize + Render loading view.
    /// </summary>
    [Benchmark]
    public string FullStartupPathConduitLoading()
    {
        var model = new ConduitModel(
            Articles: new List<Article>(),
            Tags: new List<string>(),
            CurrentUser: null,
            IsLoading: true,
            CurrentPage: 1,
            TotalPages: 1
        );
        var view = ConduitView(model);
        return Render.Html(view);
    }

    /// <summary>
    /// Full startup path for benchmark app (empty state).
    /// Initialize + Render empty view.
    /// </summary>
    [Benchmark]
    public string FullStartupPathBenchmarkEmpty()
    {
        var model = new BenchmarkModel(
            Data: new List<BenchmarkRow>(),
            Selected: null,
            NextId: 1
        );
        var view = BenchmarkView(model);
        return Render.Html(view);
    }
}
