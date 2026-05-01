# InteractiveServer and InteractiveAuto Lifecycle

This guide explains the runtime lifecycle for `InteractiveServer` and `InteractiveAuto`, with focus on transport/session behavior and the WASM handoff boundary.

## Scope

Source of truth for this behavior:

- `Picea.Abies.Server/Page.cs`
- `Picea.Abies.Server.Kestrel/Endpoints.cs`
- `Picea.Abies.Server/Session.cs`
- `Picea.Abies.Server.Kestrel/wwwroot/_abies/abies-server.js`
- `Picea.Abies.Browser/wwwroot/abies.js`

## InteractiveServer Lifecycle

### 1. Initial HTTP request renders full HTML

`Page.Render` renders the document and injects:

```html
<script src="/_abies/abies-server.js" data-ws-path="/_abies/ws"></script>
```

At this point the page is visible before any WebSocket traffic.

### 2. Browser opens a WebSocket session

`abies-server.js` computes the endpoint from the script attribute and opens:

- `ws://<host>/<wsPath>?url=<currentPath>` (or `wss://` on HTTPS)

The query-string `url` is used by the server to route initial state correctly.

### 3. Kestrel starts a per-connection Session

`MapAbies` maps a WebSocket endpoint for `InteractiveServer`.

For each connection:

1. Upgrade to WebSocket.
2. Wrap socket in `WebSocketTransport`.
3. Parse initial URL from query (`url`) with referer fallback.
4. Start `Session.Start(...)`.

Each connection gets its own `Session<TProgram, TModel, TArgument>` and its own handler registry.

### 4. Session bootstraps and pushes first patch batch

`Session.Start` starts a thread-safe runtime (`threadSafe: true`) and wires:

- patches -> binary batch writer -> transport send
- navigation commands -> text messages to the client

The first server-side render diff is sent as a binary patch batch.

### 5. Event loop drives interaction

`Session.RunEventLoop` continuously:

1. receives `DomEvent` from transport
2. resolves message via handler registry
3. dispatches message to runtime
4. sends resulting patch batches back to browser

Special event IDs:

- `__url_changed__` for browser navigation events (`pushState`, `popstate`)
- `__debugger_command__` in debug flows

### 6. Session shutdown

When the client disconnects (or cancellation is requested):

- event loop exits
- transport closes
- session and runtime are disposed

## InteractiveAuto Lifecycle

`InteractiveAuto` starts as `InteractiveServer`, then hands off to WASM.

### 1. Initial page includes both bootstraps

`Page.Render` injects two scripts:

1. `/_abies/abies-server.js` with `data-auto="true"`
2. inline module booting `/_framework/dotnet.js`

That means server interactivity begins immediately while WASM downloads in parallel.

### 2. Server phase is identical to InteractiveServer

Until handoff, all interactions are server-driven via WebSocket session and binary patch batches.

### 3. WASM takeover signal

When WASM applies its first binary patch batch, `abies.js` sets:

```html
<body data-abies-mode="wasm">
```

This is the handoff signal.

### 4. Server script observes handoff and tears down WebSocket

In auto mode, `abies-server.js` watches `data-abies-mode` via `MutationObserver`.

When mode becomes `wasm`:

- sets internal `wasmActive = true`
- closes the WebSocket
- stops sending events to server
- ignores further server patches/navigation forwarding

Server-side session then ends naturally when transport closes.

### 5. Post-handoff ownership

After handoff, browser WASM runtime is authoritative for DOM updates and event handling.

## Transport and Protocol Notes

### Server -> client

Binary patch batches encoded by `RenderBatchWriter`.

### Client -> server

JSON text event messages containing:

- `commandId`
- `eventName`
- `eventData`
- optional trace context (`traceparent`, `tracestate`)

### Navigation messages

Server can send text frames with `{ "type": "navigate", ... }`, applied by `abies-server.js` through browser history APIs.

## Handoff Boundary and State Semantics

`InteractiveAuto` handoff is a runtime ownership transition, not a guaranteed in-memory model transfer from server session to WASM session.

Practical implication:

- If application state must survive the transition, persist it explicitly through app-level mechanisms (for example URL state, backend state, or browser storage).

## Testing Hooks

The `data-abies-mode="wasm"` marker is intentionally observable and used by E2E helpers such as `WaitForWasmReady()` in Conduit tests.

## Related Docs

- [Render modes concept](../concepts/render-modes.md)
- [Choosing a render mode](./render-mode-selection.md)
- [Runtime API](../api/runtime.md)
