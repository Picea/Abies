---
name: one-route-mitigations-repeat
description: Recurring step shape — a mitigation written for a named field will be found again at a second and third field holding the same value; quantify the predicate over the value, not over the field
metadata:
  type: project
---

When a plan mitigates a data-exposure risk by naming a **field** (*"redact `Step.Cause`"*), expect
the same finding to return at the next review against a different field holding the same value.
Write the predicate over *"any place this value is retained"* the first time, and enumerate the
places underneath it — then adding a place is a line, not a new blocker.

**Why:** the `undo-redo` pass found one value — a live password — at three sites across three Critic
passes: `Step.Cause` (security room), then `Step.Model` (B7), then `Movement.Held(Anchor)` (S20).
Each mitigation was correct for its site and silent about the next.

**And the corollary that matters more than the rule:** widening the predicate is not always the fix.
`Movement.Held(Anchor)` **cannot** be covered by the same instrument — the anchor's whole job is to
answer `Subscriptions(anchor)`, which is derived from the *unprojected* model, so scrubbing it can
remove a field the subscription set reads and break the invariant the anchor exists for. Naming a
site inside a clause that is *discharged by* an instrument which cannot reach it makes the clause
false the moment it is adopted. `security-expert` declined the Critic's own suggested rewording on
exactly that ground, and gave the site its own honestly-scoped guarantee instead:
named in the trust boundary, bounded by an existing contract, and proved by a separate test that
says what it proves (*"this value reaches no release-path surface"*) rather than *"this value is
safe"*.

**How to apply:** two questions when a mitigation is written. (1) *What else holds this value?* —
enumerate every retention site in the value's own type, not just the one the finding named. (2) *Can
the instrument actually reach each site?* — if not, do **not** widen the clause; give that site a
separate guarantee, a separate test named for what it proves, and a threat-model line. A test with
two jobs is how the next version of the same gap gets missed.

Related: [[vacuously-met-invariants-are-the-loop-back-tell]], [[message-provenance-has-no-seam]].
