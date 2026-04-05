# Runtime API

The Abies runtime orchestrates the MVU loop: it wires the Automaton kernel to view rendering, DOM diffing, and subscription management. There are two hosting models — browser (WASM) and server (ASP.NET Core) — each with its own entry point.

## Browser Runtime

### Picea.Abies.Browser.Runtime.Run

```csharp
[SupportedOSPlatform("browser")]
public static class Runtime
{
    public static async Task Run<TProgram, TModel, TArgument>(
        TArgument argument = default!,
    Interpreter<Command, Message>? interpreter = null,
    JsonTypeInfo<TModel>? debuggerModelJsonTypeInfo = null)
        where TProgram : Program<TModel, TArgument>;
}
```

One-line entry point for running an Abies application in the browser. This single method handles the entire WASM bootstrap sequence:

1. Loads `abies.js` via `JSHost.ImportAsync`
2. Wires DOM event dispatch callbacks (JS → .NET)
3. Wires URL-changed callbacks (navigation → .NET)
4. Sets up document-level event delegation
5. Sets up navigation interception (popstate + link clicks)
6. Creates a `RenderBatchWriter` and browser `Apply` delegate
7. Parses the current browser URL for initial routing
8. Starts the core `Runtime<TProgram, TModel, TArgument>`
9. Blocks indefinitely to keep the WASM process alive

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `argument` | `TArgument` | `default!` | Initialization parameters passed to `TProgram.Initialize`. Use `Unit` for parameterless apps. |
| `interpreter` | `Interpreter<Command, Message>?` | `null` | Converts commands into feedback messages. When `null`, a no-op interpreter is used. |
| `debuggerModelJsonTypeInfo` | `JsonTypeInfo<TModel>?` | `null` | Optional source-generated metadata used for debugger snapshot export/import in Debug builds. |

#### Usage

```csharp
// Entire Program.cs for a browser application:
await Picea.Abies.Browser.Runtime.Run<CounterProgram, CounterModel, Unit>();
```

```csharp
// With a custom interpreter for HTTP commands:
await Picea.Abies.Browser.Runtime.Run<ConduitProgram, ConduitModel, Unit>(
    interpreter: ConduitInterpreter.Interpret);
```

```csharp
// With initialization argument:
await Picea.Abies.Browser.Runtime.Run<MyApp, MyModel, AppConfig>(
    argument: new AppConfig(ApiBaseUrl: "https://api.example.com"),
    interpreter: MyInterpreter.Interpret);
```

> **Note:** `Runtime.Run` never returns — it calls `Task.Delay(Timeout.Infinite)` to keep the WASM process alive. The entire `Program.cs` for a browser app is typically a single line.

## Server Runtime

### MapAbies Extension Method

```csharp
public static IEndpointRouteBuilder MapAbies<TProgram, TModel, TArgument>(
    this IEndpointRouteBuilder endpoints,
    string path,
    RenderMode mode,
    Interpreter<Command, Message>? interpreter = null,
    TArgument argument = default!,
    JsonTypeInfo<TModel>? debuggerModelJsonTypeInfo = null)
    where TProgram : Program<TModel, TArgument>;
```

ASP.NET Core extension method that maps an Abies application to a URL path with a specified render mode.

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `path` | `string` | — | The URL path to map (e.g., `"/"`, `"/{**catch-all}"`) |
| `mode` | `RenderMode` | — | The rendering strategy (see below) |
| `interpreter` | `Interpreter<Command, Message>?` | `null` | Command interpreter. Required for interactive modes with custom commands. |
| `argument` | `TArgument` | `default!` | Initialization parameters for the program. |
| `debuggerModelJsonTypeInfo` | `JsonTypeInfo<TModel>?` | `null` | Optional source-generated metadata used for debugger snapshot export/import in Debug builds. |

#### Render Modes

| Mode | Initial HTML | MVU Loop | Use Case |
|------|-------------|----------|----------|
| `RenderMode.Static` | Server | None | Content pages, SEO-critical pages |
| `RenderMode.InteractiveServer(webSocketPath?)` | Server | Server (WebSocket) | Instant interactivity, no WASM download |
| `RenderMode.InteractiveWasm` | Server | Client (WASM) | No persistent connection after load |
| `RenderMode.InteractiveAuto(webSocketPath?)` | Server | Server → Client | Best UX: fast interactivity + connectionless |

The `webSocketPath` parameter defaults to `"/_abies/ws"` for both `InteractiveServer` and `InteractiveAuto`.

#### Usage

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Static rendering — no interactivity
app.MapAbies<MyApp, MyModel, Unit>("/", new RenderMode.Static());

// Interactive server — MVU loop on the server over WebSocket
app.MapAbies<MyApp, MyModel, Unit>("/", new RenderMode.InteractiveServer());

// Interactive WASM — server-rendered HTML, then client takes over
app.MapAbies<MyApp, MyModel, Unit>(
    "/{**catch-all}",
    new RenderMode.InteractiveWasm(),
    interpreter: MyInterpreter.Interpret);

// Interactive Auto — server-first, transitions to WASM when ready
app.MapAbies<MyApp, MyModel, Unit>(
    "/",
    new RenderMode.InteractiveAuto(),
    interpreter: MyInterpreter.Interpret);

