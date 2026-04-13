# Render Modes

Abies supports four render modes that control where initial HTML is generated and where the MVU loop runs after the first render.

```csharp
public class MyApp : Program<MyModel, Unit>
{
    // These four functions are the same regardless of render mode:
    public static (MyModel, Command) Initialize(Unit _) => ...;
    public static (MyModel, Command) Transition(MyModel model, Message message) => ...;
    public static Document View(MyModel model) => ...;
    public static Subscription Subscriptions(MyModel model) => ...;
}
```

This is the same spectrum as Blazor's render modes, but implemented on top of the pure functional MVU architecture.

## The Four Modes

```text
Mode              │ Initial HTML  │ MVU Loop           │ JavaScript Payload
──────────────────┼───────────────┼────────────────────┼────────────────────
Static            │ Server        │ None               │ Zero
InteractiveServer │ Server        │ Server (WebSocket)  │ ~15 KB (abies-server.js)
InteractiveWasm   │ Server        │ Client (WASM)       │ ~1.1 MB (.NET WASM)
InteractiveAuto   │ Server        │ Server → Client     │ Both (handoff)
```

All four modes share the same `Program<TModel, TArgument>` interface. Your MVU code — `Initialize`, `Transition`, `View`, `Subscriptions` — doesn't change between modes. Only the hosting configuration differs.

## Static

**One-shot HTML generation with no client interactivity.**

```csharp
var html = Page.Render<MyApp, MyModel, Unit>(new RenderMode.Static());
```

The server calls `Initialize` → `View` → `Render.Html()` and produces a complete HTML page. No JavaScript is injected. No runtime starts. The page is static.

**Use when:**
- SEO-critical content pages
- Marketing pages, landing pages
- Pages where interactivity is unnecessary
- You want the absolute lightest payload (zero JS)

**Trade-offs:**
- ✅ Fastest first paint
- ✅ Zero JavaScript payload
- ✅ Works without JavaScript enabled
- ❌ No interactivity after initial render

## InteractiveServer

**Server holds the MVU state; DOM patches are sent to the client over WebSocket.**

```csharp
// Server renders initial HTML + injects WebSocket client script
var html = Page.Render<MyApp, MyModel, Unit>(
    new RenderMode.InteractiveServer(WebSocketPath: "/_abies/ws"));

// Each client connection gets its own Session
var session = await Session.Start<MyApp, MyModel, Unit>(
    sendPatches: bytes => webSocket.SendAsync(bytes, ...),
    receiveEvent: ct => DeserializeFromWebSocket(webSocket, ct),
    interpreter: MyInterpreter.Interpret);

await session.RunEventLoop(cancellationToken);
```

The server maintains a per-client `Session` containing a full MVU runtime with its own `HandlerRegistry`. User events arrive via WebSocket; binary patches flow back through the same transport.

**How it works:**

```text
[Browser]                              [Server]
   │                                      │
   │─── DOM Event (commandId, data) ───→│
   │                                      │ HandlerRegistry.CreateMessage()
   │                                      │ runtime.Dispatch(message)
   │                                      │ View(model') → Diff → Patches
   │                                      │ RenderBatchWriter.Write(patches)
   │─── Binary batch (patches) ───────│
   │ Apply patches to real DOM            │
```

**Use when:**
- You want instant interactivity (no WASM download wait)
- Your app needs server-side resources (databases, secrets)
- Thin client devices where WASM is too heavy
- Internal tools where persistent connection is acceptable

**Trade-offs:**
- ✅ Instant interactivity (no WASM bundle download)
- ✅ Full access to server resources
- ✅ Smaller client payload (~15 KB)
- ❌ Requires persistent WebSocket connection
- ❌ Network latency on every interaction
- ❌ Server memory per connected client

## InteractiveWasm

**Server renders initial HTML; the .NET WASM runtime takes over on the client.**

```csharp
var html = Page.Render<MyApp, MyModel, Unit>(new RenderMode.InteractiveWasm());
```

The server produces the initial HTML (fast first paint), including a `<script>` tag that boots the .NET WASM runtime. Once loaded, the client-side runtime diffs against the server-rendered DOM and takes over event handling.

**Use when:**
- Offline-capable apps
- Apps that work without a persistent server connection
- High-interaction UIs where latency must be minimized
- PWAs and installable web apps

**Trade-offs:**
- ✅ No persistent server connection after initial load
- ✅ Works offline (with service worker)
- ✅ Zero network latency for interactions
- ✅ Fast first paint (server-rendered HTML displayed before WASM loads)
- ❌ WASM bundle download before interactivity (~1.1 MB compressed)
- ❌ No direct access to server resources

## InteractiveAuto

**Server handles interactions immediately; WASM takes over once loaded.**

