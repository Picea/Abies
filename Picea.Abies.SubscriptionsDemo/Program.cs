// =============================================================================
// Subscriptions Demo — Abies MVU Subscription Sources
// =============================================================================
// Demonstrates the subscription system available in Abies:
//   - SubscriptionModule.Every: periodic timer subscription
//   - SubscriptionModule.Batch: combining multiple subscriptions
//   - SubscriptionModule.Create: custom subscription sources
//   - Navigation.UrlChanges: URL change subscription
//
// Planned follow-up: re-add browser subscription demos (OnResize,
// OnVisibilityChange, OnMouseMove, WebSocket) when the BrowserSubscriptions
// module is reintroduced to the framework.
// =============================================================================

using System.Runtime.Versioning;
using Picea;
using Picea.Abies;
using Picea.Abies.Browser;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Events;
using static Picea.Abies.Subscriptions.SubscriptionModule;

[assembly: SupportedOSPlatform("browser")]

await Runtime.Run<SubscriptionsDemo, Model, Arguments>(new Arguments());

/// <summary>
/// Placeholder for startup arguments.
/// </summary>
public record Arguments;

/// <summary>
/// Holds demo state for subscriptions and UI.
/// </summary>
public record Model(
    int TickCount,
    DateTimeOffset? LastTick,
    bool AutoTick,
    int FastTickCount,
    bool FastTickEnabled,
    int CustomSubCount,
    bool CustomSubEnabled,
    IReadOnlyList<string> Events,
    DateTimeOffset? LastEventAt);

/// <summary>
/// MVU messages for subscription events and UI intent.
/// </summary>
public interface Message : Picea.Abies.Message
{
    /// <summary>Emitted on timer ticks (1s interval).</summary>
    sealed record Tick : Message;

    /// <summary>Emitted on fast timer ticks (250ms interval).</summary>
    sealed record FastTick : Message;

    /// <summary>Emitted by the custom subscription source.</summary>
    sealed record CustomEvent(string Value) : Message;

    /// <summary>Toggles the 1s timer subscription.</summary>
    sealed record ToggleAutoTick : Message;

    /// <summary>Toggles the 250ms fast timer subscription.</summary>
    sealed record ToggleFastTick : Message;

    /// <summary>Toggles the custom subscription.</summary>
    sealed record ToggleCustomSub : Message;

    /// <summary>Clears the event log.</summary>
    sealed record ClearEvents : Message;
}

/// <summary>
/// Demo program showcasing available subscription sources.
/// </summary>
public class SubscriptionsDemo : Program<Model, Arguments>
{
    /// <summary>
    /// Builds the initial model with safe defaults.
    /// </summary>
    public static (Model, Command) Initialize(Arguments argument)
        => (new Model(
            TickCount: 0,
            LastTick: null,
            AutoTick: true,
            FastTickCount: 0,
            FastTickEnabled: false,
            CustomSubCount: 0,
            CustomSubEnabled: false,
            Events: [],
            LastEventAt: null), Commands.None);

    /// <summary>
    /// Composes subscriptions based on current model switches.
    /// </summary>
    public static Subscription Subscriptions(Model model)
    {
        var subscriptions = new List<Subscription>();

        if (model.AutoTick)
        {
            subscriptions.Add(Every(TimeSpan.FromSeconds(1), () => new Message.Tick()));
        }

        if (model.FastTickEnabled)
        {
            subscriptions.Add(Every("fast-tick", TimeSpan.FromMilliseconds(250), () => new Message.FastTick()));
        }

        if (model.CustomSubEnabled)
        {
            subscriptions.Add(Create("custom-counter", async (dispatch, ct) =>
            {
                var counter = 0;
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
                while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                {
                    counter++;
                    dispatch(new Message.CustomEvent($"Event #{counter}"));
                }
            }));
        }

        return subscriptions.Count > 0 ? Batch(subscriptions.ToArray()) : None;
    }

