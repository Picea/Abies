using System.Runtime.Versioning;
using Abies;
using Abies.DOM;
using Abies.Html;
using static Abies.Html.Attributes;
using static Abies.Html.Elements;
using static Abies.Html.Events;

[assembly: SupportedOSPlatform("browser")]

// Bootstrap the demo app.
await Runtime.Run<SubscriptionsDemo, Arguments, Model>(new Arguments());

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
    bool IsVisible,
    int Width,
    int Height,
    bool AutoTick,
    bool TrackMouse,
    int MouseX,
    int MouseY,
    bool WebSocketEnabled,
    bool MockWebSocketEnabled,
    string WebSocketUrl,
    IReadOnlyList<string> Events,
    DateTimeOffset? LastEventAt,
    DateTimeOffset? LastMouseLogAt);

/// <summary>
/// MVU messages for subscription events and UI intent.
/// </summary>
public interface Message : Abies.Message
{
    /// <summary>
    /// Emitted on timer ticks.
    /// </summary>
    sealed record Tick(DateTimeOffset Now) : Message;
    /// <summary>
    /// Emitted on visibility changes.
    /// </summary>
    sealed record VisibilityChanged(VisibilityState State, DateTimeOffset At) : Message;
    /// <summary>
    /// Emitted on resize events.
    /// </summary>
    sealed record Resized(ViewportSize Size, DateTimeOffset At) : Message;
    /// <summary>
    /// Emitted on mouse movement.
    /// </summary>
    sealed record MouseMoved(PointerEventData Data, DateTimeOffset At) : Message;
    /// <summary>
    /// Toggles the timer subscription.
    /// </summary>
    sealed record ToggleAutoTick : Message;
    /// <summary>
    /// Toggles mouse tracking.
    /// </summary>
    sealed record ToggleMouse : Message;
    /// <summary>
    /// Toggles the WebSocket subscription.
    /// </summary>
    sealed record ToggleWebSocket : Message;
    /// <summary>
    /// Toggles the mock WebSocket subscription.
    /// </summary>
    sealed record ToggleMockWebSocket : Message;
    /// <summary>
    /// Updates the WebSocket URL input.
    /// </summary>
    sealed record WebSocketUrlChanged(string Value) : Message;
    /// <summary>
    /// Emitted for WebSocket events.
    /// </summary>
    sealed record SocketEvent(WebSocketEvent Event, DateTimeOffset At) : Message;
    /// <summary>
    /// Emits mock WebSocket events on a timer.
    /// </summary>
    sealed record MockSocketTick(DateTimeOffset At) : Message;
    /// <summary>
    /// Clears the event log.
    /// </summary>
    sealed record ClearEvents : Message;
    /// <summary>
    /// No-op message for unused hooks.
    /// </summary>
    sealed record NoOp : Message;
}

/// <summary>
/// Demo program showcasing subscription sources.
/// </summary>
public class SubscriptionsDemo : Program<Model, Arguments>
{
    /// <summary>
    /// Builds the initial model with safe defaults.
    /// </summary>
    public static (Model, Command) Initialize(Url url, Arguments argument)
        => (new Model(
            TickCount: 0,
            LastTick: null,
            IsVisible: true,
            Width: 0,
            Height: 0,
            AutoTick: true,
            TrackMouse: false,
            MouseX: 0,
            MouseY: 0,
            WebSocketEnabled: false,
            MockWebSocketEnabled: false,
            WebSocketUrl: "wss://echo.websocket.events",
            Events: [],
            LastEventAt: null,
            LastMouseLogAt: null), Commands.None);

    /// <summary>
    /// Ignores navigation events in this demo.
    /// </summary>
    public static Abies.Message OnLinkClicked(UrlRequest urlRequest) => new Message.NoOp();

    /// <summary>
    /// Ignores URL changes in this demo.
    /// </summary>
    public static Abies.Message OnUrlChanged(Url url) => new Message.NoOp();

