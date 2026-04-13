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

### 1) 🔴 Must Fix — Runtime-wide dispatch gate introduces head-of-line blocking and likely causes Conduit navigation regression
- Evidence:
  - Picea.Abies/Runtime.cs:444 acquires _dispatchGate.
  - Picea.Abies/Runtime.cs:460 awaits _core.Dispatch for each decided event while still holding the gate.
  - Picea.Abies/Runtime.cs:288 routes subscription/navigation messages through Dispatch (fire-and-forget), which must also acquire the same gate.
- Why this is a regression risk:
  - The gate currently serializes the entire command lifecycle, including async effect interpretation and HTTP waits.
  - If one command is in-flight (for example, feed/article fetch after login), UrlChanged or UI commands are queued behind it instead of being processed promptly.
  - This changes user-visible behavior from responsive routing to potentially stalled routing under in-flight effects.
- Conduit E2E impact:
  - The failing test reported by implementation notes (DeleteArticle_AsAuthor_ShouldNavigateToHome) times out waiting for .article-page before delete action.
  - That failure point aligns with delayed UrlChanged handling after in-app navigation.
  - Most probable cause is this gate scope, not the Conduit Decide pass-through itself.
- Recommendation:
  - Narrow synchronization scope so only decision/state-transition critical section is serialized, not long-running effect interpretation.
  - Preserve command ordering guarantees without blocking unrelated navigation/subscription commands behind network waits.

### 2) 🟠 Should Fix — Missing regression tests for new decider-first concurrency behavior
- Evidence:
  - Migration adds a new runtime concurrency primitive (_dispatchGate) but no tests assert responsiveness/fairness when async effects are in-flight.
  - Existing touched tests mostly add Decide/IsTerminal stubs and do not cover command interleaving behavior.
- Gap:
  - No test asserts that UrlChanged is processed promptly while another command is awaiting interpreter IO.
  - No test asserts that queued commands cannot starve behind a long-running command.
- Recommendation:
  - Add runtime-level test(s) that:
    - Start one dispatch with a delayed interpreter effect.
    - Trigger a second command (for example UrlChanged) while first is pending.
    - Assert second command is not blocked beyond acceptable bound, or assert intended ordering explicitly if blocking is by design.

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
- Verdict: Changes requested before approval.
- Primary blocker is the runtime dispatch gate scope in Picea.Abies/Runtime.cs causing likely behavioral regression in Conduit E2E navigation flow.