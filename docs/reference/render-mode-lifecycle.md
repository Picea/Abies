---
description: 'Render Mode Lifecycle — Deep reference for InteractiveServer, InteractiveWasm, InteractiveAuto, and Static modes'
---

# Render Mode Lifecycle

This document provides a deep technical reference for each Abies render mode, including initialization, session lifecycle, component state preservation, and handoff behavior in InteractiveAuto mode.

**Audience:** Framework contributors, advanced developers building render mode–specific optimizations, Aspire orchestration users

**Related:** [Render Modes Concept](../concepts/render-modes.md), [Choosing a Render Mode](../guides/render-mode-selection.md)

---

## Overview

Abies supports four render modes, each with distinct initialization, state management, and lifecycle characteristics:

| Mode | Where | When | Session | Complexity |
|------|-------|------|---------|------------|
| **Static** | Server | Build-time | None | Lowest |
| **InteractiveServer** | Server | Runtime | Per-request | Medium |
| **InteractiveWasm** | Browser | Runtime | Browser-side | Medium |
| **InteractiveAuto** | Browser (server-first) | Runtime | Browser takes over | Highest |

---

## Static Mode

### Initialization

**Flow:**
1. Application starts (ASP.NET Core)
2. Middleware registers Abies routing components
3. Client requests a route
4. Render function called server-side, returns HTML string
5. HTML is embedded in response, sent to client

**Example:**
```csharp
app.MapAbiesRoutes<CounterApp>();
```

### Rendering

Render is called exactly ONCE per request:

```csharp
public static Document View(Model model)
{
    return div([],
        h1([], text($"Count: {model.Count}"))
    );
}
```

**Output:** Pure HTML string. No JavaScript bridge, no state synchronization.

### Session

There is no session. Each request produces a new HTML response. If the user navigates or refreshes, a new HTTP request triggers a new render cycle.

### State Preservation

State is persisted via:
- **Query strings** — `/counter?count=42`
- **Form state** — POSTs with hidden fields
- **Cookies** — Stored client-side
- **Server session store** — Requires explicit setup

### Lifecycle

```
Request arrives
    ↓
Route matched
    ↓
View() called (pure function)
    ↓
HTML generated
    ↓
Response sent
    ↓
(No further lifecycle)
```

**Pros:**
- ✅ Simplest mode (no JavaScript)
- ✅ SEO-friendly (server HTML)
- ✅ Fast cold start

**Cons:**
- ❌ No interactivity (full-page reloads required)
- ❌ No client-side state
- ❌ Network latency for every action

---

## InteractiveServer Mode

### Initialization

**Flow:**
1. Application starts (ASP.NET Core with SignalR)
2. Middleware registers Abies routes and SignalR hubs
3. Client requests page, server returns HTML + JavaScript
4. JavaScript establishes WebSocket via SignalR
5. Server creates Abies runtime instance, begins rendering
6. Initial render sent to client, rendered in browser

**Example:**
```csharp
app.MapAbiesRoutes<CounterApp>();
```

### Rendering Pipeline

```
Client sends action (click, input change)
    ↓
JavaScript captures event
    ↓
Event dispatched via SignalR to server
    ↓
Server runtime receives message
    ↓
Transition (model, command) computed
    ↓
View (new model) called
    ↓
Diff computed (old VDOM vs new)
    ↓
VDOM patches encoded to binary
    ↓
Binary patches sent via SignalR to client
    ↓
Client patches DOM
    ↓
(cycle repeats for next event)
```

### Session

Session is **server-side** and **per-connection**:

```csharp
// Server-side
public class CounterModel
{
    public int Count { get; set; }
    public string UserId { get; set; }  // Tied to WebSocket connection
}
```

**Session identity:**
- Tied to WebSocket connection (SignalR `ConnectionId`)
- Preserved across page navigations (within same session)
- Lost when client disconnects

### State Preservation

State lives in `.NET memory` on the server:

```csharp
// Server holds this in memory
var model = new CounterModel { Count = 0, UserId = "user-123" };
```

**Implications:**
- ✅ Fast (no network delay for state)
- ✅ Secure (state never crosses network)
- ❌ Server memory usage scales with active sessions
- ❌ Disconnection = state loss (unless persisted)

### Lifecycle

```
Session created
    ↓
Runtime<TProgram, TModel, Argument> instantiated
    ↓
Model initialized
    ↓
Initial render sent to client
    ↓
(Client sends messages; server processes, renders, sends patches)
    ↓
Client disconnects
    ↓
Runtime disposed, session ended
    ↓
Memory freed
```

