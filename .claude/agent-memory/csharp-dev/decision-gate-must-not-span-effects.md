---
name: decision-gate-must-not-span-effects
description: Runtime's decision gate may only cover the decide/transition critical section — never hold it across awaited effects
metadata:
  type: feedback
---

`Runtime._decisionGate` (a `SemaphoreSlim(1, 1)`) must be released before any
effect is awaited. Its scope is the decide/transition critical section only:
`IsTerminal` guard, `Decide`, state transition. Command interpretation, HTTP
effects and navigation happen after the gate is released.

**Why:** The first version of this gate (then `_dispatchGate`, 2026-04-04) was
held for the entire command lifecycle including async HTTP effect awaits. That
produced head-of-line blocking — navigation and subscription messages queued
behind an in-flight fetch — and surfaced as the Conduit E2E failure
`DeleteArticle_AsAuthor_ShouldNavigateToHome` timing out on `.article-page`.
The Reviewer raised it as a 🔴 release blocker and it was tracked as issue #245.

**How to apply:** Any change that widens the gate's `try` block reintroduces the
regression, so treat the `finally { _decisionGate.Release(); }` boundary as
load-bearing. If you need mutual exclusion across an effect, that is a different
problem and needs a different mechanism — say so rather than extending this
gate. Note the original fix shipped without concurrency-fairness tests (flagged
🟠 at the time), so there is no automated guard here; the review is the guard.

Verified fixed 2026-09-02: in `Picea.Abies/Runtime.cs` the gate is taken at the
top of the decide block and released in `finally` before the short-circuit and
effect handling that follow.
