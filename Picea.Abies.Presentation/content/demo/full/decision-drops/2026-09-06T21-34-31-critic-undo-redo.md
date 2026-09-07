---
id: critic-20260906T000000Z-undo-redo-pass-2
agent: critic
verdict: NEEDS-CHANGES
scope: architecture
created: 2026-09-06T00:00:00Z
targets:
  - path: .squad/design/undo-redo/04-realist-plan.md
  - path: .squad/design/undo-redo/00-scope.md
    lines: "183-194"
  - path: Picea.Abies/Runtime.cs
    lines: "200-220"
  - path: Picea.Abies.Conduit.App/Model.cs
    lines: "82-96"
blockers:
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 248
    reason: "B5 — the transit hold guarantees INV-7 only for undo presses closer together than SettleWindow (default 250 ms) and only while no held subscription delivers. Two presses 350 ms apart are two runs, so subscriptions reconcile against the intermediate state; a held anchor subscription that delivers ends the run mid-movement. Step 9's property runs with SettleWindow = null, an explicit terminal Settle and a non-autonomous source, so it cannot falsify any of it."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 512
    reason: "B6 — HistoryMessage.Settle carries no ordinal. DispatchFromSubscription is fire-and-forget (Runtime.cs:288-291), so a settle already dispatched by the window being cancelled ends the run that replaced it. Fix is one field plus a guard in Decide and Transition."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 472
    reason: "B7 — SEC-1/SEC-2 redact Step.Cause, but Conduit's passwords are model fields bound to controlled inputs (Model.cs:82-96, Pages/Settings.cs:49), so the retained secret arrives via Step.Model, which nothing scrubs. SEC-3 as worded in the plan is unsatisfiable for any model carrying the value, and SEC-6's threat row would claim a mitigation that does not cover the threat."
high:
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 538
    reason: "S9 — under the default policy redo still dies silently: barrier clears Future and Decide(Redo) returns the INV-4 no-op, so a lost redo is indistinguishable from nothing to redo."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 415
    reason: "S10 — the identity default blocks undo at the most recent world change permanently, not merely at the most recent tick; in Conduit that is every fetch and every navigation."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 569
    reason: "S11 — HistoryEvent.Continue is unconditionally transparent, so a projection change carried by the second event of a multi-event Decide is neither recorded nor barriered. Reachable shape, evidenced at Picea.Abies.Tests/RuntimeIsolationAndSubscriptionFaultTests.cs:218."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 542
    reason: "S12 — IsUndoable's entire remaining effect is to turn a record into a barrier; the name says the opposite. Remove it or rename it."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 148
    reason: "S13 — the erasure removed the guarantee that SameUndoable is an equivalence relation; L1-L4 do not restore symmetry."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 838
    reason: "S15 — a real 50 ms wall-clock settle test, five commits after ca2519d removed sleep-based subscription tests from this project. Use TPolicy.Time with a test-owned deterministic TimeProvider."
  - file: .squad/design/undo-redo/00-scope.md
    line: 128
    reason: "S16 — INV-1 is stated unconditionally and is falsified by the gate-2 refusal policy the pass settled; its property needs an availability precondition, which is an architect clause."
medium: []
good:
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 166
    reason: "The transit anchor is seam-free and every link verifies: Runtime.cs:214 asks the wrapper, Manager.cs:51-66 keys the diff, and Runtime.cs:212-220 reconciles outside the patch guard. Zero lines of Runtime.cs."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 716
    reason: "S2 fixed rather than deviated; InternalsVisibleTo Picea.Abies.Tests already exists at Picea.Abies.csproj:24, so the principles gate closed for free."
references: []
---

Second Critic pass on design `undo-redo`: LOOP BACK TO REALIST with three blockers — INV-7 is still falsifiable through the new transit machinery, and the folded-in security mitigation misses the field the secret actually travels in.

## Why this is a loop-back and not a mitigation list

Revision 2 closed B1, B2, B4 and all of S1–S8 with mechanisms I verified in the code, and the H1 direction is untouched. But B3 — the invariant that caused the first loop-back — is not closed. The transit hold is correct for the scenario it was designed against and is defeated by the shipped default: a settle window sized from editing-coalescence literature (250 ms) is shorter than a deliberate user's inter-press interval, a held anchor subscription delivering during a run collapses the run onto an intermediate state, and the settle signal carries no identity so a cancelled window's in-flight message ends the wrong run.

Compounding it, the planned property runs with `SettleWindow = null`, an explicit terminal `Settle` and a source that never dispatches on its own — a mode no application ships in and which excludes all three triggers by construction. That is the invariant-coverage gate: a property that excludes the clock from a clock-driven mechanism is not coverage of that mechanism.

B7 is independent. The security room's own worked example — Conduit's password fields — reaches the history through `Step.Model`, not `Step.Cause`, because a controlled input requires the value to be in the model. `SensitiveCause` cannot see it. The fix is small (`Scrub` on the policy, plus the law `Restore(Scrub(a), b) = Restore(a, b)`), but it must land before the threat-model row claims mitigation.

## Recurrence

Third pass in a row where a claim of the form "X is closed by construction" turned out to be closed by a mechanism with a precondition the artifact does not state. Revision 1: INV-2 by construction, actually by instruction. Revision 1: INV-7 vacuous, actually false for runs. Revision 2: INV-7 held by transit, actually held only within a window.
