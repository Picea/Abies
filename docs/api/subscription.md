# Subscription API

Subscriptions connect the MVU loop to external event sources: timers, WebSocket connections, browser events, server-sent events, and any other asynchronous data stream. They are the declarative counterpart to commands — while commands represent one-shot side effects, subscriptions represent ongoing event sources.

## Subscription Type

```csharp
public abstract record Subscription
{
    public sealed record None : Subscription;
    public sealed record Batch(IReadOnlyList<Subscription> Subscriptions) : Subscription;
    public sealed record Source(SubscriptionKey Key, StartSubscription Start) : Subscription;
}
```

| Variant | Description |
|---------|-------------|
| `None` | No subscriptions. Identity element of the subscription monoid. |
| `Batch` | Multiple subscriptions combined. Binary operation of the subscription monoid. |
| `Source` | A single subscription source identified by a unique key. |

### Monoid Structure

Like commands, subscriptions form a **monoid**:

- **Identity:** `Subscription.None` — no subscriptions.
- **Binary operation:** `Subscription.Batch` — combines subscriptions.

## SubscriptionModule Factory

The `SubscriptionModule` static class provides ergonomic factory methods:

```csharp
public static class SubscriptionModule
{
    public static readonly Subscription None;
    public static Subscription Batch(params Subscription[] subscriptions);
    public static Subscription Create(string key, StartSubscription start);
    public static Subscription Every(TimeSpan interval, Func<Message> message);
    public static Subscription Every(string key, TimeSpan interval, Func<Message> message);
}
```

### SubscriptionModule.None

```csharp
public static readonly Subscription None
```

Singleton instance representing no subscriptions. Return this from `Subscriptions` when the application has no active subscriptions:

```csharp
public static Subscription Subscriptions(Model model) =>
    SubscriptionModule.None;
```

### SubscriptionModule.Batch

```csharp
public static Subscription Batch(params Subscription[] subscriptions)
```

Combines multiple subscriptions. Includes smart collapsing (same as `Commands.Batch`):

| Input | Output |
|-------|--------|
| Zero subscriptions | `SubscriptionModule.None` |
| One subscription | The single subscription (unwrapped) |
| N subscriptions | `Subscription.Batch(subscriptions)` |

```csharp
public static Subscription Subscriptions(Model model) =>
    SubscriptionModule.Batch(
        Navigation.UrlChanges(url => new UrlChanged(url)),
        SubscriptionModule.Every(TimeSpan.FromSeconds(30), () => new RefreshFeed()));
```

### SubscriptionModule.Create

```csharp
public static Subscription Create(string key, StartSubscription start)
```

Creates a named subscription source with a custom start function. The `key` uniquely identifies the subscription — the runtime uses it to determine which subscriptions to start, keep, or stop when the model changes.

```csharp
public static Subscription Subscriptions(Model model) =>
    model.IsConnected
        ? SubscriptionModule.Create("websocket", async (dispatch, ct) =>
            {
                using var ws = new ClientWebSocket();
                await ws.ConnectAsync(new Uri("wss://example.com/feed"), ct);
                var buffer = new byte[4096];
                while (!ct.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(buffer, ct);
                    var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    dispatch(new WebSocketMessage(text));
                }
            })
        : SubscriptionModule.None;
```

#### StartSubscription Delegate

```csharp
public delegate Task StartSubscription(Dispatch dispatch, CancellationToken cancellationToken);
```

The function that runs the subscription. It receives:

| Parameter | Type | Description |
|-----------|------|-------------|
| `dispatch` | `Dispatch` | Function to send messages into the MVU loop |
| `cancellationToken` | `CancellationToken` | Signals when the subscription should stop |

The subscription should run until cancellation, dispatching messages as external events arrive. The returned `Task` should complete when the subscription has fully stopped.

### SubscriptionModule.Every

```csharp
public static Subscription Every(TimeSpan interval, Func<Message> message)
public static Subscription Every(string key, TimeSpan interval, Func<Message> message)
```

Creates a periodic timer that dispatches a message at fixed intervals using `PeriodicTimer`.

The auto-keyed overload uses `"every:{interval.TotalMilliseconds}"` as the key. Use the keyed overload when you have multiple timers with the same interval:

```csharp
// Single timer — auto-keyed as "every:1000"
SubscriptionModule.Every(TimeSpan.FromSeconds(1), () => new Tick())

// Multiple timers with same interval — need distinct keys
SubscriptionModule.Batch(
    SubscriptionModule.Every("animation", TimeSpan.FromMilliseconds(16), () => new AnimationFrame()),
    SubscriptionModule.Every("polling", TimeSpan.FromMilliseconds(16), () => new PollSensor()))
```

## Subscription Lifecycle

The runtime's `SubscriptionManager` manages subscription lifecycle automatically:

1. After every state transition, the runtime calls `TProgram.Subscriptions(model)`
2. The manager diffs the new subscription set against the running set
3. **New keys** → start the subscription task
4. **Same keys** → keep the existing running task (no restart)
5. **Removed keys** → cancel the subscription's `CancellationToken`

This means subscriptions are **declarative** — you describe *what* should be running, and the manager handles the imperative lifecycle.

### Key-Based Identity

Subscription identity is determined by the `SubscriptionKey` (a string wrapper). As long as the same key appears in consecutive `Subscriptions` calls, the running subscription task is preserved. This is critical for long-lived connections like WebSockets — you don't want to reconnect on every model change.

## Dispatch Delegate

```csharp
public delegate void Dispatch(Message message);
```

Fire-and-forget function that sends a message into the MVU loop. Used by subscription sources to feed external events into the application. The dispatch is asynchronous — calling it does not block.

## Example: Conditional Subscriptions

```csharp
public static Subscription Subscriptions(Model model)
{
    var subs = new List<Subscription>();

    // Always listen for URL changes
    subs.Add(Navigation.UrlChanges(url => new UrlChanged(url)));

    // Timer only when a game is active
    if (model.GameState == GameState.Playing)
        subs.Add(SubscriptionModule.Every(TimeSpan.FromMilliseconds(16), () => new Tick()));

    // WebSocket only when connected
    if (model.IsOnline)
        subs.Add(SubscriptionModule.Create("realtime", StartRealtimeConnection));

    return subs.Count switch
    {
        0 => SubscriptionModule.None,
        _ => SubscriptionModule.Batch(subs.ToArray())
    };
}
```

When `model.GameState` changes from `Playing` to `Paused`, the timer subscription's key disappears from the set, and the `SubscriptionManager` automatically cancels it. When it changes back to `Playing`, a new timer is started.

## See Also

- [Program](program.md) — Where `Subscriptions` is declared
- [Navigation](navigation.md) — The `Navigation.UrlChanges` subscription
- [Runtime](runtime.md) — How the runtime manages subscription lifecycle
- [Command](command.md) — One-shot side effects (counterpart to subscriptions)
