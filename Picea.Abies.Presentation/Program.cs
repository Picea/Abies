// =============================================================================
// Abies — Conference Presentation
// =============================================================================
// A self-hosting Abies application that presents the framework itself.
// Demonstrates MVU architecture, virtual DOM, subscriptions, and commands
// by using the very patterns it explains.
//
// Single presentation mode:
//   Deck — ~18 slides, condensed (≈30 min)
// =============================================================================

using System.Runtime.Versioning;
using Picea;
using Picea.Abies;
using Picea.Abies.Browser;
using Picea.Abies.DOM;
using Picea.Abies.Html;
using Picea.Abies.Subscriptions;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Events;
using static Picea.Abies.Subscriptions.SubscriptionModule;

[assembly: SupportedOSPlatform("browser")]

await Runtime.Run<Presentation, Model, Arguments>(new Arguments());

// ─── Data types ─────────────────────────────────────────────────────────────────

public record Arguments;

public enum SlideKind { Intro, Concept, Demo, Outro }

public enum PresentationMode { Menu, Deck }

public record Slide(
    string Id,
    string Kicker,
    string Title,
    string Subtitle,
    string[] Points,
    string? MarkdownBody = null,
    string? Code = null,
    string? Callout = null,
    string? Takeaway = null,
    string? NextStep = null,
    SlideKind Kind = SlideKind.Concept);

public record Model(
    PresentationMode Mode,
    int SlideIndex,
    int DemoCount,
    int TickCount,
    DateTimeOffset? LastTick,
    bool DemoTimer,
    bool TrackMouse,
    int MouseX,
    int MouseY,
    IReadOnlyList<string> Log,
    DateTimeOffset? LastMouseLogAt,
    string DemoInput);

// ─── Messages ───────────────────────────────────────────────────────────────────

public interface Message : Picea.Abies.Message
{
    record SelectPresentation : Message;
    record BackToMenu : Message;
    record NextSlide : Message;
    record PrevSlide : Message;
    record GoToSlide(int Index) : Message;
    record KeyPressed(KeyEventData? Data) : Message;
    record IncrementDemo : Message;
    record ResetDemo : Message;
    record ToggleDemoTimer : Message;
    record Tick(DateTimeOffset At) : Message;
    record ToggleMouse : Message;
    record MouseMoved(int X, int Y) : Message;
    record ClearLog : Message;
    record DemoInputChanged(string Value) : Message;
    record NoOp : Message;
}

// ─── Application ────────────────────────────────────────────────────────────────

public class Presentation : Program<Model, Arguments>
{
    public static (Model, Command) Initialize(Arguments _) =>
        (new Model(PresentationMode.Menu, 0, 0, 0, null, false, false, 0, 0, [], null, ""), Commands.None);

    public static (Model, Command) Transition(Model model, Picea.Abies.Message message) => message switch
    {
        Message.SelectPresentation => (SetSlide(model with
        {
            Mode = PresentationMode.Deck,
            SlideIndex = 0,
            Log = [],
            DemoCount = 0,
            TickCount = 0,
            LastTick = null,
            DemoTimer = false,
            TrackMouse = false,
            DemoInput = ""
        }, 0), Navigation.ReplaceUrl(SlideUrl(0))),
        Message.BackToMenu => ResetToMenu(model),
        Message.NextSlide => SetSlideWithNavigation(model, model.SlideIndex + 1),
        Message.PrevSlide => SetSlideWithNavigation(model, model.SlideIndex - 1),
        Message.GoToSlide m => SetSlideWithNavigation(model, m.Index),
        Message.KeyPressed m => HandleKeyPress(model, m.Data),
        Message.IncrementDemo => (AddLog(model with { DemoCount = model.DemoCount + 1 }, $"Count → {model.DemoCount + 1}"), Commands.None),
        Message.ResetDemo => (AddLog(model with { DemoCount = 0 }, "Counter reset"), Commands.None),
        Message.ToggleDemoTimer => (AddLog(model with { DemoTimer = !model.DemoTimer, TickCount = 0, LastTick = null }, model.DemoTimer ? "Timer stopped" : "Timer started"), Commands.None),
        Message.Tick m => (model with { TickCount = model.TickCount + 1, LastTick = m.At }, Commands.None),
        Message.ToggleMouse => (AddLog(model with { TrackMouse = !model.TrackMouse, LastMouseLogAt = null }, model.TrackMouse ? "Mouse tracking off" : "Mouse tracking on"), Commands.None),
        Message.MouseMoved m => UpdateMouse(model, m.X, m.Y),
        Message.ClearLog => (model with { Log = [] }, Commands.None),
        Message.DemoInputChanged m => (AddLog(model with { DemoInput = m.Value }, $"Input: \"{m.Value}\""), Commands.None),
        UrlChanged m => HandleUrlChanged(model, m.Url),
        _ => (model, Commands.None)
    };

    public static Result<Picea.Abies.Message[], Picea.Abies.Message> Decide(Model _, Picea.Abies.Message command) =>
        Result<Picea.Abies.Message[], Picea.Abies.Message>.Ok([command]);

    public static bool IsTerminal(Model _) => false;

    public static Subscription Subscriptions(Model model)
    {
        var subs = new List<Subscription>();

        subs.Add(Navigation.UrlChanges(url => new UrlChanged(url)));

        if (model.DemoTimer)
        {
            subs.Add(Every(TimeSpan.FromSeconds(1), () => new Message.Tick(DateTimeOffset.UtcNow)));
        }

        return subs.Count > 0 ? Batch(subs.ToArray()) : None;
    }

    // ── View ──

    public static Document View(Model model) => model.Mode switch
    {
        PresentationMode.Menu => new Document("Abies — Presentations", ViewMenu(model)),
        _ => ViewDeck(model)
    };

    private static Node ViewMenu(Model _) =>
        div([class_("app")],
        [
            div([class_("menu")],
            [
                div([class_("menu-header")],
                [
                    img([class_("menu-logo"), src("abies-logo.png"), width("80"), height("80")]),
                    h1([], [text("Abies")]),
                    p([class_("subtitle")], [text("Model-View-Update for .NET WebAssembly")])
                ]),
                input([
                    class_("keyboard-capture"),
                    ariaLabel("Keyboard navigation"),
                    autofocus(),
                    onkeydown(data => new Message.KeyPressed(data))
                ]),
                div([class_("presentation-cards")],
                [
                    button([type("button"), class_("presentation-card"), autofocus(), onclick(new Message.SelectPresentation())],
                    [
                        div([class_("card-icon")], [text("\U0001F332")]),
                        h2([], [text("Coderen met AI in 2026")]),
                        p([class_("card-desc")], [text("Het complete deck voor deze presentatie met workflow, praktijkervaring en live demo.")]),
                        div([class_("card-meta")],
                        [
                            span([class_("pill")], [text($"{_expressSlides.Length} slides")]),
                            span([class_("pill")], [text("\u2248 30 min")])
                        ])
                    ])
                ])
            ])
        ]);

