# Abies Time Travel Debugger

> **Scope:** Debugger panel defaults are available in Debug builds for browser runtime and server template local startup. In Release builds, debugger panel assets are absent.

The Abies Time Travel Debugger provides a complete trace of your application's MVU loop: every message, state transition, and rendered DOM patch.

## Quick Start (Auto-Enabled in Debug)

In Debug builds, the debugger panel is enabled by default:

- Browser runtime apps started via `Picea.Abies.Browser.Runtime.Run(...)` auto-mount the panel.
- Server template local debug startup includes the debugger panel by default.

No host-page markup is required.

When you run your app in Debug mode:
1. Open your application in the browser
2. Look for the **Abies Time Travel Debugger** panel (appears as a timeline at the bottom or side of the screen)
3. Interact with your app — see each action, transition, and render recorded in the timeline

In Release builds, debugger panel assets are absent.

The supported configuration surface is the C# API:

```csharp
DebuggerConfiguration.ConfigureDebugger(new DebuggerOptions { Enabled = false });
```

Use `ConfigureDebugger(...)` before `Runtime.Run(...)`; do not treat `DebuggerConfiguration.Default` as a mutable configuration object.

## Disabling the Debugger

If you want to disable the debugger in Debug builds (e.g., in CI environments or shared installations), configure it before starting your application:

**Example (in Program.cs):**

```csharp
using Picea.Abies.Debugger;

// Before calling Runtime.Run():
DebuggerConfiguration.ConfigureDebugger(new DebuggerOptions { Enabled = false });
await Picea.Abies.Browser.Runtime.Run<MyProgram, MyModel, Unit>();
```

The debugger will not appear, but your app runs normally.

## Debugger UI — Timeline Tab

The debugger timeline shows a chronological list of every event in your MVU loop.

### Event Details

Each event in the timeline captures:

| Field | What It Means |
|-------|---------------|
| **Message** | The name and details of the message (e.g., `Increment`, `UserClicked { id = 42 }`) |
| **Old Model** | Your application state **before** the message was processed |
| **New Model** | Your application state **after** the transition |
| **Patches** | The DOM changes that were rendered (e.g., "Add node", "Set class", "Update text") |
| **Subscriptions** | Any subscriptions that were live during this transition |

### Interaction

- **Hover** over an event to see a compact summary
- **Click** an event to expand it and see full details
- **Search** timeline by message name (e.g., search "Increment" to find all increment messages)
- **Clear** the timeline to reset and start fresh

## Time Travel (Advanced)

You can rewind your application to any past state by clicking on an event in the timeline.

**How it works:**
1. Click a past event in the timeline
2. The debugger reloads your entire application state to that point
3. The UI re-renders to match that state
4. Continue forward from that point, or inspect further

This is useful for:
- Reproducing bugs (step back to the exact moment before the bug occurred)
- Understanding state evolution (see how your model changed over time)
- Testing edge cases (rewind to a specific scenario and make different choices)

## Release Builds

**Release builds have zero debugger overhead:**
- No JavaScript or WebAssembly bytes spent on the debugger
- All debugger code is compiled out at build time (via `#if DEBUG` preprocessor directives)
- Your Release bundle is smaller and faster

## Troubleshooting

### Q: I don't see the debugger panel

**A:** One of these reasons:
- You're running in Release mode (debugger is stripped out)
- You're using `DebuggerConfiguration.ConfigureDebugger(new DebuggerOptions { Enabled = false })` (explicitly disabled)
- Your app did not start through the current browser runtime auto-mount path
- Your app hasn't rendered yet (wait a moment for the page to load)

**Solution:** Check your build configuration and confirm `#DEBUG` is enabled, or remove the explicit disable if you added it.

### Q: The timeline grows too large / memory usage is high

**A:** The debugger keeps all events in memory until you close the browser tab or clear the timeline.

**Solution:**
- Click **Clear** in the debugger UI to reset the timeline
- Reload the page (`Ctrl+R` or `Cmd+R`) to start fresh
- For long development sessions, periodically clear the timeline

### Q: What about Server Render mode?

**A:** In local Debug startup, the server template includes the debugger panel by default. You can hide the panel UI with `?abies-debugger=off`, `<meta name="abies-debugger" content="off">`, or `window.__abiesDebugger = { enabled: false }`. To disable server-side debugger runtime in Debug startup, set `ABIES_DEBUG_UI=0`. In Release builds, debugger panel assets are absent; use server logs, browser DevTools, and tracing.

## See Also

- [Debugging Guide](debugging.md) — Additional debugging strategies for MVU applications
- [ADR-026: Debugger Auto-Mount with C# API](../adr/ADR-026-debugger-auto-mount-with-csharp-api.md) — Design decisions and rationale
- [ADR-025: Debugger Boundary Contract](../adr/ADR-025-issue-160-debugger-boundary-contract-phase1.md) — Debugger interface and semantics