    /// <summary>
    /// Composes subscriptions based on current model switches.
    /// </summary>
    public static Subscription Subscriptions(Model model)
    {
        var subscriptions = new List<Subscription>
        {
            SubscriptionModule.OnResize(size => new Message.Resized(size, DateTimeOffset.UtcNow)),
            SubscriptionModule.OnVisibilityChange(evt => new Message.VisibilityChanged(evt.State, DateTimeOffset.UtcNow))
        };

        if (model.AutoTick)
        {
            subscriptions.Add(SubscriptionModule.Every(TimeSpan.FromMilliseconds(250), now => new Message.Tick(now)));
        }

        if (model.TrackMouse)
        {
            subscriptions.Add(SubscriptionModule.OnMouseMove(evt => new Message.MouseMoved(evt, DateTimeOffset.UtcNow)));
        }

        if (model.WebSocketEnabled && !string.IsNullOrWhiteSpace(model.WebSocketUrl))
        {
            subscriptions.Add(SubscriptionModule.WebSocket(
                new WebSocketOptions(model.WebSocketUrl),
                evt => new Message.SocketEvent(evt, DateTimeOffset.UtcNow)));
        }

        if (model.MockWebSocketEnabled)
        {
            subscriptions.Add(SubscriptionModule.Every(
                "mock-websocket",
                TimeSpan.FromSeconds(2),
                now => new Message.MockSocketTick(now)));
        }

        return SubscriptionModule.Batch(subscriptions);
    }

    /// <summary>
    /// Updates the model state from messages.
    /// </summary>
    public static (Model model, Command command) Update(Abies.Message message, Model model)
        => message switch
        {
            Message.Tick tick => (
                model with
                {
                    TickCount = model.TickCount + 1,
                    LastTick = tick.Now,
                    Events = AddEvent(model.Events, "Tick", tick.Now),
                    LastEventAt = tick.Now
                },
                Commands.None
            ),
            Message.VisibilityChanged visibility => (
                model with
                {
                    IsVisible = visibility.State == VisibilityState.Visible,
                    Events = AddEvent(model.Events, $"Visibility {visibility.State}", visibility.At),
                    LastEventAt = visibility.At
                },
                Commands.None
            ),
            Message.Resized resized => (
                model with
                {
                    Width = resized.Size.Width,
                    Height = resized.Size.Height,
                    Events = AddEvent(model.Events, $"Resize {resized.Size.Width}x{resized.Size.Height}", resized.At),
                    LastEventAt = resized.At
                },
                Commands.None
            ),
            Message.MouseMoved moved => (
                UpdateMouse(model, moved),
                Commands.None
            ),
            Message.ToggleAutoTick => (
                model with
                {
                    AutoTick = !model.AutoTick,
                    Events = AddEvent(model.Events, $"Auto tick {(model.AutoTick ? "off" : "on")}", DateTimeOffset.UtcNow),
                    LastEventAt = DateTimeOffset.UtcNow
                },
                Commands.None
            ),
            Message.ToggleMouse => (
                model with
                {
                    TrackMouse = !model.TrackMouse,
                    Events = AddEvent(model.Events, $"Mouse tracking {(model.TrackMouse ? "off" : "on")}", DateTimeOffset.UtcNow),
                    LastEventAt = DateTimeOffset.UtcNow
                },
                Commands.None
            ),
            Message.ToggleWebSocket => (
                model with
                {
                    WebSocketEnabled = !model.WebSocketEnabled,
                    Events = AddEvent(model.Events, $"WebSocket {(model.WebSocketEnabled ? "off" : "on")}", DateTimeOffset.UtcNow),
                    LastEventAt = DateTimeOffset.UtcNow
                },
                Commands.None
            ),
            Message.ToggleMockWebSocket => (
                model with
                {
                    MockWebSocketEnabled = !model.MockWebSocketEnabled,
                    Events = AddEvent(model.Events, $"Mock WebSocket {(model.MockWebSocketEnabled ? "off" : "on")}", DateTimeOffset.UtcNow),
                    LastEventAt = DateTimeOffset.UtcNow
                },
                Commands.None
            ),
            Message.WebSocketUrlChanged url => (
                model with { WebSocketUrl = url.Value },
                Commands.None
            ),
            Message.SocketEvent socketEvent => (
                model with
                {
                    Events = AddEvent(model.Events, DescribeSocketEvent(socketEvent.Event), socketEvent.At),
                    LastEventAt = socketEvent.At
                },
                Commands.None
            ),
            Message.MockSocketTick tick => (
                model with
                {
                    Events = AddEvent(model.Events, $"WebSocket {WebSocketMessageKind.Text}: mock {tick.At:HH:mm:ss}", tick.At),
                    LastEventAt = tick.At
                },
                Commands.None
            ),
            Message.ClearEvents => (
                model with { Events = [] },
                Commands.None
            ),
            Message.NoOp => (model, Commands.None),
            _ => (model, Commands.None)
        };