    private static Document ViewDeck(Model model)
    {
        var slides = _expressSlides;
        var current = slides[model.SlideIndex];
        var progress = slides.Length > 1 ? (double)model.SlideIndex / (slides.Length - 1) * 100 : 0;
        var modeLabel = "Presentatie";

        return new Document(
            $"Abies — {current.Title}",
            div([class_("app"), tabindex("0")],
            [
                input([
                    class_("keyboard-capture"),
                    ariaLabel("Slide keyboard navigation"),
                    autofocus(),
                    onkeydown(data => new Message.KeyPressed(data))
                ]),
                div([class_("topbar")],
                [
                    div([class_("brand")],
                    [
                        img([class_("brand-logo"), src("abies-logo.png"), width("40"), height("40")]),
                        div([class_("brand-meta")],
                        [
                            div([class_("brand-line")],
                            [
                                span([class_("brand-wordmark")], [text("Abies")]),
                                span([class_("pill brand-badge")], [text(modeLabel)])
                            ]),
                            span([class_("brand-subtitle")], [text("Model-View-Update for .NET WebAssembly")])
                        ])
                    ]),
                    div([class_("topbar-actions")],
                    [
                        span([class_("pill")], [text($"{model.SlideIndex + 1} / {slides.Length}")]),
                        button([class_("ghost"), autofocus(), onclick(new Message.BackToMenu())], [text("\u2630 Menu")]),
                        div([class_("keys")],
                        [
                            span([class_("key")], [text("\u2190")]),
                            span([class_("key")], [text("\u2192")]),
                            span([class_("key")], [text("Space")])
                        ])
                    ])
                ]),
                div([class_("progress")],
                [
                    div([class_("progress-bar"), style($"width:{progress:F1}%")], [])
                ]),
                div([class_("deck")],
                [
                    nav([class_("agenda")],
                    [
                        h3([], [text("Agenda")]),
                        ul([],
                            slides.Select((s, i) =>
                                li([], [
                                    button([type("button"),
                                        class_(i == model.SlideIndex ? "agenda-link active" : "agenda-link"),
                                        onclick(new Message.GoToSlide(i)),
                                        ariaCurrent(i == model.SlideIndex ? "true" : "false")],
                                [
                                    span([class_("agenda-index")], [text($"{i + 1:D2}")]),
                                    span([class_("agenda-title")], [text(s.Title)])
                                ], id: $"agenda-{s.Id}")
                                ])
                            ).ToArray()
                        )
                    ]),
                    div([class_("content")],
                    [
                        div([class_("content-grid")],
                        [
                            ViewSlide(current),
                            current.Kind == SlideKind.Demo
                                ? ViewDemoPanel(model)
                                : ViewTakeawayPanel(current)
                        ])
                    ])
                ])
            ]));
    }

    private static Node ViewSlide(Slide current)
    {
        var children = new List<Node>
        {
            span([class_("kicker")], [text(current.Kicker)]),
            h1([], [text(current.Title)])
        };

        if (current.Subtitle.Length > 0)
        {
            children.Add(p([class_("subtitle")], [text(current.Subtitle)]));
        }

        if (!string.IsNullOrWhiteSpace(current.MarkdownBody))
        {
            children.Add(MarkdownView.Render(current.MarkdownBody, current.Id));
        }
        else if (current.Points.Length > 0)
        {
            children.Add(ul([class_("points")], RenderPoints(current.Points)));
        }

        if (current.Code is not null)
        {
            children.Add(div([class_("code-block")],
            [
                div([class_("code-title")], [text("C#")]),
                pre([], [text(current.Code)])
            ]));
        }

        if (current.Callout is not null)
        {
            children.Add(div([class_("callout")], [text(current.Callout)]));
        }
        return div([class_("slide")], children.ToArray());
    }

    private static Node ViewTakeawayPanel(Slide slide)
    {
        var body = new List<Node>
        {
            div([class_("takeaway")], [text(slide.Takeaway ?? "\u2014")])
        };

        if (slide.NextStep is not null)
        {
            body.Add(span([class_("next-step-title")], [text("Up Next")]));
            body.Add(div([class_("next-step")], [text(slide.NextStep)]));
        }

        return div([class_("panel takeaway-panel")],
        [
            span([class_("panel-title")], [text("Key Takeaway")]),
            div([class_("panel-body")], body.ToArray())
        ]);
    }

    private static Node ViewDemoPanel(Model model) =>
        div([class_("panel")],
        [
            span([class_("panel-title")], [text("Live MVU Loop")]),
            div([class_("panel-body")],
            [
                div([class_("demo-metrics")],
                [
                    div([class_("demo-metric")],
                    [
                        span([class_("metric-label")], [text("Counter")]),
                        span([class_("metric-value")], [text(model.DemoCount.ToString())])
                    ]),
                    div([class_("demo-metric")],
                    [
                        span([class_("metric-label")], [text("Timer ticks")]),
                        span([class_("metric-value")], [text(model.TickCount.ToString())])
                    ]),
                    div([class_("demo-metric")],
                    [
                        span([class_("metric-label")], [text("Mouse")]),
                        span([class_("metric-value")], [text($"{model.MouseX}, {model.MouseY}")])
                    ])
                ]),
                div([class_("demo-actions")],
                [
                    button([class_("primary"), onclick(new Message.IncrementDemo())], [text("+1")]),
                    button([onclick(new Message.ResetDemo())], [text("Reset")])
                ]),
                input([placeholder("Type to dispatch messages..."),
                       value(model.DemoInput),
                       oninput(e => new Message.DemoInputChanged(e?.Value ?? ""))]),
                div([class_("toggle-row")],
                [
                    button([class_(model.DemoTimer ? "toggle active" : "toggle"),
                            onclick(new Message.ToggleDemoTimer())],
                           [text(model.DemoTimer ? "\u23F8 Timer" : "\u25B6 Timer")]),
                    button([class_(model.TrackMouse ? "toggle active" : "toggle"),
                            onclick(new Message.ToggleMouse())],
                           [text(model.TrackMouse ? "\U0001F5B1 On" : "\U0001F5B1 Off")])
                ]),
                div([class_("log")],
                [
                    div([class_("log-header")],
                    [
                        span([], [text("Event Log")]),
                        button([class_("ghost"), onclick(new Message.ClearLog())], [text("Clear")])
                    ]),
                    model.Log.Count > 0
                        ? ul([], model.Log.Select((entry, i) => li([], [text(entry)], id: $"log-{i}")).ToArray())
                        : p([class_("log-empty")], [text("Interact to see messages flow...")])
                ])
            ])
        ]);

    // ── Helpers ──

    private static (Model, Command) HandleKeyPress(Model model, KeyEventData? data)
    {
        if (data is null)
        {
            return (model, Commands.None);
        }

        if (model.Mode == PresentationMode.Menu)
        {
            return data.Key switch
            {
                "Enter" or " " => Transition(model, new Message.SelectPresentation()),
                _ => (model, Commands.None)
            };
        }

        return data.Key switch
        {
            "ArrowRight" or " " or "l" or "j" => SetSlideWithNavigation(model, model.SlideIndex + 1),
            "ArrowLeft" or "h" or "k" => SetSlideWithNavigation(model, model.SlideIndex - 1),
            "Home" => SetSlideWithNavigation(model, 0),
            "End" => SetSlideWithNavigation(model, int.MaxValue),
            "Escape" => ResetToMenu(model),
            _ => (model, Commands.None)
        };
    }

    private static Model SetSlide(Model model, int index)
    {
        var slides = _expressSlides;
        return model with { SlideIndex = ClampSlide(index, slides.Length) };
    }

