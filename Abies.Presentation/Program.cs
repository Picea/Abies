using System.Linq;
using System.Runtime.Versioning;
using Abies;
using Abies.DOM;
using Abies.Html;
using static Abies.Html.Attributes;
using static Abies.Html.Elements;
using static Abies.Html.Events;

[assembly: SupportedOSPlatform("browser")]

await Runtime.Run<Presentation, Arguments, Model>(new Arguments());

public record Arguments;

public record Model(
    int SlideIndex,
    int DemoCount,
    int TickCount,
    DateTimeOffset? LastTick,
    bool DemoTimer,
    bool TrackMouse,
    int MouseX,
    int MouseY,
    IReadOnlyList<string> Log,
    DateTimeOffset? LastMouseLogAt);

public interface Message : Abies.Message
{
    public sealed record NextSlide : Message;
    public sealed record PrevSlide : Message;
    public sealed record GoToSlide(int Index) : Message;
    public sealed record KeyPressed(string Key, bool Repeat) : Message;
    public sealed record IncrementDemo : Message;
    public sealed record ResetDemo : Message;
    public sealed record ToggleDemoTimer : Message;
    public sealed record Tick(DateTimeOffset Now) : Message;
    public sealed record ToggleMouse : Message;
    public sealed record MouseMoved(PointerEventData Data, DateTimeOffset At) : Message;
    public sealed record ClearLog : Message;
    public sealed record NoOp : Message;
}

