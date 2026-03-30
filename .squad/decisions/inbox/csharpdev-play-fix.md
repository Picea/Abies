# Decision: step-forward already triggers ApplySnapshot — no C# fix needed

**Date:** 2026-03-29  
**Author:** C# Dev  
**Requested by:** Maurice  

## Context

The JS play loop calls `step-forward` repeatedly during playback. The question was whether that message triggers `TryApplyDebuggerSnapshot` (and thus `Render()`) so the running app UI updates.

## Finding

**step-forward DOES call `TryApplyDebuggerSnapshot` — unconditionally.**

The full call chain (Browser):

```
JS: invokeRuntimeBridge('step-forward', -1)
  → C#: Interop.DispatchDebuggerMessage(messageType, entryId, dataJson)
      → DebuggerRuntimeBridge.Execute(message, Debugger)
          → DebuggerMachine.StepForward()
              - _cursorPosition++
              - _currentModelSnapshot = _timelineModelSnapshots[_cursorPosition].Snapshot
      → ApplyDebuggerSnapshot(Debugger.CurrentModelSnapshot)   // ← always called
          → runtime.TryApplyDebuggerSnapshot(snapshot)
              → ApplySnapshot(model)
                  - TrySetCoreState(model)
                  - Render(model)                               // ← DOM patches emitted
```

The Server path mirrors this: `Session.cs:321` calls `TryApplyDebuggerSnapshot` unconditionally after every `DebuggerRuntimeBridge.Execute` call.

**All 9 message types trigger `TryApplyDebuggerSnapshot` after bridge execution:**
| Message Type | Machine method called |
|---|---|
| `jump-to-entry` | `Jump(entryId)` |
| `step-forward` | `StepForward()` |
| `step-back` | `StepBackward()` |
| `play` | `Play()` |
| `pause` | `Pause()` |
| `clear-timeline` | `ClearTimeline()` |
| `get-timeline` | *(no-op on machine)* |
| `export-session` | `ExportSession(...)` |
| `import-session` | `ImportSession(session)` |

## Decision

No C# code change required. `step-forward` already triggers `ApplySnapshot` and `Render()`.

## If app still doesn't update during play

The root cause is **not** missing `ApplySnapshot`. Likely causes:

1. **Imported session**: `_timelineModelSnapshots[i].Snapshot` holds a string preview (not a full model object) — `TryApplyDebuggerSnapshot` will attempt JSON deserialization. If TModel has abstract polymorphic types (e.g. Conduit's `Page` DU), deserialization fails silently and `Render()` is never called.
2. **Live recording**: The full `TModel` object is stored — replay should always work.

## Status

Investigation complete. No fix applied. 214/214 tests pass.