    private static (Model, Command) SetSlideWithNavigation(Model model, int index)
    {
        var updated = SetSlide(model, index);
        return (updated, Navigation.ReplaceUrl(SlideUrl(updated.SlideIndex)));
    }

    private static int ClampSlide(int index, int count) => Math.Clamp(index, 0, count - 1);

    private static bool TryGetSlideIndex(Url url, Slide[] slides, out int index)
    {
        index = 0;
        if (!url.Fragment.IsSome)
        {
            return false;
        }

        var frag = url.Fragment.Value;
        if (!frag.StartsWith("slide-"))
        {
            return false;
        }

        if (!int.TryParse(frag["slide-".Length..], out var parsed))
        {
            return false;
        }

        index = ClampSlide(parsed, slides.Length);
        return true;
    }

    private static (Model, Command) HandleUrlChanged(Model model, Url url)
    {
        if (TryGetSlideIndex(url, _expressSlides, out var index))
        {
            return (SetSlide(model with { Mode = PresentationMode.Deck }, index), Commands.None);
        }

        if (model.Mode == PresentationMode.Deck && !url.Fragment.IsSome)
        {
            return ResetToMenu(model);
        }

        return (model, Commands.None);
    }

    private static (Model, Command) ResetToMenu(Model model) =>
        (model with
        {
            Mode = PresentationMode.Menu,
            SlideIndex = 0,
            Log = [],
            DemoCount = 0,
            TickCount = 0,
            LastTick = null,
            DemoTimer = false,
            TrackMouse = false,
            DemoInput = ""
        }, Navigation.ReplaceUrl(Url.Root));

    private static Url SlideUrl(int index) =>
        new Url(Array.Empty<string>(), new Dictionary<string, string>(), Option.Some($"slide-{index}"));

    private static bool IsGraphLine(string text) =>
        text.StartsWith("  ", StringComparison.Ordinal) ||
        text.Contains('┤') ||
        text.Contains('╭') ||
        text.Contains('╯') ||
        text.Contains('│') ||
        text.Contains('┴');

    private static Node[] RenderPoints(string[] points)
    {
        var nodes = new List<Node>();
        var graphBuffer = new List<string>();
        var pointIndex = 0;

        void FlushGraphBuffer()
        {
            if (graphBuffer.Count == 0)
            {
                return;
            }

            nodes.Add(li([class_("point-graph-block")],
            [
                pre([class_("point-graph-pre")], [text(string.Join("\n", graphBuffer))])
            ], id: $"point-{pointIndex++}"));
            graphBuffer.Clear();
        }

        foreach (var point in points)
        {
            if (IsGraphLine(point))
            {
                graphBuffer.Add(point);
                continue;
            }

            FlushGraphBuffer();

            if (string.IsNullOrWhiteSpace(point))
            {
                nodes.Add(li([class_("point-spacer")], [], id: $"point-{pointIndex++}"));
                continue;
            }

            if (point.Contains('\n'))
            {
                nodes.Add(li([class_("point-multiline")],
                [
                    pre([class_("point-multiline-pre")], [text(point)])
                ], id: $"point-{pointIndex++}"));
                continue;
            }

            nodes.Add(li([], [text(point)], id: $"point-{pointIndex++}"));
        }

        FlushGraphBuffer();
        return nodes.ToArray();
    }

    private static Model AddLog(Model model, string entry)
    {
        var log = model.Log.Prepend(entry).Take(50).ToList();
        return model with { Log = log };
    }

