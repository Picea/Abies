---
name: assertion-shapes-are-not-verified-by-me
description: First re-approval event — I locked a spec whose assertion shapes had never been compiled; TUnit's .Or does not cross subjects and IsEquivalentTo ignores order, and both defects were in my approved text
metadata:
  type: feedback
---

**I have no `Bash`, so every assertion I draft is unverified.** "The owning specialist will observe
it red at the end of step 6" does not cover this: a shape that never compiles, and a shape that
passes for the wrong reason forever, are both invisible to that check.

**Why:** `undo-redo` PR 0. `reviewer-reconcile` compiled my assertion *shapes* against the pinned
TUnit 1.19.57 in a scratch project — the one check that neither the missing feature types nor the
missing fixtures prevented — then extracted every `csharp` fence from `06-spec.md`, reassembled it
and diffed it against the transcribed `.cs`. Verdict: **both compile-verified defects were in my
approved text, verbatim.** `reviewer-blind` put it plainly: *"A file that has never been compiled is
being frozen."*

The two, and they are different failure modes:
- **`Assert.That(a).IsNotEqualTo(x).Or.That(b).IsTrue()`** → `CS1061: 'OrContinuation<int>' does not
  contain a definition for 'That'`. TUnit's `.Or` continues on the **same subject**. A disjunction
  across two different values is not expressible; compute one boolean and assert it once.
- **`IsEquivalentTo` is order-insensitive**, and I used it for two *ordering* claims whose expected
  sequences were permutations of each other. `IsEqualTo` on a collection **is** order-sensitive on
  this version; `CollectionOrdering` does not exist. Grep every `IsEquivalentTo` in a draft and ask
  *"is this a set or a sequence?"* — a reconciliation set and a `.Distinct()` set are correct uses.

**How to apply:** prefer assertion shapes already executed in this repo's test suite over shapes
that merely read well. When there is a choice, take the plainest subject — a `bool` computed on the
line above beats any fluent continuation. And say in the artifact that the shapes are unverified by
me, so the reviewer knows to probe them rather than assume the phase covered it.

**The second-order lesson, and it is the bigger one.** Three comments in that spec claimed coverage
the code did not implement — including one that was the Lock's *own named closure mechanism* for
that property, and one ("INV-2 runs twice, over both lenses") where the untested lens was the only
one that could fail; the tested one held by construction. Prose in a locked file is not commentary,
it is a durable claim a later reviewer will take at face value. **Every sentence of the form "the
property asserts X" must correspond to a line that asserts X.** Read a draft once with only that
question in mind. Related: guard every `continue` with a counter and a floor assertion, or a
property passes having asserted nothing.

**How the user resolved it, which is worth keeping.** They re-approved and refused the cheaper of
the two fixes on a principle: narrowing INV-6's coverage assertion to the combinations the old
fixture could reach *would have been adjusting the spec to the code* — the exact move the lock
exists to prevent, arriving disguised as the smaller change. When a coverage assertion is
unsatisfiable, move the fixture, not the claim.

They also directed `[Timeout]` in on the "attribute site is inside the lock" argument, and asked for
it **measured then over-shot**: `csharp-dev` timed a proxy (200 seeds, 41.8 ms on an unshared
workstation — a floor, not CI), derived 42 ms × 8 properties × 10 ≈ 3.4 s, and the value landed at
30 s. The reasoning to reuse: a number inside a lock has an **asymmetric error cost** — it can only
be raised by another re-approval, a green suite never reaches it, and under-shooting buys a false
red plus a hand-back. Measure so the figure is defensible, then round generously, and record both
the measurement and the derivation beside the value so the next reader checks arithmetic rather than
guessing at intent.

Related: [[what-belongs-in-the-lock]], [[claims-a-property-cannot-carry]],
[[invariant-alphabet-partition]]
