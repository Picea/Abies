# Tutorial 6: Subscriptions

Learn how to connect your application to external event sources like timers, WebSockets, and browser APIs.

**Prerequisites:** [Tutorial 5: Forms](05-forms.md)

**Time:** 25 minutes

**What you'll learn:**

- What subscriptions are and how they differ from events
- The subscription lifecycle (start, keep, stop)
- Using `SubscriptionModule.Every` for timers
- Using `SubscriptionModule.Create` for custom event sources
- Combining subscriptions with `SubscriptionModule.Batch`
- Conditional subscriptions based on model state

## What Are Subscriptions?

In Abies, the `View` function describes what the UI looks like. But what about events that don't come from the UI? Timers, WebSocket messages, keyboard shortcuts, server-sent events — these are **external event sources** that exist outside the DOM.

**Subscriptions** connect your application to these external sources. Each render cycle, the runtime calls your `Subscriptions(model)` function to get the set of active subscriptions. The runtime then:

1. **Starts** new subscriptions that weren't active before
2. **Keeps** subscriptions that are still active (same key)
3. **Stops** subscriptions that are no longer active

This declarative approach means subscriptions are managed the same way as the DOM — you describe *what* you want, and the runtime figures out the minimal changes.

> **Principle:** Subscriptions form a [Monoid](https://en.wikipedia.org/wiki/Monoid) with `SubscriptionModule.None` as the identity and `SubscriptionModule.Batch` as the binary operation. This is the same algebraic structure as commands — you can combine any number of subscriptions without special-casing zero, one, or many.

## The Subscription API

Abies provides four factory methods:

| Method | Purpose |
| --- | --- |
| `SubscriptionModule.None` | No subscriptions (identity element) |
| `SubscriptionModule.Batch(...)` | Combine multiple subscriptions |
| `SubscriptionModule.Every(interval, msgFactory)` | Periodic timer |
| `SubscriptionModule.Create(key, startFn)` | Custom event source |

## Example 1: A Clock with `Every`

Let's build a clock that updates every second.

### Model and Messages

```csharp
using Abies.DOM;
using Abies.Subscriptions;
using Automaton;
using static Abies.Html.Attributes;
using static Abies.Html.Elements;

namespace ClockApp;

public record Model(DateTime CurrentTime, bool IsRunning);

public interface ClockMessage : Message;
public record Tick : ClockMessage;
public record ToggleClock : ClockMessage;
```

### Transition

```csharp
public sealed class Clock : Program<Model, Unit>
{
    public static (Model, Command) Initialize(Unit _) =>
        (new Model(DateTime.Now, IsRunning: true), Commands.None);

    public static (Model, Command) Transition(Model model, Message message) =>
        message switch
        {
            Tick => (model with { CurrentTime = DateTime.Now }, Commands.None),
            ToggleClock => (model with { IsRunning = !model.IsRunning }, Commands.None),
            _ => (model, Commands.None)
        };
```

### Subscriptions — The Key Part

```csharp
    public static Subscription Subscriptions(Model model) =>
        model.IsRunning
            ? SubscriptionModule.Every(
                TimeSpan.FromSeconds(1),
                () => new Tick())
            : SubscriptionModule.None;
```

When `IsRunning` is true, the timer is active. When the user toggles it off, the subscription disappears from the returned set, and the runtime automatically stops the timer.

**How `Every` works internally:**

1. Creates a `PeriodicTimer` with the given interval
2. On each tick, calls the message factory (`() => new Tick()`)
3. Dispatches the message into the MVU loop
4. Runs until the cancellation token is triggered (when the subscription is removed)

**Automatic key generation:** `Every` generates a key based on the interval: `"every:1000"` for 1-second intervals. If you have multiple timers with the same interval, use the overload with a custom key:

```csharp
SubscriptionModule.Every(
    key: "animation-timer",
    TimeSpan.FromMilliseconds(16),
    () => new AnimationFrame())
```

### View

```csharp
    public static Document View(Model model) =>
        new("Clock",
            div([class_("clock")],
            [
                h1([], [text(model.CurrentTime.ToString("HH:mm:ss"))]),
                button([
                    onclick(new ToggleClock())
                ], [text(model.IsRunning ? "Pause" : "Resume")])
            ]));
}
```

## Example 2: Custom Subscriptions with `Create`

For event sources beyond timers, use `SubscriptionModule.Create`. It takes a key and a start function:

```csharp
SubscriptionModule.Create(key, (dispatch, cancellationToken) => { ... })
```

The start function receives:

- **`dispatch`** — A function to send messages into the MVU loop
- **`cancellationToken`** — Cancelled when the subscription should stop

### WebSocket Example

```csharp
public static Subscription Subscriptions(Model model) =>
    model.IsConnected
        ? SubscriptionModule.Create(
            "chat-websocket",
            async (dispatch, ct) =>
            {
                using var ws = new ClientWebSocket();
                await ws.ConnectAsync(
                    new Uri("wss://chat.example.com/ws"), ct);

                var buffer = new byte[4096];
                while (!ct.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(buffer, ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    var text = Encoding.UTF8.GetString(
                        buffer, 0, result.Count);
                    dispatch(new ChatMessageReceived(text));
                }
            })
        : SubscriptionModule.None;
```

**Key behaviors:**

- When `model.IsConnected` becomes `true`, the runtime starts the WebSocket subscription
- When it becomes `false`, the runtime cancels the token, which closes the WebSocket
- The `dispatch` function sends messages back into `Transition` from the background task
- The string key `"chat-websocket"` identifies this subscription for diffing

### Server-Sent Events Example

```csharp
public static Subscription Subscriptions(Model model) =>
    SubscriptionModule.Create(
        "notifications-sse",
        async (dispatch, ct) =>
        {
            using var client = new HttpClient();
            using var stream = await client.GetStreamAsync(
                "https://api.example.com/events", ct);
            using var reader = new StreamReader(stream);

            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line?.StartsWith("data: ") == true)
                {
                    var data = line[6..];
                    dispatch(new NotificationReceived(data));
                }
            }
        });
```

## Combining Subscriptions with `Batch`

Use `SubscriptionModule.Batch` to combine multiple subscriptions:

```csharp
public static Subscription Subscriptions(Model model) =>
    SubscriptionModule.Batch(
        // Always run: update clock every second
        SubscriptionModule.Every(
            TimeSpan.FromSeconds(1),
            () => new Tick()),

        // Conditional: only when connected
        model.IsConnected
            ? SubscriptionModule.Create("websocket", ConnectWebSocket)
            : SubscriptionModule.None,

        // Conditional: only when on the dashboard
        model.CurrentPage is Page.Dashboard
            ? SubscriptionModule.Every(
                key: "poll-stats",
                TimeSpan.FromSeconds(30),
                () => new PollStats())
            : SubscriptionModule.None
    );
```

`Batch` is smart about its inputs:

- Passing zero subscriptions returns `None`
- Passing one subscription returns it directly (no wrapper)
- Passing multiple creates a `Batch` node

## The Subscription Lifecycle

The runtime diffs subscriptions by their **key** (the `SubscriptionKey` string):

```
Previous cycle: { "every:1000", "websocket" }
Current cycle:  { "every:1000", "poll-stats:30000" }

→ "every:1000"     — same key, KEEP running
→ "websocket"      — removed, STOP (cancel token)
→ "poll-stats:30000" — new key, START
```

This is exactly how the virtual DOM diff works for elements — same key means keep, missing means remove, new means create. The runtime uses the same reconciliation principle for both UI and subscriptions.

## Conditional Subscriptions

Subscriptions that depend on model state are automatically managed:

```csharp
public static Subscription Subscriptions(Model model) =>
    model switch
    {
        // Game is playing: run the game loop
        { GameState: GameState.Playing } =>
            SubscriptionModule.Every(
                key: "game-loop",
                TimeSpan.FromMilliseconds(16),  // ~60 FPS
                () => new GameTick()),

        // Game is paused: no subscriptions
        { GameState: GameState.Paused } =>
            SubscriptionModule.None,

        // Menu: no subscriptions
        _ => SubscriptionModule.None
    };
```

When the game transitions from `Playing` to `Paused`, the `Subscriptions` function returns `None`, and the runtime automatically stops the game loop timer. When it transitions back, the timer is restarted.

## Navigation Subscription

URL changes are delivered via a subscription:

```csharp
public static Subscription Subscriptions(Model model) =>
    Navigation.UrlChanges(url => new UrlChanged(url));
```

`Navigation.UrlChanges` is a convenience wrapper around `SubscriptionModule.Create` that listens for browser `popstate` events and intercepted link clicks. It's keyed as `"navigation:urlChanges"`.

## Testing Subscriptions

You can test the `Subscriptions` function like any other pure function — verify it returns the right subscription structure based on the model:

```csharp
[Fact]
public void Subscriptions_WhenRunning_ReturnsTimerSubscription()
{
    var model = new Model(DateTime.Now, IsRunning: true);

    var sub = Clock.Subscriptions(model);

    Assert.IsType<Subscription.Source>(sub);
}

[Fact]
public void Subscriptions_WhenPaused_ReturnsNone()
{
    var model = new Model(DateTime.Now, IsRunning: false);

    var sub = Clock.Subscriptions(model);

    Assert.IsType<Subscription.None>(sub);
}
```

For integration tests, verify that subscription messages flow correctly through the `Transition` function:

```csharp
[Fact]
public void Tick_UpdatesCurrentTime()
{
    var oldTime = DateTime.Now.AddMinutes(-5);
    var model = new Model(oldTime, IsRunning: true);

    var (newModel, _) = Clock.Transition(model, new Tick());

    Assert.True(newModel.CurrentTime > oldTime);
}
```

## Exercises

1. **Countdown timer** — Build a countdown that starts from a user-entered number, decrements every second, and stops at zero. Use conditional subscriptions to stop the timer automatically.

2. **Auto-save** — Add a subscription that saves form data every 30 seconds (using a command + interpreter). Only active when there are unsaved changes.

3. **Polling** — Poll an API endpoint every 10 seconds for new data. Show a "last updated" timestamp.

4. **Multiple timers** — Create an app with a stopwatch and a countdown running simultaneously. Use `Batch` with different keys.

## Key Concepts

| Concept | In This Tutorial |
| --- | --- |
| `SubscriptionModule.None` | No subscriptions (identity element) |
| `SubscriptionModule.Every` | Periodic timer with auto-generated or custom key |
| `SubscriptionModule.Create` | Custom event source (WebSocket, SSE, etc.) |
| `SubscriptionModule.Batch` | Combine multiple subscriptions |
| `dispatch(message)` | Send messages from background tasks into the MVU loop |
| Key-based diffing | Same key = keep running; missing = stop; new = start |
| Conditional subs | Return `None` to stop, return a source to start |

## Next Steps

→ [Tutorial 7: Real-World App](07-real-world-app.md) — See all these concepts come together in a production application