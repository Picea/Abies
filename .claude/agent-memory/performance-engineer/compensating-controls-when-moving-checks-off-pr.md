---
name: compensating-controls-when-moving-checks-off-pr
description: Moving a check off the PR path requires four compensating controls, or the speed gain buys false confidence
metadata:
  type: feedback
---

When a check genuinely has to move off the PR path, four controls travel with
it:

1. Strict path-aware required checks, so the PR still blocks when the relevant
   inputs changed.
2. A merge queue, so main is tested in the order it will actually be merged.
3. Nightly full-suite sweeps as the catch-all.
4. An auto-revert / escalation policy for post-merge failures.

**Why:** Agreed 2026-04-01. Without these, "we moved it to nightly" means
regressions land on main and sit there until someone notices — the PR got
faster and the signal got worse. Control 4 is the one most often skipped and the
one that decides whether a nightly failure is acted on or accumulates.

**How to apply:** Treat this as a package, not a menu. If a proposal to
de-PR a check cannot name who acts on the nightly failure and how fast main is
reverted, the proposal is not finished. Prefer
[[required-check-with-conditional-heavy-work]] first — it usually removes the
need for this conversation entirely.
