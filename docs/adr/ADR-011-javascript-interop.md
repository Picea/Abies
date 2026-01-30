# ADR-011: JavaScript Interop Strategy

**Status:** Accepted  
**Date:** 2024-01-15  
**Decision Makers:** Abies Core Team  
**Supersedes:** None  
**Superseded by:** None

## Context

Abies runs in WebAssembly but must interact with browser APIs that are only available through JavaScript:

- DOM manipulation
- Browser history/navigation
- Local storage
- Event listeners
- Timers and animation frames

The .NET WebAssembly runtime provides several interop mechanisms:

1. `[JSImport]` / `[JSExport]` attributes (modern)
2. `IJSRuntime` service (Blazor-style)
3. Direct memory sharing
4. Custom JS-to-WASM bridges

## Decision

We use the **modern `[JSImport]`/`[JSExport]` attributes** for JavaScript interop, with a thin `abies.js` JavaScript layer that handles browser APIs.

Architecture:

```
┌─────────────────────────────────────────────────────────┐
│                    C# (.NET WASM)                        │
│  ┌──────────────────────────────────────────────────┐   │
│  │              Interop.cs                           │   │
│  │   [JSImport] void SetAppContent(string html)     │   │
│  │   [JSImport] void UpdateAttribute(...)           │   │
│  │   [JSExport] void Dispatch(string messageId)     │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
                           ↕
┌─────────────────────────────────────────────────────────┐
│                     JavaScript                           │
│  ┌──────────────────────────────────────────────────┐   │
│  │                  abies.js                         │   │
│  │   setAppContent(html) { ... }                    │   │
│  │   updateAttribute(id, name, value) { ... }       │   │
│  │   // Event listeners call Dispatch()             │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
                           ↕
┌─────────────────────────────────────────────────────────┐
│                   Browser DOM                            │
└─────────────────────────────────────────────────────────┘
```

Key interop methods in `Interop.cs`:

```csharp
public static partial class Interop
{
    // Navigation
    [JSImport("pushState", "abies.js")]
    public static partial Task PushState(string url);

    // DOM manipulation
    [JSImport("setAppContent", "abies.js")]
    public static partial Task SetAppContent(string html);
    
    [JSImport("updateAttribute", "abies.js")]
    public static partial Task UpdateAttribute(string id, string name, string value);

    // Event callbacks
    [JSImport("onUrlChange", "abies.js")]
    public static partial void OnUrlChange(Action<string> handler);

    // Storage
    [JSImport("setLocalStorage", "abies.js")]
    public static partial Task SetLocalStorage(string key, string value);
}
```

Event dispatch from JavaScript back to C#:

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
    // Deserialize and dispatch with event data
}
```

## Consequences

### Positive

- **Type-safe interop**: C# signatures define the contract
- **Modern API**: Uses latest .NET WASM interop features
- **Async-friendly**: Returns `Task` for async operations
- **Thin JS layer**: JavaScript code is minimal and focused
- **Centralized**: All interop goes through `Interop.cs`
- **Testable**: Can mock `Interop` for unit testing

### Negative

- **Async overhead**: Each call crosses JS boundary asynchronously
- **Serialization cost**: Complex data must be serialized (JSON)
- **Two codebases**: Must maintain both C# and JS files
- **Debug complexity**: Call stack crosses language boundaries
- **Breaking changes**: JS API changes require coordinating both sides

### Neutral

- String-based message IDs map handlers to events
- Event data is JSON-serialized for type safety
- Subscription handlers use separate registry from one-shot handlers

## Alternatives Considered

### Alternative 1: Direct DOM Access via Memory

Share memory buffers between JS and WASM for DOM operations:

- Maximum performance
- Extremely complex to implement correctly
- Memory safety concerns
- Not practical for typical UI operations

Rejected as impractical for this use case.

### Alternative 2: Blazor's IJSRuntime

Use Blazor's JSRuntime abstraction:

```csharp
await JSRuntime.InvokeVoidAsync("someFunction", arg1, arg2);
```

- Familiar to Blazor developers
- Stringly-typed (function names as strings)
- Dependency on Blazor runtime
- Less compile-time safety

Rejected to stay independent of Blazor infrastructure.

### Alternative 3: Full DOM in C#

Implement complete DOM API in C#, marshal everything:

- Familiar DOM API
- Massive interop surface
- Poor performance
- Difficult to maintain

Rejected because virtual DOM approach is more efficient.

### Alternative 4: Third-party Bridge (e.g., Uno Platform)

Use an existing platform's interop layer:

- Proven at scale
- Large dependency
- Different abstractions
- Less control

Rejected to keep framework minimal and focused.

## Related Decisions

- [ADR-003: Virtual DOM Implementation](./ADR-003-virtual-dom.md)
- [ADR-005: WebAssembly Runtime](./ADR-005-webassembly-runtime.md)
- [ADR-007: Subscription Model](./ADR-007-subscriptions.md)

## References

- [.NET WASM JavaScript Interop](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability)
- [JSImport/JSExport](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/import-export-interop)
- [`Abies/Interop.cs`](../../Abies/Interop.cs) - C# interop declarations
- [`Abies/wwwroot/abies.js`](../../Abies/wwwroot/abies.js) - JavaScript implementation
