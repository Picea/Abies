---
name: collected-lists-must-be-a-prefix-of-the-downstream-one
description: When a Critic mitigation is routed straight into the downstream artifact, the plan's collected obligations list stops matching the spec's and the same ordinal names two different behaviours
metadata:
  type: feedback
---

A numbered "collected obligations" table in the plan is a **shared index with the downstream
artifact**, not a private list. When a Critic finding is routed from `05-critic.md` straight into
`06-spec.md` — the efficient move, and the one the Critic usually recommends — the spec's table
gains a row the plan's never gets, and every ordinal after that point names a different behaviour in
the two files.

**Why:** in the `undo-redo` pass the plan's table had thirteen rows and the spec's sixteen (S26's
superseded-branch line and S25(a)'s mid-bracket line went spec-only). The navigation obligation was
line 14 in the plan and line 16 in the spec, and the plan's 14 and the spec's 14 were different
behaviours. `reviewer-reconcile` reads the plan as *claims to verify* and the spec as *the bar*: it
would have ticked "obligation 14" against the wrong row, and the row with no plan-side entry at all
was the answer to the scope's open question 3 — the thing the collected list exists to stop falling
through.

**How to apply:** when a mitigation is routed downstream rather than into your artifact, still add
the row, pointing at `05-critic.md` as its source. The plan's table should be a **prefix or the
equal** of the downstream one, never a different sequence. On any re-open of a plan, diff your
collected list against the spec's by row *content*, not by count — and fix the header
cross-reference that names the row number with it. Related: [[vacuously-met-invariants-are-the-loop-back-tell]]
for the other way a row goes missing.
