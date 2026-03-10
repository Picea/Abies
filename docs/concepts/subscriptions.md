# Subscriptions

Subscriptions connect the MVU loop to external event sources: timers, WebSockets, browser events, server-sent events, and any other ongoing stream of data.

## The Problem

View describes what the UI looks like. Transition describes how the model changes. But how does the application react to events that originate _outside_ user interaction — a clock ticking, a WebSocket message arriving, a window resizing?

## The Solution: Subscriptions

The `Subscriptions` function declares which external event sources the application wants to listen to, based on the current model:

```csharp
public static Subscription Subscriptions(Model model)
    => model.IsRunning
        ? SubscriptionModule.Every(TimeSpan.FromSeconds(1), () => new Tick())
        : SubscriptionModule.None;
```

When the model changes, the runtime calls `Subscriptions(model)` again and **diffs** the result against the previous set. New subscriptions are started, removed subscriptions are stopped, and unchanged subscriptions continue running.

## The Subscription Type Hierarchy

Subscriptions are an algebraic data type with three variants:

```text
Subscription
├── None                              — no subscriptions (identity element)
├── Batch(IReadOnlyList<Subscription>) — multiple subscriptions (binary operation)
└── Source(SubscriptionKey, Start)     — a single subscription source
```

This forms a **monoid**:

| Property | Value |
| -------- | ----- |
| Identity | `Subscription.None` |
| Binary operation | `Subscription.Batch([...])` |

The monoid structure means subscriptions compose cleanly — you can combine any number of subscriptions without special-casing.

## Creating Subscriptions

### No Subscriptions

```csharp
public static Subscription Subscriptions(Model model)
    => SubscriptionModule.None;
```

### Periodic Timer

The built-in `Every` factory creates a timer subscription using `PeriodicTimer` for efficient, low-allocation periodic scheduling:

```csharp
// Tick every second
SubscriptionModule.Every(TimeSpan.FromSeconds(1), () => new Tick())

// With custom key (for multiple timers at the same interval)
SubscriptionModule.Every("animation-timer", TimeSpan.FromMilliseconds(16), () => new AnimationFrame())
```

The key is auto-generated as `"every:{interval.TotalMilliseconds}"` for the simple overload. Use the keyed overload when you have multiple timers with the same interval.

### Custom Subscription

For anything beyond timers, use `Create` with a custom start function:

```csharp
SubscriptionModule.Create("websocket", async (dispatch, cancellationToken) =>
{
    using var ws = new ClientWebSocket();
    await ws.ConnectAsync(uri, cancellationToken);

    var buffer = new byte[4096];
    while (!cancellationToken.IsCancellationRequested)
    {
        var result = await ws.ReceiveAsync(buffer, cancellationToken);
        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
        dispatch(new WebSocketMessage(json));
    }
});
```

The `StartSubscription` delegate signature:

```csharp
public delegate Task StartSubscription(Dispatch dispatch, CancellationToken cancellationToken);
```

- **`dispatch`** — Call this to feed messages into the MVU loop
- **`cancellationToken`** — Cancelled when the subscription should stop (the runtime handles this)

### Batching Subscriptions

Combine multiple subscriptions:

```csharp
public static Subscription Subscriptions(Model model)
    => SubscriptionModule.Batch(
        SubscriptionModule.Every(TimeSpan.FromSeconds(1), () => new Tick()),
        SubscriptionModule.Create("resize", ListenForResize),
        model.IsConnected
            ? SubscriptionModule.Create("ws", ConnectWebSocket)
            : SubscriptionModule.None
    );
```

The `Batch` factory is smart:

| Input | Returns |
| ----- | ------- |
| 0 subscriptions | `None` |
| 1 subscription | That subscription directly (no wrapper) |
| N subscriptions | `Batch(subscriptions)` |

## Subscription Keys and Lifecycle

Every subscription source has a `SubscriptionKey` that uniquely identifies it. The runtime's **SubscriptionManager** uses these keys to diff subscriptions:

```text
Previous subscriptions: { "every:1000", "ws" }
New subscriptions:      { "every:1000", "resize" }

Result:
  Keep: "every:1000"    (same key → running task continues)
  Stop: "ws"            (removed → cancellation token triggered)
  Start: "resize"       (new → start function called)
```

This means:

- **Same key, same render** → Subscription keeps running uninterrupted
- **Key disappears** → Subscription is cancelled via `CancellationToken`
- **New key appears** → Subscription is started

### Key Design

Choose keys that reflect the subscription's identity:

```csharp
// Good: descriptive, stable keys
SubscriptionModule.Create("chat-room:123", ConnectToRoom(123))
SubscriptionModule.Create("notifications", ListenForNotifications)

// Bad: keys that change every render (causes stop/restart)
SubscriptionModule.Create(Guid.NewGuid().ToString(), ...)  // ❌ New key each time!
```

