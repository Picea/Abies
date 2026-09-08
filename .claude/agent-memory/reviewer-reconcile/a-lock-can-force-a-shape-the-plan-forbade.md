---
name: a-lock-can-force-a-shape-the-plan-forbade
description: When the implementation's type shape differs from the plan's, check whether a locked artifact forced it — then re-check every downstream analysis written against the plan's shape, and every remedy the blind half proposed
metadata:
  type: feedback
---

A shape divergence between the plan and the code has three possible causes, and
they grade differently: the author chose (a deviation), the plan is stale, or a
**higher-ranking locked artifact made the plan's shape uncompilable**. The third
is not a deviation and must not be reverted — but it silently invalidates
whatever was reasoned about the plan's shape.

**Why:** undo-redo PR 1 (2026-09-08). The plan specified `Movement.Held(TModel Anchor)`
in three places; the code shipped `Held(object Anchor)`. The locked spec asserts
`IsTypeOf<Movement.Held>()` **non-generically**, which cannot resolve if
`Movement` is generic — so the erasure was forced, and the lock outranks the plan.
Correct call. But `security-expert`'s entire `[R4-6]` treatment, Trust Boundary
7's wording and a named step-8 regression test were all drafted against the
un-erased shape, and none of them had been re-checked: the erasure made a
wrong-typed anchor constructible, changed the DEBUG `JsonTypeInfo` obligation from
a `TModel` property to an `object` one, and widened the read surface. The
divergence was reported by the implementer as a fact, never as a flag.

**How to apply:** on any shape divergence, first establish which artifact forced
it (grep the locked file for the construct — here, a non-generic type reference in
an `IsTypeOf<>`), then list every downstream artifact that quotes the *old* shape
by name and check each. If the forced shape is right, the finding is not "revert
it" but "nobody has re-run the analysis" — with a named owner and an `expires:`,
which makes it registrable under criterion (b) rather than blocking.

**Corollary, and the one that saved me repeating an error:** `reviewer-blind`'s
proposed remedies are formed without the locked artifacts fully weighed. Verify
each one before echoing it. Both remedies it offered for the
`Picea.Abies.History.History` namespace/type collision were unavailable — moving
the types to the root namespace does not remove a shadow caused by the *test*
namespace inside the locked file, and moving the factory onto `History<TModel>`
breaks the locked spec's `History.Backward(h)` / `History.Forward(h)` calls on the
static class. Repeating them would have sent the author at two dead ends. See
[[state-the-finding-not-the-remedy]] and
[[inherited-or-introduced-decides-the-verdict]].