**Pros:**
- ✅ Interactive (fast feedback, no full reloads)
- ✅ Secure (state on server)
- ✅ Simple to reason about (server-side semantics)

**Cons:**
- ❌ Server-side state scales with clients
- ❌ Requires SignalR/WebSocket
- ❌ Disconnection = session loss
- ❌ Network latency (every interaction over network)

### Scaling Considerations

Each active client holds a server-side model instance:

```
100 concurrent users × ~10KB per model = ~1MB
1000 concurrent users × ~10KB per model = ~10MB
```

For large models or user bases, consider:
- State compression
- Periodic save to database
- Sticky-session load balancing (user stays on same server)
- Distributed session store (Redis, caching layer)

---

## InteractiveWasm Mode

### Initialization

**Flow:**
1. Application starts (ASP.NET Core)
2. Middleware serves WASM bundle
3. Client requests page, server returns HTML + links to `abies.js` + WASM bundle
4. JavaScript loads `abies.js`
5. `abies.js` loads WASM runtime
6. WASM runtime instantiated in browser
7. Initial render happens in WASM, DOM updated

### Rendering Pipeline

```
Client sends action (click, input change)
    ↓
JavaScript captures event (via event delegation)
    ↓
Event dispatched to WASM runtime
    ↓
WASM transition (model, command) computed
    ↓
WASM view (new model) called
    ↓
WASM diff computed
    ↓
WASM binary patches generated
    ↓
Binary patches passed back to JavaScript
    ↓
JavaScript patches DOM
    ↓
(cycle repeats for next event)
```

**All computation is local — no server involvement** (except for async commands like HTTP).

### Session

Session is **browser-side**:

```csharp
// Browser-side WASM runtime
public class CounterModel
{
    public int Count { get; set; }
}
```

Model state lives in WASM linear memory. Each browser tab is an independent session.

### State Preservation

**Client-side only:**

Methods:
- **In memory** — Lost on page refresh
- **localStorage** — Survives page refresh, persists until cleared
- **IndexedDB** — Structured storage, persists until cleared
- **Server API** — Send state to server for true persistence

Example with localStorage:

```csharp
public class CounterProgram
{
    public static async Task<Model> Init(Unit _)
    {
        var savedCount = await JS.InvokeAsync<int?>("localStorage.getItem", "count");
        return new Model { Count = savedCount ?? 0 };
    }
    
    public static (Model, Command) Transition(Model model, Message msg)
    {
        var (newModel, cmd) = msg switch
        {
            Increment => (model with { Count = model.Count + 1 }, Commands.None),
            _ => (model, Commands.None)
        };
        
        // Persist to localStorage
        JS.InvokeVoidAsync("localStorage.setItem", "count", newModel.Count);
        
        return (newModel, cmd);
    }
}
```

### Lifecycle

```
Page loads
    ↓
WASM bundle downloaded
    ↓
WASM runtime starts
    ↓
Init() called (load from localStorage if needed)
    ↓
Initial render
    ↓
(Client processes events locally; no server involvement)
    ↓
Page unloads / tab closed
    ↓
WASM runtime disposed
    ↓
(State can be persisted to localStorage before disposal)
```

**Pros:**
- ✅ Fully interactive (no network for UI actions)
- ✅ Works offline
- ✅ No server-side state scaling
- ✅ Can be deployed as static files (no backend required)

**Cons:**
- ❌ Client gets full app code (larger bundle)
- ❌ State persistence requires manual work
- ❌ WASM startup delay (~1-2 seconds typical)
- ❌ Async commands still require server

### Performance Characteristics

**First Paint:** ~2-3 seconds (WASM startup) → 4-5 seconds (vs ~100ms for InteractiveServer)

**Subsequent interactions:** <100ms (local computation) → vs 100-300ms + network for InteractiveServer

---

## InteractiveAuto Mode

### Initialization

**Flow:**
1. Application starts (ASP.NET Core + WASM bundle)
2. Client requests page, server returns HTML + JavaScript
3. **Server renders first** (like InteractiveServer)
4. JavaScript establishes SignalR connection while WASM loads
5. Server manages interactions via SignalR (full server-side semantics)
6. **In parallel**, WASM bundle downloads and initializes
7. **When WASM is ready**, model state is transferred from server to browser
8. Client disconnects from SignalR, switches to browser-side execution
9. **Browser now handles interactions locally** (like InteractiveWasm)

