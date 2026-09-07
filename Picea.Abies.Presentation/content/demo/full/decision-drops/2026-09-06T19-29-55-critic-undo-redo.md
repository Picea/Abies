---
id: critic-20260906T184500Z-undo-redo
agent: critic
verdict: NEEDS-CHANGES
scope: architecture
created: 2026-09-06T18:45:00Z
targets:
  - path: .squad/design/undo-redo/04-realist-plan.md
  - path: .squad/design/undo-redo/00-scope.md
    lines: "182-190"
  - path: Picea.Abies/Runtime.cs
    lines: "212-220, 288-291, 465-500"
blockers:
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 284
    reason: "Bare-event drift: a model change arriving outside the Decide envelope (interpreter feedback, decided-error, later events of a multi-event Decide, or a policy-excluded message) moves Present without pushing a Step, so the next undo silently discards it. Conduit: a profile fetch landing mid-typing is erased by one undo press. INV-1, INV-2 and INV-3 all pass while the feature loses user data."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 291
    reason: "Redo is unreachable in any application with a live subscription: record and continue both clear Future, so a 250 ms tick (SubscriptionsDemo/Program.cs:110) caps redo's lifetime at 250 ms, and DefaultHistoryPolicy (IsUndoable => true) fills the 100-entry history with ticks in 25 s. The plan states this outcome at line 578 as an argument for the projection lens, then proceeds with the lens deferred and no substitute. Open question 4 is therefore unanswered against the scope's Done-means."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 396
    reason: "INV-7 claimed vacuous/by-construction but falsified by any undo sequence of length >= 2 over a subscription-toggling model: reconciling at a passed-through state starts a subscription (Subscriptions/Manager.cs:51-66) which a Subscription.Create can deliver from deterministically. The saving reading -- each undo press is its own destination -- is unstated and load-bearing, and it forecloses any future multi-step undo. INV-7 also has no test vehicle: it is a Runtime.Render property, not a WithHistory property, and step 7 places all seven properties in workflow-direct domain tests."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 279
    reason: "The late-NothingToUndo path delegates UndoRefused to TProgram.Transition, so undo with nothing left to undo delivers a message that may change state or emit a command -- INV-4's stated falsifier. Reachable by two fast clicks: dispatch is fire-and-forget (Runtime.cs:288-291) and _decisionGate is released at :474 before the transition is awaited at :494, so no threading is required. Fix is one branch: NothingToUndo returns (h, Command.None)."
high:
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 444
    reason: "HistoryStack 'amortised O(1)' is unachievable for a persistently-used bounded stack (Okasaki: amortisation and persistence are incompatible without laziness/scheduling), and the history IS used persistently. Restate as O(Depth) worst case and pick the array copy-on-write representation."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 88
    reason: "INV-2 'by construction' is by instruction: History<TModel> and Step<TModel> are constructible with arbitrary Past. Principles gate -- Make Illegal States Unrepresentable is cited as constraining. Needs internal constructors plus a test seam, or explicit user approval as a deviation."
  - file: .github/workflows/pr-validation.yml
    line: 211
    reason: "1500-line hard PR-size limit fails the build; docs are exempt, source and tests are not. The plan has no PR decomposition and § Parallelisable implies one branch."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 472
    reason: "No demo adopts WithHistory, so js-framework-benchmark measures a non-adopting application and its 5% gate is structurally silent on this pass. Step 10 must be an explicit BenchmarkDotNet A/B with a derived budget, and must run after step 6 and before steps 7-9."
  - file: Picea.Abies.Conduit.ServiceDefaults/Extensions.cs
    line: 32
    reason: "AddSource matches exact names, so Picea.Abies.History (and, already, Picea.Abies.Runtime and Picea.Abies.Subscriptions) is collected nowhere. Step 9's acceptance criterion is unverifiable, and fixing it touches files the plan declares unchanged."
medium: []
good:
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 297
    reason: "IsSilent must be a type test, not reference equality -- Commands.None allocates a fresh Command.None() per call (Command.cs:12). Caught at plan time; would otherwise have marked every edge committed and made the feature refuse everything."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 148
    reason: "INV-4 and INV-5 routed through two runtime paths that already exist and already behave differently (Runtime.cs:465-468 vs :482-489), verified rather than assumed."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 213
    reason: "Overrules 01-track-a.md and 03-convergence.md on RingBuffer<T> reuse after reading Debugger/RingBuffer.cs and finding a mutable class. Corrects inherited material in public rather than propagating it."
references: []
---

Critic verdict on the `undo-redo` design pass: LOOP BACK TO REALIST. Four blockers, all in the mechanism rather than the direction; H1/`WithHistory` remains the right shape.

## Why realist and not Dreamer

Nothing found reopens the H1 ranking. The two-track convergence holds, the kernel does force the zipper shape, and every alternative still pays in runtime seams or reflection. Three of the four blockers share one root — the wrapper has no story for model changes that did not arrive through the `Decide` envelope — and the instrument that was going to supply that story, A3's projection lens, was deferred at gate 3 without its concrete consequences having been put in front of the user. The fourth is a one-branch defect inside an otherwise correct rule.

## What the loop-back owes

1. A chosen resolution for bare-event drift: un-defer the lens, record bare changes as a refusing barrier reusing the INV-5/INV-6 machinery, or state plainly that this pass ships undo without usable redo for applications with subscriptions or asynchronous effects. Rebasing (re-applying the delta onto the restored model) is not admissible — it computes a model rather than moving to one and destroys INV-2-by-construction, which is H1's principal claim.
2. An explicit reading of INV-7's "state the user asked for" across a run of undo presses, plus a `Runtime`-level test vehicle with an instrumented subscription. The reading may be an `architect` amendment to `00-scope.md` rather than a `realist` fix.
3. `NothingToUndo` returning a true no-op from `Transition`, and the same recompute treatment extended to `Redo` and `Clear`.
4. The principles deviation on `History`/`Step` constructibility either fixed or approved.

## Spawns

`security-expert` — a change from the knowledge scan's "not summoned". `Step.Cause` retains raw messages for up to `Depth` entries; the DEBUG snapshot path now needs `JsonTypeInfo<History<TModel>>`, dragging the 2026-03-29 `JsonPolymorphic` obligation onto the application's whole message hierarchy, which in Conduit contains three credential-bearing message types; and `DebuggerMachine.ExportSession`/`ImportSession` is a reachable export surface. The scan's own re-summon trigger is met.

`performance-engineer` and `ux-expert` as the plan already asks, with re-sequencing and three added questions respectively.
