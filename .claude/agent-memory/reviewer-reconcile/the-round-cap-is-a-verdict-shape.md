---
name: the-round-cap-is-a-verdict-shape
description: At review round 3 the Merge Criterion's round cap changes what verdict you are allowed to issue — check the round count before grading, not after
metadata:
  type: feedback
---

`.claude/docs/principles-enforcement.md` § The Merge Criterion — Continuous
Improvement ends with **"Round cap: two."** On the third review round of one
changeset, the changeset is *split*: what passes under (a)–(c) ships, the rest is
registered and becomes the next changeset. If nothing passes, the cap escalates
to the `architect` — it does not merge.

**Why:** the cap exists so a changeset cannot be ground down by an indefinite
sequence of reviewer findings, each individually correct. On PR #358 round 3 I
had already assembled a normal ⚠️/💡 finding set before noticing the cap applied —
one more 🔴 would have opened a round 4 that the binding document forbids.

**How to apply:**

- **Count the rounds before you grade, not after.** Evidence, as the criterion
  prescribes: archived `reviewer-reconcile` drops in
  `.squad/decisions/archive/<YYYY-MM>/` carrying the same `commit:` as the base
  you are reviewing, cross-checked against the `**Round:** n` line in the
  previous `09-review-verdict.md`. On disagreement the higher wins. Note that a
  round-1 drop may never have been archived, so the archive count can undercount —
  the `Round:` line is the safer of the two.
- At round 3, the verdict is the **split**, and the split is usually the user's
  call rather than yours: say what passes, say what must be registered, and say
  explicitly that you are **not** requesting another fix round.
- The remaining findings at a cap are typically pre-existing residuals needing a
  ledger append, not code. You cannot write the append (`enforce-reviewer-readonly.sh`),
  and the criterion is explicit that you must not — *"the reviewer classifies, the
  author registers, the reviewer verifies."* So ⚠️ Needs Human Review, with the
  cap itself as the first blocker, is the honest shape.

Related: [[audit-the-residual-ledger-in-both-directions]],
[[a-fix-can-reintroduce-the-finding-next-to-it]].
