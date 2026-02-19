# Runtime Internals

This document describes the internal implementation of the Abies runtime.

## Overview

The runtime is the engine that drives Abies applications. It:

1. Manages the message loop
2. Coordinates virtual DOM updates
3. Handles JavaScript interop
4. Manages subscriptions
5. Provides OpenTelemetry instrumentation

## Architecture

```text
┌──────────────────────────────────────────────────────────────────┐
│                           Runtime                                 │
├──────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐          │
│  │   Message   │    │   Handler   │    │Subscription │          │
│  │   Channel   │    │  Registries │    │   Manager   │          │
│  └──────┬──────┘    └──────┬──────┘    └──────┬──────┘          │
│         │                  │                   │                  │
│  ┌──────┴──────────────────┴───────────────────┴──────┐          │
│  │                    Message Loop                     │          │
│  │  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐    │          │
│  │  │ Update │→ │  View  │→ │  Diff  │→ │ Apply  │    │          │
│  │  └────────┘  └────────┘  └────────┘  └────────┘    │          │
│  └─────────────────────────────────────────────────────┘          │
│                              │                                    │
│  ┌───────────────────────────┴───────────────────────────┐       │
│  │                   JavaScript Interop                   │       │
│  │  SetAppContent, UpdateAttribute, AddChild, etc.        │       │
│  └────────────────────────────────────────────────────────┘       │
└──────────────────────────────────────────────────────────────────┘
```

## Message Channel

Messages are queued in an unbounded channel:

```csharp
private static readonly Channel<Message> _messageChannel = Channel.CreateUnbounded<Message>();
```

### Why Unbounded?

- No backpressure needed (UI events are rare relative to processing speed)
- Prevents blocking on dispatch
- Simple implementation

### Message Sources

Messages enter the channel from:

1. **Event handlers** — User interactions
2. **Commands** — Async operation results
3. **Subscriptions** — External events (timers, WebSocket, etc.)
4. **Navigation** — URL changes

## Handler Registries

Event handlers are stored in dictionaries (non-concurrent, since WASM is single-threaded):

```csharp
// Simple handlers (message only)
private static readonly Dictionary<string, Message> _handlers = new();

// Handlers with data (e.g., input value)
private static readonly Dictionary<string, (Func<object?, Message> handler, Type dataType)> _dataHandlers = new();

// Subscription handlers
private static readonly Dictionary<string, (Func<object?, Message> handler, Type dataType)> _subscriptionHandlers = new();
```

### Handler Lifecycle

1. **Registration** — When DOM is rendered, handlers are added
2. **Dispatch** — JavaScript calls `Dispatch(handlerId)` or `DispatchData(handlerId, json)`
3. **Unregistration** — When element is removed, handlers are cleaned up

## Message Loop

The core message processing loop:

```csharp
await foreach (var message in _messageChannel.Reader.ReadAllAsync())
{
    using var activity = Instrumentation.ActivitySource.StartActivity("Message");
    
    // 1. Update model
    var (newModel, command) = TProgram.Update(message, model);
    model = newModel;
    
    // 2. Generate new virtual DOM
    var newDom = TProgram.View(model);
    var alignedBody = PreserveIds(dom, newDom.Body);
    
    // 3. Compute patches
    var patches = Operations.Diff(dom, alignedBody);
    
    // 4. Apply patches
    foreach (var patch in patches)
        await Operations.Apply(patch);
    
    dom = alignedBody;
    
    // 5. Update subscriptions
    subscriptionState = SubscriptionManager.Update(
        subscriptionState, 
        TProgram.Subscriptions(model), 
        Dispatch
    );
    
    // 6. Execute command
    await ExecuteCommand(command);
}
```

### ID Preservation

The `PreserveIds` function maintains element identity across renders:

```csharp
private static Node PreserveIds(Node? oldNode, Node newNode)
{
    if (oldNode is Element oldEl && newNode is Element newEl && oldEl.Tag == newEl.Tag)
    {
        // Copy IDs from old tree to new tree
        var attrs = newEl.Attributes.Select(attr =>
        {
            var oldAttr = Array.Find(oldEl.Attributes, a => a.Name == attr.Name);
            return attr with { Id = oldAttr?.Id ?? attr.Id };
        }).ToArray();
        
        var children = newEl.Children
            .Select((child, i) => i < oldEl.Children.Length 
                ? PreserveIds(oldEl.Children[i], child) 
                : child)
            .ToArray();
        
        return new Element(oldEl.Id, newEl.Tag, attrs, children);
    }
    return newNode;
}
```

