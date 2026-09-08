---
name: grade-a-compound-finding-by-which-half-survives
description: When a finding bundles an introduced defect with an inherited one and only the introduced half is fixed, regrade the residue on the criterion's introduced-vs-inherited line and write the reasoning down, or the regrade becomes laundering
metadata:
  type: feedback
---

A finding written as *"this change makes X wrong, **and** Y was already wrong"*
is two findings wearing one number. When the author fixes X, do not carry the
whole thing forward at its original grade out of consistency, and do not close
it because "the finding was addressed." **Grade Y on its own merits, against the
Merge Criterion's own dividing line.**

**Why:** `undo-redo-followups` ⚠️-7 was exactly this shape — a csproj comment
whose count the changeset falsified (introduced) next to a clause about `None`
items that was false before the changeset existed (inherited). Round 3 fixed the
count only. The criterion is explicit that *a regression introduced by the
changeset* blocks unconditionally while *pre-existing gaps the changeset did not
introduce* do not, once registered — so the two halves were never the same
grade; the compound had hidden that. The ⚠️ had been driven by the introduced
half plus the Boy Scout argument *"you are already editing this sentence"*, and
that argument expires the moment the sentence has been edited correctly.

What made the downgrade defensible, and what to check before doing one:

- The introduced half is **closed and measured**, not merely claimed.
- The residue is **byte-identical to what stood at HEAD** — check it, do not
  assume; the fix can widen an inherited defect while closing the introduced one.
- The residue has **no verified consequence** — here `dotnet build` clean, 0
  `Compile` items under `content/`, the outcome the comment exists to explain
  intact.
- The residue's grade **matches what you gave structurally identical findings
  elsewhere in the same review**. If you graded a dropped sentence in a snippet
  💡 and an equally inconsequential false clause ⚠️, one of the two is wrong.
- The fix **costs the same after the merge as before** — the cap-forcing test
  from [[the-split-can-be-degenerate]].

**How to apply:** write the downgrade as a numbered argument in the verdict, say
explicitly that you are *not* claiming the residue is true, keep it open at the
new grade with a named owner, and state that a recurring pattern of
half-fix-then-regrade is laundering and should be called out. Criterion (b) then
closes honestly rather than by attrition — and note that at the round cap this is
how a **split round can end ✅**: the split does not require a carve line, only
that everything remaining passes (a)–(c). Related:
[[a-blocker-can-be-reduced-rather-than-closed]] (a *narrowed check* residue, a
different shape), [[the-round-cap-is-a-verdict-shape]],
[[state-which-grades-criterion-b-binds-on]].

One caution learned here: do **not** rest the downgrade on your own previous
round's parenthetical ("fix it, *if the Boy Scout call is taken*"). That
softening lived in the verdict prose and **not** in the decision drop, whose
`high` entry stated both clauses flatly. Two of your documents disagreed; the
author could have read either. Rest the call on measurement, not on which one
they read — and keep prose and drop aligned next time.
