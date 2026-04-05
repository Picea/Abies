# Reviewer Full Audit — Decider Runtime Migration

Date: 2026-04-04
Reviewer: Reviewer
Requested by: Maurice Cornelius Gerardus Petrus Peters

## Scope Reviewed
- .squad/principles-enforcement.md
- .squad/decisions.md
- Core migration files:
  - Picea.Abies/Program.cs
  - Picea.Abies/Runtime.cs
  - docs/api/program.md
  - docs/reference/runtime-internals.md
- Touched app/program surfaces:
  - Picea.Abies.Conduit.App/Conduit.cs
  - Picea.Abies.Counter/Counter.cs
  - Picea.Abies.Browser/Debugger/DebuggerUI.cs
  - Picea.Abies.Presentation/Program.cs
  - Picea.Abies.SubscriptionsDemo/Program.cs
  - Picea.Abies.UI.Demo/Program.cs
  - Picea.Abies.Benchmark.Wasm/Program.cs
  - Template Program.cs files
- Related tests changed in Picea.Abies.Tests and server tests.

## Findings (Severity Ordered)

### 1) ✅ Resolved — Decision-phase serialization now matches runtime behavior
- Evidence:
  - `Runtime.Dispatch` serializes only the decision phase (`IsTerminal` + `Decide`) via a narrow gate.
  - Event dispatch and command interpretation execute outside the decision gate.
  - This prevents stale-state decisioning under concurrent dispatch while avoiding broad head-of-line blocking across long-running effects.
- Outcome:
  - The earlier concern about a runtime-wide gate no longer applies to the current implementation.
  - Audit note updated to reflect the code that is actually being merged.

### 2) 🟠 Should Fix — Missing explicit regression tests for decider-first concurrency guarantees
- Evidence:
  - Migration adds a new runtime concurrency primitive (_dispatchGate) but no tests assert responsiveness/fairness when async effects are in-flight.
  - Existing touched tests mostly add Decide/IsTerminal stubs and do not cover command interleaving behavior.
- Gap:
  - No test asserts a strict non-interleaving guarantee across multi-event decisions under concurrent dispatch.
  - Existing responsiveness coverage validates that fast UI dispatch remains responsive while slow interpretation is in-flight.
-- Recommendation:
  - Add runtime-level test(s) that specify intended ordering/interleaving semantics across concurrent commands whose `Decide` results contain multiple events.

### 3) 🟡 Should Fix — Copied browser runtime assets were edited instead of canonical source file
- Evidence:
  - Changed files include:
    - Picea.Abies.Presentation/wwwroot/abies.js
    - Picea.Abies.SubscriptionsDemo/wwwroot/abies.js
    - Picea.Abies.UI.Demo/wwwroot/abies.js
    - corresponding abies-otel.js copies
  - Canonical source file Picea.Abies.Browser/wwwroot/abies.js was not changed in this working set.
- Risk:
  - Build sync can overwrite these edits, creating non-reproducible behavior and review ambiguity.
- Recommendation:
  - Apply JS runtime changes in canonical source only, then sync via build target.

## Decider Contract Review Summary
- Program contract migration to strict decider shape is correctly applied at interface and implementer level.
- Runtime now enforces Decide/IsTerminal before transition path as required.
- Docs were updated to decider-first semantics.

## Verdict
- Verdict: Comment-only follow-up.
- No blocking issue remains in this audit note after synchronization with the current runtime implementation.