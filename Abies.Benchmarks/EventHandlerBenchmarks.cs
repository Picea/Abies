using Abies.DOM;
using Abies.Html;
using BenchmarkDotNet.Attributes;
using static Abies.Html.Attributes;
using static Abies.Html.Elements;
using static Abies.Html.Events;

namespace Abies.Benchmarks;

/// <summary>
/// Benchmarks for event handler creation and registration.
/// Measures the optimized atomic counter CommandId generation.
/// </summary>
/// <remarks>
/// These benchmarks measure the OPTIMIZED handler creation performance using:
/// - Atomic counter for CommandId (instead of Guid.NewGuid().ToString())
/// - FrozenDictionary cache for event attribute names
/// 
/// Scenarios covered:
/// - Handler creation with Message (simple case)
/// - Handler creation with factory function (complex case)
/// - Bulk handler creation (simulating page load)
/// 
/// Quality gates should alert when:
/// - Memory allocations increase by >10%
/// - Gen0 collections increase
/// </remarks>
[MemoryDiagnoser]
[MarkdownExporter]
[JsonExporterAttribute.Full]
[JsonExporterAttribute.FullCompressed]
public class EventHandlerBenchmarks
{
    // Test messages
    private record TestClick : Message;
    private record TestInput(string Value) : Message;
    private record TestKey(string Key) : Message;

    private static readonly Message _clickMessage = new TestClick();
    
    // Factory functions for data-carrying handlers
    private static readonly Func<InputEventData?, Message> _inputFactory = 
        data => new TestInput(data?.Value ?? "");
    
    private static readonly Func<KeyEventData?, Message> _keyFactory = 
        data => new TestKey(data?.Key ?? "");

    // =============================================================================
    // HANDLER CREATION BENCHMARKS
    // =============================================================================

    /// <summary>
    /// Single handler with static message (most common case).
    /// Uses optimized atomic counter for CommandId generation.
    /// </summary>
    [Benchmark(Baseline = true)]
    public Handler CreateSingleHandler_Message()
    {
        return onclick(_clickMessage);
    }

    /// <summary>
    /// Single handler with factory function.
    /// Measures overhead of closure capture with optimized CommandId.
    /// </summary>
    [Benchmark]
    public Handler CreateSingleHandler_Factory()
    {
        return oninput(_inputFactory);
    }

    /// <summary>
    /// Create 10 handlers (typical interactive component).
    /// Measures bulk creation overhead.
    /// </summary>
    [Benchmark]
    public Handler[] Create10Handlers()
    {
        return
        [
            onclick(_clickMessage),
            onmouseenter(_clickMessage),
            onmouseleave(_clickMessage),
            onfocus(_clickMessage),
            onblur(_clickMessage),
            oninput(_inputFactory),
            onchange(_inputFactory),
            onkeydown(_keyFactory),
            onkeyup(_keyFactory),
            onsubmit(_clickMessage)
        ];
    }

    /// <summary>
    /// Create 50 handlers (full page with many interactive elements).
    /// Simulates Conduit home page load.
    /// </summary>
    [Benchmark]
    public Handler[] Create50Handlers()
    {
        var handlers = new Handler[50];
        for (int i = 0; i < 50; i++)
        {
            handlers[i] = i switch
            {
                < 20 => onclick(_clickMessage),
                < 35 => oninput(_inputFactory),
                < 45 => onmouseenter(_clickMessage),
                _ => onkeydown(_keyFactory)
            };
        }
        return handlers;
    }

    /// <summary>
    /// Create 100 handlers (data table with row actions).
    /// Stress test for handler creation.
    /// </summary>
    [Benchmark]
    public Handler[] Create100Handlers()
    {
        var handlers = new Handler[100];
        for (int i = 0; i < 100; i++)
        {
            handlers[i] = onclick(_clickMessage);
        }
        return handlers;
    }

    // =============================================================================
    // ELEMENT WITH HANDLERS CREATION BENCHMARKS
    // =============================================================================

    /// <summary>
    /// Create a single interactive element (button with click).
    /// </summary>
    [Benchmark]
    public Node CreateButtonWithHandler()
    {
        return button([type("button"), onclick(_clickMessage)], [text("Click me")]);
    }

    /// <summary>
    /// Create an input with multiple handlers (typical form field).
    /// </summary>
    [Benchmark]
    public Node CreateInputWithMultipleHandlers()
    {
        return input([
            type("text"),
            placeholder("Enter value"),
            oninput(_inputFactory),
            onblur(_clickMessage),
            onfocus(_clickMessage),
            onkeydown(_keyFactory)
        ]);
    }

    /// <summary>
    /// Create a form with multiple interactive elements.
    /// </summary>
    [Benchmark]
    public Node CreateFormWithHandlers()
    {
        return form([onsubmit(_clickMessage)],
        [
            div([class_("form-group")],
            [
                input([
                    type("text"),
                    name("username"),
                    oninput(_inputFactory),
                    onblur(_clickMessage)
                ])
            ]),
            div([class_("form-group")],
            [
                input([
                    type("password"),
                    name("password"),
                    oninput(_inputFactory),
                    onblur(_clickMessage)
                ])
            ]),
            button([type("submit"), onclick(_clickMessage)], [text("Login")])
        ]);
    }

    /// <summary>
    /// Create article list items with favorite buttons (Conduit pattern).
    /// </summary>
    [Benchmark]
    public Node[] CreateArticleListWithHandlers()
    {
        var articles = new Node[10];
        for (int i = 0; i < 10; i++)
        {
            articles[i] = div([class_("article-preview")],
            [
                div([class_("article-meta")],
                [
                    a([href($"/profile/user{i}"), onclick(_clickMessage)], [text($"User {i}")]),
                    button([class_("btn btn-sm"), onclick(_clickMessage)],
                    [
                        Elements.i([class_("ion-heart")], []),
                        text(" 0")
                    ])
                ]),
                a([href($"/article/slug-{i}"), onclick(_clickMessage)],
                [
                    h1([], [text($"Article {i}")]),
                    p([], [text("Preview text...")])
                ])
            ]);
        }
        return articles;
    }
}