    /// <summary>
    /// Updates the model state from messages.
    /// </summary>
    public static (Model, Command) Transition(Model model, Picea.Abies.Message message)
        => message switch
        {
            Message.Tick => (
                model with
                {
                    TickCount = model.TickCount + 1,
                    LastTick = DateTimeOffset.UtcNow,
                    Events = AddEvent(model.Events, "Tick"),
                    LastEventAt = DateTimeOffset.UtcNow
                },
                Commands.None
            ),
            Message.FastTick => (
                model with
                {
                    FastTickCount = model.FastTickCount + 1,
                    Events = AddEvent(model.Events, "Fast tick"),
                    LastEventAt = DateTimeOffset.UtcNow
                },
                Commands.None
            ),
            Message.CustomEvent e => (
                model with
                {
                    CustomSubCount = model.CustomSubCount + 1,
                    Events = AddEvent(model.Events, $"Custom: {e.Value}"),
                    LastEventAt = DateTimeOffset.UtcNow
                },
                Commands.None
            ),
            Message.ToggleAutoTick => (
                model with
                {
                    AutoTick = !model.AutoTick,
                    Events = AddEvent(model.Events, $"Timer {(model.AutoTick ? "off" : "on")}"),
                    LastEventAt = DateTimeOffset.UtcNow
                },
                Commands.None
            ),
            Message.ToggleFastTick => (
                model with
                {
                    FastTickEnabled = !model.FastTickEnabled,
                    FastTickCount = 0,
                    Events = AddEvent(model.Events, $"Fast timer {(model.FastTickEnabled ? "off" : "on")}"),
                    LastEventAt = DateTimeOffset.UtcNow
                },
                Commands.None
            ),
            Message.ToggleCustomSub => (
                model with
                {
                    CustomSubEnabled = !model.CustomSubEnabled,
                    CustomSubCount = 0,
                    Events = AddEvent(model.Events, $"Custom sub {(model.CustomSubEnabled ? "off" : "on")}"),
                    LastEventAt = DateTimeOffset.UtcNow
                },
                Commands.None
            ),
            Message.ClearEvents => (
                model with { Events = [] },
                Commands.None
            ),
            _ => (model, Commands.None)
        };

    public static Result<Picea.Abies.Message[], Picea.Abies.Message> Decide(Model _, Picea.Abies.Message command) =>
        Result<Picea.Abies.Message[], Picea.Abies.Message>.Ok([command]);

    public static bool IsTerminal(Model _) => false;

    /// <summary>
    /// Renders the demo UI.
    /// </summary>
    public static Document View(Model model)
        => new("Subscriptions Demo",
            div([class_("container")], [
                h1([], [text("Subscriptions Demo")]),
                p([], [text("Live updates via timer and custom subscription sources.")]),
                div([class_("controls")], [
                    button([type("button"), onclick(new Message.ToggleAutoTick())], [
                        text($"Timer (1s): {(model.AutoTick ? "on" : "off")}")
                    ]),
                    button([type("button"), onclick(new Message.ToggleFastTick())], [
                        text($"Fast timer (250ms): {(model.FastTickEnabled ? "on" : "off")}")
                    ]),
                    button([type("button"), onclick(new Message.ToggleCustomSub())], [
                        text($"Custom sub (3s): {(model.CustomSubEnabled ? "on" : "off")}")
                    ]),
                    button([type("button"), onclick(new Message.ClearEvents())], [
                        text("Clear events")
                    ])
                ]),
                div([class_("stats")], [
                    p([], [text($"Ticks: {model.TickCount}")]),
                    p([], [text($"Fast ticks: {model.FastTickCount}")]),
                    p([], [text($"Custom events: {model.CustomSubCount}")]),
                    p([], [text($"Last tick: {(model.LastTick?.ToString("HH:mm:ss.fff") ?? "n/a")}")]),
                    p([], [text($"Last event: {(model.LastEventAt?.ToString("HH:mm:ss.fff") ?? "n/a")}")])
                ]),
                h2([], [text("Event log")]),
                ul([class_("events")],
                    [..model.Events.Select(entry => li([], [text(entry)]))])
            ])
        );

    /// <summary>
    /// Adds a timestamped entry to the event log.
    /// </summary>
    private static IReadOnlyList<string> AddEvent(IReadOnlyList<string> events, string entry)
    {
        var stamp = DateTimeOffset.UtcNow.ToString("HH:mm:ss.fff");
        var formatted = $"{stamp} {entry}";
        var next = (string[])[formatted, .. events];
        return next.Length > 12 ? next[..12] : next;
    }
}
