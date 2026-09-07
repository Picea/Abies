---
name: a-record-is-checkable-only-where-its-evidence-survives
description: Design-record PRs fail on provenance, not substance — check that every citation into .squad/design/ still resolves, because the documented re-run flows overwrite artifacts in place
metadata:
  type: feedback
---

Reviewing a design-record PR (PR #359, `undo-redo`), every substantive
engineering claim checked out — twenty-for-twenty on citations outside the design
directory, and the same discipline inside it. Everything that blocked was
**metadata and provenance**.

**Why:** the framework's own governance documents cite `.squad/design/`
artifacts as evidence, and several of those artifacts are **overwritten in place
by documented happy paths**:

- `CLAUDE.md` § 3 rule 2 — when a scope is sent back, the warden runs again and
  writes to the same `00-warden.md`. The first report, findings and all, is gone.
  On PR #359 both `.claude/enforcement/refutations.md`'s R-21 (`source:` → a
  section "Findings on Adjacent Artifacts") and `flow-changelog.md`'s "gate 1
  caught two prior-work leaks" claim pointed at that destroyed report. Both
  claims were **true** — I confirmed them against the *deleted* draft at the base
  commit and against the hook source — but neither was checkable from its own
  citation.
- The loop-back protocol has `realist` overwrite `04-realist-plan.md` in place
  across revisions, so any citation to "revision 2 said X" is unverifiable too.

**How to apply:** on any PR that lands or amends a design record, run every
citation that points into `.squad/design/` and confirm the target section exists
*in the committed file*, not in the version the citing author was looking at.
`grep -rn "<the quoted section heading>" . --include=*.md` returning only the
citation itself is the tell. Then check whether the underlying claim is
independently verifiable from something durable — the base commit's deleted
files (`git show <base>:<path>`), the hook source, the code. A true claim with a
dangling citation is ⚠️/🔴 on the citation, not on the claim; say which, or the
author fixes the wrong thing.

The general rule: **a governance record is only as good as the evidence that
outlives the pass that produced it.** Prefer inline quotation over a pointer for
anything a control's effectiveness claim rests on — it is also the only form a
`reviewer-blind` can check, since `.squad/design/` is denied to it.

See [[audit-the-residual-ledger-in-both-directions]] for the complementary sweep,
and [[the-clock-field-is-a-toolchain-job]] for the same PR's other metadata
failure.