    private static (Model, Command) UpdateMouse(Model model, int x, int y)
    {
        var now = DateTimeOffset.UtcNow;
        var updated = model with { MouseX = x, MouseY = y };
        if (model.LastMouseLogAt is null || (now - model.LastMouseLogAt.Value).TotalMilliseconds > 200)
        {
            return (AddLog(updated with { LastMouseLogAt = now }, $"Mouse ({x}, {y})"), Commands.None);
        }

        return (updated, Commands.None);
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // FULL PRESENTATION — Deep-Dive (~36 slides)
    // ═════════════════════════════════════════════════════════════════════════════

    private static readonly Slide[] _fullSlides =
    [
        new("intro", "Welcome", "Abies",
            "Pure functional UI for .NET WebAssembly",
            ["Model-View-Update architecture brought to .NET 10",
             "Zero JavaScript — C# all the way down",
             "Built on the same principles as Elm, adapted for the .NET ecosystem",
             "This presentation is itself an Abies app — dogfooding in action"],
            Kind: SlideKind.Intro,
            Takeaway: "Abies brings the proven MVU pattern to .NET WebAssembly, offering a principled alternative to component-based frameworks."),

        new("why-mvu", "Section 1", "Why MVU?",
            "The case for a different architecture",
            ["Component-based UIs scatter state across a tree of mutable objects",
             "Shared mutable state leads to unpredictable bugs, race conditions, stale closures",
             "MVU: one model, one update function, one render — total predictability",
             "Proven at scale by Elm, SwiftUI, Jetpack Compose, and Flutter"],
            Takeaway: "MVU eliminates entire categories of bugs by making state changes explicit and centralized.",
            NextStep: "Let's trace the architecture back to its origins."),

        new("elm-architecture", "Section 1", "The Elm Architecture",
            "Where it all comes from",
            ["Elm (2012) pioneered MVU for the browser — no runtime exceptions in production",
             "Model: single immutable state tree — the source of truth",
             "Update: pure function (Message, Model) to Model — deterministic transitions",
             "View: pure function Model to HTML — a snapshot of the UI at any point in time",
             "Commands and Subscriptions: controlled side-effects without breaking purity"],
            Takeaway: "The Elm Architecture proved that pure functional UI is practical, performant, and delightful.",
            NextStep: "How Abies adapts these ideas for .NET."),

        new("abies-overview", "Section 1", "Abies for .NET",
            ".NET 10 - WebAssembly - Apache 2.0",
            ["Targets .NET 10 WebAssembly — runs natively in the browser via Blazor WASM host",
             "Pure C# — no Razor syntax, no JSX, no templating language",
             "Source-generated compile-time unique IDs (Praefixum) for stable DOM diffing",
             "Type-safe HTML DSL — if it compiles, it renders",
             "OpenTelemetry integration — traces from browser to backend"],
            Takeaway: "Abies is a production-grade MVU framework, not a toy — it targets the latest .NET with real observability.",
            NextStep: "Time to look at the core MVU loop in detail."),

        new("mvu-loop", "Section 1", "The MVU Loop",
            "Model to View to Update, repeat",
            ["1. Initialize: create the initial Model and optional startup Commands",
             "2. View: render the Model to a virtual DOM tree — pure function, no side effects",
             "3. User interaction dispatches a Message",
             "4. Update: takes (Message, Model) and returns (Model, Command) — still pure",
             "5. Runtime applies the new Model, diffs the virtual DOM, patches the real DOM",
             "6. Commands execute asynchronously, dispatching result Messages back into the loop"],
            Code: "public interface Program<TModel, TArgument>\n{\n    static abstract (TModel, Command) Initialize(TArgument argument);\n    static abstract Result<Message[], Message> Decide(TModel state, Message command);\n    static abstract (TModel, Command) Transition(TModel state, Message @event);\n    static abstract bool IsTerminal(TModel state);\n    static abstract Document View(TModel model);\n    static abstract Subscription Subscriptions(TModel model);\n}",
            Takeaway: "The entire application is defined by six static functions — no inheritance, no lifecycle hooks, no hidden state.",
            NextStep: "Let's explore the Model."),

        new("model-records", "Section 2 - Model", "Immutable Records",
            "C# records as the state container",
            ["C# records give us value equality, with-expressions, and deconstruction for free",
             "The model is a single immutable tree — every update returns a new instance",
             "No mutation means no defensive copying, no stale references, no race conditions",
             "Nested records compose naturally — Model to Page to Form to Field"],
            Code: "record Model(\n    PresentationMode Mode,\n    int SlideIndex,\n    int DemoCount,\n    bool DemoTimer,\n    IReadOnlyList<string> Log);",
            Takeaway: "Records + immutability = a state container that is trivially testable and debuggable.",
            NextStep: "The Model changes only through Messages."),

        new("messages", "Section 2 - Model", "Messages",
            "Describing what happened",
            ["Messages are sealed records implementing a marker interface",
             "Each message describes an event — not an action to take",
             "Pattern matching in Update handles every case exhaustively",
             "New features = new message types — existing code does not change"],
            Code: "interface Message : Abies.Message\n{\n    record NextSlide : Message;\n    record GoToSlide(int Index) : Message;\n    record KeyPressed(KeyEventData Data) : Message;\n    record IncrementDemo : Message;\n    record ToggleDemoTimer : Message;\n    record Tick(DateTimeOffset At) : Message;\n}",
            Takeaway: "Messages are data, not behavior — the Update function decides what to do with them.",
            NextStep: "Now let's look at the Update function."),

        new("update-function", "Section 2 - Model", "The Update Function",
            "Pure state transitions",
            ["Takes a Message and the current Model, returns the new Model + optional Command",
             "Pattern matching via C# switch expressions — exhaustive and concise",
             "No I/O, no async, no side effects — purely transforming data",
             "Easy to test: given this model and this message, assert the result"],
            Code: "public static (Model, Command) Update(Message msg, Model model)\n    => msg switch\n{\n    Message.NextSlide\n        => (SetSlide(model, model.SlideIndex + 1), Commands.None),\n    Message.IncrementDemo\n        => (model with { DemoCount = model.DemoCount + 1 }, Commands.None),\n    Message.Tick m\n        => (model with { TickCount = model.TickCount + 1 }, Commands.None),\n    _ => (model, Commands.None)\n};",
            Takeaway: "Update is the beating heart of MVU — a single pure function that governs all state transitions.",
            NextStep: "From state to pixels — the View."),

        new("demo-mvu", "Section 2 - Model", "Live Demo",
            "See the MVU loop in action",
            ["Use the panel on the right to interact with a live MVU loop",
             "Click +1 to dispatch IncrementDemo then Update then View then DOM patch",
             "Start the timer to see subscriptions firing Tick messages every second",
             "Track the mouse to see OnMouseMove subscriptions dispatching coordinates",
             "Type in the input to see DemoInputChanged flowing through Update",
             "Watch the event log — every interaction is a message, every message produces a new model"],
            Kind: SlideKind.Demo,
            Takeaway: "This panel is running the exact same MVU loop that powers the entire presentation."),

        new("html-dsl", "Section 3 - View", "Type-Safe HTML",
            "A C# DSL for the DOM",
            ["HTML elements as C# functions: div(), span(), ul(), li(), h1(), etc.",
             "Attributes: class_(), href(), src(), style(), onclick(), oninput()",
             "No strings, no templates, no Razor — compile-time checked",
             "Composes with LINQ, pattern matching, and all of C# expression power"],
            Code: "div([class_(\"slide\")],\n[\n    span([class_(\"kicker\")], [text(\"Section 3\")]),\n    h1([], [text(\"Type-Safe HTML\")]),\n    ul([class_(\"points\")],\n        points.Select(p => li([], [text(p)])).ToArray())\n]);",
            Takeaway: "The view is just a function — if it compiles, it produces valid HTML. No runtime template errors.",
            NextStep: "How Abies efficiently updates the real DOM."),

        new("virtual-dom", "Section 3 - View", "Virtual DOM",
            "Efficient diffing and patching",
            ["View returns a virtual DOM tree — a lightweight in-memory representation",
             "The runtime diffs the previous tree against the new one",
             "Only the differences are applied to the real browser DOM",
             "Praefixum generates compile-time unique IDs for every element — stable identity across renders"],
            Takeaway: "Virtual DOM gives us the simplicity of re-render everything with the performance of surgical updates.",
            NextStep: "The diffing algorithm in detail."),

        new("keyed-diffing", "Section 3 - View", "Keyed Diffing",
            "LIS algorithm - head/tail skip - O(n) fast path",
            ["Head/tail skip: matching elements at the start and end are skipped in O(1)",
             "Longest Increasing Subsequence (LIS): minimizes DOM moves for reordered lists",
             "Keyed by Praefixum IDs: compile-time stable, no runtime key generation",
             "Fallback: unkeyed children use positional diffing with type matching",
             "Result: optimal patch sets even for complex list reorderings"],
            Takeaway: "LIS + head/tail skip means Abies handles list updates as efficiently as React or Svelte.",
            NextStep: "Getting those patches to the browser."),

        new("binary-batching", "Section 3 - View", "Binary Batching Protocol",
            "Minimizing interop overhead",
            ["DOM patches are serialized into a compact binary format, not individual JS interop calls",
             "Batch operations: SetAttribute, RemoveAttribute, InsertBefore, RemoveChild, SetText, etc.",
             "Single ArraySegment<byte> crossed over the interop boundary per render cycle",
             "JavaScript decoder applies the entire batch in one synchronous pass",
             "Inspired by Blazor RenderBatch — adapted for Abies virtual DOM model"],
            Code: "// One interop call per render cycle\nenum OpCode : byte\n{\n    SetAttribute = 1, RemoveAttribute = 2,\n    InsertBefore = 3, RemoveChild = 4,\n    MoveChild = 5, SetText = 6,\n    ClearChildren = 7\n}",
            Takeaway: "Binary batching reduces interop overhead to near zero — one call per frame, not one per DOM mutation.",
            NextStep: "Caching renders for even less work."),

        new("memoization", "Section 3 - View", "Memoization",
            "Skip renders you don't need",
            ["Memo<TKey> wraps a subtree with a cache key — if the key has not changed, reuse the cached Node",
             "View cache avoids re-running View functions for unchanged parts of the model",
             "Leverages record value equality — deep comparison is free",
             "Particularly effective for large lists and complex nested views",
             "Zero-allocation fast path when the key matches"],
            Takeaway: "Memoization lets you write simple, top-down View functions without paying for unnecessary re-renders.",
            NextStep: "Managing side effects with Commands."),

        new("commands", "Section 4 - Effects", "Commands",
            "Side effects without breaking purity",
            ["Update never performs I/O — it returns a Command describing the intent",
             "The runtime executes Commands asynchronously and dispatches result Messages",
             "Command.None: no side effect",
             "Command.Batch: execute multiple commands in sequence",
             "Custom commands: HTTP requests, local storage, navigation, etc."],
            Code: "record FetchArticles(int Offset, int Limit) : Command;\n\n// In HandleCommand:\ncase FetchArticles cmd:\n    var articles = await api.GetArticles(cmd.Offset, cmd.Limit);\n    dispatch(new ArticlesLoaded(articles));\n    break;",
            Takeaway: "Commands keep Update pure while still allowing arbitrary I/O — the runtime is the only impure layer.",
            NextStep: "Subscriptions for external events."),

        new("subscriptions", "Section 4 - Effects", "Subscriptions",
            "Declarative external event sources",
            ["Subscriptions are declared as a function of the Model — they activate and deactivate automatically",
             "Timer: Subscription.Every(interval, tick => new Tick(tick))",
             "Keyboard: Subscription.OnKeyDown(data => new KeyPressed(data))",
             "Mouse: Subscription.OnMouseMove(data => new MouseMoved(x, y))",
             "Browser: OnResize, OnVisibilityChange, OnAnimationFrame",
             "The runtime diffs subscriptions just like the DOM — only changes are applied"],
            Code: "public static Subscription Subscriptions(Model model)\n{\n    var subs = new List<Subscription>\n    {\n        Subscription.OnKeyDown(data => new KeyPressed(data))\n    };\n    if (model.DemoTimer)\n        subs.Add(Subscription.Every(TimeSpan.FromSeconds(1),\n            at => new Tick(at)));\n    return Subscription.Batch(subs);\n}",
            Takeaway: "Subscriptions are the declarative answer to addEventListener — they are part of the model, not imperative glue code.",
            NextStep: "Live demo time."),

        new("demo-subscriptions", "Section 4 - Effects", "Live Demo",
            "Subscriptions in action",
            ["Toggle the timer to activate/deactivate Subscription.Every",
             "Toggle mouse tracking to see Subscription.OnMouseMove",
             "Notice how the event log fills up — each entry is a Message flowing through Update",
             "Type in the input field — oninput dispatches DemoInputChanged messages",
             "This is the same subscription system that powers real apps"],
            Kind: SlideKind.Demo,
            Takeaway: "Subscriptions activate and deactivate as the model changes — no manual cleanup needed."),

        new("routing", "Section 5 - Routing", "URL-Based Navigation",
            "Type-safe routing without a router library",
            ["URLs are parsed into a Route sum type — Home, Article, Profile, Settings, etc.",
             "OnUrlChanged maps the URL to a Message that updates the Model",
             "OnLinkClicked distinguishes Internal vs External navigation",
             "URL fragments (e.g. #slide-5) are first-class — no page reload",
             "Route-to-page mapping is a pure function — easily testable"],
            Code: "interface Route\n{\n    record Home : Route;\n    record Article(string Slug) : Route;\n    record Profile(string Username) : Route;\n    record Editor(string? Slug) : Route;\n}",
            Takeaway: "Routing is just another message — URLs are data, navigation is a state transition.",
            NextStep: "A real-world application built with Abies."),

        new("conduit", "Section 6 - Real World", "Conduit",
            "A full-stack production application",
            ["RealWorld Conduit spec: articles, comments, tags, auth, profiles, favorites",
             "Abies frontend + ASP.NET Core API backend",
             ".NET Aspire orchestration — AppHost manages both services",
             "Full CRUD: create, read, update, delete articles and comments",
             "Pagination, filtering by tag, feed vs global views"],
            Takeaway: "Conduit proves Abies works for real applications, not just demos.",
            NextStep: "How we test it."),

        new("conduit-arch", "Section 6 - Real World", "Conduit Architecture",
            "Frontend + API + Aspire",
            ["Abies.Conduit: the frontend — Pages, Commands, Route, Navigation",
             "Abies.Conduit.Api: ASP.NET Core Minimal API with in-memory store",
             "Abies.Conduit.AppHost: .NET Aspire orchestration",
             "Abies.Conduit.ServiceDefaults: shared OpenTelemetry + health check configuration",
             "Source-generated JSON serialization (AbiesJsonContext) — no reflection, trim-safe"],
            Takeaway: "The full stack is .NET all the way — frontend, backend, orchestration, telemetry.",
            NextStep: "Testing this stack."),

        new("testing", "Section 7 - Testing", "Testing Strategy",
            "E2E + Integration + Unit",
            ["E2E tests: Playwright + NUnit — real browser, real interactions",
             "Integration tests: test the full MVU cycle (Update + HandleCommand) without a browser",
             "Unit tests: pure functions (Update, View, Route parsing) tested in isolation",
             "The Update function is trivially testable — given model + message, assert new model",
             "No mocking frameworks needed — pure functions have no dependencies to mock"],
            Code: "[Test]\npublic async Task CanCreateAndViewArticle()\n{\n    await LoginAsDefaultUser();\n    await Page.ClickAsync(\"text=New Article\");\n    await Page.FillAsync(\"[placeholder=Title]\", \"Test\");\n    await Page.ClickAsync(\"text=Publish\");\n    await Expect(Page.Locator(\"h1\")).ToHaveTextAsync(\"Test\");\n}",
            Takeaway: "Pure functions + E2E coverage = high confidence with minimal test infrastructure.",
            NextStep: "Performance — does it actually perform well?"),

        new("benchmarks", "Section 8 - Performance", "E2E Benchmarks",
            "js-framework-benchmark results",
            ["Benchmarked against js-framework-benchmark — the industry-standard suite",
             "Create 1,000 rows: Abies ~ 85 ms (Blazor WASM ~ 220 ms)",
             "Replace 1,000 rows: Abies ~ 90 ms (Blazor WASM ~ 250 ms)",
             "Partial update (every 10th row): Abies ~ 25 ms",
             "Select row: Abies ~ 4 ms — near-instant feedback",
             "Swap rows: Abies ~ 18 ms — LIS algorithm at work",
             "Remove row: Abies ~ 12 ms",
             "Binary batching keeps interop overhead under 1 ms per frame"],
            Takeaway: "Abies outperforms Blazor WASM on every js-framework-benchmark operation thanks to LIS diffing and binary batching.",
            NextStep: "The BenchmarkDotNet micro-benchmarks."),

        new("micro-benchmarks", "Section 8 - Performance", "Micro-Benchmarks",
            "BenchmarkDotNet deep-dive",
            ["DomDiffingBenchmarks: virtual DOM diff + patch throughput",
             "RenderingBenchmarks: View function execution time",
             "EventHandlerBenchmarks: message dispatch latency",
             "UrlParsingBenchmarks: route parsing throughput",
             "StartupBenchmarks: time-to-interactive measurement",
             "All benchmarks run on .NET 10 with NativeAOT-style trimming"],
            Takeaway: "Micro-benchmarks complement E2E benchmarks — together they give a complete performance picture.",
            NextStep: "How Abies compares to Blazor."),

        new("abies-vs-blazor", "Section 8 - Performance", "Abies vs Blazor",
            "A principled comparison",
            ["Architecture: MVU (single state tree) vs Component (distributed state)",
             "Rendering: Virtual DOM + LIS diffing vs RenderTree + sequential diffing",
             "Interop: Binary batching (1 call/frame) vs Individual JS interop calls",
             "State: Immutable records vs Mutable properties + StateHasChanged()",
             "Testing: Pure functions vs Mock-heavy component testing",
             "Syntax: C# functions vs Razor templates (.razor files)",
             "Abies is not better — it is a different paradigm for developers who value purity and predictability"],
            Takeaway: "Choose Abies if you want Elm-style architecture in .NET. Choose Blazor if you want the component model.",
            NextStep: "Observability in production."),

        new("opentelemetry", "Section 9 - Production", "OpenTelemetry",
            "Browser-to-backend tracing",
            ["OTLP traces from the browser — every MVU cycle, every command, every HTTP request",
             "Spans flow from the Abies frontend through to the ASP.NET Core API",
             "Aspire Dashboard visualizes traces, logs, and metrics in real time",
             "Custom ActivitySource per application — Abies.Conduit, Abies.Conduit.Api",
             "W3C Trace Context propagation — distributed tracing out of the box"],
            Takeaway: "Abies is the only WASM MVU framework with built-in OpenTelemetry support.",
            NextStep: "Deploying to production."),

        new("deployment", "Section 9 - Production", "Deployment",
            ".NET 10 - Publish - Aspire",
            ["dotnet publish -c Release produces static WASM files ready for any CDN or static host",
             "Output: bin/Release/net10.0/publish/wwwroot/ — HTML, CSS, JS, WASM, DLLs",
             "Aspire AppHost: one-click local dev with API + frontend + dashboard",
             "Docker support: Dockerfile in AppHost for containerized deployment",
             "Global.json pins the SDK version for reproducible builds across teams"],
            Code: "dotnet publish Abies.Conduit -c Release\n\n# Serve statically — no server runtime needed\n# Output: bin/Release/net10.0/publish/wwwroot/",
            Takeaway: "Deployment is just dotnet publish — Abies apps are static files that run entirely in the browser.",
            NextStep: "Getting started from scratch."),

        new("templates", "Section 10 - Getting Started", "Project Templates",
            "dotnet new abies-browser",
            ["dotnet new install Abies.Templates — installs the project template",
             "dotnet new abies-browser -n MyApp — scaffolds a complete MVU application",
             "Includes: Program.cs, wwwroot/, .csproj, launchSettings.json",
             "Pre-configured for .NET 10, Praefixum, and the Abies HTML DSL",
             "From dotnet new to running in the browser in under 60 seconds"],
            Takeaway: "The template gives you a working Abies app in seconds — no boilerplate to write.",
            NextStep: "Where to go from here."),

        new("resources", "Section 10 - Getting Started", "Resources",
            "Links and community",
            ["GitHub: github.com/Picea/Abies — source, issues, discussions",
             "Docs: comprehensive guides, ADRs, and API reference",
             "Conduit: full-stack reference application",
             "Benchmarks: reproducible performance data with BenchmarkDotNet + js-framework-benchmark",
             "License: Apache 2.0 — use it commercially, contribute back"],
            Takeaway: "Everything is open source — star the repo, file issues, submit PRs.",
            NextStep: "Let's wrap up."),

        new("summary", "Section 11", "What We Covered",
            "A complete picture",
            ["MVU architecture — Model, View, Update, Commands, Subscriptions",
             "Type-safe HTML DSL — no templates, no Razor, no runtime errors",
             "Virtual DOM with LIS keyed diffing and binary batching",
             "Memoization and view caching for optimal performance",
             "Real-world Conduit application — full CRUD, auth, pagination",
             "E2E + integration + unit testing strategy",
             "OpenTelemetry browser-to-backend tracing",
             ".NET 10 deployment and Aspire orchestration"],
            Takeaway: "Abies proves that pure functional MVU is a viable, performant, and enjoyable way to build .NET web applications."),

        new("thank-you", "Closing", "Thank You", "",
            ["github.com/Picea/Abies",
             "Slides built with Abies — source in Abies.Presentation",
             "Questions?"],
            Kind: SlideKind.Outro,
            Takeaway: "Try it: dotnet new install Abies.Templates && dotnet new abies-browser -n MyApp")
    ];

    // ═════════════════════════════════════════════════════════════════════════════
    // EXPRESS PRESENTATION — Condensed Overview (~18 slides)
    // ═════════════════════════════════════════════════════════════════════════════

    private static readonly Slide[] _expressSlides =
    [
        new("intro", "Opening", "Coderen met AI in 2026",
            "Hoe ik het doe, en waarom ik denk dat het werkt",
            [],
            MarkdownBody: """
            - Een eerlijk beeld van de stand van zaken — april 2026
            - De rol van de developer verschuift, maar niet op de manier die je denkt
            - Hoe ik met een AI-team samenwerk: Squad, charters, en een echte workflow
            - Aan het eind: live demo. We bouwen een feature die ik eigenlijk nodig had voor deze talk.
            """,
            Kind: SlideKind.Intro,
            Takeaway: "Na 30 minuten weet je waar AI-coderen staat, hoe ik het aanpak, en waar je mee kunt beginnen."),

        new("adoption", "Deel 1 — Stand van zaken", "De adoptie is al gebeurd",
            "Niet meer de vraag óf, maar hoe",
            [],
            MarkdownBody: """
            JetBrains AI Pulse, januari 2026:

            - 90% van professionele developers gebruikt minstens één AI-tool wekelijks
            - 74% gebruikt gespecialiseerde dev-AI (geen chatbot)
            - 51% dagelijks

            Pragmatic Engineer survey, maart 2026:

            - 95% wekelijks · 55% gebruikt agents · staff+ engineers leiden de adoptie

            Claude Code: van 0 → #1 most-loved tool in 8 maanden tijd
            """,
            Takeaway: "Dit is geen toekomstgesprek meer. Het is een achterstandsgesprek."),

        new("tools", "Deel 1 — Stand van zaken", "Het tool-landschap",
            "Drie spelers, drie strategieën",
            [],
            MarkdownBody: """
            - Claude Code  ·  van 0 → #1 most-loved (46%) in 8 maanden · CSAT 91% · NPS 54 · 18% dagelijks gebruik · 24% in VS+CA
            - Cursor      ·  groeide ~35% in 9 maanden — bedreigt Copilot
            - GitHub Copilot · enterprise default — 4.7M paid subs, +75% YoY

            Adoptie over tijd (% professionele devs):

            | Segment | 2023 | 2024 | 2025 | Jan 2026 | Trend |
            | --- | ---: | ---: | ---: | ---: | --- |
            | Any AI tool | 60% | 76% | 84% | 90% | ↑ |
            | Specialized dev AI | — | ~25% | ~55% | 74% | ↑↑ |
            | Claude Code daily | — | — | 3% | 18% | ↑↑↑ |

            Bron: JetBrains AI Pulse · Stack Overflow · Pragmatic Engineer
            """,
            Takeaway: "Markt convergeert op specialized agents. Best-of-breed wint van ecosystem lock-in."),

        new("productivity", "Deel 1 — Stand van zaken", "De productiviteit is genuanceerd",
            "Positief beeld: wat de grote surveys zeggen",
            [],
            MarkdownBody: """
            DX Q4 2025 · 135.000 developers geanalyseerd:

            - 3,6 uur per week bespaard (gemiddeld)
            - Daily users mergen 60% meer PRs

            Stack Overflow 2025 · 84% gebruikt of plant gebruik · 51% dagelijks
            """,
            Takeaway: "Brede surveys zijn positief. Maar er zijn ook andere cijfers."),

        new("productivity-nuance", "Deel 1 — Stand van zaken", "De nuance",
            "Wat gecontroleerde trials en grote codebases zeggen",
            [],
            MarkdownBody: """
            METR, juli 2025 · randomized controlled trial:

            - 16 senior open-source devs · eigen repos · 246 taken
            - Ervaren devs waren 19% LANGZAMER met AI
            - Terwijl ze dáchten 20% sneller te zijn

            Microsoft, .NET runtime, mei 2025 → maart 2026:

            - 878 cloud-agent PRs · 68% merge rate · 0.6% revert rate
            - Voorbereiding verhoogde succes van 38% → 69% — niet betere modellen

            Drie cijfers, drie verhalen. Eén ding ontbrak: context.
            """,
            Takeaway: "Aggregate cijfers liegen niet. Maar ze vertellen ook niet het hele verhaal."),
        new("metr-followup", "Deel 1 — Stand van zaken", "Wat er daarna gebeurde",
            "De follow-up van februari 2026",
            [],
            MarkdownBody: """
            METR herhaalde de studie met dezelfde devs, betere modellen:

            - Speedup van -18% (CI -38% tot +9%) — niet significant
            - Voor nieuwe devs: -4% (CI -15% tot +9%) — ook niet significant

            Maar de echte bevinding zit in wat ze NIET konden meten:

            - Devs weigerden om aan de 'no AI' conditie mee te doen
            - De ene dev: 'mijn hoofd ontploft als ik het op de oude manier moet doen — alsof ik plotseling moet lópen door de stad terwijl ik gewend was Uber te nemen'
            - Selectie-effecten maakten een schoon getal onmogelijk

            De afwezigheid van het cijfer IS het cijfer.
            """,
            Takeaway: "Tussen de twee studies in is iets geknikt. Niet de tools — de manier waarop devs eraan gewend zijn."),

        new("trust", "Deel 2 — Bronnen van scepsis", "Het vertrouwen daalt",
            "Stack Overflow Developer Survey, 2023 → 2025",
            [],
            MarkdownBody: """
            - Gebruik:    84% gebruikt of plant gebruik (was 76%)
            - Vertrouwen: slechts 29% vertrouwt de output (was 70%+)
            - Actief wantrouwen: 46%
            - 'Highly trust': 3%

            Stack Overflow Developer Survey, 2023 → 2025:

            ```text
                Gebruik AI-tools
                    2023  ████████████████████████          60%
                    2024  ██████████████████████████████    76%
                    2025  ██████████████████████████████████ 84%   ↑

                Vertrouwen in output
                    2023  ████████████████████████████      70%
                    2024  ████████████████████████          60%
                    2025  ████████████                      29%   ↓
            ```

            Ervaren devs zijn het meest sceptisch — en dat is gezond.
            """,
            Takeaway: "Dit is geen ironie. Dit is volwassen worden."),

        new("objections", "Deel 2 — Bronnen van scepsis", "Drie bezwaren die hout snijden",
            "Eerlijk over wat er fout gaat",
            [],
            MarkdownBody: """
            **'AI maakt brakke code'**
            - Veracode: 45% van AI-snippets faalt security tests
            - AI-code heeft 2.74× meer kwetsbaarheden dan handgeschreven
            - ✓ Waar — als je geen reviewproces hebt

            **'AI sloopt mijn flow'**
            - METR's screen-recordings: meer idle-tijd, meer context-switching
            - Het 'slot machine' effect — 1% kans dat het alles oplost
            - ✓ Waar — als je AI als magic button gebruikt

            **'Het werkt niet op mijn complexe codebase'**
            - Bewezen waar voor grote, mature codebases bij eerste contact
            - ✓ Waar — als je je codebase niet leesbaar maakt voor agents
            """,
            Takeaway: "Geen van deze bezwaren is onzin. Maar elk heeft een 'als'."),

        new("common-cause", "Deel 2 — Bronnen van scepsis", "Wat alle slechte ervaringen delen",
            "De tool wordt geframed, de workflow is de bottleneck",
            [],
            MarkdownBody: """
            **Wat mensen vaak doen:**

            - AI behandelen als autocomplete-op-steroïden
            - Vragen stellen zonder duidelijke acceptance criteria
            - Suggesties accepteren zonder ze te verifiëren
            - Geen tests — of tests pas erna
            - Code in lange ongestructureerde files mikken

            **Wat het oplevert:**

            - Plausibel ogende code die net niet klopt
            - Een merge-pile van halve oplossingen
            - De ervaring dat je met een uitzinnige stagiair werkt
            - Het gevoel: 'AI werkt gewoon niet voor mijn werk'

            De tool krijgt de schuld. De workflow is de bottleneck.
            """,
            Takeaway: "Het probleem zit zelden in de AI. Het zit in hoe we ermee samenwerken."),

        new("role-shift", "Deel 3 — Rol van de developer", "Wat verdwijnt en wat blijft",
            "De waardevolle delen van het werk verschuiven",
            [],
            MarkdownBody: """
            **Wat verdwijnt (of irrelevant wordt):**

            - Typsnelheid
            - Syntax uit het hoofd kennen
            - Boilerplate kunnen produceren
            - 'Ik ken alle stdlib functies' als skill

            **Wat blijft — en belangrijker wordt:**

            - Probleemformulering
            - Systeemdenken
            - Domeinkennis
            - Verificatie & smaak
            - Wéten wanneer iets fout is
            - Architectuurkeuzes met de lange termijn in gedachten
            """,
            Takeaway: "Het waardevolle deel van programmeren verschuift van syntax naar denken."),

        new("spec-and-verify", "Deel 3 — Rol van de developer", "Spec-schrijver én verificateur",
            "Twee competenties, niet één",
            [],
            MarkdownBody: """
            Het cliché: 'we worden allemaal spec-schrijvers'

            Dat klopt — maar het is maar de helft van het verhaal.

            De andere helft: verificatie.

            - Weten wat je gevraagd hebt
            - Kunnen lezen of het systeem dat ook doet
            - Tests die de spec zíjn, niet er omheen
            - Een neus voor wanneer 'het werkt' niet 'het werkt' is

            Het is niet 'de developer als manager'. Het is meer:
            de developer als senior reviewer met heel veel handen.
            """,
            Callout: "Sturen + smaak = de nieuwe kerncompetentie",
            Takeaway: "Schrijven wat je wilt is makkelijker dan zien of je het hebt gekregen. Verifiëren is de schaarse skill."),

        new("ai-readability", "Deel 3 — Rol van de developer", "AI-readability als nieuwe skill",
            "Code structureren zodat agents er goed in kunnen werken",
            [],
            MarkdownBody: """
            De vraag: hoe maak je een codebase waarin een agent effectief is?

            **Antwoord: hetzelfde als wat een mens-vriendelijke codebase is.**
            Maar nu beloont het systeem het direct.

            - Kleine modules met scherpe verantwoordelijkheid
            - Sterke types — maak fouten onmogelijk in plaats van detecteerbaar
            - Pure functies waar het kan, side effects geïsoleerd
            - Determinisme — gegeven X, altijd Y
            - Tests als spec, niet als after-thought
            - Contracts expliciet, geen impliciete kennis

            Wat lang werd weggezet als 'purisme' (FP, DDD, sterke types)
            blijkt nu een productiviteitsmultiplier.
            """,
            Takeaway: "Goed engineerwerk is altijd al goed engineerwerk geweest. Nu krijg je er direct voor betaald."),

        new("evolution", "Deel 4 — Mijn workflow", "Hoe ik er zelf gekomen ben",
            "Van één prompt naar een echt team",
            [],
            MarkdownBody: """
            **Fase 1 — De grote prompt file**

            - Eén markdown bestand met 'rollen': architect, dev, tester, reviewer
            - Werkte... maar het was een trucje
            - Eén context, één geheugen, geen echte parallelliteit
            - Het model moest doen alsof het meerdere mensen was

            **Fase 2 — Het verlangen**

            - Wat ik wilde: échte agents
            - Elk in eigen context, met eigen kennis
            - Samen aan een gemeenschappelijk doel
            - Zoals een echt team werkt

            **Fase 3 — Squad ontdekt**

            - Precies wat ik mentaal al had bedacht
            - Maar dan echt
            """,
            Takeaway: "Eén model dat doet alsof het een team is, is iets anders dan een team van modellen."),

        new("agent-landscape", "Deel 4 — Mijn workflow", "Het multi-agent landschap, april 2026",
            "Drie tiers — pak de juiste voor de taak",
            [],
            MarkdownBody: """
            **Tier 1 — Conductor (jij stuurt direct)**

            - Claude Code · Cursor · GitHub Copilot · Aider
            - Synchroon · pair programming · single context

            **Tier 2 — Orchestrator (team agents, jij merget)**

            - Squad · Claude Agent Teams · Conductor · Vibe Kanban
            - Antigravity · Gas Town · Multiclaude
            - Parallel · jij checkt periodiek in
            - ← waar ík werk

            **Tier 3 — Cloud (async, je komt terug naar een PR)**

            - Claude Code Web · Codex Web · Copilot Coding Agent · Jules
            - Fire-and-forget · backlog draining · sleep on it

            De meeste devs gebruiken alle drie. Verschillende taken,
            verschillende tiers.
            """,
            Takeaway: "De industrie convergeert rond het orchestrator-model. Tier 2 is waar het serieuze werk gebeurt."),

        new("squad-what", "Deel 4 — Mijn workflow", "Squad — een echt team in je repo",
            "Brady Gaster · github.com/bradygaster/squad · Apache 2.0",
            [],
            MarkdownBody: """
            Eén commando, je krijgt een team van specialisten:

            - lead · frontend · backend · tester · scribe

            Elk leeft als een bestand in jouw repo onder .squad/

            Wat er in .squad/ zit:

            ```text
                team.md           — wie zit in het team
                routing.md        — wie pakt wat op
                decisions.md      — gedeeld brein
                agents/{naam}/
                    charter.md      — identiteit, expertise, voice
                    history.md      — wat ze van JOUW project geleerd hebben
                skills/           — gecomprimeerde learnings
                log/              — sessiegeschiedenis
            ```

            Je commit het. Het zit in git. Iedereen die cloont krijgt het team.
            Met al hun opgebouwde kennis.
            """,
            Takeaway: "Geen chatbot met hoeden op. Eigen context, eigen kennis, eigen geheugen — dat compoundt over sessies."),

        new("workflow", "Deel 4 — Mijn workflow", "Van issue tot merge",
            "Concreet: hoe ziet een feature er bij mij uit?",
            [],
            MarkdownBody: """
            **1. Issue refinen** — sámen met Claude in een chat
            - Ik begin nooit bij de squad. Ik begin bij een gesprek.
            - Domein, doel, edge cases, acceptance criteria, test plan.

            **2. Issue committen op GitHub**
            - Met scherpe acceptance criteria die geverifieerd kunnen worden.

            **3. Squad starten**
            - Lead-agent leest het issue, decomposeert in subtaken.

            **4. Parallel werk**
            - Frontend, backend, tester werken tegelijk in eigen worktrees.
            - Scribe legt onderweg besluiten vast in decisions.md.

            **5. PR review** — door mij
            - Lees ik de code? Ja. Draaien tests? Verifiëren.
            - Klopt de architectuur? Past het bij de rest?

            **6. Merge** — of feedback terug
            - Bij feedback: scherpere prompt, meer tests, opnieuw.
            """,
            Takeaway: "De menselijke checkpoints zijn niet weg. Ze zijn juist scherper geworden."),

        new("lessons", "Deel 4 — Mijn workflow", "Wat ik geleerd heb",
            "De echte tips, geen marketing",
                        [],
                        MarkdownBody: """
                        - Sloppy spec → uren verspilde compute. Investeer vooraf.

                        - Tests laten genereren én verifiëren is niet optioneel. Tests zijn de spec die de agent niet kan jokeren.

                        - FP + DDD werken disproportioneel goed. Sterke types geven agents harde leuningen.

                        - Ken je codebase. Als jij niet weet wat goed is, weet de agent het ook niet.

                        - Token-budget is een echte constraint — Steve Yegge draait drie Claude Max accounts parallel. Geen grap.

                        - Multi-agent voegt overhead toe. Gebruik het waar het bijdraagt, niet automatisch.
                        """,
            Takeaway: "De vaardigheid is niet 'AI gebruiken'. De vaardigheid is wéten wanneer en hoe."),

        new("picea-abies", "Deel 5 — Synthese", "Picea, Abies, en de squad",
            "Het verhaal komt samen — en ja, dit is de shameless plug",
            [],
            MarkdownBody: """
            Alles wat ik beweerde over FP + DDD + AI komt samen in:

            Picea — Mealy machine kernel voor .NET

            - Eén pure transitiefunctie: (state, event) → (state, effect)
            - Draait als MVU runtime, event-sourced aggregate, of actor
            - Decider pattern voor command-validatie
            - Result types in plaats van exceptions

            Abies — MVU framework op Picea

            - C# helemaal naar beneden, geen Razor, geen JS
            - Verslaat Blazor WASM op vrijwel alle js-framework duration tests*
            - LIS keyed diffing, binary batching, memoization

            Glauca · Rubens · Mariana — event sourcing, actors, resilience

            Het hele ecosysteem is gebouwd door mij + de squad,
            in de afgelopen maanden.

            * exacte cijfers in beweging — zie repo voor recent results
            """,
            Takeaway: "Snel én AI-readable — hetzelfde principe, twee keer bewezen in de praktijk."),

        new("today", "Deel 5 — Synthese", "Vandaag is jullie beurt",
            "Een ambitieus project, een hele dag, samen ervaren",
            [],
            MarkdownBody: """
            Aanbevelingen voor vandaag:

            - Begin bij de spec, niet bij de code
            - Schrijf eerst tests of acceptance criteria
            - Probeer minstens één tool uit elk tier (single + orchestrator)
            - Houd bij wat NIET werkt — dat is goud voor de showcase
            - Vraag elkaar om hulp, deel patronen die werken

            Aan het eind van de dag: showcase + lessons learned.

            Maar eerst, één laatste ding —
            deze talk had eigenlijk grafieken nodig.
            Zoals jullie hebben gemerkt :)
            """,
            Takeaway: "Klaar om iets te bouwen?",
            Kind: SlideKind.Outro)
    ];
}
