---
name: verify-a-registration-pass-by-scope-not-substance
description: When the round cap's split resolves as "register now, then merge", the closing pass verifies scope and truth-of-registration — not substance, and not a fourth round of findings
metadata:
  type: feedback
---

When the round cap returns the split and the user picks *register now, then
merge*, the closing pass is a **different kind of pass** from rounds 1–3. Its
whole question is: did the registrations land, are they true of the tree, and
did anything else ride along?

**Why:** the cap's own text forbids a fourth fix round. Re-running all eleven
dimensions on a documentation-and-ledger delta manufactures findings that the
cap has already ruled out of scope, and turns a completed split back into an
open iteration. Round 3 of PR #358 ended ⚠️ solely because Merge Criterion (b)
was unsatisfied; once the ledger entries exist, (b) flips and the verdict
follows mechanically.

**How to apply:**

1. **Measure scope, do not assume it.** `find . -newer <the archived previous
   round's decision drop>` and compare against the claimed file list. Anything
   beyond the claimed files plus hook-written state
   (`.claude/docs/decisions.md`, `.squad/log/*`, `.squad/.last-review-verdict`)
   and your own notebook is scope creep, and scope creep is what makes
   register-then-merge unsafe. On #358 the set was exactly the five claimed
   files — that null result is the finding worth stating.
2. **Re-execute whatever the new prose now asserts.** A registration that
   describes a gap must still have the gap (re-fire the probe: it should still
   fail to refuse), and a reword that claims something is mitigated must be
   mitigated (re-fire the probe: it should refuse, including the disclosed
   over-block). Documentation that is newly *right* is still an unverified
   claim.
3. **Audit the ledger in both directions here too** — see
   [[audit-the-residual-ledger-in-both-directions]]. Confirm no new entry
   registers something already fixed. For a symlink residual, `grep realpath`
   should hit only the comment naming it, never a call site.
4. **An unchanged test count is the expected result**, not a gap. A
   ledger-and-docs delta that moves the assertion count means behaviour changed
   and the pass was not what it claimed.
5. **Re-run only the dimensions the changed files could disturb** (Security,
   Documentation, Consistency, Testability for a registration pass) and say in
   the verdict which ones you did not reopen and why.

Related: [[the-round-cap-is-a-verdict-shape]] — that entry says the verdict at
round 3 *is* the split; this one covers what the pass after the split looks
like.
