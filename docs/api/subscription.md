# Subscription API Reference

Subscriptions are declarative event sources for long-lived external events.

## Overview

Unlike commands (one-time effects), subscriptions stay active as long as they're returned from the `Subscriptions` function. They're managed by the runtime—started when added, stopped when removed.

## Subscription Function

Every `Program` declares its subscriptions:

```csharp
public static Subscription Subscriptions(Model model)
    => model.TimerActive
        ? Every(TimeSpan.FromSeconds(1), _ => new TimerTick())
        : SubscriptionModule.None;
```

## SubscriptionModule

The `SubscriptionModule` provides subscription utilities:

```csharp
public static class SubscriptionModule
{
    public static Subscription None { get; }
    public static Subscription Batch(IEnumerable<Subscription> subscriptions);
}
```

### SubscriptionModule.None

A subscription that does nothing:

```csharp
public static Subscription Subscriptions(Model model)
    => SubscriptionModule.None;
```

### SubscriptionModule.Batch

Combines multiple subscriptions:

```csharp
public static Subscription Subscriptions(Model model)
    => SubscriptionModule.Batch([
        Every(TimeSpan.FromSeconds(1), _ => new Tick()),
        OnResize((w, h) => new Resized(w, h)),
        OnKeyDown(k => new KeyPressed(k))
    ]);
```

## Timer Subscriptions

```csharp
using static Abies.Subscriptions.Timer;
```

### Every

Fires repeatedly at an interval:

```csharp
Every(TimeSpan.FromSeconds(1), now => new SecondTick(now))
Every(TimeSpan.FromMilliseconds(16), _ => new AnimationFrame())  // ~60fps
Every(TimeSpan.FromMinutes(5), _ => new RefreshData())
```

**Parameters:**

- `interval` — Time between firings
- `toMessage` — Function that creates a message (receives current `DateTime`)

### After

Fires once after a delay:

```csharp
After(TimeSpan.FromSeconds(5), () => new TimeoutReached())
After(TimeSpan.FromMilliseconds(300), () => new DebounceComplete())
```

**Parameters:**

- `delay` — Time to wait
- `toMessage` — Function that creates the message

## Browser Event Subscriptions

```csharp
using static Abies.Subscriptions.Browser;
```

### OnResize

Fires when the window is resized:

```csharp
OnResize((width, height) => new WindowResized(width, height))
```

### OnVisibilityChange

Fires when the tab visibility changes:

```csharp
OnVisibilityChange(visible => new VisibilityChanged(visible))
```

### OnBeforeUnload

Fires before the page unloads:

```csharp
OnBeforeUnload(() => new PageClosing())
```

### OnOnline / OnOffline

Fires when network status changes:

```csharp
OnOnline(() => new BackOnline())
OnOffline(() => new WentOffline())
```

## Keyboard Subscriptions

```csharp
using static Abies.Subscriptions.Keyboard;
```

### OnKeyDown

Fires when any key is pressed:

```csharp
OnKeyDown(key => new KeyPressed(key))
```

**Filtering keys:**

```csharp
OnKeyDown(key => key switch
{
    "Escape" => new EscapePressed(),
    "Enter" => new EnterPressed(),
    _ => null  // Ignore other keys
})
```

### OnKeyUp

Fires when a key is released:

```csharp
OnKeyUp(key => new KeyReleased(key))
```

## Mouse Subscriptions

```csharp
using static Abies.Subscriptions.Mouse;
```

### OnMouseMove

Fires when the mouse moves:

```csharp
OnMouseMove((x, y) => new MouseMoved(x, y))
```

### OnMouseDown / OnMouseUp

Fires on mouse button events:

```csharp
OnMouseDown(button => new MousePressed(button))
OnMouseUp(button => new MouseReleased(button))
```

## Conditional Subscriptions

Subscriptions are declarative—return them based on model state:

```csharp
public static Subscription Subscriptions(Model model)
{
    var subs = new List<Subscription>();
    
    // Timer only when game is playing
    if (model.GameState == GameState.Playing)
    {
        subs.Add(Every(TimeSpan.FromMilliseconds(16), _ => new GameTick()));
    }
    
    // Keyboard only when input is focused
    if (model.InputFocused)
    {
        subs.Add(OnKeyDown(k => new KeyInput(k)));
    }
    
    // Always track visibility for analytics
    subs.Add(OnVisibilityChange(v => new TrackVisibility(v)));
    
    return subs.Count > 0 
        ? SubscriptionModule.Batch(subs) 
        : SubscriptionModule.None;
}
```

## Subscription Lifecycle