app.Run();
```

> **Catch-all routes:** When the path contains `**`, `MapFallback` is used instead of `MapGet` so that static file middleware gets priority over the HTML page handler.

## Core Runtime

The `Runtime<TProgram, TModel, TArgument>` is the platform-agnostic MVU execution loop used internally by both browser and server runtimes.

### Runtime.Start

```csharp
public static async Task<Runtime<TProgram, TModel, TArgument>> Start(
    Apply apply,
    Interpreter<Command, Message> interpreter,
    TArgument argument = default!,
    Action<string>? titleChanged = null,
    Action<NavigationCommand>? navigationExecutor = null,
    Url? initialUrl = null,
    bool threadSafe = false);
```

Starts the MVU runtime with a platform-specific `Apply` delegate. This is the low-level API — most applications use `Browser.Runtime.Run` or `MapAbies` instead.

#### Startup Sequence

1. `TProgram.Initialize(argument)` → `(model, command)`
2. `TProgram.View(model)` → `document`
3. `Operations.Diff(null, document.Body)` → initial patches
4. `HeadDiff.Diff([], document.Head)` → head patches
5. `apply(allPatches)` → render to platform
6. `TProgram.Subscriptions(model)` → start initial subscriptions
7. Interpret initial command
8. Dispatch `UrlChanged(initialUrl)` if provided

### Runtime Properties

| Property | Type | Description |
|----------|------|-------------|
| `Model` | `TModel` | The current application state |
| `CurrentDocument` | `Document?` | The current virtual DOM document |
| `Handlers` | `HandlerRegistry` | Event handler registry for this runtime instance |

### Runtime.Dispatch

```csharp
public ValueTask<Result<Unit, PipelineError>> Dispatch(
    Message message, CancellationToken cancellationToken = default);
```

Dispatches a message into the MVU loop: transition → render → diff → apply → subscriptions.

### Apply Delegate

```csharp
public delegate void Apply(IReadOnlyList<Patch> patches);
```

The boundary between the pure Abies core and platform-specific rendering:

| Platform | Implementation |
|----------|----------------|
| Browser | JS interop to mutate the real DOM via binary batch protocol |
| Server | Binary patches sent over WebSocket transport |
| Tests | Captures patches for assertions |

## Interpreter Delegate

```csharp
public delegate ValueTask<Result<Message[], PipelineError>> Interpreter<TEffect, TEvent>(
    TEffect effect);
```

Converts commands into feedback messages. The runtime wraps the caller-supplied interpreter with built-in handling:

| Command | Handling |
|---------|----------|
| `Command.None` | No-op (monoid identity) |
| `Command.Batch` | Recursively interprets each sub-command |
| `NavigationCommand` | Executed by the runtime's navigation executor |
| Everything else | Falls through to your interpreter |

## Server Sessions

For interactive server modes, each connected client gets its own `Session` wrapping an MVU runtime:

```csharp
public static class Session
{
    public static Task<Session<TProgram, TModel, TArgument>> Start<TProgram, TModel, TArgument>(
        SendPatches sendPatches,
        ReceiveEvent receiveEvent,
        Interpreter<Command, Message> interpreter,
        SendText? sendText = null,
        TArgument argument = default!,
        Url? initialUrl = null)
        where TProgram : Program<TModel, TArgument>;
}
```

Sessions use `threadSafe: true` since server sessions are accessed from async I/O threads. Each session's `HandlerRegistry` is instance-based — no cross-session contention.

## OpenTelemetry

The runtime emits spans via `System.Diagnostics.ActivitySource`:

| Span | Source | Description |
|------|--------|-------------|
| `Picea.Abies.Start` | `Picea.Abies.Runtime` | Runtime initialization |
| `Picea.Abies.Render` | `Picea.Abies.Runtime` | View + diff + apply cycle |
| `Picea.Abies.Stop` | `Picea.Abies.Runtime` | Runtime disposal |
| `Picea.Abies.Server.Session.Start` | `Picea.Abies.Server.Session` | Server session initialization |
| `Picea.Abies.Server.Session.EventLoop` | `Picea.Abies.Server.Session` | Server event loop |

The browser-side `abies.js` also creates spans for DOM events and propagates `traceparent` headers on fetch requests.

## Hot Reload (Debug)

Abies supports Debug-only hot reload for view functions.

### Scope

- Applies to view rendering changes (`View` and called view helper functions).
- Applies to active runtime instances in browser (WASM) and server sessions.
- Preserves current model state and triggers a re-render.

### Not in Scope

- `Initialize`, `Transition`, commands/interpreters, and subscriptions are not hot-reloaded by this feature.
- If your edit changes behavior outside view rendering, restart the app.

### Supported Hosts

- Server host (`*.Server`) in interactive modes.
- Browser host (`*.Wasm.Host` or single-project `*.Wasm`).

### Build Configuration

- Enabled for Debug workflows.
- Handler registration is auto-injected for consuming app assemblies in Debug.
- Release builds are unaffected.

### When Restart Is Required

- You hit a .NET hot reload unsupported edit (rude edit).
- You change startup/runtime wiring, not view rendering.
- You change logic that is not part of the view pipeline.

## See Also

- [Program](program.md) — The application interface the runtime executes
- [Command](command.md) — Side effects and the interpreter pattern
- [Subscription](subscription.md) — External event sources managed by the runtime
- [DOM Types](dom-types.md) — Virtual DOM types produced by `View`
