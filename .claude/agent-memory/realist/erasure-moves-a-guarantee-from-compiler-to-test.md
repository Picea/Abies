---
name: erasure-moves-a-guarantee-from-compiler-to-test
description: When a locked spec forces a type parameter off a type, the guarantee it carried becomes a test, a boundary provenance clause and an open risk — three sites, not one
metadata:
  type: project
---

A locked spec assertion can force an implementation type to lose a type parameter
(`Movement.Held(TModel Anchor)` → `Held(object Anchor)`, forced by a non-generic
`IsTypeOf<Movement.Held>()`). When that happens, ask what the type parameter was silently
*proving*, and cost its replacement at three sites:

1. **A test** that proves by execution what the type proved by construction — kept *beside* the
   existing test on the same subject, never merged into it. Confidentiality ("the payload does not
   leak") and identity ("the value is the one we stored") are different proofs; one mechanism per
   test, named for what it proves.
2. **A provenance clause on the trust boundary** that already names the site. The boundary's
   sentence usually stays *true* and stops being true *by construction* — a boundary that reads as
   compiler-backed when it is test-backed over-promises.
3. **A low-severity open risk** naming the test as the only thing standing where the compiler
   stood, so deleting the test is visibly a security-relevant act.

**Why:** `[R4-anchor]` in `undo-redo/04-realist-plan.md`. An internal constructor closes
construction from *outside* the assembly; it cannot close it from inside, because `object` accepts
anything and there is no `TModel` left to check against. The security room's answer was explicitly
"no new threat-model row" — the exposure surfaces did not change, only the mechanism keeping the
value honest.

**How to apply:** the tell is a review finding that a shipped signature differs from the plan's
because of the spec. Do not treat it as a shipped-vs-planned discrepancy to reconcile; treat it as
a guarantee that needs re-housing. Also grep the File-Level Changes table — a new test in an
existing planned file makes that row stale. Related:
[[an-exception-named-by-element-has-four-sites]],
[[style-rule-cost-depends-on-checker-scope]].
