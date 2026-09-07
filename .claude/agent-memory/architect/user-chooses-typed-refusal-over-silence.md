---
name: user-chooses-typed-refusal-over-silence
description: Across every gate of a pass this user takes the diagnosable option over the silent one, even when it costs memory or contradicts a settled rule
metadata:
  type: feedback
---

When a design has a path where something is declined, destroyed or skipped
without the application being told, this user closes it — every time, at every
gate, even when closing it costs something and even when it overturns a decision
they made two gates earlier.

**Why:** established across the five gates of the `undo-redo` pass
([[undo-redo-withhistory-decision]]). They chose refusal over crossing at the
effect boundary *and* required the refusal to be typed, explained, and answerable
before the press. They then overturned their own gate-2 "discard the forward
branch" rule — the industry-unanimous answer — for "refuse across the superseded
branch", spending a doubled retention ceiling purely to buy a named reason. They
reversed a gate-3 deferral of the projection lens the moment the Critic showed
redo was being destroyed silently. Their stated standard: *"a refusal that does
not explain is a silent failure."*

**How to apply:** when scoping, write the "declined, and here is why, in advance"
requirement into the invariants rather than leaving it to the UI — in that pass it
became INV-5 (typed refusal, distinguishable from a no-op) and INV-6 (availability
and its reason computable from the value alone, before the attempt), and those two
ids drove more of the design than any other constraint. When presenting options at
a 🛑, price the diagnosable one honestly rather than pre-filtering it as too
expensive; they will usually take it and they want the cost named. And when a
mechanism change makes an old narrative false, say so out loud — they consistently
reward a phase agent that flags its own divergence over one that absorbs it.
