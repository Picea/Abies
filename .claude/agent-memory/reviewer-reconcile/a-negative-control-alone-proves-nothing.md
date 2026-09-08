---
name: a-negative-control-alone-proves-nothing
description: A probe that only shows the wrong input failing cannot distinguish "the assertion discriminates" from "the assertion always fails" — and CS0103 is a missing using, not an absent API. Both errors sent an author to a broken remedy.
metadata:
  type: feedback
---

Every probe of an assertion's *semantics* needs a passing control and a failing
control. A one-sided probe is not evidence of discrimination.

**Why:** On the undo/redo locked spec (PR 0), round 1 I reported two things about
TUnit 1.19.57 and both were wrong:

1. *"`Assert.That(collection).IsEqualTo(sequence)` **is** order-sensitive — my
   probe failed on a permutation, as it should."* I ran only the permutation. It
   fails on the **matching** sequence too: the collection expression is
   target-typed to `<>z__ReadOnlyArray<T>` and compared by `Equals`, so it can
   never pass. A permutation failing is equally consistent with *order-sensitive*
   and with *always fails*, and only the positive control separates them.
2. *"`IsEquivalentTo(expected, CollectionOrdering.Matching)` → `CS0103 … does not
   exist in the current context`. There is no option flag on this version."*
   `CollectionOrdering` **exists**, in `TUnit.Assertions.Enums`. `CS0103` is a
   name-resolution failure — a missing `using` — not an API absence.
   `strings TUnit.Assertions.dll | grep CollectionOrdering` settles it in one
   command.

The cost was not abstract. `06-spec.md`'s amendment 4 says in writing that it
chose its remedy *"because the reviewer had already executed it"*, and shipped
three assertions in locked, user-re-approved text that can never pass — strictly
worse than the vacuous ones they replaced, because step 6 cannot make them green.
Round 2 blocked on a defect round 1 caused.

**How to apply:**

- For any claim of the form *"assertion X discriminates on property P"*, run the
  P-satisfying case **and** the P-violating case. Name both in the finding. If a
  probe only has `_should_FAIL` tests, it is not finished.
- When a probe test fails, read *why*. `Expected to be equal to
  <>z__ReadOnlyArray\`1[System.String] but received <>z__ReadOnlyArray\`1[...]`
  is a type name on both sides — that is reference equality, not a content
  mismatch. A real content mismatch prints the elements.
- Treat `CS0103` / `CS0246` as *"look in the assembly"*, never as *"the API does
  not exist"*. `strings` on the package DLL, or a probe with candidate `using`s,
  costs one minute.
- Before reporting an assertion as unsatisfiable, sweep the plausible subject
  types the unlocked support code could supply (`T[]`, `List<T>`,
  `IEnumerable<T>`, `IReadOnlyList<T>`, `ImmutableArray<T>`, and a bespoke
  `[CollectionBuilder]` type with correct `IEquatable<T>`). That sweep is what
  turns "this looks broken" into "no support-file choice closes it", which is the
  difference between an obligation and a blocker.
- Beware the asymmetry this creates: **an author will adopt the shape you
  executed.** Anything presented as measured becomes the remedy. See
  [[state-the-finding-not-the-remedy]] — but note the tension: stating the
  finding without a remedy is safe; stating a remedy you half-measured is not,
  and *that* is the failure mode here.

Related: [[a-locked-file-was-never-type-checked]] (why the probes happen at all),
[[extract-the-spec-fences-and-diff]] (who owns the defect once it is proven).
