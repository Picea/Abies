# JS Dev Decision Inbox — Timeline Debugger Runtime Fixes (2026-03-27)

## Scope
Fix timeline debugger runtime behavior regressions in browser/runtime paths while preserving Release strip behavior.

## Decisions

### 1) Wire WASM runtime bridge during debug bootstrap
- Changed browser runtime startup to call `Interop.SetRuntimeBridge(Interop.DispatchDebuggerMessage)` after `runtime.UseDebugger()` and before mount.
- Why: debugger UI could mount but report "Runtime bridge unavailable" in WASM because JS adapter never received a callback.

### 2) Treat runtime bridge callback as async-safe in debugger adapter
- Updated browser and server `debugger.js` adapters so `invokeRuntimeBridge` awaits `Promise.resolve(runtimeBridge(...))`.
- Why: server adapter callback returns a Promise; string-splitting that Promise produced `[object Promise]` in timeline summary/action logs.

### 3) Add resilient debugger module resolution in browser bootstrap
- Updated browser `abies.js` debugger loader to try sibling `./debugger.js` first, then fallback to `/debugger.js`, caching the successful URL.
- Why: host/static-web-asset path differences can make sibling resolution fail even in debug builds where debugger should be available.
- Release-strip safety: still best-effort with catch/no-op behavior; no change to Release exclusion of `debugger.js`.

## Validation
- Added focused tests in `Picea.Abies.Browser.Tests/DebuggerJavaScriptAdapterContractTests.cs` for:
  - async bridge invocation pattern in debugger adapter
  - runtime bootstrap wiring of `SetRuntimeBridge`