public class Presentation : Program<Model, Arguments>
{
    private static readonly Slide[] Slides =
    [
        new(
            Id: "intro",
            Kicker: "Tutorial series",
            Title: "Abies MVU: from zero to working app",
            Subtitle: "A step-by-step walkthrough of the Abies loop, HTML API, subscriptions, and commands.",
            Points:
            [
                "Goal: build a tiny app that feels production-ready",
                "Each step adds one concept (no big jumps)",
                "Navigate with ← → or Space"
            ],
            Code: null,
            Callout: "Keep questions; we answer them as we go.",
            Takeaway: "You’ll leave with a complete mental model.",
            NextStep: "Define the smallest possible model and message set.",
            Kind: SlideKind.Intro
        ),
        new(
            Id: "setup",
            Kicker: "Step 1",
            Title: "Start with a model + message",
            Subtitle: "Define your state and the messages that can change it. Keep it small and explicit.",
            Points:
            [
                "Model is just data",
                "Messages describe intent",
                "No hidden state"
            ],
            Code:
            "public record Model(int Count);\n\npublic interface Message : Abies.Message\n{\n    public sealed record Increment : Message;\n    public sealed record Decrement : Message;\n}",
            Callout: "We’ll wire this into Update next.",
            Takeaway: "State + messages are the foundation of MVU.",
            NextStep: "Implement Update as the only place that changes state.",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "loop",
            Kicker: "Step 2",
            Title: "Model → View → Update",
            Subtitle: "The loop is the whole system. Every state change is visible and testable.",
            Points:
            [
                "View is pure: same model → same UI",
                "Update is the only state transition",
                "Commands stay separate from state"
            ],
            Code:
            "public static (Model, Command) Update(Message msg, Model model)\n    => msg switch\n    {\n        Message.Increment => (model with { Count = model.Count + 1 }, Commands.None),\n        Message.Decrement => (model with { Count = model.Count - 1 }, Commands.None),\n        _ => (model, Commands.None)\n    };",
            Callout: "No surprises: one entry point for change.",
            Takeaway: "Determinism makes debugging trivial.",
            NextStep: "Render a UI with typed HTML helpers.",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "html-api",
            Kicker: "Step 3",
            Title: "HTML API: elements, attributes, events",
            Subtitle: "Abies exposes typed helpers for HTML + events, with a safe escape hatch for custom tags.",
            Points:
            [
                "Elements/attributes/events are C# functions",
                "Typed event payloads (input, key, pointer)",
                "Use element(\"tag\", ...) for custom components"
            ],
            Code:
            "div([class_(\"toolbar\")], [\n    input([\n        type(\"text\"),\n        value(model.Query),\n        oninput(d => new QueryChanged(d?.Value ?? \"\"))\n    ]),\n    button([onclick(new Submit())], [text(\"Search\")])\n]);",
            Callout: "No template compiler. Just code you can debug.",
            Takeaway: "Views are just functions over state.",
            NextStep: "Wire subscriptions for external events.",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "subscriptions",
            Kicker: "Step 4",
            Title: "Subscriptions: external events",
            Subtitle: "Timers, input, and IO become messages without lifecycle tangles.",
            Points:
            [
                "Declarative, composable sources",
                "Keyed for stability",
                "No hidden lifecycle"
            ],
            Code:
            "var subs = new List<Subscription>\n{\n    SubscriptionModule.Every(TimeSpan.FromSeconds(1), now => new Tick(now)),\n    SubscriptionModule.OnKeyDown(e => new KeyPressed(e.Key))\n};\n\nreturn SubscriptionModule.Batch(subs);",
            Callout: "Effects are explicit and easy to test.",
            Takeaway: "All external signals flow into Update as messages.",
            NextStep: "Handle side effects with Commands.",
            Kind: SlideKind.Demo
        ),
        new(
            Id: "commands",
            Kicker: "Step 5",
            Title: "Commands: side effects on purpose",
            Subtitle: "Commands isolate async work and keep Update pure.",
            Points:
            [
                "Async work lives in HandleCommand",
                "Easy to test and retry",
                "No hidden side effects"
            ],
            Code:
            "public static (Model, Command) Update(Message msg, Model model)\n    => msg switch\n    {\n        Message.Load => (model, new LoadData()),\n        _ => (model, Commands.None)\n    };",
            Callout: "Effects are explicit and isolated.",
            Takeaway: "Commands make side effects intentional.",
            NextStep: "Test the loop with pure functions.",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "testing",
            Kicker: "Step 6",
            Title: "Testing + debugging",
            Subtitle: "Predictable state transitions make tests straightforward.",
            Points:
            [
                "Deterministic updates",
                "Replayable message sequences",
                "UI is a pure projection"
            ],
            Code:
            "var (next, _) = Update(new Increment(), model);\nAssert.Equal(model.Count + 1, next.Count);",
            Callout: "Debugging is about data, not magic.",
            Takeaway: "Tests become simple input → output checks.",
            NextStep: "Recap and apply the loop to a real screen.",
            Kind: SlideKind.Concept
        ),
        new(
            Id: "wrap",
            Kicker: "Step 7",
            Title: "Recap: the MVU mental model",
            Subtitle: "You now have everything needed to ship a real app.",
            Points:
            [
                "Model + Update + View",
                "Subscriptions and Commands",
                "Everything is plain C#"
            ],
            Code: null,
            Callout: "Next: build the first screen together.",
            Takeaway: "The architecture scales without adding mystery.",
            NextStep: "Apply the loop to your product’s hardest screen.",
            Kind: SlideKind.Outro
        )
    ];

    public static (Model, Command) Initialize(Url url, Arguments argument)
    {
        var initialIndex = TryGetSlideIndex(url) ?? 0;
        return (new Model(
            SlideIndex: ClampSlide(initialIndex),
            DemoCount: 0,
            TickCount: 0,
            LastTick: null,
            DemoTimer: true,
            TrackMouse: false,
            MouseX: 0,
            MouseY: 0,
            Log: [],
            LastMouseLogAt: null),
            Commands.None);
    }

    public static Abies.Message OnLinkClicked(UrlRequest urlRequest)
        => urlRequest switch
        {
            UrlRequest.Internal internalRequest => TryGetSlideIndex(internalRequest.Url) is { } index
                ? new Message.GoToSlide(index)
                : new Message.NoOp(),
            _ => new Message.NoOp()
        };

    public static Abies.Message OnUrlChanged(Url url)
        => TryGetSlideIndex(url) is { } index
            ? new Message.GoToSlide(index)
            : new Message.NoOp();

