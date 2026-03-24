# Abies DevTools: Time Travel Debugger

Debug builds of Abies include a built-in time-travel debugger. It captures every message and model snapshot as they flow through `Transition`, and lets you step backward and forward through your app's history — or replay sequences automatically.

This guide shows you how to enable it, navigate the timeline, and use the intent transport contract for advanced integrations.

> **Debug builds only.** The debugger is compiled out of Release builds entirely. There is no runtime overhead in production. See [Release build behaviour](#release-build-behaviour).

---

## Prerequisites

- An Abies app with a `Wasm.Host` or `.Server` project that you can modify
- Running in Debug configuration (`dotnet run` defaults to Debug)
- Familiarity with the [MVU Architecture](../concepts/mvu-architecture.md)

---

## Step 1: Add the mount point to your host HTML

The debugger UI mounts into a DOM element with `id="abies-debugger-timeline"`. Add it to your host page — typically `wwwroot/index.html` (WASM) or `_Host.cshtml` / `App.razor` equivalent (Server).

```html
<!-- Anywhere in <body> — typically at the bottom -->
<div id="abies-debugger-timeline"></div>
```

The debugger renders its own full Abies MVU application inside that element. If the element is absent, the debugger silently does nothing.

---

## Step 2: Start your app in Debug

Run your app normally:

```bash
dotnet run --project YourApp.Wasm.Host
# or
dotnet run --project YourApp.Server
```

Debug is the default configuration. The debugger activates automatically — no additional flags or packages required.

Open your app in the browser. The timeline panel appears at the bottom of the page as soon as the first message is dispatched.

---

## The Timeline UI

### What each entry shows

Every message that passes through `Transition` creates one entry in the timeline:

| Field | Description |
| --- | --- |
| **Message type** | The C# type name of the dispatched message (e.g., `CounterMessage.Increment`) |
| **Args preview** | A short string preview of the message payload |
| **Timestamp** | Wall-clock time when the message was processed |
| **Model snapshot preview** | A short string preview of the model state *after* the transition |

Entries are stored in a ring buffer. The buffer holds the last N messages — older entries are discarded when the buffer is full.

### Current cursor position

The active entry (the one the app is currently replaying from) is highlighted. When you are at the live end of the timeline, the app behaves normally. When you step back, execution pauses at the selected snapshot.

---

## Playback Controls

### Buttons

| Button | Action |
| --- | --- |
| ▶ Play | Replay forward from the current cursor position |
| ⏸ Pause | Stop automatic replay and freeze at the current entry |
| → Step Forward | Advance the cursor one entry forward |
| ← Step Back | Move the cursor one entry backward |
| ✕ Clear | Remove all entries from the timeline and reset the cursor |

### Keyboard shortcuts

The debugger panel captures keyboard input when focused:

| Key | Action |
| --- | --- |
| `Space` | Toggle play / pause |
| `→` ArrowRight | Step forward one entry |
| `←` ArrowLeft | Step back one entry |
| `J` | Focus the jump input |
| `Escape` | Blur the debugger panel |

Click inside the timeline panel to focus it before using keyboard shortcuts.

---

## Jumping to a specific point

To jump directly to an entry:

1. Press `J` (or click the jump input field).
2. Type the entry ID shown in the timeline.
3. Press `Enter`.

The cursor moves to that entry immediately. The app replays state up to that point.

You can also click any timeline entry directly — the cursor jumps to it.

---

## Release build behaviour

`debugger.js` is excluded from Release builds via a `Condition` on its `<Content>` item in the `.csproj`:

```xml
<Content Update="wwwroot/debugger.js"
         Condition="'$(Configuration)' == 'Release'"
         CopyToPublishDirectory="Never" />
```

The C# debugger classes are guarded by `#if DEBUG`. Neither the UI code nor the bridge code is compiled into Release binaries.

This means:
- **Zero production bundle size increase** — the JS file is not deployed
- **Zero runtime overhead** — no event listeners, no ring buffer, no DOM element
- **No opt-out required** — stripping is automatic

Do not add the `<div id="abies-debugger-timeline">` mount point to production HTML templates. It has no effect in Release, but it is unnecessary noise in the DOM.

---

## Advanced: The intent transport contract

The debugger uses a custom event protocol to decouple the UI from the runtime bridge. If you need to drive the debugger programmatically (integration tests, custom tooling), you can dispatch intents directly.

### Attribute contract

Debugger-aware elements carry two attributes:

| Attribute | Value |
| --- | --- |
| `data-abies-debugger-intent` | The intent name (e.g., `"Jump"`, `"StepForward"`, `"StepBackward"`, `"Play"`, `"Pause"`, `"ClearTimeline"`) |
| `data-abies-debugger-payload` | Optional. A string payload for intents that require one (e.g., an entry ID for `Jump`) |

### Event contract

`debugger.js` listens for click events on elements with `data-abies-debugger-intent` and translates them to the `abies:debugger:intent` custom event on `window`.

You can also dispatch the event directly from JavaScript:

```javascript
window.dispatchEvent(new CustomEvent('abies:debugger:intent', {
  detail: {
    intent: 'Jump',
    payload: '42'   // entry ID
  }
}));

window.dispatchEvent(new CustomEvent('abies:debugger:intent', {
  detail: { intent: 'StepForward' }
}));

window.dispatchEvent(new CustomEvent('abies:debugger:intent', {
  detail: { intent: 'Play' }
}));
```

**Available intents:**

| Intent | Payload | Description |
| --- | --- | --- |
| `Jump` | Entry ID (string) | Move cursor to the specified timeline entry |
| `StepForward` | — | Advance cursor one step |
| `StepBackward` | — | Move cursor one step back |
| `Play` | — | Start automatic replay |
| `Pause` | — | Stop automatic replay |
| `ClearTimeline` | — | Remove all entries, reset cursor |

The `DebuggerRuntimeBridge` handles these events and translates them into commands for the `DebuggerMachine`.

---

## Troubleshooting

**Timeline panel does not appear.**

- Confirm the app is running in Debug, not Release.
- Confirm `<div id="abies-debugger-timeline"></div>` is present in the host HTML.
- Check the browser console for errors during startup.

**Keyboard shortcuts do not respond.**

- Click inside the timeline panel to give it focus first.

**Entries stop appearing.**

- The ring buffer has a capacity limit. The oldest entries are dropped when it fills. Use the Clear button to reset and resume capturing.

---

## See Also

- [Debugging](./debugging.md) — Tracing, console logging, and hot reload
- [Tutorial 8: Distributed Tracing](../tutorials/08-tracing.md) — OpenTelemetry instrumentation
- [MVU Architecture](../concepts/mvu-architecture.md) — How Model-View-Update works
- [ADR-025](../adr/ADR-025-issue-160-debugger-boundary-contract-phase1.md) — Architecture decision for the debugger boundary contract
