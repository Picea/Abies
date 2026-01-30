# Runtime API Reference

The `Runtime` module manages the MVU message loop and application lifecycle.

## Usage

```csharp
using Abies;
```

## Overview

The runtime is the engine that drives Abies applications. It:

1. Initializes the application with a URL and arguments
2. Renders the initial virtual DOM
3. Starts subscriptions
4. Processes messages in order
5. Updates the model and re-renders
6. Executes commands (side effects)

## Runtime.Run

Starts the MVU message loop:

```csharp
public static async Task Run<TProgram, TArguments, TModel>(TArguments arguments)
    where TProgram : Program<TModel, TArguments>
```

**Type Parameters:**

| Parameter | Description |
| --------- | ----------- |
| `TProgram` | Type implementing `Program<TModel, TArguments>` |
| `TArguments` | Type of initialization arguments |
| `TModel` | Type of application model |

**Parameters:**

| Parameter | Type | Description |
| --------- | ---- | ----------- |
| `arguments` | `TArguments` | Arguments passed to `Initialize` |

**Returns:** `Task` that runs for the application lifetime.

## Starting an Application

### Basic Startup

```csharp
using Abies;
using System.Runtime.Versioning;

[assembly: SupportedOSPlatform("browser")]

public partial class Program
{
    static async Task Main()
    {
        await Runtime.Run<MyProgram, Unit, Model>(Unit.Value);
    }
}

public class MyProgram : Program<Model, Unit>
{
    // Implementation...
}
```

### With Arguments

```csharp
public record AppConfig(string ApiUrl, bool Debug);

static async Task Main()
{
    var config = new AppConfig(
        ApiUrl: "https://api.example.com",
        Debug: true
    );
    
    await Runtime.Run<MyProgram, AppConfig, Model>(config);
}

public class MyProgram : Program<Model, AppConfig>
{
    public static (Model, Command) Initialize(Url url, AppConfig config)
    {
        var model = new Model(ApiUrl: config.ApiUrl);
        return (model, Commands.None);
    }
}
```

## Message Loop

The runtime processes messages sequentially:

```
┌─────────────────────────────────────────────────────────┐
│                    Runtime.Run                          │
├─────────────────────────────────────────────────────────┤
│  1. Initialize(url, args) → (model, cmd)                │
│  2. View(model) → document                              │
│  3. Render initial DOM                                  │
│  4. Start subscriptions                                 │
│  5. Execute initial command                             │
│                                                         │
│  ┌─── Message Loop ────────────────────────────────┐    │
│  │  foreach message:                               │    │
│  │    1. Update(msg, model) → (newModel, cmd)      │    │
│  │    2. View(newModel) → newDocument              │    │
│  │    3. Diff(oldDom, newDom) → patches            │    │
│  │    4. Apply patches to real DOM                 │    │
│  │    5. Update subscriptions                      │    │
│  │    6. Execute command                           │    │
│  └─────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
```

## Message Dispatch

Messages are dispatched through an unbounded channel:

```csharp
// Internal implementation
private static readonly Channel<Message> _messageChannel = 
    Channel.CreateUnbounded<Message>();
```

### From Events

Event handlers dispatch messages automatically:

```csharp
button(onClick(new Increment()), text("+"))
// Click → Dispatch(new Increment())
```

### From Commands

Commands can dispatch messages via the `Dispatch` function:

```csharp
public static async Task HandleCommand(Command command, Func<Message, ValueTuple> dispatch)
{
    switch (command)
    {
        case LoadDataCommand:
            var data = await FetchData();
            dispatch(new DataLoaded(data));
            break;
    }
}
```

### From Subscriptions

Subscriptions dispatch messages when events occur:

```csharp
public static Subscription Subscriptions(Model model)
    => Subscription.Batch(
        Every(TimeSpan.FromSeconds(1), () => new Tick())
    );
```

## URL Handling

The runtime intercepts URL changes:

### OnUrlChanged

Called when browser navigation occurs:

```csharp
public static Message OnUrlChanged(Url url)
    => new UrlChanged(url);
```

### OnLinkClicked

Called when a link is clicked:

```csharp
public static Message OnLinkClicked(UrlRequest request)
    => request switch
    {
        UrlRequest.Internal internal_ => new Navigate(internal_.Url),
        UrlRequest.External external => new ExternalNavigation(external.Url),
        _ => new NoOp()
    };
```

## Command Execution

Commands are executed after each update:

### Navigation Commands