    public static Subscription Subscriptions(Model model)
    {
        var subscriptions = new List<Subscription>
        {
            SubscriptionModule.OnKeyDown(evt => new Message.KeyPressed(evt.Key, evt.Repeat))
        };

        if (model.DemoTimer)
        {
            subscriptions.Add(SubscriptionModule.Every(TimeSpan.FromSeconds(1), now => new Message.Tick(now)));
        }

        if (model.TrackMouse)
        {
            subscriptions.Add(SubscriptionModule.OnMouseMove(evt => new Message.MouseMoved(evt, DateTimeOffset.UtcNow)));
        }

        return SubscriptionModule.Batch(subscriptions);
    }

    public static Task HandleCommand(Command command, Func<Abies.Message, System.ValueTuple> dispatch)
        => Task.CompletedTask;

    public static (Model model, Command command) Update(Abies.Message message, Model model)
        => message switch
        {
            Message.NextSlide => SetSlide(model, ClampSlide(model.SlideIndex + 1)),
            Message.PrevSlide => SetSlide(model, ClampSlide(model.SlideIndex - 1)),
            Message.GoToSlide goTo => SetSlide(model, ClampSlide(goTo.Index)),
            Message.KeyPressed key => HandleKeyPress(key.Key, key.Repeat, model),
            Message.IncrementDemo => (
                model with
                {
                    DemoCount = model.DemoCount + 1,
                    Log = AddLog(model.Log, "Demo increment", DateTimeOffset.UtcNow)
                },
                Commands.None
            ),
            Message.ResetDemo => (
                model with
                {
                    DemoCount = 0,
                    TickCount = 0,
                    LastTick = null,
                    Log = AddLog(model.Log, "Demo reset", DateTimeOffset.UtcNow)
                },
                Commands.None
            ),
            Message.ToggleDemoTimer => (
                model with
                {
                    DemoTimer = !model.DemoTimer,
                    Log = AddLog(model.Log, $"Timer {(model.DemoTimer ? "off" : "on")}", DateTimeOffset.UtcNow)
                },
                Commands.None
            ),
            Message.ToggleMouse => (
                model with
                {
                    TrackMouse = !model.TrackMouse,
                    Log = AddLog(model.Log, $"Mouse tracking {(model.TrackMouse ? "off" : "on")}", DateTimeOffset.UtcNow)
                },
                Commands.None
            ),
            Message.Tick tick => (
                model with
                {
                    TickCount = model.TickCount + 1,
                    LastTick = tick.Now,
                    Log = AddLog(model.Log, "Tick", tick.Now)
                },
                Commands.None
            ),
            Message.MouseMoved moved => (
                UpdateMouse(model, moved),
                Commands.None
            ),
            Message.ClearLog => (model with { Log = [] }, Commands.None),
            Message.NoOp => (model, Commands.None),
            _ => (model, Commands.None)
        };

