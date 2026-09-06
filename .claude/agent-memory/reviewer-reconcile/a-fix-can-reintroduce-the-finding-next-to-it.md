---
name: a-fix-can-reintroduce-the-finding-next-to-it
description: On a re-review, check each fix against the OTHER findings of the same round, not just the one it targets — dedup fixed here is often dedup created there.
metadata:
  type: feedback
---

On a re-review, verify each fix against the **other** findings in the same
round, not only the finding it was assigned. Cross-check especially when two
findings name the same defect class.

**Why:** PR #358 round 2 closed ⚠️-7 ("~100 lines of security-critical path
logic duplicated verbatim between the two blindness hooks") by extracting
`lib/path_containment.py`, complete with a suite assertion that both hooks
import it and neither redefines the functions. The fix for 🔴-3, in the same
working tree, pasted ~90 lines of `split_simple_commands`/`push_destinations`
verbatim into two other hooks, with no assertion that the copies agree — and
justified it in a comment with "no established sourcing convention between
them", which was refutable by `grep` in two independent ways (a bash `source`
convention already existed at HEAD in four hooks; the python `HOOKS_LIB_DIR`
convention was created by that same changeset).

The register tracked the *instance* (R-10, the two blindness hooks) rather than
the *class*, so nothing caught it.

**How to apply:** build the round's finding list as classes, not file
locations. Before accepting a fix, grep the whole tree for the class it
belongs to — duplication, fail-open polarity, dangling citation, unbounded
regex. And treat any "we could not do X because no convention exists" comment
as a claim to verify, not a rationale to accept. Related:
[[a-disclaimer-does-not-fix-a-false-claim]],
[[audit-the-residual-ledger-in-both-directions]],
[[command-text-matching-is-not-a-gate]].
