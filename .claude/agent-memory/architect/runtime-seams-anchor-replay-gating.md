---
name: runtime-seams-anchor-replay-gating
description: Debugger/replay gating must be anchored to the four Runtime.cs seams so it is enforceable in the core runtime, not in an adapter
metadata:
  type: project
---

Debugger and time-travel replay gating is anchored to four seams in
`Picea.Abies/Runtime.cs`: the `_apply` patch callback, `InterpretCommand`,
`SubscriptionManager.Start`/`SubscriptionManager.Update`, and the
`navigationExecutor` delegate. Decided 2026-03-23 while designing the debugger
for issue #160.

**Why:** Replay gating has to be enforceable in the core runtime. If it lives
anywhere above these seams, an effect (a DOM patch, a command interpretation, a
subscription start, a navigation) can escape the gate and mutate the world
during replay. Anchoring at the seams makes the gate structural rather than a
convention adapters are trusted to honour.

**How to apply:** When designing anything that must observe or suppress runtime
effects — replay, record, hot reload, deterministic tests — express it in terms
of these four seams rather than inventing a new interception point. If a design
needs a fifth seam, that is an architecture change worth an explicit decision,
not an incidental addition.

Verified present in `Picea.Abies/Runtime.cs` on 2026-09-02. Related:
[[debugger-domain-stays-in-core]].
