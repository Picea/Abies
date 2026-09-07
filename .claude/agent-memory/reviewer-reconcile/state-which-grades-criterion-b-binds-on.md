---
name: state-which-grades-criterion-b-binds-on
description: Merge Criterion (b) read literally sweeps in nitpicks and never closes — say in the verdict which finding grades it binds on, and why, or the closing pass re-litigates it
metadata:
  type: feedback
---

Merge Criterion (b) says "**every** finding that is not a regression of a stated
property is registered", and its "no longer blocking, once registered" list names
*advisory findings* explicitly. Read at maximum extension, that sweeps in 💡
nitpicks too — and then it never closes, because every registration pass produces
nitpicks of its own that would need registering in turn.

**Why:** on PR #359 I had already been burned in the other direction — my round-1
blocker enumerated four findings and the fixer read the list as the scope, so (b)
came back unmet (see [[a-blocker-list-is-not-the-criterion]]). The obvious
correction, "bind by the rule, not the list", overshoots if the rule is taken
literally. Both failures are the same failure: leaving the criterion's *extent*
implicit and letting the next reader pick.

**How to apply:** in the verdict that closes on (b), write one sentence naming
the grades it binds on and disposing of the rest by reason, not by silence. What
worked at #359's confirmation: (b) binds on 🔴 and ⚠️; the two 💡 items are not
registered and do not block, because neither asserts a defect requiring a remedy
(one observes a routing table is silent, one observes an append-only file cannot
carry a backward pointer), both are findable in the merged decision drop, and the
maximal reading regresses without limit. A 💡 that *does* assert a defect
requiring a remedy is a mis-graded ⚠️ — regrade it rather than arguing scope.

Two checks that belong to the same pass, both of which paid off there:

- **Did the fix commit's own new artifacts reproduce the defect it registers?**
  The commit landing R-22's correction added a decision drop whose `created:` sat
  19s off its archive stamp — evidence the author read a clock, and the cheapest
  possible refutation of "fixed the wording, kept the habit". Cf.
  [[a-fix-can-reintroduce-the-finding-next-to-it]].
- **Does a bundled residual register anything already fixed?** A sub-item closed
  in the same commit belongs *inline as closed*, never as `status: open`. See
  [[audit-the-residual-ledger-in-both-directions]].
