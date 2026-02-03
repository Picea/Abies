# Subscription API Reference

Subscriptions are declarative event sources for long-lived external events.

## Overview

Unlike commands (one-time effects), subscriptions stay active as long as they're returned from the `Subscriptions` function. They're managed by the runtime—started when added, stopped when removed.

## Subscription Function

Every `Program` declares its subscriptions:

```csharp
public static Subscription Subscriptions(Model model)
    => model.TimerActive
        ? SubscriptionModule.Every(TimeSpan.FromSeconds(1), _ => new TimerTick())
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
        SubscriptionModule.Every(TimeSpan.FromSeconds(1), _ => new Tick()),
        SubscriptionModule.OnResize(size => new Resized(size.Width, size.Height)),
        SubscriptionModule.OnKeyDown(data => new KeyPressed(data?.Key ?? ""))
    ]);
```

## Timer Subscriptions

### Every

Fires repeatedly at an interval:

```csharp
SubscriptionModule.Every(TimeSpan.FromSeconds(1), now => new SecondTick(now))
SubscriptionModule.Every(TimeSpan.FromMilliseconds(16), _ => new AnimationFrame())  // ~60fps
SubscriptionModule.Every(TimeSpan.FromMinutes(5), _ => new RefreshData())
```

**Parameters:**

- `interval` — Time between firings
- `toMessage` — Function that creates a message (receives current `DateTimeOffset`)

## Browser Event Subscriptions

### OnResize

Fires when the window is resized:

```csharp
SubscriptionModule.OnResize(size => new WindowResized(size.Width, size.Height))
```

### OnVisibilityChange

Fires when the tab visibility changes:

```csharp
SubscriptionModule.OnVisibilityChange(evt => new VisibilityChanged(evt.State))
```

## Keyboard Subscriptions

### OnKeyDown

Fires when any key is pressed:

```csharp
SubscriptionModule.OnKeyDown(data => new KeyPressed(data?.Key ?? ""))
```

**Filtering keys:**

```csharp
SubscriptionModule.OnKeyDown(data => data?.Key switch
{
    "Escape" => new EscapePressed(),
    "Enter" => new EnterPressed(),
    _ => null  // Ignore other keys
})
```

### OnKeyUp

Fires when a key is released:

```csharp
SubscriptionModule.OnKeyUp(data => new KeyReleased(data?.Key ?? ""))
```

## Mouse Subscriptions

### OnMouseMove

Fires when the mouse moves:

```csharp
SubscriptionModule.OnMouseMove(data => new MouseMoved(data?.ClientX ?? 0, data?.ClientY ?? 0))
```

### OnMouseDown / OnMouseUp

Fires on mouse button events:

```csharp
SubscriptionModule.OnMouseDown(data => new MousePressed(data?.Button ?? 0))
SubscriptionModule.OnMouseUp(data => new MouseReleased(data?.Button ?? 0))
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
        subs.Add(SubscriptionModule.Every(TimeSpan.FromMilliseconds(16), _ => new GameTick()));
    }
    
    // Keyboard only when input is focused
    if (model.InputFocused)
    {
        subs.Add(SubscriptionModule.OnKeyDown(data => new KeyInput(data?.Key ?? "")));
    }
    
    // Always track visibility for analytics
    subs.Add(SubscriptionModule.OnVisibilityChange(evt => new TrackVisibility(evt.State)));
    
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
        ? SubscriptionModule.Every(TimeSpan.FromSeconds(30), _ => new AutoSave())
        : SubscriptionModule.None;
```

### Polling

```csharp
public static Subscription Subscriptions(Model model)
    => model.CurrentPage == Page.Dashboard
        ? SubscriptionModule.Every(TimeSpan.FromMinutes(1), _ => new RefreshDashboard())
        : SubscriptionModule.None;
```

### Keyboard Shortcuts

```csharp
public static Subscription Subscriptions(Model model)
    => SubscriptionModule.OnKeyDown(data => (data?.Key, model.CtrlPressed) switch
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
        SubscriptionModule.OnMouseMove(_ => new UserActive()),
        SubscriptionModule.OnKeyDown(_ => new UserActive()),
        SubscriptionModule.Every(TimeSpan.FromMinutes(5), _ => new CheckInactivity())
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
    => model.WantsTimer 
        ? SubscriptionModule.Every(TimeSpan.FromSeconds(1), _ => new Tick()) 
        : SubscriptionModule.None;

// ❌ Bad - imperative side effects
public static Subscription Subscriptions(Model model)
{
    if (model.WantsTimer)
        StartTimer();  // Side effect!
    return SubscriptionModule.None;
}
```

### 2. Use Appropriate Intervals

```csharp
// Animation: 16ms (~60fps)
SubscriptionModule.Every(TimeSpan.FromMilliseconds(16), _ => new Frame())

// UI updates: 100-1000ms
SubscriptionModule.Every(TimeSpan.FromMilliseconds(500), _ => new UpdateClock())

// Data refresh: minutes
SubscriptionModule.Every(TimeSpan.FromMinutes(5), _ => new RefreshData())
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