    public static Document View(Model model)
    {
        var slide = Slides[model.SlideIndex];
        var slideNumber = model.SlideIndex + 1;
        var progress = (double)slideNumber / Slides.Length * 100;

        return new Document("Abies Presentation",
            div([class_("app")],
            [
                header([class_("topbar")],
                [
                    div([class_("brand")],
                    [
                        img([class_("brand-logo"), src("abies-logo.svg"), alt("Abies logo")]),
                        div([class_("brand-meta")],
                        [
                            div([class_("brand-line")],
                            [
                                span([class_("brand-wordmark")], [text("Abies")]),
                                span([class_("brand-title")], [text("MVU in production")]),
                                FluentBadge([class_("brand-badge"), attribute("appearance", "tint")], [text("Conference edition")])
                            ]),
                            span([class_("brand-subtitle")], [text("Abies conference keynote")])
                        ])
                    ]),
                    div([class_("topbar-actions")],
                    [
                        FluentBadge([class_("pill")], [text($"Slide {slideNumber}/{Slides.Length}")]),
                        div([class_("keys")],
                        [
                            kbd([class_("key")], [text("←")]),
                            kbd([class_("key")], [text("→")]),
                            kbd([class_("key")], [text("Space")])
                        ])
                    ])
                ]),
                div([class_("progress"), role("progressbar"), ariaValuemin("0"), ariaValuemax("100"), ariaValuenow($"{progress:0}"), ariaValuetext($"Slide {slideNumber} of {Slides.Length}")],
                    [div([class_("progress-bar"), Abies.Html.Attributes.style($"width:{progress:0.##}%")], [])]),
                main([class_("deck")],
                [
                    nav([class_("agenda")],
                    [
                        h3([], [text("Agenda")]),
                        ul([], [..Slides.Select((entry, index) =>
                            li([attribute("data-key", $"{index + 1}")],
                                [
                                    a(
                                        [
                                            class_($"agenda-link{(index == model.SlideIndex ? " active" : "")}"),
                                            href($"#slide-{index + 1}"),
                                            ariaCurrent(index == model.SlideIndex ? "page" : "false")
                                        ],
                                        [
                                            span([class_("agenda-index")], [text($"{index + 1:00}")]),
                                            span([class_("agenda-title")], [text(entry.Title)])
                                        ],
                                        $"agenda-link-{index + 1}")
                                ])
                        )])
                    ]),
                    section([class_("content")],
                    [
                        div([class_("content-grid")],
                        [
                            div([class_("slide")], [
                                div([class_("kicker")], [text(slide.Kicker)]),
                                h1([], [text(slide.Title)]),
                                p([class_("subtitle")], [text(slide.Subtitle)]),
                                ul([class_("points")],
                                    [..slide.Points.Select((point, idx) =>
                                        li([attribute("data-key", $"point-{model.SlideIndex + 1}-{idx}")], [text(point)])
                                    )]),
                                slide.Code is null
                                    ? text("")
                                    : div([class_("code-block")],
                                    [
                                        div([class_("code-title")], [text("Snapshot")]),
                                        pre([], [code([], [text(slide.Code)])])
                                    ]),
                                slide.Callout is null
                                    ? text("")
                                    : div([class_("callout")], [text(slide.Callout)])
                            ], $"slide-{slideNumber}"),
                            RenderSidePanel(model, slide)
                        ])
                    ])
                ])
            ]));
    }

    private static Node RenderSidePanel(Model model, Slide slide)
        => slide.Kind == SlideKind.Demo
            ? RenderDemoPanel(model)
            : div([class_("panel")],
            [
                div([class_("panel-title")], [text("Key takeaway")]),
                div([class_("panel-body")],
                [
                    div([class_("takeaway")], [text(slide.Takeaway ?? "—")]),
                    div([class_("next-step-title")], [text("Next step")]),
                    div([class_("next-step")], [text(slide.NextStep ?? "—")])
                ])
            ]);

