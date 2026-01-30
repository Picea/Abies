# Subscriptions

Subscriptions let your application react to external events like timers, browser events, and WebSocket messages. This document explains how subscriptions work and when to use them.

## What are Subscriptions?

Subscriptions are declarative event sources. Unlike commands (one-time effects), subscriptions are active as long as they're returned from your `Subscriptions` function.

```csharp
public static Subscription Subscriptions(Model model)
    => model.TimerRunning
        ? Every(TimeSpan.FromSeconds(1), _ => new TimerTick())
        : SubscriptionModule.None;
```

## Subscriptions vs Commands

| Aspect | Commands | Subscriptions |
| ------ | -------- | ------------- |
| When triggered | Once, from Update | Continuously while active |
| Controlled by | Update return value | Subscriptions function return |
| Lifecycle | Fire-and-forget | Start/stop based on model |
| Use case | HTTP requests | Timers, browser events |

## The Subscription Function

Every program can define subscriptions based on the current model:

```csharp
public class MyProgram : Program<Model>
{
    public static Subscription Subscriptions(Model model)
    {
        var subs = new List<Subscription>();
        
        // Always listen for keyboard shortcuts
        subs.Add(OnKeyDown(key => new KeyPressed(key)));
        
        // Timer only when running
        if (model.IsTimerRunning)
        {
            subs.Add(Every(TimeSpan.FromMilliseconds(100), _ => new TimerTick()));
        }
        
        // Resize only on certain pages
        if (model.CurrentPage is HomePage)
        {
            subs.Add(OnResize((w, h) => new WindowResized(w, h)));
        }
        
        return Batch(subs);
    }
}
```

## Built-in Subscriptions

### Timer Subscriptions

```csharp
using static Abies.Subscriptions.Timer;

// Fire every interval
Every(TimeSpan.FromSeconds(1), now => new SecondPassed(now))

// Fire once after delay
After(TimeSpan.FromSeconds(5), () => new TimeoutReached())
```

### Browser Event Subscriptions

```csharp
using static Abies.Subscriptions.Browser;

// Window resize
OnResize((width, height) => new WindowResized(width, height))

// Visibility change (tab focus)
OnVisibilityChange(visible => new VisibilityChanged(visible))

// Before page unload
OnBeforeUnload(() => new PageUnloading())
```

### Keyboard Subscriptions

```csharp
using static Abies.Subscriptions.Keyboard;

// Key down anywhere
OnKeyDown(key => new KeyPressed(key))

// Key up anywhere
OnKeyUp(key => new KeyReleased(key))

// Specific key combinations
OnKeyDown(key => key == "Escape" ? new EscapePressed() : null)
```

### Mouse Subscriptions

```csharp
using static Abies.Subscriptions.Mouse;

// Mouse movement
OnMouseMove((x, y) => new MouseMoved(x, y))

// Mouse buttons
OnMouseDown(button => new MousePressed(button))
OnMouseUp(button => new MouseReleased(button))
```

## Creating Custom Subscriptions

For custom event sources, implement the subscription pattern:

```csharp
public static Subscription OnWebSocketMessage(
    string url, 
    Func<string, Message> toMessage)
{
    return new WebSocketSubscription(url, toMessage);
}

public class WebSocketSubscription : Subscription
{
    private readonly string _url;
    private readonly Func<string, Message> _toMessage;
    private WebSocket? _socket;
    
    public WebSocketSubscription(string url, Func<string, Message> toMessage)
    {
        _url = url;
        _toMessage = toMessage;
    }
    
    public override async Task Start(Dispatch dispatch, CancellationToken ct)
    {
        _socket = new WebSocket(_url);
        _socket.OnMessage += msg => dispatch(_toMessage(msg));
        await _socket.ConnectAsync(ct);
    }
    
    public override async Task Stop()
    {
        if (_socket is not null)
        {
            await _socket.CloseAsync();
            _socket = null;
        }
    }
}
```

## Subscription Lifecycle

Subscriptions are managed by the runtime:

```text
┌──────────────────────────────────────────────────────────────┐
│                  Subscription Lifecycle                      │
│                                                              │
│  1. Model changes                                            │
│     ↓                                                        │
│  2. Subscriptions(model) called                              │
│     ↓                                                        │
│  3. Compare with previous subscriptions                      │
│     ↓                                                        │
│  ┌─────────────────┬────────────────┬─────────────────┐     │
│  │ New sub found   │ Sub removed    │ Sub unchanged   │     │
│  │ → Start it      │ → Stop it      │ → Keep running  │     │
│  └─────────────────┴────────────────┴─────────────────┘     │
│     ↓                                                        │
│  4. Events dispatch messages → Update → repeat              │
└──────────────────────────────────────────────────────────────┘
```

## Composing Subscriptions

### Batch Multiple Subscriptions

```csharp
public static Subscription Subscriptions(Model model)
    => Batch([
        Every(TimeSpan.FromSeconds(1), _ => new Tick()),
        OnResize((w, h) => new Resized(w, h)),
        OnKeyDown(k => new KeyDown(k))
    ]);
```

### Conditional Subscriptions