## Conditional Subscriptions

Since `Subscriptions` is a pure function of the model, subscriptions naturally start and stop based on model state:

```csharp
public static Subscription Subscriptions(Model model)
    => model.CurrentPage switch
    {
        Page.Dashboard => SubscriptionModule.Batch(
            SubscriptionModule.Every(TimeSpan.FromSeconds(30), () => new RefreshData()),
            SubscriptionModule.Create("alerts", ListenForAlerts)),

        Page.Chat room => SubscriptionModule.Create(
            $"chat:{room.Id}",
            (dispatch, ct) => ConnectToChat(room.Id, dispatch, ct)),

        _ => SubscriptionModule.None
    };
```

When the user navigates from Dashboard to Chat:
1. `RefreshData` timer is stopped (key removed)
2. `alerts` listener is stopped (key removed)
3. `chat:{room.Id}` connection is started (new key)

## Platform Independence

Subscriptions work identically across all render modes:

| Render Mode | Subscription Host |
| ----------- | ----------------- |
| `InteractiveWasm` | Runs in browser WASM runtime |
| `InteractiveServer` | Runs on server, messages sent over WebSocket |
| `InteractiveAuto` | Server initially, then browser after WASM loads |
| `Static` | Not applicable (no interactivity) |

The same `Subscriptions` function works everywhere because it's pure — it only depends on the model.

## Testing Subscriptions

### Test Subscription Selection

```csharp
[Fact]
public void RunningModel_HasTimerSubscription()
{
    var model = new Model(IsRunning: true);

    var sub = MyApp.Subscriptions(model);

    Assert.IsType<Subscription.Source>(sub);
    Assert.Equal("every:1000", ((Subscription.Source)sub).Key.Value);
}

[Fact]
public void StoppedModel_HasNoSubscriptions()
{
    var model = new Model(IsRunning: false);

    var sub = MyApp.Subscriptions(model);

    Assert.IsType<Subscription.None>(sub);
}
```

### Test with Runtime

```csharp
[Fact]
public async Task Timer_DispatchesTickMessages()
{
    var patches = new List<IReadOnlyList<Patch>>();
    var runtime = await Runtime<ClockApp, Model, Unit>.Start(
        apply: p => patches.Add(p));

    // Start the timer by dispatching a Start message
    await runtime.Dispatch(new Start());

    // Wait for a few ticks
    await Task.Delay(3500);

    // Model should have received tick messages
    Assert.True(runtime.Model.TickCount >= 3);
}
```

## Commands vs Subscriptions

| Aspect | Commands | Subscriptions |
| ------ | -------- | ------------- |
| Trigger | Once, from Transition | Continuous, based on model |
| Lifetime | Fire-and-forget | Active while key present |
| Lifecycle | Created → Interpreted → Done | Started → Running → Stopped |
| Use case | API calls, navigation | Timers, events, sockets |
| Pure side | Transition returns command | Subscriptions returns set |
| Impure side | Interpreter executes | Start function runs |

## Best Practices

### 1. Use Stable Keys

```csharp
// ✅ Stable key based on data identity
SubscriptionModule.Create($"chat:{room.Id}", ...)

// ❌ Unstable key causes constant restart
SubscriptionModule.Create($"chat:{DateTime.Now}", ...)
```

### 2. Handle Cancellation Gracefully

```csharp
SubscriptionModule.Create("stream", async (dispatch, ct) =>
{
    try
    {
        await foreach (var item in stream.ReadAllAsync(ct))
        {
            dispatch(new ItemReceived(item));
        }
    }
    catch (OperationCanceledException)
    {
        // Normal shutdown — subscription was removed from the model
    }
});
```

### 3. Keep Subscription Logic Simple

Subscriptions should dispatch messages, not make decisions:

```csharp
// ✅ Simple: dispatch raw events
(dispatch, ct) => {
    ws.OnMessage += msg => dispatch(new WsMessage(msg));
}

// ❌ Complex: making decisions in the subscription
(dispatch, ct) => {
    ws.OnMessage += msg => {
        if (msg.Type == "error") dispatch(new ShowError(msg));
        else if (msg.Type == "data") dispatch(new UpdateData(msg));
    };
}
```

Let Transition handle the decision logic — that's where it's testable.

## Summary

Subscriptions are the MVU answer to external event sources:

- **Declared as a function of the model** (pure)
- **Diffed by key** — automatic start/stop lifecycle
- **Platform-agnostic** — same code for browser and server
- **Composable** — monoid structure (None + Batch)

## See Also

- [Commands and Effects](./commands-effects.md) — One-time side effects
- [MVU Architecture](./mvu-architecture.md) — The overall pattern
- [Pure Functions](./pure-functions.md) — Why Subscriptions is a pure function