### Rendering Pipeline

#### Phase 1: Server-side (0–2 seconds)

```
Client action (click, input)
    ↓
Sent to server via SignalR
    ↓
Server renders, sends patches
    ↓
Browser updates DOM
```

**Fast feedback during WASM startup.**

#### Phase 2: Handoff (2–5 seconds)

```
WASM bundle downloaded and initialized
    ↓
Server sends serialized model to WASM runtime
    ↓
WASM runtime instantiated with server model
    ↓
Client disconnects SignalR
    ↓
Browser switches to local computation
```

#### Phase 3: Browser-side (5+ seconds)

```
Client action (click, input)
    ↓
All computation local in WASM
    ↓
No server involvement (except async commands)
```

### Session Management

#### Server-side Session (Phase 1)

```csharp
// Server maintains model during SignalR phase
public class CounterModel
{
    public int Count { get; set; }
}
```

**Lifecycle:**
- Created when client connects
- Updated on each client message
- Serialized when WASM is ready

#### Browser-side Session (Phase 3)

```csharp
// WASM receives serialized model
public class CounterModel
{
    public int Count { get; set; }
}
```

**Lifecycle:**
- Initialized from server serialization
- Runs independently in browser
- Persisted to localStorage if needed

### State Transfer (Handoff)

The critical phase is transferring state from server to browser:

```csharp
// Server serializes model
var json = JsonSerializer.Serialize(model);  // e.g., {"count": 3}

// SendAsync("handoff", json)  transmitted via SignalR

// Client receives JSON
const model = JSON.parse(json);

// WASM runtime initialized with model
abies.initializeModel(model);

// SignalR disconnected
signalRConnection.close();

// WASM takes over
```

**Requirements for handoff:**
1. Model must be **JSON-serializable** (no circular refs, no unsupported types)
2. Serialization must be **deterministic** (same model → same JSON)
3. WASM deserialization must match server (see [Source-Generated JSON](../adr/ADR-023-source-generated-json.md))

**Risks during handoff:**
- ⚠️ **Race condition:** User clicks during handoff → message sent to server or browser?
  - Solution: Server buffers messages during handoff, replays only if needed
- ⚠️ **State divergence:** Server and WASM init with different models
  - Solution: Use source-generated JSON serialization (deterministic, version-safe)

### Lifecycle

```
Page loads
    ↓
Server renders, sends HTML + JavaScript + patches
    ├─ DOM updated with server content
    ├─ User interacts (fast feedback via SignalR)
    │
    └─ WASM bundle downloads in parallel
        ↓
        WASM runtime initializes
        ↓
        (User continues interacting via SignalR)
        ↓
        WASM ready, model transferred from server
        ↓
        SignalR disconnected
        ↓
        Browser takes over (WASM-side execution)
        ↓
        User interacts locally (no network)
        ↓
        Page unloads
        ↓
        WASM runtime disposed
```

### Transition Timing

| Metric | Typical Value |
|--------|---------------|
| Initial server render | 50–200ms |
| First paint (DOM updated) | 50–200ms |
| Full page interactive (Initial SignalR) | 100–300ms |
| WASM bundle download | 500–1500ms |
| WASM startup | 500–2000ms |
| **Handoff latency** | **< 1ms** |
| **Full browser-side interactive** | **2–5 seconds** |

### Pros and Cons

**Pros:**
- ✅ Fast first interaction (server-side)
- ✅ Portable (works offline after handoff)
- ✅ Best UX (server for speed, WASM for responsiveness)
- ✅ No network latency after handoff

**Cons:**
- ❌ Complex handoff logic
- ❌ Model serialization overhead
- ❌ Server must keep state until WASM ready
- ❌ WASM startup delay before switching
- ❌ Requires careful error handling during transition

### Error Handling During Handoff

#### Handoff Timeout

If WASM doesn't initialize after 30 seconds, fallback to server-side:

```csharp
// Server-side fallback
if (!wasmReady && DateTime.UtcNow > handoffDeadline)
{
    // Keep server-side execution
    // Continue using SignalR
}
```

#### Serialization Mismatch

If server model can't serialize to JSON:

```csharp
// Before handoff, ensure model is JSON-serializable
if (!IsJsonSerializable(model))
{
    // Either:
    // 1. Simplify model (extract only serializable fields)
    // 2. Stay server-side (skip handoff)
    // 3. Use custom JSON converters
}
```

#### Stale Model on Browser

If server state changes between serialization and browser init:

```csharp
// Server tracks version during handoff
long modelVersion = 123;

// Send to browser with version
const handoff = {
  model: {...},
  version: 123
};

// If new events came in before handoff,
// browser skips them (using version check)
```

---

## Comparison Table

| Aspect | Static | InteractiveServer | InteractiveWasm | InteractiveAuto |
|--------|--------|-------------------|-----------------|-----------------|
| Computation | Server | Server | Browser | Server → Browser |
| Interactivity | None | Full | Full | Full |
| State location | Stateless | Server | Browser | Server → Browser |
| Network latency | Per action | Per action | None (after init) | Per action → None |
| First paint | ~50ms | ~100ms | ~2000ms | ~100ms |
| Deployment | Static files | ASP.NET Core | Static files | ASP.NET Core + static files |
| Scaling | O(1) | O(users) | O(1) | O(users) × short duration |
| Offline capable | No | No | Yes | Yes (after handoff) |
| SEO friendly | Yes | No | No | No |
| Simplicity | Highest | Medium | Medium | Lowest |

---

## Implementation Considerations

### For Framework Contributors

**Static Mode:**
- See `Abies/Runtime.cs` static render path
- No WebSocket/SignalR involved
- HTML string generation via `Render.cs`

**InteractiveServer:**
- See `Abies.Server.Kestrel/Runtime.cs`
- SignalR hub at `Abies.Server/Hub.cs`
- Binary batch protocol (see [Binary Patch Protocol](./binary-patch-protocol.md))

**InteractiveWasm:**
- See `Picea.Abies.Browser/Runtime.cs`
- WASM runtime in `Picea.Abies.Browser/wwwroot/abies.js`
- No server involvement (except async commands)

**InteractiveAuto:**
- See `Picea.Abies.Server/Session.cs` (auto mode detection)
- WebSocket transport in `Picea.Abies.Server/Transport.cs`
- Model serialization via source-generated JSON

### For Application Developers

**Choosing a mode:**

1. **Static** — Blogs, documentation, marketing sites
2. **InteractiveServer** — Real-time synchronization, shared state
3. **InteractiveWasm** — Offline apps, independent clients, rich interactions
4. **InteractiveAuto** — Most applications (best tradeoff)

**Best practices:**

- Keep models **JSON-serializable** for InteractiveAuto handoff
- Avoid large models (size affects handoff time)
- Test handoff with slow networks (set throttle in DevTools)
- Handle disconnection gracefully in InteractiveServer

---

## Performance Profiling

### StaticMode

No profiling needed (one-shot render). Use standard ASP.NET benchmarking.

### InteractiveServer

```csharp
// Measure round-trip: client message → server → patches → client
var sw = Stopwatch.StartNew();
await dispatchMessage(message);
sw.Stop();
Console.WriteLine($"Round-trip: {sw.ElapsedMilliseconds}ms");
```

### InteractiveWasm

```javascript
// Measure local computation
console.time('wasm-render');
abies.dispatchMessage(message);
console.timeEnd('wasm-render');

// Measure WASM startup
console.time('wasm-init');
const wasm = await WebAssembly.instantiate(wasmBuffer);
console.timeEnd('wasm-init');
```

### InteractiveAuto

```csharp
// Measure handoff latency
var handoffStart = DateTime.UtcNow;
await handoffModel(model);
var handoffTime = DateTime.UtcNow - handoffStart;
Console.WriteLine($"Handoff time: {handoffTime.TotalMilliseconds}ms");
```

---

## See Also

- [Render Modes Concept](../concepts/render-modes.md) — Conceptual overview
- [Choosing a Render Mode](../guides/render-mode-selection.md) — Decision guide
- [Binary Patch Protocol](./binary-patch-protocol.md) — How patches are encoded
- [JavaScript Interop](./js-interop.md) — JS/WASM bridge mechanics
- [Performance Guide](../guides/performance.md) — Optimization techniques

## Implementation Notes

**Tracking Issue:** [#219: InteractiveServer and InteractiveAuto Lifecycle Documentation](https://github.com/Picea/Abies/issues/219)

**Source Files:**
- Static: `Abies/Runtime.cs`
- InteractiveServer: `Abies.Server.Kestrel/Runtime.cs`, `Abies.Server/Hub.cs`
- InteractiveWasm: `Abies.Browser/Runtime.cs`
- InteractiveAuto: `Abies.Server/Runtime.cs` (auto detection), `Abies.Server/Handoff.cs`

Last updated: 2026-04-12