```csharp
public static Subscription Subscriptions(Model model)
{
    var subs = new List<Subscription>();
    
    if (model.IsPlaying)
        subs.Add(Every(TimeSpan.FromMilliseconds(16), _ => new Frame()));
    
    if (model.ListenForKeys)
        subs.Add(OnKeyDown(k => new KeyPressed(k)));
    
    if (model.TrackMouse)
        subs.Add(OnMouseMove((x, y) => new MouseAt(x, y)));
    
    return subs.Count > 0 ? Batch(subs) : SubscriptionModule.None;
}
```

### No Subscriptions

```csharp
public static Subscription Subscriptions(Model model)
    => SubscriptionModule.None;
```

## Common Patterns

### Auto-Save

Save draft content periodically:

```csharp
public static Subscription Subscriptions(Model model)
    => model.HasUnsavedChanges
        ? Every(TimeSpan.FromSeconds(30), _ => new AutoSave())
        : SubscriptionModule.None;
```

### Polling

Refresh data periodically:

```csharp
public static Subscription Subscriptions(Model model)
    => model.CurrentPage is DashboardPage
        ? Every(TimeSpan.FromMinutes(1), _ => new RefreshDashboard())
        : SubscriptionModule.None;
```

### Activity Tracking

Track user activity:

```csharp
public static Subscription Subscriptions(Model model)
    => Batch([
        OnMouseMove((_, _) => new UserActive()),
        OnKeyDown(_ => new UserActive()),
        Every(TimeSpan.FromMinutes(5), _ => new CheckInactivity())
    ]);
```

### Keyboard Shortcuts

Global hotkeys:

```csharp
public static Subscription Subscriptions(Model model)
    => OnKeyDown(key => key switch
    {
        "Escape" => new CloseModal(),
        "s" when model.CtrlPressed => new Save(),
        "z" when model.CtrlPressed => new Undo(),
        _ => null  // Ignore other keys
    });
```

### Real-Time Updates

WebSocket for live data:

```csharp
public static Subscription Subscriptions(Model model)
    => model.IsConnected
        ? OnWebSocketMessage(
            "wss://api.example.com/live",
            msg => new LiveUpdate(msg))
        : SubscriptionModule.None;
```

## Testing Subscriptions

### Test Subscription Logic

```csharp
[Fact]
public void Subscriptions_WhenTimerRunning_ReturnsTimerSubscription()
{
    var model = new Model(IsTimerRunning: true);
    
    var sub = Program.Subscriptions(model);
    
    Assert.IsType<TimerSubscription>(sub);
}

[Fact]
public void Subscriptions_WhenTimerStopped_ReturnsNone()
{
    var model = new Model(IsTimerRunning: false);
    
    var sub = Program.Subscriptions(model);
    
    Assert.Equal(SubscriptionModule.None, sub);
}
```

### Test Subscription Messages

```csharp
[Fact]
public void TimerTick_UpdatesElapsedTime()
{
    var model = new Model(Elapsed: TimeSpan.Zero);
    
    var (newModel, _) = Update(new TimerTick(), model);
    
    Assert.Equal(TimeSpan.FromSeconds(1), newModel.Elapsed);
}
```

## Best Practices

### 1. Keep Subscriptions Declarative

Return subscriptions based on model state:

```csharp
// ✅ Declarative
public static Subscription Subscriptions(Model model)
    => model.WantsUpdates ? TimerSub() : None;

// ❌ Imperative (don't do this)
public static Subscription Subscriptions(Model model)
{
    if (model.WantsUpdates)
        StartTimer();  // Side effect!
    return None;
}
```

### 2. Use Appropriate Intervals

Don't poll too frequently:

```csharp
// ❌ Too fast for most use cases
Every(TimeSpan.FromMilliseconds(1), _ => new Update())

// ✅ Appropriate intervals
Every(TimeSpan.FromSeconds(1), _ => new Tick())           // UI updates
Every(TimeSpan.FromMilliseconds(16), _ => new Frame())    // Animation (60fps)
Every(TimeSpan.FromMinutes(1), _ => new Refresh())        // Data refresh
```

### 3. Clean Up Resources

Subscriptions automatically stop when removed, but ensure custom subscriptions clean up properly:

```csharp
public override async Task Stop()
{
    _timer?.Dispose();
    await _socket?.CloseAsync();
    _eventSource?.Close();
}
```

### 4. Handle Subscription Errors

Gracefully handle subscription failures:

```csharp
public override async Task Start(Dispatch dispatch, CancellationToken ct)
{
    try
    {
        await _socket.ConnectAsync(ct);
        _socket.OnMessage += msg => dispatch(new MessageReceived(msg));
    }
    catch (Exception ex)
    {
        dispatch(new ConnectionFailed(ex.Message));
    }
}
```

## Summary

Subscriptions enable reactive applications:

- **Declarative** — Define what events to listen for based on model
- **Lifecycle managed** — Runtime starts/stops subscriptions automatically
- **Composable** — Combine multiple subscriptions with `Batch`
- **Testable** — Logic is pure, effects are isolated

Common use cases:

- ⏱️ Timers and intervals
- 🖥️ Window resize/visibility
- ⌨️ Keyboard shortcuts
- 🖱️ Mouse tracking
- 🔌 WebSocket connections
- 📡 Server-sent events

## See Also

- [Commands and Effects](./commands-effects.md) — One-time side effects
- [Tutorial: Subscriptions](../tutorials/06-subscriptions.md) — Hands-on examples
- [API: Subscription Module](../api/subscription.md) — Full API reference
