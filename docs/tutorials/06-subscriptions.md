# Tutorial 6: Subscriptions

This tutorial teaches you to work with external event sources: timers, browser events, and WebSockets.

**Prerequisites:** [Tutorial 5: Forms](./05-forms.md)

**Time:** 25 minutes

## What You'll Build

An application demonstrating:

- Periodic timer updates
- Keyboard event handling
- Mouse tracking
- Window resize detection
- WebSocket connections

## Commands vs Subscriptions

| Commands | Subscriptions |
| -------- | ------------- |
| One-time effects | Continuous event sources |
| Fire and forget | Lifecycle managed by runtime |
| HTTP requests, storage | Timers, browser events, sockets |
| Returned from Update | Returned from Subscriptions |

## The Subscriptions Function

```csharp
public static Subscription Subscriptions(Model model) =>
    SubscriptionModule.None;  // No subscriptions
```

The runtime calls `Subscriptions(model)` after every update. It diffs the result against the previous subscriptions and starts/stops as needed.

## Step 1: Timer Subscription

Let's add a clock that updates every second:

```csharp
public record Model(DateTimeOffset CurrentTime);

public record Tick(DateTimeOffset Time) : Message;

public static (Model, Command) Initialize(Url url, Arguments argument)
    => (new Model(DateTimeOffset.UtcNow), Commands.None);

public static (Model model, Command command) Update(Message message, Model model)
    => message switch
    {
        Tick tick => (model with { CurrentTime = tick.Time }, Commands.None),
        _ => (model, Commands.None)
    };

public static Subscription Subscriptions(Model model) =>
    SubscriptionModule.Every(TimeSpan.FromSeconds(1), time => new Tick(time));

public static Document View(Model model)
    => new("Clock",
        div([], [
            text($"Current time: {model.CurrentTime:HH:mm:ss}")
        ]));
```

`SubscriptionModule.Every` creates a timer that fires at the specified interval.

## Step 2: Keyboard Events

Add keyboard handling:

```csharp
public record Model(
    DateTimeOffset CurrentTime,
    string LastKeyPressed
);

public record KeyDown(string Key) : Message;

public static (Model model, Command command) Update(Message message, Model model)
    => message switch
    {
        Tick tick => (model with { CurrentTime = tick.Time }, Commands.None),
        KeyDown key => (model with { LastKeyPressed = key.Key }, Commands.None),
        _ => (model, Commands.None)
    };

public static Subscription Subscriptions(Model model) =>
    SubscriptionModule.Batch([
        SubscriptionModule.Every(TimeSpan.FromSeconds(1), time => new Tick(time)),
        SubscriptionModule.OnKeyDown(data => new KeyDown(data?.Key ?? ""))
    ]);
```

`SubscriptionModule.Batch` combines multiple subscriptions.

## Step 3: Mouse Tracking

Track mouse position:

```csharp
public record Model(
    DateTimeOffset CurrentTime,
    string LastKeyPressed,
    int MouseX,
    int MouseY
);

public record MouseMoved(int X, int Y) : Message;

public static (Model model, Command command) Update(Message message, Model model)
    => message switch
    {
        Tick tick => (model with { CurrentTime = tick.Time }, Commands.None),
        KeyDown key => (model with { LastKeyPressed = key.Key }, Commands.None),
        MouseMoved mouse => (model with { MouseX = mouse.X, MouseY = mouse.Y }, Commands.None),
        _ => (model, Commands.None)
    };

public static Subscription Subscriptions(Model model) =>
    SubscriptionModule.Batch([
        SubscriptionModule.Every(TimeSpan.FromSeconds(1), time => new Tick(time)),
        SubscriptionModule.OnKeyDown(data => new KeyDown(data?.Key ?? "")),
        SubscriptionModule.OnMouseMove(data => new MouseMoved(data?.ClientX ?? 0, data?.ClientY ?? 0))
    ]);
```

## Step 4: Conditional Subscriptions

Subscribe only when needed:

```csharp
public record Model(
    DateTimeOffset CurrentTime,
    bool TrackingEnabled
);

public record ToggleTracking : Message;

public static (Model model, Command command) Update(Message message, Model model)
    => message switch
    {
        ToggleTracking => (model with { TrackingEnabled = !model.TrackingEnabled }, Commands.None),
        // ...
    };

public static Subscription Subscriptions(Model model)
{
    var subs = new List<Subscription>
    {
        SubscriptionModule.Every(TimeSpan.FromSeconds(1), time => new Tick(time))
    };
    
    if (model.TrackingEnabled)
    {
        subs.Add(SubscriptionModule.OnMouseMove(data => 
            new MouseMoved(data?.ClientX ?? 0, data?.ClientY ?? 0)));
    }
    
    return SubscriptionModule.Batch(subs);
}
```

