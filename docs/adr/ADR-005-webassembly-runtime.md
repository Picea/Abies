# ADR-005: WebAssembly Runtime

**Status:** Accepted  
**Date:** 2024-01-15  
**Decision Makers:** Abies Core Team  
**Supersedes:** None  
**Superseded by:** None

## Context

Abies is a .NET-based web application framework. The framework needed to run .NET code in a browser. There are several approaches:

1. **Server-side rendering with AJAX** (traditional ASP.NET)
2. **Blazor Server** (SignalR-based server rendering)
3. **Blazor WebAssembly** (full .NET runtime in browser)
4. **.NET WASM SDK** (direct WebAssembly compilation)

Key requirements:

- Client-side interactivity without server round-trips
- Access to .NET libraries and language features
- Good performance for UI updates
- Minimal framework overhead

## Decision

We use **.NET WebAssembly** to run applications entirely in the browser, using the .NET 8+ WASM SDK with ahead-of-time (AOT) compilation support.

The runtime architecture:

```
┌─────────────────────────────────────────────────────────┐
│                      Browser                             │
│  ┌──────────────────────────────────────────────────┐   │
│  │                .NET WebAssembly                   │   │
│  │  ┌────────────────────────────────────────────┐  │   │
│  │  │              Abies Runtime                  │  │   │
│  │  │  ┌─────────────────────────────────────┐   │  │   │
│  │  │  │          User Application            │   │  │   │
│  │  │  │   Model | Update | View | Commands   │   │  │   │
│  │  │  └─────────────────────────────────────┘   │  │   │
│  │  └────────────────────────────────────────────┘  │   │
│  └──────────────────────────────────────────────────┘   │
│                         ↕ JS Interop                     │
│  ┌──────────────────────────────────────────────────┐   │
│  │                   JavaScript                      │   │
│  │              DOM | Events | Browser APIs          │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

Key implementation details:

1. `Runtime.Run<TProgram>` starts the MVU message loop
2. Messages are queued via `Channel<Message>` for ordered processing
3. JavaScript interop uses `[JSImport]` and `[JSExport]` attributes
4. DOM operations are batched through `abies.js`
5. Event handlers dispatch back to the .NET runtime via `Runtime.Dispatch`

## Consequences

### Positive

- **Full .NET in browser**: Access to C# features, LINQ, records, pattern matching
- **Offline capable**: Application runs entirely client-side after download
- **Type safety**: Compile-time checking across the entire application
- **Ecosystem access**: Can use compatible NuGet packages
- **AOT performance**: Ahead-of-time compilation reduces startup time

### Negative

- **Download size**: Initial payload includes .NET runtime (mitigated by trimming)
- **Startup time**: WASM initialization takes longer than vanilla JS
- **JavaScript interop overhead**: Each DOM operation crosses the JS boundary
- **Browser compatibility**: Requires modern browsers with WASM support
- **Debugging experience**: WASM debugging is less mature than JS debugging

### Neutral

- Threading model is single-threaded (aligns with DOM's single-threaded nature)
- Memory management is handled by .NET GC running in WASM
- No server infrastructure required for static hosting

## Alternatives Considered

### Alternative 1: Blazor Server

Render UI on server, push updates via SignalR:

- Lower initial download
- Requires persistent server connection
- Latency on every interaction
- Server scaling challenges

Rejected because client-side execution is a core goal.

### Alternative 2: Blazor WebAssembly (Full Framework)

Use Blazor's component model and rendering pipeline:

- Established ecosystem and tooling
- Heavier framework overhead
- Component lifecycle doesn't match MVU
- Less control over rendering

Rejected to maintain Elm-style simplicity and control.

### Alternative 3: Fable (F# to JavaScript)

Compile F# to JavaScript:

- Lighter runtime than WASM
- Better interop with JS ecosystem
- Requires F# (limits reach)
- Different language

Rejected to stay in the C# ecosystem.

### Alternative 4: JavaScript Framework with C# Backend

Use React/Vue/Svelte with ASP.NET API:

- Mature frontend tooling
- Split technology stack
- No shared types between frontend and backend
- Impedance mismatch between FP .NET and typical JS

Rejected because unified C# is a key benefit.

## Related Decisions

- [ADR-001: Model-View-Update Architecture](./ADR-001-mvu-architecture.md)
- [ADR-003: Virtual DOM Implementation](./ADR-003-virtual-dom.md)
- [ADR-011: JavaScript Interop Strategy](./ADR-011-javascript-interop.md)

## References

- [.NET WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly)
- [Blazor WASM vs Server](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models)
- [WebAssembly Specification](https://webassembly.github.io/spec/)
- [`Abies/Runtime.cs`](../../Abies/Runtime.cs) - WASM runtime loop
