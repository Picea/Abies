# ADR-024: Four Render Modes — Static, InteractiveServer, InteractiveWasm, InteractiveAuto

**Status:** Accepted  
**Date:** 2026-03-11  
**Decision Makers:** Maurice Peters  
**Related:** ADR-001 (MVU Architecture), ADR-005 (WebAssembly Runtime), ADR-011 (JavaScript Interop)

## Context

Abies was originally designed as a **WebAssembly-only** framework (ADR-005). The entire application ran in the browser via .NET WASM, with JavaScript interop for DOM operations (ADR-011).

As the framework matured, several forces pushed toward server-side rendering (SSR) support:

1. **First-paint performance** — WASM apps must download and initialize the .NET runtime before anything renders. Even with loading placeholders, there is a multi-second delay before the application becomes interactive.

2. **SEO and social sharing** — Search engines and social media crawlers need server-rendered HTML. A WASM-only app is invisible to these agents.

3. **Progressive enhancement** — The application should work (at least partially) before JavaScript/WASM loads. Server-rendered HTML provides immediate content.

4. **Resource-constrained devices** — Not all devices can efficiently run WASM. Server-side rendering offloads computation to the server.

5. **Blazor precedent** — ASP.NET Blazor successfully demonstrated that the same component model can work across browser and server render modes. This validated the approach for .NET web frameworks.

## Decision

Abies supports **four render modes**, following the same taxonomy as Blazor but implemented for the MVU architecture:

### 1. Static

The `Program<TModel, TArgument>` is evaluated **once** on the server. The `Initialize` function produces a model, `View` renders it to HTML, and the result is sent as a complete HTML page. No interactivity — no MVU loop runs.

**Use case:** Landing pages, documentation sites, content pages.

### 2. InteractiveServer

The MVU loop runs **on the server**. The server maintains the model, processes messages, diffs the virtual DOM, and sends binary patches over a WebSocket connection. The browser applies patches to the real DOM via a thin JavaScript client.

**Use case:** Internal tools, dashboards, applications where WASM download size is a concern.

**Architecture:**

```text
Browser                          Server
┌──────────────┐    WebSocket    ┌──────────────────────┐
│ abies.js     │ ◄────────────► │ Session              │
│ (DOM patches │                │ ├── Runtime           │
│  + events)   │                │ │   ├── Model         │
│              │  binary batch  │ │   ├── View()        │
│ Real DOM ◄───┤ ◄──────────── │ │   ├── Transition()  │
│              │                │ │   └── Diff()        │
│ Events ──────┤ ──────────── ► │ └── Transport (WS)   │
└──────────────┘   JSON event   └──────────────────────┘
```

### 3. InteractiveWasm

The MVU loop runs **in the browser** via .NET WebAssembly. This is the original Abies mode (ADR-005). The `abies.js` module handles DOM operations and event delegation.

**Use case:** Offline-capable apps, high-interactivity applications, avoiding server costs.

### 4. InteractiveAuto

Starts as **InteractiveServer** (immediate interactivity) while the WASM runtime downloads in the background. Once WASM is ready, the session seamlessly transitions to **InteractiveWasm**.

**Use case:** Applications that need both fast first-interaction and offline capability.

### Shared MVU Contract

All four modes use the **same `Program<TModel, TArgument>` interface**:

```csharp
public interface Program<TModel, TArgument>
{
    static abstract (TModel Model, Command Command) Initialize(TArgument argument, Url url);
    static abstract (TModel Model, Command Command) Transition(TModel model, Message message);
    static abstract Document View(TModel model);
    static abstract Subscription Subscriptions(TModel model);
    static abstract (TModel Model, Command Command) OnUrlRequested(TModel model, UrlRequest request);
    static abstract (TModel Model, Command Command) OnUrlChanged(TModel model, Url url);
}
```

Application code is **identical** across render modes. The only difference is the hosting entry point:

```csharp
// InteractiveWasm (browser)
await Picea.Abies.Browser.Runtime.Run<App, Args, Model>(args, handler);

// InteractiveServer (Kestrel)
app.MapAbies<App, Args, Model>("/", args, handler);

// Static (Kestrel)
app.MapAbiesStatic<App, Args, Model>("/", args);
```

## Consequences

### Positive

- Application code is write-once, deploy-anywhere — the same `Program` implementation works across all four modes
- Server-side rendering enables SEO, social sharing, and fast first-paint
- InteractiveAuto provides the best of both worlds: instant interactivity + offline capability
- The MVU architecture naturally supports SSR because `View` is a pure function from model to virtual DOM — no browser dependencies

### Negative

- The framework surface area increases significantly (Picea.Abies.Server, Picea.Abies.Server.Kestrel packages)
- Server-side sessions require WebSocket management and per-session memory
- Testing matrix expands: each render mode needs its own E2E test coverage
- InteractiveAuto adds complexity for state transfer during the WASM handoff

### Neutral

- The binary batch protocol (RenderBatchWriter) works identically over both JS interop (WASM) and WebSocket (Server) — the transport is abstracted
- Navigation commands are transport-agnostic: `NavigationCommand.Push` works the same way regardless of whether the runtime is in the browser or on the server

## Alternatives Considered

### Alternative 1: WASM Only (Status Quo)

Keep Abies as a WASM-only framework and let users solve SSR externally (e.g., prerendering tools).

**Rejected** because: SSR is a first-class concern for production web applications. External prerendering tools cannot provide InteractiveServer or InteractiveAuto modes, and they break the MVU abstraction.

### Alternative 2: Server-Only (No WASM)

Drop WASM support and go server-only, similar to Phoenix LiveView.

**Rejected** because: WASM is a core differentiator for Abies. Many use cases (offline apps, reducing server costs, privacy-sensitive applications) require client-side execution. Dropping WASM would lose the existing user base.

### Alternative 3: Static + WASM Only (No Server Interactivity)

Support static server rendering and WASM, but not InteractiveServer.

**Rejected** because: InteractiveServer fills an important gap — applications that need instant interactivity without WASM download time. The architecture naturally supports it since the MVU loop is transport-agnostic.

## Related Decisions

- [ADR-001: MVU Architecture](./ADR-001-mvu-architecture.md) — The core architecture that enables render mode portability
- [ADR-005: WebAssembly Runtime](./ADR-005-webassembly-runtime.md) — The original WASM-only decision, now extended
- [ADR-011: JavaScript Interop](./ADR-011-javascript-interop.md) — The browser-side interop that both WASM and Server modes share
- [ADR-022: Picea Ecosystem Migration](./ADR-022-picea-ecosystem-migration.md) — The migration that enabled these architectural changes

## References

- [Blazor Render Modes (Microsoft Learn)](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes)
- [Phoenix LiveView Architecture](https://hexdocs.pm/phoenix_live_view/Phoenix.LiveView.html)
- [The Elm Architecture](https://guide.elm-lang.org/architecture/)
- [Picea.Abies Render Modes Concept Guide](../concepts/render-modes.md)
