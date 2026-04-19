# InteractiveAuto Handoff Lifecycle

`RenderMode.InteractiveAuto` runs in two phases:

1. **Server-interactive phase** (WebSocket + `Session`)
2. **WASM-interactive phase** (browser runtime)

The handoff is coordinated entirely by bootstrap scripts and a DOM marker: `data-abies-mode="wasm"` on `<body>`.

## Bootstrap Sequence

`Page.Render(InteractiveAuto)` injects both scripts in order:

1. `/_abies/abies-server.js` with:
   - `data-ws-path="..."`
   - `data-auto="true"`
2. inline module that imports `/_framework/dotnet.js` and runs `dotnet.run()`

That gives immediate server interactivity while WASM downloads.

## Phase A — Server-Interactive

After page load:

- `abies-server.js` opens WebSocket to `{wsPath}?url={currentPath}`
- server starts a `Session`
- browser events are sent to server (`commandId/eventData`)
- server sends binary patch batches back
- browser applies patches in real time

During this phase, server mode owns event processing and patch application.

## Phase B — WASM Activation Signal

When the browser runtime (`abies.js`) applies its first binary batch, it sets:

```html
<body data-abies-mode="wasm">
```

This is the canonical handoff signal.

## Phase C — Server Teardown

In auto mode, `abies-server.js` installs a `MutationObserver` for `data-abies-mode`.
When it sees `"wasm"`:

1. sets internal `wasmActive = true`
2. closes the WebSocket
3. disconnects the observer

Safety guards prevent overlap:

- incoming server patches are ignored when `wasmActive === true`
- event forwarding to server is disabled when `wasmActive === true`
- navigation listeners in `abies-server.js` become no-ops once WASM is active

This ensures only one runtime controls the DOM after handoff.

## Handoff Timeline

```text
Initial HTML ready
  ├─ abies-server.js starts (server phase active)
  ├─ WebSocket connects, Session runs, user can interact immediately
  └─ dotnet.js downloads/boots in parallel

WASM first render batch applied
  └─ abies.js sets body[data-abies-mode="wasm"]

abies-server.js observes marker
  ├─ wasmActive = true
  ├─ WebSocket close()
  └─ server Session exits/disposes

WASM runtime now owns events + rendering
```

## Routing Behavior During Handoff

Before handoff, navigation events flow to the server via the reserved `__url_changed__` command ID.
After handoff, `abies-server.js` stops forwarding navigation events and the WASM runtime handles navigation subscriptions directly.

## Important Behavior Notes

- InteractiveAuto does **not** keep both runtimes active after handoff; server side is intentionally torn down.
- The handoff trigger is runtime-driven (`data-abies-mode="wasm"`), not time-based.
- The same WebSocket path configuration is used as InteractiveServer (`WebSocketPath`, default `/_abies/ws`).