    /// <summary>
    /// Renders the demo UI.
    /// </summary>
    public static Document View(Model model)
        => new("Subscriptions Demo",
            div([class_("container")], [
                h1([], [text("Subscriptions Demo")]),
                p([], [text("Live updates via timer, visibility, resize, mouse, and WebSocket subscriptions.")]),
                div([class_("controls")], [
                    button([type("button"), onclick(new Message.ToggleAutoTick())], [
                        text($"Auto tick: {(model.AutoTick ? "on" : "off")}")
                    ]),
                    button([type("button"), onclick(new Message.ToggleMouse())], [
                        text($"Mouse tracking: {(model.TrackMouse ? "on" : "off")}")
                    ]),
                    button([type("button"), onclick(new Message.ToggleWebSocket())], [
                        text($"WebSocket: {(model.WebSocketEnabled ? "on" : "off")}")
                    ]),
                    button([type("button"), onclick(new Message.ToggleMockWebSocket())], [
                        text($"Mock WebSocket: {(model.MockWebSocketEnabled ? "on" : "off")}")
                    ]),
                    button([type("button"), onclick(new Message.ClearEvents())], [
                        text("Clear events")
                    ])
                ]),
                div([class_("stats")], [
                    p([], [text($"Visible: {model.IsVisible}")]),
                    p([], [text($"Viewport: {model.Width}x{model.Height}")]),
                    p([], [text($"Mouse: {model.MouseX}, {model.MouseY}")]),
                    p([], [text($"Ticks: {model.TickCount}")]),
                    p([], [text($"Last tick: {(model.LastTick?.ToString("HH:mm:ss.fff") ?? "n/a")}")]),
                    p([], [text($"Last event: {(model.LastEventAt?.ToString("HH:mm:ss.fff") ?? "n/a")}")])
                ]),
                div([class_("websocket")], [
                    label([], [text("WebSocket URL")]),
                    input([
                        type("text"),
                        value(model.WebSocketUrl),
                        oninput(data => new Message.WebSocketUrlChanged(data?.Value ?? string.Empty))
                    ])
                ]),
                h2([], [text("Event log")]),
                ul([class_("events")],
                    [..model.Events.Select(entry => li([], [text(entry)]))])
            ])
        );

    // Add a timestamped entry to the event log.
    private static IReadOnlyList<string> AddEvent(IReadOnlyList<string> events, string entry, DateTimeOffset? at = null)
    {
        var stamp = at?.ToString("HH:mm:ss.fff");
        var formatted = stamp is null ? entry : $"{stamp} {entry}";
        var next = (string[])[formatted, .. events];
        return next.Length > 12 ? next[..12] : next;
    }

    // Throttle mouse move logging while keeping coordinates fresh.
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
            Events = AddEvent(next.Events, $"Mouse {next.MouseX}, {next.MouseY}", moved.At),
            LastEventAt = moved.At,
            LastMouseLogAt = moved.At
        };
    }

    // Summarize WebSocket events for the log.
    private static string DescribeSocketEvent(WebSocketEvent evt)
        => evt switch
        {
            WebSocketEvent.Opened => "WebSocket opened",
            WebSocketEvent.Errored => "WebSocket error",
            WebSocketEvent.Closed closed => $"WebSocket closed ({closed.Code}) {closed.Reason}",
            WebSocketEvent.MessageReceived message => $"WebSocket {message.Kind}: {message.Data}",
            _ => "WebSocket event"
        };
}
