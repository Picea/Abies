---
name: a-locked-file-was-never-type-checked
description: A spec file excluded from compilation has never been compiled, so a lock applied to it locks the typos too — compile-probe every assertion shape before the lock closes
metadata:
  type: feedback
---

Spec-by-example passes sometimes commit the spec **before** the types exist,
parking it with `<Compile Remove="X.cs" />` and declaring it immutable. The
structural weakness is one sentence: **an excluded file is never type-checked,
and this one is being frozen.** Every wrong overload is frozen with it, and by
the protocol becomes a `// SPEC CONFLICT:` hand-back instead of a one-line fix.

**How to apply — the probe, not the argument.** Build a scratch project against
the *pinned* test-framework version from the local NuGet cache and compile the
assertion shapes the spec uses. On `undo-redo-pr0` (TUnit 1.19.57) two probes
paid for themselves:

- `Assert.That(a).IsNotEqualTo(b).Or.That(c).IsTrue()` →
  `CS1061: 'OrContinuation<int>' does not contain a definition for 'That'`.
  `.Or` continues on the **same subject**; a disjunction across two different
  values is not expressible at all, so it is a wrong *shape*, not a wrong
  overload.
- `IsEquivalentTo` is **order-insensitive** — I ran it and a permutation passed.
  Any test whose whole purpose is an ordering claim is vacuous under it.
  `CollectionOrdering.Matching` does not exist on that version (`CS0103`);
  `IsEqualTo` on a collection *is* order-sensitive. So the remedy is a different
  assertion, not a parameter.

**Also check the prose against the code.** Locked commentary becomes a durable
false claim. Three in that file said the property asserted coverage / ran over
two policies / counted both branches; none was implemented, and one of them
*was* the named falsifier the spec's own "The Lock" section relied on to close
its unlocked-support-file hole. Grep the claim, then read the method body to its
closing brace.

**And enumerate what the lock obliges the *unlocked* files to do** — a sealed,
non-`partial` class with unqualified helper calls silently requires a
`global using static`; a static log requires either per-test isolation or a
`[NotInParallel]` that is itself inside the lock. Those obligations are
invisible and are the review's job to list while the lock is still open.

**Timing is the whole point:** the approval commit *is* the moment the lock
closes. Say so in the verdict. Related:
[[extract-the-spec-fences-and-diff]], [[state-the-finding-not-the-remedy]].