## JavaScript Interop

### Setup

The runtime sets up interop handlers during initialization:

```csharp
private static void SetupInteropHandlers<TProgram, TArguments, TModel>()
{
    // URL change handler
    Interop.OnUrlChange(newUrlString =>
    {
        var newUrl = Url.Create(new(newUrlString));
        var message = TProgram.OnUrlChanged(newUrl);
        Dispatch(message);
    });
    
    // Link click handler
    Interop.OnLinkClick(urlString =>
    {
        var currentUrl = Url.Create(Interop.GetCurrentUrl());
        var newUrl = Url.Create(urlString);
        
        if (AreSameOrigin(currentUrl, newUrl))
            Dispatch(TProgram.OnLinkClicked(new UrlRequest.Internal(newUrl)));
        else
            Dispatch(TProgram.OnLinkClicked(new UrlRequest.External(urlString)));
    });
    
    // Form submit handler
    Interop.OnFormSubmit(urlString => /* similar to link click */);
}
```

### Exported Functions

JavaScript can call these .NET methods:

```csharp
[JSExport]
public static void Dispatch(string messageId)
{
    if (_handlers.TryGetValue(messageId, out var message))
    {
        _messageChannel.Writer.TryWrite(message);
    }
}

[JSExport]
public static void DispatchData(string messageId, string? json)
{
    if (_dataHandlers.TryGetValue(messageId, out var entry))
    {
        object? data = json is null ? null : JsonSerializer.Deserialize(json, entry.dataType);
        var message = entry.handler(data);
        _messageChannel.Writer.TryWrite(message);
    }
}

[JSExport]
public static void DispatchSubscriptionData(string key, string? json)
{
    if (_subscriptionHandlers.TryGetValue(key, out var entry))
    {
        object? data = json is null ? null : JsonSerializer.Deserialize(json, entry.dataType);
        var message = entry.handler(data);
        _messageChannel.Writer.TryWrite(message);
    }
}
```

### DOM Operations

The `Interop` class provides DOM manipulation methods:

```csharp
public static partial class Interop
{
    [JSImport("setAppContent", "app")]
    public static partial Task SetAppContent(string html);
    
    [JSImport("updateAttribute", "app")]
    public static partial Task UpdateAttribute(string id, string name, string value);
    
    [JSImport("addAttribute", "app")]
    public static partial Task AddAttribute(string id, string name, string value);
    
    [JSImport("removeAttribute", "app")]
    public static partial Task RemoveAttribute(string id, string name);
    
    [JSImport("addChildHtml", "app")]
    public static partial Task AddChildHtml(string parentId, string html);
    
    [JSImport("removeChild", "app")]
    public static partial Task RemoveChild(string parentId, string childId);
    
    [JSImport("replaceChildHtml", "app")]
    public static partial Task ReplaceChildHtml(string oldId, string newHtml);
    
    [JSImport("updateTextContent", "app")]
    public static partial Task UpdateTextContent(string id, string text);
}
```

## Command Execution

Commands are executed after each update:

```csharp
switch (command)
{
    // Navigation commands handled specially
    case Navigation.Command.PushState pushState:
        await Interop.PushState(pushState.Url.ToString());
        Dispatch(TProgram.OnUrlChanged(pushState.Url));
        break;
        
    case Navigation.Command.Load load:
        await Interop.Load(load.Url.ToString());
        break;
        
    case Navigation.Command.ReplaceState replaceState:
        await Interop.ReplaceState(replaceState.Url.ToString());
        Dispatch(TProgram.OnUrlChanged(replaceState.Url));
        break;
    
    // Batch commands
    case Command.Batch batch:
        foreach (var cmd in batch.Commands)
            await ExecuteCommand(cmd);
        break;
    
    // Custom commands delegated to program
    default:
        await TProgram.HandleCommand(command, Dispatch);
        break;
}
```

## Subscription Manager

The subscription manager tracks active subscriptions:

```csharp
internal static class SubscriptionManager
{
    public static SubscriptionState Start(Subscription subscription, Func<Message, ValueTuple> dispatch)
    {
        var state = new SubscriptionState();
        AddSubscriptions(state, subscription, dispatch);
        return state;
    }
    
    public static SubscriptionState Update(
        SubscriptionState currentState, 
        Subscription newSubscription, 
        Func<Message, ValueTuple> dispatch)
    {
        // Compare old and new subscriptions
        var oldKeys = currentState.ActiveKeys;
        var newKeys = ExtractKeys(newSubscription);
        
        // Stop removed subscriptions
        foreach (var key in oldKeys.Except(newKeys))
            StopSubscription(currentState, key);
        
        // Start new subscriptions
        foreach (var key in newKeys.Except(oldKeys))
            StartSubscription(currentState, key, newSubscription, dispatch);
        
        return currentState;
    }
}
```

