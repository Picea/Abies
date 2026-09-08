---
name: arithmetic-adjudicates-a-missing-baseline
description: When an untracked file has no byte baseline to diff against, line-count arithmetic decides whether the artifact changed or my own prior measurement was carried prose
metadata:
  type: feedback
---

For an **untracked** file there is no `git show` baseline, so "unchanged since
last round" cannot be diffed directly. Do not settle it by trusting either the
author's report or my own previous verdict — derive it.

**Why:** on PR 0's `UndoRedoSpec.cs` confirmation, my round-3 section recorded
the fence extract-and-diff as *"74 lines"* with the bridge note at *":564-587"*.
Re-running the identical extraction over an unchanged spec gave **69** and
*":568-591"*. With the file untracked there was nothing to diff against, and the
tempting readings were both wrong: "the commentary changed by 5 lines behind the
scope statement" and "my numbers are fine".

The arithmetic decided it. The file is 1,068 lines (round 3's own metric), the
six extracted fences are 1,009, so net must be +59 — 60 added, 1 removed — in
**both** rounds, which forces the same 69-line diff both times. Round 3's
figures were carried prose, never re-derived. Note the irony: the finding I was
confirming closed, ⚠️-13, was itself a stale date carried into a header.

**How to apply:** on any re-review of an untracked or otherwise unbaselined
file, capture the load-bearing invariants as *numbers that must reconcile* —
total file lines, extracted-source lines, test count, property count — and check
they close against each other. When they do, a discrepancy in the prose is the
prose's fault, not the artifact's. Then say so in the verdict: a reviewer that
silently corrects its own earlier measurement has stopped being a record. Prefer
re-deriving every figure each round over quoting the previous round's; see
[[doc-inventory-counts-go-stale-within-the-same-pr]] for the same failure in the
author's direction and [[a-record-is-checkable-only-where-its-evidence-survives]]
for why the baseline was missing in the first place.

A stronger habit that made this safe: prove fence faithfulness **against the
committed spec**, not against the prior round's notes. `diff` the reassembly and
check that *every added line is a comment, a blank or a moved brace* — that is a
claim about the artifact, independent of any bookkeeping I might have got wrong.