```csharp
var html = Page.Render<MyApp, MyModel, Unit>(
    new RenderMode.InteractiveAuto(WebSocketPath: "/_abies/ws"));
```

This combines the instant interactivity of `InteractiveServer` with the connectionless scalability of `InteractiveWasm`. The server holds the MVU session initially; once WASM is loaded and hydrated, the server session is disposed and the client takes over.

**Use when:**
- You want the best possible user experience
- First interaction must be immediate (no waiting for WASM)
- Long-running sessions should eventually be connectionless
- Public-facing apps where first impression matters

**Trade-offs:**
- ✅ Instant interactivity (server-first)
- ✅ No persistent connection after handoff
- ✅ Best overall user experience
- ❌ Most complex mode
- ❌ Requires both server session management and WASM handoff protocol
- ❌ State transfer during handoff adds implementation complexity

## The Same Program Runs Everywhere

This is the key architectural principle: all four render modes consume the same `Program<TModel, TArgument>` interface. The program's `Initialize`, `Transition`, `View`, and `Subscriptions` functions are pure — they don't know or care where they're running.

```csharp
public class MyApp : Program<MyModel, Unit>
{
    // These four functions are the same regardless of render mode:
    public static (MyModel, Command) Initialize(Unit _) => ...;
    public static (MyModel, Command) Transition(MyModel model, Message message) => ...;
    public static Document View(MyModel model) => ...;
    public static Subscription Subscriptions(MyModel model) => ...;
}
```

The `Runtime` class is parameterized by an `Apply` delegate — this is the seam between the pure MVU core and the platform. The command interpreter is provided separately at the application boundary.

| Platform | Apply Implementation |
| --- | --- |
| `Picea.Abies.Browser` | JS interop → mutate the real DOM in the browser |
| `Picea.Abies.Server` | Binary batch → send over WebSocket → client-side JS applies patches |
| Tests | Capture patches in a list for assertions |

## Decision Flowchart

```text
Do you need interactivity?
├── No → Static (zero JS, fastest first paint)
└── Yes → Do you need offline support?
          ├── Yes → InteractiveWasm
          └── No → Is first-interaction latency critical?
                    ├── No → InteractiveWasm (simpler, no persistent connection)
                    └── Yes → Can you accept a persistent WebSocket?
                              ├── Yes → InteractiveServer (simplest for instant interaction)
                              └── No → InteractiveAuto (server-first → WASM handoff)
```

## Server-Side Infrastructure

The server-side render modes are provided by two packages:

### `Picea.Abies.Server`

Platform-agnostic server runtime. No ASP.NET Core dependency. Provides:

- **`Page`** — Pure function: `(Program, RenderMode) → HTML string`
- **`Session`** — Server-side MVU session wrapping the runtime
- **`RenderMode`** — The four mode discriminated union
- **`Transport`** delegates — `SendPatches`, `ReceiveEvent`, `SendText`

### `Picea.Abies.Server.Kestrel`

ASP.NET Core Kestrel integration. Provides:

- **`Endpoints`** — `MapAbies<TProgram>()` extension method for `WebApplication`
- **`WebSocketTransport`** — WebSocket-based transport implementation
- Static file serving for `abies-server.js`

```csharp
var app = builder.Build();
app.MapAbies<MyApp, MyModel, Unit>(
    mode: new RenderMode.InteractiveServer(),
    interpreter: MyInterpreter.Interpret);
app.Run();
```

## Comparison with Blazor

Abies render modes map directly to Blazor's render modes, but the underlying architecture differs:

| Aspect | Abies | Blazor |
| --- | --- | --- |
| Architecture | Pure functional MVU | Component-based OOP |
| State management | Single model, unidirectional flow | Per-component state |
| Render decision | Always render, diff, patch | `ShouldRender()` per component |
| Patch format | Binary batch protocol | Binary RenderBatch |
| DOM updates | innerHTML + keyed diffing | Direct DOM commands |
| Server session | `Session` (MVU runtime) | Circuit (Blazor Hub) |
| WASM payload | ~1.1 MB compressed | ~1.4 MB compressed |

See the [Performance section](../README.md#performance-abies-browser-vs-blazor-wasm) in the README for detailed benchmark comparisons.

## Next

- [**Choosing a Render Mode**](../guides/render-mode-selection.md) — Practical guidance for your project
- [**App Lifecycle Reference**](../reference/app-lifecycle-reference.md) — Session, command, message, and update flow
- [**MVU Architecture**](mvu-architecture.md) — The Model-View-Update pattern
- [**Tutorial 1: Counter App**](../tutorials/01-counter-app.md) — Build your first Abies app
- [**Deployment Guide**](../guides/deployment.md) — Deploying each render mode to production