### Subscription Registration

Subscriptions register handlers with the runtime:

```csharp
internal static void RegisterSubscriptionHandler(
    string key, 
    Func<object?, Message> handler, 
    Type dataType)
{
    _subscriptionHandlers[key] = (handler, dataType);
}

internal static void UnregisterSubscriptionHandler(string key)
{
    _subscriptionHandlers.TryRemove(key, out _);
}
```

## OpenTelemetry Integration

The runtime creates activities for tracing:

```csharp
public static class Instrumentation
{
    public static readonly ActivitySource ActivitySource = new("Abies");
}

// Usage in runtime
using var runActivity = Instrumentation.ActivitySource.StartActivity("Run");
// ... initialization

await foreach (var message in _messageChannel.Reader.ReadAllAsync())
{
    using var messageActivity = Instrumentation.ActivitySource.StartActivity("Message");
    messageActivity?.SetTag("message.type", message.GetType().FullName);
    // ... processing
}
```

### Activity Hierarchy

```text
Run
├── Message (UrlChanged)
│   └── HandleCommand
├── Message (DataLoaded)
│   └── HandleCommand
└── Message (ButtonClicked)
```

## Error Handling

The runtime handles errors gracefully:

```csharp
[JSExport]
public static void Dispatch(string messageId)
{
    if (_handlers.TryGetValue(messageId, out var message))
    {
        _messageChannel.Writer.TryWrite(message);
        return;
    }
    
    // Handler may be missing during rapid DOM updates
    Debug.WriteLine($"[Abies] Missing handler for messageId={messageId}");
}
```

### Why Handlers Go Missing

1. Element is clicked during DOM replacement
2. Race condition between patch application and event
3. Event bubbling from removed element

The runtime ignores these cases rather than throwing.

## Thread Safety

Since WASM is single-threaded, the runtime uses non-concurrent collections for
handler registries and object pools. This avoids the overhead of thread-safe
abstractions that provide no benefit in a single-threaded environment:

- `Channel<T>` — Thread-safe message queue (retained for its async enumeration API)
- `Dictionary<K,V>` — Handler registries (replaced `ConcurrentDictionary`)
- `Stack<T>` — Object pools (replaced `ConcurrentQueue`, LIFO is more cache-friendly)

## Memory Management

### Object Pooling

Frequently allocated objects are pooled using `Stack<T>` (LIFO pattern for cache
locality). Since WASM is single-threaded, no concurrent collections are needed:

```csharp
private static readonly Stack<List<Patch>> _patchListPool = new();

private static List<Patch> RentPatchList()
{
    if (_patchListPool.TryPop(out var list))
    {
        list.Clear();
        return list;
    }
    return new List<Patch>();
}

private static void ReturnPatchList(List<Patch> list)
{
    if (list.Count < 1000)  // Prevent memory bloat
        _patchListPool.Push(list);
}
```

### Handler Cleanup

Handlers are cleaned up when elements are removed:

```csharp
internal static void UnregisterHandlers(Node node)
{
    switch (node)
    {
        case Element element:
            foreach (var attr in element.Attributes)
            {
                if (attr is Handler handler)
                {
                    _handlers.Remove(handler.CommandId);
                    _dataHandlers.Remove(handler.CommandId);
                }
            }
            foreach (var child in element.Children)
                UnregisterHandlers(child);
            break;
        
        case ILazyMemoNode lazyMemo:
            UnregisterHandlers(lazyMemo.RenderedNode);
            break;
        
        case IMemoNode memo:
            UnregisterHandlers(memo.RenderedNode);
            break;
    }
}
```

The `ILazyMemoNode` and `IMemoNode` cases ensure that handlers inside memoized
subtrees are properly cleaned up when those subtrees are removed from the DOM.

## See Also

- [API: Runtime](../api/runtime.md) — Public API
- [API: Program](../api/program.md) — Program interface
- [Concepts: MVU Architecture](../concepts/mvu-architecture.md) — Conceptual overview
- [Reference: Virtual DOM Algorithm](./virtual-dom-algorithm.md) — Diffing details
- [ADR-005: WebAssembly Runtime](../adr/ADR-005-webassembly-runtime.md) — Design decision
