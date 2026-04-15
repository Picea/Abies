# InteractiveServer Transport & Session Lifecycle

`RenderMode.InteractiveServer` keeps the MVU loop on the server. The browser becomes a thin transport client: it sends events and applies binary patch batches.

## End-to-End Flow

```text
HTTP GET /page
  → Page.Render(InteractiveServer) returns HTML + <script src="/_abies/abies-server.js" data-ws-path="...">

Browser loads abies-server.js
  → opens WebSocket ws(s)://host/{wsPath}?url={currentPath}

Server accepts WebSocket
  → Endpoints.MapWebSocketEndpoint(...)
  → new WebSocketTransport(webSocket)
  → Session.Start(...)
  → Runtime.Start(threadSafe: true)

Runtime startup render
  → View(initialModel) → Diff(null, body) + HeadDiff
  → RenderBatchWriter.Write(patches)
  → SendPatches(binary)
  → browser applyBinaryBatch(...)

Steady state loop
  browser DOM event
    → JSON text frame { commandId, eventName, eventData, traceparent?, tracestate? }
    → Session.RunEventLoop()
    → HandlerRegistry.CreateMessage(...)
    → Runtime.Dispatch(message)
    → new patches
    → binary frame back to browser

Disconnect
  → ReceiveEvent returns null
  → Session event loop exits
  → transport closes WebSocket
  → session disposed
```

## Transport Contract

`Picea.Abies.Server.Transport` defines three delegates used by `Session`:

- `SendPatches(ReadOnlyMemory<byte>)` — server → client binary patch batches
- `ReceiveEvent(CancellationToken)` — client → server event stream (`DomEvent?`)
- `SendText(string)` — server → client text frames (currently navigation/debugger messages)

`Picea.Abies.Server.Kestrel.WebSocketTransport` adapts those delegates to WebSocket frames:

- **Binary frames** carry `RenderBatchWriter` output (same format as WASM interop)
- **Text frames** carry event JSON from `abies-server.js`
- **Text frames (server-initiated)** carry navigation payloads like:
  `{"type":"navigate","action":"push|replace|back|forward|external","url":"..."}`

## Session Lifecycle Phases

### 1. Session creation

`Session.Start(...)` wires:

- `ServerApply`: patches → `RenderBatchWriter.Write(...)` → `SendPatches(...)`
- optional navigation executor: `NavigationCommand` → `SendText(...)`
- runtime startup with `threadSafe: true`

Each connected WebSocket gets its **own** runtime and handler registry.

### 2. Runtime startup render

`Runtime.Start(...)` immediately renders and diffs from empty DOM, which produces the initial patch batch sent to the client after the WebSocket opens.

### 3. Event loop

`Session.RunEventLoop(...)` repeatedly:

1. Awaits `ReceiveEvent`
2. Handles reserved events:
   - `__url_changed__` → dispatches `UrlChanged`
   - `__debugger_command__` (Debug builds only)
3. Resolves normal events through `HandlerRegistry.CreateMessage(...)`
4. Calls `Runtime.Dispatch(...)`

### 4. Close/dispose

The loop ends when `ReceiveEvent` yields `null` (client close/drop) or cancellation triggers. The hosting adapter then closes the socket and disposes the session/runtime.

## Navigation During InteractiveServer

Server-side `NavigationCommand` values do not mutate browser history directly. Instead:

1. runtime emits navigation command
2. session serializes a text frame
3. `abies-server.js` applies `history.pushState/replaceState/back/forward`
4. client emits `__url_changed__` event back to server
5. session dispatches `UrlChanged` into the MVU loop

This keeps URL state and model transitions synchronized through the same message pipeline.

## Observability Hooks

Server-side spans:

- `Picea.Abies.Server.Session.Start`
- `Picea.Abies.Server.Session.EventLoop`
- `Picea.Abies.Server.Session.ReceiveEvent`
- `Picea.Abies.WebSocket.SendPatches`
- `Picea.Abies.WebSocket.ReceiveEvent`

`traceparent`/`tracestate` from browser events are propagated into the receive-event activity context when available.