```text
1. Model changes
2. Subscriptions(model) called
3. Runtime compares with previous subscriptions
4. New subscriptions → Started
5. Removed subscriptions → Stopped
6. Unchanged subscriptions → Keep running
7. Active subscriptions dispatch messages
8. Messages trigger Update, loop continues
```

## Creating Custom Subscriptions

For specialized event sources, implement `Subscription`:

```csharp
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
        
        try
        {
            await _socket.ConnectAsync(ct);
        }
        catch (Exception ex)
        {
            dispatch(new ConnectionFailed(ex.Message));
        }
    }
    
    public override async Task Stop()
    {
        if (_socket is not null)
        {
            await _socket.CloseAsync();
            _socket = null;
        }
    }
    
    public override bool Equals(Subscription other)
        => other is WebSocketSubscription ws && ws._url == _url;
    
    public override int GetHashCode()
        => _url.GetHashCode();
}

// Factory function
public static Subscription OnWebSocket(string url, Func<string, Message> toMessage)
    => new WebSocketSubscription(url, toMessage);
```

## Common Patterns

### Auto-Save

```csharp
public static Subscription Subscriptions(Model model)
    => model.HasUnsavedChanges
        ? Every(TimeSpan.FromSeconds(30), _ => new AutoSave())
        : SubscriptionModule.None;
```

### Polling

```csharp
public static Subscription Subscriptions(Model model)
    => model.CurrentPage == Page.Dashboard
        ? Every(TimeSpan.FromMinutes(1), _ => new RefreshDashboard())
        : SubscriptionModule.None;
```

### Keyboard Shortcuts

```csharp
public static Subscription Subscriptions(Model model)
    => OnKeyDown(key => (key, model.CtrlPressed) switch
    {
        ("s", true) => new SaveShortcut(),
        ("z", true) => new UndoShortcut(),
        ("Escape", _) => new CloseModal(),
        _ => null
    });
```

### Inactivity Detection

```csharp
public static Subscription Subscriptions(Model model)
    => SubscriptionModule.Batch([
        OnMouseMove((_, _) => new UserActive()),
        OnKeyDown(_ => new UserActive()),
        Every(TimeSpan.FromMinutes(5), _ => new CheckInactivity())
    ]);
```

## Testing Subscriptions

### Test Subscription Selection

```csharp
[Fact]
public void WhenTimerRunning_ReturnsTimerSubscription()
{
    var model = new Model(TimerRunning: true);
    
    var sub = Program.Subscriptions(model);
    
    Assert.IsType<TimerSubscription>(sub);
}

[Fact]
public void WhenTimerStopped_ReturnsNone()
{
    var model = new Model(TimerRunning: false);
    
    var sub = Program.Subscriptions(model);
    
    Assert.Equal(SubscriptionModule.None, sub);
}
```

### Test Messages from Subscriptions

```csharp
[Fact]
public void TimerTick_UpdatesElapsedTime()
{
    var model = new Model(Elapsed: TimeSpan.Zero);
    
    var (result, _) = Update(new TimerTick(), model);
    
    Assert.Equal(TimeSpan.FromSeconds(1), result.Elapsed);
}
```

## Best Practices

### 1. Keep Subscriptions Declarative

```csharp
// ✅ Good - declarative based on model
public static Subscription Subscriptions(Model model)
    => model.WantsTimer ? TimerSub() : None;

// ❌ Bad - imperative side effects
public static Subscription Subscriptions(Model model)
{
    if (model.WantsTimer)
        StartTimer();  // Side effect!
    return None;
}
```

### 2. Use Appropriate Intervals

```csharp
// Animation: 16ms (~60fps)
Every(TimeSpan.FromMilliseconds(16), _ => new Frame())

// UI updates: 100-1000ms
Every(TimeSpan.FromMilliseconds(500), _ => new UpdateClock())

// Data refresh: minutes
Every(TimeSpan.FromMinutes(5), _ => new RefreshData())
```

### 3. Implement Equality for Custom Subscriptions

```csharp
public override bool Equals(Subscription other)
    => other is MySubscription ms && ms._key == _key;

public override int GetHashCode()
    => _key.GetHashCode();
```

### 4. Handle Cleanup

```csharp
public override async Task Stop()
{
    _timer?.Dispose();
    await _socket?.CloseAsync();
    _eventSource?.Close();
}
```

## See Also

- [Command API](./command.md) — One-time side effects
- [Concepts: Subscriptions](../concepts/subscriptions.md) — Deep dive
- [Tutorial: Subscriptions](../tutorials/06-subscriptions.md) — Hands-on examples
