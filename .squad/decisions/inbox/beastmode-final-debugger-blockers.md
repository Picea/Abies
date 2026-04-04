# Beast Mode Final Note: Debugger Blockers After d1540b2

Date: 2026-03-27
Requested by: Maurice Cornelius Gerardus Petrus Peters

## Scope

Address exactly two blockers with minimal independent changes:

1. Reviewer blocker: debugger runtime bridge handoff could be unavailable due to missing debugger instance handoff.
2. Automated failure: `ReleaseAssemblyNoDebuggerReferencesInJavaScript`.

No additional feature scope was added.

## Decisions

1. Explicitly hand off the runtime-owned debugger instance to browser interop at bootstrap.
2. Remove debugger bootstrap/remount/fallback logic from `abies.js` and keep debugger UI logic in `debugger.js` only.

## Implemented Changes

### 1) Bridge handoff fix

- File: `Picea.Abies.Browser/Runtime.cs`
- Change: after `runtime.UseDebugger();`, assign `Interop.Debugger = runtime.Debugger;`
- Why: ensures `Interop.DispatchDebuggerMessage` has a concrete debugger machine instance immediately, preventing `unavailable|-1|0` responses caused by missing handoff.

### 2) Release JavaScript strip fix

- File: `Picea.Abies.Browser/wwwroot/abies.js`
- Change: removed debugger-specific startup/config/remount/fallback code and related root-patch remount hooks from core runtime script.
- Why: release-strip contract requires the core `abies.js` to contain no debugger references; debugger behavior remains in debug-only `debugger.js`.

## Validation Summary

Required commands executed:

1. `dotnet test --project Picea.Abies.Tests/Picea.Abies.Tests.csproj -c Debug -v minimal`
   - Passed, total 192, failed 0.
2. `dotnet test --project Picea.Abies.Browser.Tests/Picea.Abies.Browser.Tests.csproj -c Debug -v minimal`
   - Passed, total 29, failed 0.
3. `dotnet test --project Picea.Abies.Templates.Testing.E2E/Picea.Abies.Templates.Testing.E2E.csproj -c Debug -v minimal`
   - Passed, total 20, failed 0.

Additional required sanity:

- Quick Playwright sanity was executed against:
  - `Counter.Server` at `http://localhost:5411`
  - `Conduit.Server` at `http://localhost:5412`
- Behavior verified:
  - debugger shell appears
  - panel toggles open/close
  - Play/Pause command clicks are accepted and status/summary updates are returned

Observed output:

- `Counter.Server: shell+panel OK, status=paused, summary=Runtime playing | cursor -1 | timeline 0`
- `Counter.Server: panel close OK`
- `Conduit.Server: shell+panel OK, status=paused, summary=Runtime playing | cursor -1 | timeline 0`
- `Conduit.Server: panel close OK`

## Residual Risks

1. Because debugger remount support was removed from `abies.js`, if debug overlays are externally removed during full-root replacement workflows, recovery relies on `debugger.js` bootstrap/runtime lifecycle instead of core runtime fallback hooks.
2. Play/Pause sanity confirms bridge roundtrip and command acceptance; deeper timeline semantics (e.g., non-empty history replay paths) remain covered by existing automated debugger tests.
