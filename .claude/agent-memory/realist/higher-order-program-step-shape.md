---
name: higher-order-program-step-shape
description: Reusable step sequence for adding an opt-in higher-order Program wrapper to Abies (the WithView/WithHistory shape) without touching Runtime.cs
metadata:
  type: project
---

When a feature can be expressed as *a `Program` that wraps another `Program`*, the plan has a
repeatable shape. Established on the `undo-redo` pass (2026-09-06, `WithHistory`), following
the existing `WithView` precedent in `Picea.Abies/Program.cs`.

Step order (each step small and separately testable):

1. internal data structure (immutable; records + `with` per ADR-008)
2. the wrapping model record
3. pure query functions over it — these are what invariants get expressed against
4. the framework messages the wrapper answers, plus any envelope type
5. a static-abstract policy interface + a `Default…` implementation (config without DI, AOT-safe)
6. the wrapper itself (`Decide` / `Transition` / forwarding members)
7. invariant properties
8. a composition test proving `WithView` still composes on the outside
9. telemetry on the non-hot paths only
10. performance-engineer: per-dispatch cost + trimmed-size delta for non-adopters

**Why:** `Program` is a static-forwarding interface, so a wrapper needs no framework change —
only a type argument at `Runtime.Start`. That is what keeps these passes off `Runtime.cs` and
away from the `runtime-seams-anchor-replay-gating` revisit trigger.

**How to apply:** four facts that keep recurring. (a) `WithView` composes **outside** such a
wrapper, which is how an application draws chrome over the wrapper's own state — no new
machinery needed. It also composes **inside** it; both orders type-check and differ
semantically, so never claim one direction is impossible. (b) `Runtime.Dispatch` calls `Decide`
once per incoming message but dispatches each decided event separately (`Runtime.cs:492-500`),
and interpreter feedback events re-enter through `Transition` only — so a wrapper that needs
"one incoming message = one unit" must envelope the decided events in `Decide`; there is no
provenance parameter and adding one would be a fifth seam. Envelope **all** of them (open/extend
cases), not just the first, or events 2…N of the user's own action are indistinguishable from
the world's. (c) **The wrapper is the sole author of `Subscriptions(state)`**, because
`Runtime.Render` asks `TProgram.Subscriptions` (`Runtime.cs:214`, initial set at `:424`) and
`SubscriptionManager.Update` is keyed and diff-based (`Manager.cs:51-74`). Reporting an
unchanged key set is therefore a free, seamless way to *hold* subscription reconciliation across
several transitions — and varying a key is a free way to re-arm a timer. (d) A framework-owned
subscription is a legitimate home for a clock the wrapper needs; putting it in `Transition` is
the impurity worth avoiding.

Related: [[namespace-plan]], [[picea-package-xml-answers-kernel-questions]].
