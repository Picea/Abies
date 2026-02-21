// =============================================================================
// Abies — Conference Presentation
// =============================================================================
// A self-hosting Abies application that presents the framework itself.
// Demonstrates MVU architecture, virtual DOM, subscriptions, and commands
// by using the very patterns it explains.
//
// Two presentation modes:
//   Full    — ~36 slides, deep-dive (≈60 min)
//   Express — ~18 slides, condensed (≈30 min)
// =============================================================================

using System.Runtime.Versioning;
using Abies;
using Abies.DOM;
using Abies.Html;
using static Abies.Html.Attributes;
using static Abies.Html.Elements;
using static Abies.Html.Events;
using static Abies.SubscriptionModule;

[assembly: SupportedOSPlatform("browser")]

await Runtime.Run<Presentation, Arguments, Model>(new Arguments());

// ─── Data types ─────────────────────────────────────────────────────────────────

public record Arguments;

public enum SlideKind { Intro, Concept, Demo, Outro }

public enum PresentationMode { Menu, Full, Express }

public record Slide(
    string Id,
    string Kicker,
    string Title,
    string Subtitle,
    string[] Points,
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

public interface Message : Abies.Message
{
    record SelectPresentation(PresentationMode Mode) : Message;
    record BackToMenu : Message;
    record NextSlide : Message;
    record PrevSlide : Message;
    record GoToSlide(int Index) : Message;
    record KeyPressed(KeyEventData Data) : Message;
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
    public static (Model, Command) Initialize(Url url, Arguments _)
    {
        var mode = PresentationMode.Menu;
        var slideIndex = 0;

        if (TryGetSlideIndex(url, FullSlides, out var fi))
        {
            mode = PresentationMode.Full;
            slideIndex = fi;
        }
        else if (TryGetSlideIndex(url, ExpressSlides, out var ei))
        {
            mode = PresentationMode.Express;
            slideIndex = ei;
        }

        return (new Model(mode, slideIndex, 0, 0, null, false, false, 0, 0, [], null, ""), Commands.None);
    }

    public static Abies.Message OnUrlChanged(Url url) => new Message.NoOp();
    public static Abies.Message OnLinkClicked(UrlRequest urlRequest) => new Message.NoOp();

    public static (Model model, Command command) Update(Abies.Message message, Model model) => message switch
    {
        Message.SelectPresentation m => (model with { Mode = m.Mode, SlideIndex = 0, Log = [], DemoCount = 0, TickCount = 0, LastTick = null, DemoTimer = false, TrackMouse = false, DemoInput = "" }, Commands.None),
        Message.BackToMenu => (model with { Mode = PresentationMode.Menu, SlideIndex = 0, Log = [], DemoCount = 0, TickCount = 0, LastTick = null, DemoTimer = false, TrackMouse = false, DemoInput = "" }, Commands.None),
        Message.NextSlide => (SetSlide(model, model.SlideIndex + 1), Commands.None),
        Message.PrevSlide => (SetSlide(model, model.SlideIndex - 1), Commands.None),
        Message.GoToSlide m => (SetSlide(model, m.Index), Commands.None),
        Message.KeyPressed m => HandleKeyPress(model, m.Data),
        Message.IncrementDemo => (AddLog(model with { DemoCount = model.DemoCount + 1 }, $"Count → {model.DemoCount + 1}"), Commands.None),
        Message.ResetDemo => (AddLog(model with { DemoCount = 0 }, "Counter reset"), Commands.None),
        Message.ToggleDemoTimer => (AddLog(model with { DemoTimer = !model.DemoTimer, TickCount = 0, LastTick = null }, model.DemoTimer ? "Timer stopped" : "Timer started"), Commands.None),
        Message.Tick m => (model with { TickCount = model.TickCount + 1, LastTick = m.At }, Commands.None),
        Message.ToggleMouse => (AddLog(model with { TrackMouse = !model.TrackMouse, LastMouseLogAt = null }, model.TrackMouse ? "Mouse tracking off" : "Mouse tracking on"), Commands.None),
        Message.MouseMoved m => UpdateMouse(model, m.X, m.Y),
        Message.ClearLog => (model with { Log = [] }, Commands.None),
        Message.DemoInputChanged m => (AddLog(model with { DemoInput = m.Value }, $"Input: \"{m.Value}\""), Commands.None),
        _ => (model, Commands.None)
    };

    public static Subscription Subscriptions(Model model)
    {
        var subs = new List<Subscription>
        {
            OnKeyDown(data => new Message.KeyPressed(data))
        };
        if (model.DemoTimer)
        {
            subs.Add(Every(TimeSpan.FromSeconds(1), at => new Message.Tick(at)));
        }

        if (model.TrackMouse)
        {
            subs.Add(OnMouseMove(data => new Message.MouseMoved((int)data.ClientX, (int)data.ClientY)));
        }
        return Batch(subs);
    }

    public static Task HandleCommand(Command command, Func<Abies.Message, Unit> dispatch) => Task.CompletedTask;

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
                div([class_("presentation-cards")],
                [
                    div([class_("presentation-card"), onclick(new Message.SelectPresentation(PresentationMode.Full))],
                    [
                        div([class_("card-icon")], [text("\U0001F332")]),
                        h2([], [text("Full Conference")]),
                        p([class_("card-desc")], [text("Deep-dive into every aspect of Abies — from core MVU concepts through virtual DOM internals, the binary batching protocol, E2E benchmarks, and production deployment.")]),
                        div([class_("card-meta")],
                        [
                            span([class_("pill")], [text($"{FullSlides.Length} slides")]),
                            span([class_("pill")], [text("\u2248 60 min")])
                        ])
                    ]),
                    div([class_("presentation-card"), onclick(new Message.SelectPresentation(PresentationMode.Express))],
                    [
                        div([class_("card-icon")], [text("\u26A1")]),
                        h2([], [text("Express Overview")]),
                        p([class_("card-desc")], [text("A focused walkthrough of Abies highlighting the architecture, key differentiators from Blazor, performance results, and how to get started.")]),
                        div([class_("card-meta")],
                        [
                            span([class_("pill")], [text($"{ExpressSlides.Length} slides")]),
                            span([class_("pill")], [text("\u2248 30 min")])
                        ])
                    ])
                ])
            ])
        ]);

    private static Document ViewDeck(Model model)
    {
        var slides = model.Mode == PresentationMode.Full ? FullSlides : ExpressSlides;
        var current = slides[model.SlideIndex];
        var progress = slides.Length > 1 ? (double)model.SlideIndex / (slides.Length - 1) * 100 : 0;
        var modeLabel = model.Mode == PresentationMode.Full ? "Full Conference" : "Express Overview";

        return new Document(
            $"Abies — {current.Title}",
            div([class_("app")],
            [
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
                        button([class_("ghost"), onclick(new Message.BackToMenu())], [text("\u2630 Menu")]),
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
                                li([class_(i == model.SlideIndex ? "agenda-link active" : "agenda-link"),
                                    onclick(new Message.GoToSlide(i))],
                                [
                                    span([class_("agenda-index")], [text($"{i + 1:D2}")]),
                                    span([class_("agenda-title")], [text(s.Title)])
                                ], id: $"agenda-{s.Id}")
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

        if (current.Points.Length > 0)
        {
            children.Add(ul([class_("points")],
                current.Points.Select((pt, i) => li([], [text(pt)], id: $"point-{i}")).ToArray()));
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

        return div([class_("panel")],
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

    private static (Model, Command) HandleKeyPress(Model model, KeyEventData data)
    {
        if (model.Mode == PresentationMode.Menu)
        {
            return (model, Commands.None);
        }
        return data.Key switch
        {
            "ArrowRight" or " " or "l" or "j" => (SetSlide(model, model.SlideIndex + 1), Commands.None),
            "ArrowLeft" or "h" or "k" => (SetSlide(model, model.SlideIndex - 1), Commands.None),
            "Home" => (SetSlide(model, 0), Commands.None),
            "End" => (SetSlide(model, int.MaxValue), Commands.None),
            "Escape" => (model with { Mode = PresentationMode.Menu }, Commands.None),
            _ => (model, Commands.None)
        };
    }

    private static Model SetSlide(Model model, int index)
    {
        var slides = model.Mode == PresentationMode.Full ? FullSlides : ExpressSlides;
        return model with { SlideIndex = ClampSlide(index, slides.Length) };
    }

    private static int ClampSlide(int index, int count) => Math.Clamp(index, 0, count - 1);

    private static bool TryGetSlideIndex(Url url, Slide[] slides, out int index)
    {
        index = 0;
        var fragment = url.Fragment;
        if (fragment is null)
        {
            return false;
        }

        string frag = fragment;
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

    private static readonly Slide[] FullSlides =
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
            Code: "public interface Program<TModel, in TArgument>\n{\n    static abstract (TModel, Command) Initialize(Url url, TArgument arg);\n    static abstract (TModel, Command) Update(Message msg, TModel model);\n    static abstract Document View(TModel model);\n    static abstract Subscription Subscriptions(TModel model);\n}",
            Takeaway: "The entire application is defined by four static functions — no inheritance, no lifecycle hooks, no hidden state.",
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
            "dotnet new abies",
            ["dotnet new install Abies.Templates — installs the project template",
             "dotnet new abies -n MyApp — scaffolds a complete MVU application",
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
            Takeaway: "Try it: dotnet new install Abies.Templates && dotnet new abies -n MyApp")
    ];

    // ═════════════════════════════════════════════════════════════════════════════
    // EXPRESS PRESENTATION — Condensed Overview (~18 slides)
    // ═════════════════════════════════════════════════════════════════════════════

    private static readonly Slide[] ExpressSlides =
    [
        new("x-intro", "Express", "Abies",
            "Pure functional UI for .NET WebAssembly — in 30 minutes",
            ["Model-View-Update architecture for .NET 10",
             "Zero JavaScript - Type-safe HTML - Binary batching - OpenTelemetry",
             "This presentation is itself an Abies app"],
            Kind: SlideKind.Intro,
            Takeaway: "Abies brings Elm-style MVU to .NET WebAssembly."),

        new("x-problem", "The Problem", "Why Not Components?",
            "The pain points of mutable state",
            ["Component frameworks scatter state across a mutable tree",
             "StateHasChanged(), OnParametersSet(), lifecycle soup — complexity grows with every feature",
             "Shared mutable state leads to race conditions, stale closures, defensive copying",
             "MVU: one immutable model, one update function, one render — total predictability"],
            Takeaway: "MVU trades lifecycle complexity for a single, predictable state loop."),

        new("x-architecture", "Architecture", "The MVU Loop",
            "Model to View to Update, repeat",
            ["Initialize: create the Model + startup Commands",
             "View(Model) produces virtual DOM — pure function, no side effects",
             "User interaction dispatches a Message to Update(Message, Model) returning (Model, Command)",
             "Runtime diffs virtual DOM, patches real DOM, executes Commands",
             "Commands dispatch result Messages back into the loop"],
            Code: "public interface Program<TModel, in TArgument>\n{\n    static abstract (TModel, Command) Initialize(Url url, TArgument arg);\n    static abstract (TModel, Command) Update(Message msg, TModel model);\n    static abstract Document View(TModel model);\n    static abstract Subscription Subscriptions(TModel model);\n}",
            Takeaway: "Four functions define your entire application — no base classes, no lifecycle hooks."),

        new("x-model", "Model and Messages", "Immutable State",
            "Records + sealed message types",
            ["Model: C# record with value equality and with-expressions",
             "Messages: sealed records describing events — NextSlide, Tick, KeyPressed, etc.",
             "Update: pattern match on messages, return new model + optional command",
             "Testing: given model + message then assert result — no mocking needed"],
            Code: "record Model(int Count, bool TimerActive);\n\ninterface Message : Abies.Message\n{\n    record Increment : Message;\n    record Reset : Message;\n    record Tick(DateTimeOffset At) : Message;\n}",
            Takeaway: "Immutable records make state changes explicit, trackable, and testable."),

        new("x-update", "Update", "Pure State Transitions",
            "Switch expressions for exhaustive handling",
            ["Update is a pure function — no I/O, no async, no side effects",
             "C# switch expressions with pattern matching handle every message type",
             "Returns (Model, Command) — the Command describes intent without performing it",
             "New features = new message types — existing Update cases do not change"],
            Code: "public static (Model, Command) Update(Message msg, Model m) => msg switch\n{\n    Message.Increment => (m with { Count = m.Count + 1 }, Commands.None),\n    Message.Reset     => (m with { Count = 0 }, Commands.None),\n    Message.Tick t    => (m with { TickCount = m.TickCount + 1 }, Commands.None),\n    _ => (m, Commands.None)\n};",
            Takeaway: "Pure functions are the simplest code to reason about, test, and refactor."),

        new("x-view", "View", "Type-Safe HTML DSL",
            "C# functions for every HTML element",
            ["div(), span(), h1(), ul(), li(), button() — all C# functions",
             "Attributes: class_(), href(), style(), onclick(), oninput()",
             "No Razor, no templates — if it compiles, it renders valid HTML",
             "Composes with LINQ, pattern matching, string interpolation"],
            Code: "div([class_(\"card\")],\n[\n    h2([], [text(article.Title)]),\n    p([], [text(article.Description)]),\n    button([onclick(new Favorite(article.Slug))],\n        [text($\"heart {article.FavoritesCount}\")])\n]);",
            Takeaway: "HTML-as-code eliminates template parsing errors and enables full IDE support."),

        new("x-vdom", "Virtual DOM", "Efficient Diffing and Patching",
            "LIS - binary batching - memoization",
            ["Virtual DOM: lightweight in-memory tree diffed against the previous render",
             "Head/tail skip: matching nodes at edges skipped in O(1)",
             "Longest Increasing Subsequence (LIS): minimal DOM moves for reordered lists",
             "Binary batching: all patches serialized into one ArraySegment<byte> per frame",
             "Memoization: Memo<TKey> skips re-rendering unchanged subtrees"],
            Takeaway: "Three layers of optimization make Abies competitive with the fastest JS frameworks."),

        new("x-commands", "Commands", "Controlled Side Effects",
            "Pure Update, impure runtime",
            ["Update returns a Command — it never performs I/O directly",
             "Runtime executes Commands asynchronously and dispatches results back",
             "Custom commands for HTTP, storage, navigation, clipboard, etc.",
             "Command.Batch for executing multiple effects from one Update"],
            Code: "record FetchArticles(int Offset, int Limit) : Command;\n\n// HandleCommand:\ncase FetchArticles cmd:\n    var result = await api.GetArticles(cmd.Offset, cmd.Limit);\n    dispatch(new ArticlesLoaded(result));\n    break;",
            Takeaway: "Commands separate what from how — Update decides, runtime executes."),

        new("x-subscriptions", "Subscriptions", "Declarative Event Sources",
            "Timers - keyboard - mouse - resize",
            ["Subscriptions are a function of the model — they activate/deactivate automatically",
             "Every(1s, ...) — periodic timer",
             "OnKeyDown(...) — global keyboard events",
             "OnMouseMove(...) — pointer tracking",
             "OnResize(...), OnVisibilityChange(...), OnAnimationFrame(...)",
             "Runtime diffs subscriptions like the DOM — only changes take effect"],
            Kind: SlideKind.Demo,
            Takeaway: "Subscriptions are the functional equivalent of addEventListener with automatic cleanup."),

        new("x-routing", "Routing", "URLs as Data",
            "Sum types for routes, pure parsing",
            ["Route is a sum type: Home, Article(slug), Profile(username), etc.",
             "OnUrlChanged maps URL to Message to Model update",
             "Internal vs External link handling via UrlRequest",
             "URL fragments for in-page navigation (#slide-5)"],
            Takeaway: "Routing is just another state transition — no router library needed."),

        new("x-conduit", "Real World", "Conduit Application",
            "Full-stack CRUD with Aspire",
            ["RealWorld Conduit specification: articles, comments, auth, tags, profiles",
             "Abies frontend + ASP.NET Core Minimal API backend",
             ".NET Aspire AppHost orchestrates both services",
             "Source-generated JSON (no reflection) — trim-safe and fast",
             "OpenTelemetry traces from browser click to database query"],
            Takeaway: "Conduit proves Abies works for production applications — not just counter demos."),

        new("x-testing", "Testing", "Pure Functions = Easy Tests",
            "E2E + integration + unit",
            ["Unit: test Update in isolation — given model + message then assert result",
             "Integration: test full MVU cycle with HandleCommand",
             "E2E: Playwright + NUnit — real browser interactions",
             "No mocking frameworks needed — pure functions have no dependencies"],
            Code: "// Unit test — pure function\nvar (model, _) = Update(new Increment(), initial);\nAssert.That(model.Count, Is.EqualTo(1));\n\n// E2E test — Playwright\nawait Page.ClickAsync(\"text=New Article\");\nawait Expect(Page.Locator(\"h1\")).ToHaveTextAsync(\"Test\");",
            Takeaway: "MVU purity makes testing straightforward at every level."),

        new("x-performance", "Performance", "Benchmark Results",
            "Faster than Blazor WASM on every metric",
            ["js-framework-benchmark — industry standard E2E suite:",
             "  Create 1k rows: Abies ~ 85ms vs Blazor ~ 220ms",
             "  Replace 1k rows: Abies ~ 90ms vs Blazor ~ 250ms",
             "  Partial update: Abies ~ 25ms",
             "  Select row: Abies ~ 4ms — near instant",
             "  Swap rows: Abies ~ 18ms — LIS algorithm",
             "Binary batching: < 1ms interop overhead per frame",
             "BenchmarkDotNet: micro-benchmarks for diffing, rendering, routing"],
            Takeaway: "Binary batching + LIS diffing + memoization = Blazor-beating performance."),

        new("x-vs-blazor", "Comparison", "Abies vs Blazor WASM",
            "Different paradigms, honest trade-offs",
            ["State: single immutable model vs distributed mutable components",
             "Rendering: virtual DOM + LIS vs RenderTree + sequential diff",
             "Interop: 1 binary batch/frame vs N individual JS calls",
             "Syntax: C# functions vs Razor templates",
             "Testing: pure functions vs component mocking",
             "Ecosystem: Elm-inspired niche vs large Blazor ecosystem",
             "Choose Abies for purity and predictability; Blazor for ecosystem and familiarity"],
            Takeaway: "Abies is not better — it is a different paradigm for developers who value functional purity."),

        new("x-otel", "Observability", "OpenTelemetry",
            "Traces from browser to backend",
            ["OTLP traces exported from the browser — every render, every HTTP call",
             "W3C Trace Context propagation across frontend to API",
             "Aspire Dashboard: live traces, logs, metrics visualization",
             "Custom ActivitySource per service for fine-grained tracing"],
            Takeaway: "Built-in observability — no third-party packages needed."),

        new("x-deployment", "Ship It", "Deployment",
            "Static files - .NET 10 - Aspire",
            ["dotnet publish -c Release produces static WASM files for any CDN",
             "Output: wwwroot/ with HTML, CSS, JS, WASM — no server runtime",
             "Aspire AppHost for local dev and container orchestration",
             "global.json pins SDK version for reproducible builds"],
            Takeaway: "Abies apps are static files — deploy to GitHub Pages, Azure Static Web Apps, S3, or any CDN."),

        new("x-getting-started", "Get Started", "Try Abies Today",
            "From zero to running in 60 seconds",
            ["dotnet new install Abies.Templates",
             "dotnet new abies -n MyApp",
             "cd MyApp && dotnet run",
             "Open http://localhost:5000 — your first MVU app is running",
             "GitHub: github.com/Picea/Abies - License: Apache 2.0"],
            Code: "dotnet new install Abies.Templates\ndotnet new abies -n MyApp\ncd MyApp && dotnet run",
            Takeaway: "One template install, one command, and you are building with MVU."),

        new("x-thanks", "Closing", "Thank You",
            "Questions and discussion",
            ["github.com/Picea/Abies — star, fork, contribute",
             "These slides: Abies.Presentation — built with Abies itself",
             "The Conduit app: Abies.Conduit — full-stack reference",
             "Questions?"],
            Kind: SlideKind.Outro,
            Takeaway: "Pure functional UI is here for .NET. Give Abies a try.")
    ];
}