When `TrackingEnabled` becomes false, the runtime automatically unsubscribes from mouse events.

## Step 5: Window Resize

Track viewport size:

```csharp
public record WindowResized(int Width, int Height) : Message;

SubscriptionModule.OnResize(data => 
    new WindowResized(data?.Width ?? 0, data?.Height ?? 0))
```

## Step 6: Animation Frames

For smooth animations, use animation frame subscriptions:

```csharp
public record AnimationFrame(double Timestamp) : Message;
public record AnimationDelta(double Delta) : Message;

// Regular animation frame (provides timestamp)
SubscriptionModule.OnAnimationFrame(data => 
    new AnimationFrame(data?.Timestamp ?? 0))

// Delta variant (provides time since last frame)
SubscriptionModule.OnAnimationFrameDelta(data => 
    new AnimationDelta(data?.DeltaTime ?? 0))
```

## Step 7: WebSocket Connections

Connect to a WebSocket server:

```csharp
public record SocketMessage(WebSocketEvent Event) : Message;

SubscriptionModule.WebSocket(
    new WebSocketOptions("wss://echo.websocket.events"),
    evt => new SocketMessage(evt))
```

Handle different WebSocket events:

```csharp
public static (Model model, Command command) Update(Message message, Model model)
    => message switch
    {
        SocketMessage { Event: WebSocketEvent.Opened } =>
            (model with { ConnectionStatus = "Connected" }, Commands.None),
        
        SocketMessage { Event: WebSocketEvent.Closed closed } =>
            (model with { ConnectionStatus = $"Closed: {closed.Reason}" }, Commands.None),
        
        SocketMessage { Event: WebSocketEvent.Error error } =>
            (model with { ConnectionStatus = $"Error: {error.Message}" }, Commands.None),
        
        SocketMessage { Event: WebSocketEvent.MessageReceived msg } =>
            (model with { LastMessage = msg.Data }, Commands.None),
        
        _ => (model, Commands.None)
    };
```

## Step 8: Custom Subscriptions

Create custom subscriptions for any async event source:

```csharp
public static Subscription Subscriptions(Model model) =>
    SubscriptionModule.Create(
        "my-custom-sub",  // Unique key for diffing
        async (dispatch, cancellationToken) =>
        {
            // Your async logic here
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(5000, cancellationToken);
                dispatch(new CustomEvent());
            }
        });
```

## Complete Example

```csharp
using Abies;
using Abies.DOM;
using Abies.Html;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

await Runtime.Run<SubsDemo, Arguments, Model>(new Arguments());

public record Arguments;

public record Model(
    DateTimeOffset CurrentTime,
    string LastKey,
    int MouseX,
    int MouseY,
    int WindowWidth,
    int WindowHeight,
    bool TrackMouse,
    string? WsMessage
);

// Messages
public record Tick(DateTimeOffset Time) : Message;
public record KeyDown(string Key) : Message;
public record MouseMoved(int X, int Y) : Message;
public record Resized(int W, int H) : Message;
public record ToggleMouseTracking : Message;
public record WsEvent(WebSocketEvent Event) : Message;

public class SubsDemo : Program<Model, Arguments>
{
    public static (Model, Command) Initialize(Url url, Arguments argument)
        => (new Model(
            DateTimeOffset.UtcNow, "", 0, 0, 0, 0, false, null
        ), Commands.None);

    public static (Model model, Command command) Update(Message message, Model model)
        => message switch
        {
            Tick t => (model with { CurrentTime = t.Time }, Commands.None),
            KeyDown k => (model with { LastKey = k.Key }, Commands.None),
            MouseMoved m => (model with { MouseX = m.X, MouseY = m.Y }, Commands.None),
            Resized r => (model with { WindowWidth = r.W, WindowHeight = r.H }, Commands.None),
            ToggleMouseTracking => (model with { TrackMouse = !model.TrackMouse }, Commands.None),
            WsEvent { Event: WebSocketEvent.MessageReceived msg } => 
                (model with { WsMessage = msg.Data }, Commands.None),
            _ => (model, Commands.None)
        };

    public static Document View(Model model)
        => new("Subscriptions Demo",
            div([class_("demo")], [
                h1([], [text("Subscriptions Demo")]),
                
                section([], [
                    h2([], [text("⏰ Timer")]),
                    p([], [text($"Time: {model.CurrentTime:HH:mm:ss}")])
                ]),
                
                section([], [
                    h2([], [text("⌨️ Keyboard")]),
                    p([], [text($"Last key: {model.LastKey}")])
                ]),
                
                section([], [
                    h2([], [text("🖱️ Mouse")]),
                    button([onclick(new ToggleMouseTracking())], [
                        text(model.TrackMouse ? "Stop tracking" : "Start tracking")
                    ]),
                    model.TrackMouse
                        ? p([], [text($"Position: ({model.MouseX}, {model.MouseY})")])
                        : text("")
                ]),
                
                section([], [
                    h2([], [text("📐 Window")]),
                    p([], [text($"Size: {model.WindowWidth} × {model.WindowHeight}")])
                ]),
                
                section([], [
                    h2([], [text("🔌 WebSocket")]),
                    p([], [text(model.WsMessage ?? "No messages yet")])
                ])
            ]));

    public static Subscription Subscriptions(Model model)
    {
        var subs = new List<Subscription>
        {
            SubscriptionModule.Every(TimeSpan.FromSeconds(1), t => new Tick(t)),
            SubscriptionModule.OnKeyDown(d => new KeyDown(d?.Key ?? "")),
            SubscriptionModule.OnResize(d => new Resized(d?.Width ?? 0, d?.Height ?? 0)),
            SubscriptionModule.WebSocket(
                new WebSocketOptions("wss://echo.websocket.events"),
                e => new WsEvent(e))
        };
        
        if (model.TrackMouse)
        {
            subs.Add(SubscriptionModule.OnMouseMove(d => 
                new MouseMoved(d?.ClientX ?? 0, d?.ClientY ?? 0)));
        }
        
        return SubscriptionModule.Batch(subs);
    }

    public static Message OnUrlChanged(Url url) => new Tick(DateTimeOffset.UtcNow);
    public static Message OnLinkClicked(UrlRequest r) => new Tick(DateTimeOffset.UtcNow);
    public static Task HandleCommand(Command c, Func<Message, ValueTuple> d) 
        => Task.CompletedTask;
}
```