Handled specially by the runtime:

```csharp
case Navigation.Command.PushState pushState:
    await Interop.PushState(pushState.Url.ToString());
    var msg = TProgram.OnUrlChanged(pushState.Url);
    Dispatch(msg);
    break;
    
case Navigation.Command.Load load:
    await Interop.Load(load.Url.ToString());
    break;
    
case Navigation.Command.ReplaceState replaceState:
    await Interop.ReplaceState(replaceState.Url.ToString());
    var msg = TProgram.OnUrlChanged(replaceState.Url);
    Dispatch(msg);
    break;
```

### Batch Commands

Executed sequentially:

```csharp
case Command.Batch batch:
    foreach (var cmd in batch.Commands)
    {
        await ExecuteCommand(cmd);
    }
    break;
```

### Custom Commands

Passed to `HandleCommand`:

```csharp
default:
    await TProgram.HandleCommand(command, Dispatch);
    break;
```

## Subscription Management

Subscriptions are updated after each model change:

```csharp
subscriptionState = SubscriptionManager.Update(
    subscriptionState, 
    TProgram.Subscriptions(model), 
    Dispatch
);
```

The manager:
- Starts new subscriptions
- Stops removed subscriptions
- Keeps unchanged subscriptions running

## Handler Registration

Event handlers are registered with the runtime:

```csharp
// Internal - called during DOM patching
internal static void RegisterHandler(DOM.Handler handler);
internal static void UnregisterHandler(DOM.Handler handler);
```

Handler IDs are stored in concurrent dictionaries for lookup during dispatch.

## JavaScript Interop

The runtime exposes entry points for JavaScript:

### Dispatch (from events)

```csharp
[JSExport]
public static void Dispatch(string messageId)
```

Called by JavaScript when an event occurs on an element with a `data-event-*` attribute.

### DispatchData (events with data)

```csharp
[JSExport]
public static void DispatchData(string messageId, string? json)
```

Called when an event includes data (e.g., input value).

### DispatchSubscriptionData

```csharp
[JSExport]
public static void DispatchSubscriptionData(string key, string? json)
```

Called by JavaScript when a subscription produces data.

## Instrumentation

The runtime integrates with OpenTelemetry:

```csharp
using var runActivity = Instrumentation.ActivitySource.StartActivity("Run");
// ...
using var messageActivity = Instrumentation.ActivitySource.StartActivity("Message");
messageActivity?.SetTag("message.type", message.GetType().FullName);
```

Activities are created for:
- Overall run lifecycle
- Each message processing
- Command handling

## Error Handling

The runtime handles missing handlers gracefully:

```csharp
if (_handlers.TryGetValue(messageId, out var message))
{
    Dispatch(message);
    return;
}
// Missing handler can occur during DOM replacement; ignore gracefully
System.Diagnostics.Debug.WriteLine($"[Abies] Missing handler for messageId={messageId}");
```

This prevents crashes during rapid DOM updates.

## Best Practices

### 1. Keep Commands Fast

Long-running commands block the message loop:

```csharp
// ❌ Avoid blocking calls
case LongCommand:
    Thread.Sleep(5000);  // Blocks all messages!
    break;

// ✅ Use async properly
case LongCommand:
    await Task.Delay(5000);  // Allows other processing
    break;
```

### 2. Don't Store Dispatch

The dispatch function is stable but shouldn't be stored:

```csharp
// ❌ Avoid
static Func<Message, ValueTuple>? _savedDispatch;

public static async Task HandleCommand(Command cmd, Func<Message, ValueTuple> dispatch)
{
    _savedDispatch = dispatch;  // Don't do this
}

// ✅ Use immediately
public static async Task HandleCommand(Command cmd, Func<Message, ValueTuple> dispatch)
{
    dispatch(new SomeMessage());  // Use directly
}
```

### 3. Batch Related Commands

```csharp
// Instead of dispatching multiple commands
return (model, new Command.Batch(
    new LoadUser(userId),
    new LoadArticles(userId),
    new LoadNotifications(userId)
));
```

## See Also

- [Program API](./program.md) — Program interface
- [Command API](./command.md) — Command types
- [Subscription API](./subscription.md) — Subscription system
- [Concepts: MVU Architecture](../concepts/mvu-architecture.md)
- [ADR-001: MVU Architecture](../adr/ADR-001-mvu-architecture.md)
- [ADR-005: WebAssembly Runtime](../adr/ADR-005-webassembly-runtime.md)