    private static Node RenderDemoPanel(Model model)
    {
        var logItems = RenderLogItems(model.Log);

        return div([class_("panel demo")],
        [
            div([class_("panel-title")], [text("Live MVU loop")]),
            div([class_("panel-body")],
            [
                div([class_("demo-metrics")],
                [
                    div([class_("demo-metric")],
                    [
                        span([class_("metric-label")], [text("Demo count")]),
                        span([class_("metric-value")], [text(model.DemoCount.ToString())])
                    ]),
                    div([class_("demo-metric")],
                    [
                        span([class_("metric-label")], [text("Ticks")]),
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
                    FluentButton([attribute("appearance", "primary"), onclick(new Message.IncrementDemo())], [text("Dispatch message")]),
                    FluentButton([attribute("appearance", "outline"), onclick(new Message.ResetDemo())], [text("Reset state")])
                ]),
                div([class_("toggle-row")],
                [
                    FluentButton([class_(model.DemoTimer ? "toggle active" : "toggle"), attribute("appearance", "outline"), ariaPressed(model.DemoTimer ? "true" : "false"), onclick(new Message.ToggleDemoTimer())],
                        [text(model.DemoTimer ? "Timer on" : "Timer off")]),
                    FluentButton([class_(model.TrackMouse ? "toggle active" : "toggle"), attribute("appearance", "outline"), ariaPressed(model.TrackMouse ? "true" : "false"), onclick(new Message.ToggleMouse())],
                        [text(model.TrackMouse ? "Mouse on" : "Mouse off")])
                ]),
                div([class_("log")],
                [
                    div([class_("log-header")],
                    [
                        span([], [text("Event log")]),
                        FluentButton([class_("ghost"), attribute("appearance", "subtle"), onclick(new Message.ClearLog())], [text("Clear")])
                    ]),
                    ul([ariaLive("polite")], logItems)
                ])
            ])
        ]);
    }

    private static Element FluentButton(Abies.DOM.Attribute[] attributes, Node[] children, string? id = null)
        => Abies.Html.Elements.element("fluent-button", attributes, children, id);

    private static Element FluentBadge(Abies.DOM.Attribute[] attributes, Node[] children, string? id = null)
        => Abies.Html.Elements.element("fluent-badge", attributes, children, id);

    private static (Model model, Command command) HandleKeyPress(string key, bool repeat, Model model)
        => repeat
            ? (model, Commands.None)
            : key switch
            {
                "ArrowRight" or "PageDown" or " " or "Spacebar" or "Enter" or "l" or "j"
                    => SetSlide(model, ClampSlide(model.SlideIndex + 1)),
                "ArrowLeft" or "PageUp" or "Backspace" or "h" or "k"
                    => SetSlide(model, ClampSlide(model.SlideIndex - 1)),
                "Home" => SetSlide(model, 0),
                "End" => SetSlide(model, Slides.Length - 1),
                _ => (model, Commands.None)
            };

    private static int ClampSlide(int index)
        => Math.Clamp(index, 0, Slides.Length - 1);

    private static (Model model, Command command) SetSlide(Model model, int index)
        => index == model.SlideIndex
            ? (model, Commands.None)
            : (model with { SlideIndex = index }, new Navigation.Command.ReplaceState(Url.Create($"#slide-{index + 1}")));

    private static int? TryGetSlideIndex(Url url)
    {
        var fragment = url.Fragment.Value;
        if (string.IsNullOrWhiteSpace(fragment))
        {
            return null;
        }

        var value = fragment.TrimStart('#');
        if (!value.StartsWith("slide-", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var numberPart = value.Substring("slide-".Length);
        return int.TryParse(numberPart, out var index)
            ? index - 1
            : null;
    }

    private static IReadOnlyList<string> AddLog(IReadOnlyList<string> entries, string entry, DateTimeOffset? at = null)
    {
        var stamp = at?.ToString("HH:mm:ss");
        var formatted = stamp is null ? entry : $"{stamp} {entry}";
        var next = (string[])[formatted, ..entries];
        return next.Length > 8 ? next[..8] : next;
    }

    private static Node[] RenderLogItems(IReadOnlyList<string> entries)
        => entries.Count == 0
            ? [li([class_("log-empty")], [text("No events yet. Trigger a message to see updates.")])]
            : [..entries.Select((entry, idx) => li([attribute("data-key", $"log-{idx}-{entry.GetHashCode()}")], [text(entry)]))];

    private static Model UpdateMouse(Model model, Message.MouseMoved moved)
    {
        var next = model with
        {
            MouseX = (int)moved.Data.ClientX,
            MouseY = (int)moved.Data.ClientY
        };

        var last = model.LastMouseLogAt;
        if (last is not null && moved.At - last.Value < TimeSpan.FromMilliseconds(200))
        {
            return next;
        }

        return next with
        {
            Log = AddLog(next.Log, $"Mouse {next.MouseX}, {next.MouseY}", moved.At),
            LastMouseLogAt = moved.At
        };
    }

    private sealed record Slide(
        string Id,
        string Kicker,
        string Title,
        string Subtitle,
        string[] Points,
        string? Code,
        string? Callout,
        string? Takeaway,
        string? NextStep,
        SlideKind Kind);

    private enum SlideKind
    {
        Intro,
        Concept,
        Demo,
        Outro
    }

}