## Available Subscriptions

| Subscription | Description |
| ------------ | ----------- |
| `Every(interval, toMessage)` | Periodic timer |
| `OnAnimationFrame(toMessage)` | RAF with timestamp |
| `OnAnimationFrameDelta(toMessage)` | RAF with delta time |
| `OnResize(toMessage)` | Window resize |
| `OnVisibilityChange(toMessage)` | Page visibility |
| `OnKeyDown(toMessage)` | Key press |
| `OnKeyUp(toMessage)` | Key release |
| `OnMouseDown(toMessage)` | Mouse button down |
| `OnMouseUp(toMessage)` | Mouse button up |
| `OnMouseMove(toMessage)` | Mouse movement |
| `OnClick(toMessage)` | Mouse click |
| `WebSocket(options, toMessage)` | WebSocket connection |
| `Create(key, start)` | Custom subscription |
| `Batch(subscriptions)` | Combine multiple |
| `None` | No subscriptions |

## What You Learned

| Concept | Application |
| ------- | ----------- |
| Timer subscriptions | Periodic updates |
| Browser event subscriptions | Keyboard, mouse, resize |
| Conditional subscriptions | Enable/disable based on state |
| WebSocket subscriptions | Real-time connections |
| Subscription batching | Combine multiple sources |
| Custom subscriptions | Any async event source |

## Best Practices

### Use stable keys

When using `Create`, ensure keys are stable:

```csharp
// Good: Key changes when room changes
SubscriptionModule.Create($"chat:{model.RoomId}", ...)

// Bad: Key is always different
SubscriptionModule.Create(Guid.NewGuid().ToString(), ...)
```

### Avoid expensive subscriptions

Mouse move fires frequently. Consider throttling:

```csharp
// Only track when needed
if (model.IsDragging)
    subs.Add(SubscriptionModule.OnMouseMove(...));
```

### Clean up in custom subscriptions

Use the cancellation token:

```csharp
SubscriptionModule.Create("my-sub", async (dispatch, ct) =>
{
    try
    {
        while (!ct.IsCancellationRequested)
        {
            await DoWork(ct);
        }
    }
    catch (OperationCanceledException)
    {
        // Expected on cleanup
    }
})
```

## Exercises

1. **Stopwatch**: Build a stopwatch with start/stop/reset
2. **Drag and drop**: Implement dragging using mouse subscriptions
3. **Idle detection**: Detect when user is idle
4. **Live search**: Combine input with debounced API calls

## Next Tutorial

→ [Tutorial 7: Real-World App](./07-real-world-app.md) — Explore the Conduit sample
