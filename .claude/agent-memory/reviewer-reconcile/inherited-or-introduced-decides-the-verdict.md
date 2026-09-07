---
name: inherited-or-introduced-decides-the-verdict
description: For copy/snapshot bundles, diff against the pinned commit before grading a discrepancy — a faithful copy of an inconsistent source is a different finding from an error made here
metadata:
  type: feedback
---

When a change ships **copies** of other files, the first question about any internal
contradiction is not "is this wrong" but **"was it wrong before it was copied."** The two
answers produce different verdicts, and only `git show <pinned-sha>:<path>` separates them.

**Why:** on `presentation-demo` (2026-09-07) `reviewer-blind` flagged as its most serious
finding that a bundled warden report claimed a 153-line scope while the scope shipped beside
it was 254 lines. It could not settle the cause — `.squad/design/` is denied to it by hook —
and correctly said so. Diffing against the pinned commit settled it in one command: the
source at `07607bf` says 153, the scope at `07607bf` is 254. **Inherited.** All 19 whole-file
copies were byte-identical to the pinned commit. What looked like a 🔴 fabrication was a
faithful copy of a genuinely inconsistent source, and the fix collapsed from "correct the
data" to "annotate the index."

Two further habits earned on the same pass:

- **Verify copies against the pinned commit, not only the working tree.** They agreed here,
  but design artifacts are overwritten in place, so a copy taken at sha X and reviewed later
  can match neither. Run both diffs; say which one you ran.
- **Re-read the accused file's exact wording before endorsing a "X contradicts Y" finding.**
  The blind reading also charged the bundle's one authored file with asserting the warden
  checked 254 lines. It does not: it attributes "254-line" to the *mechanical scan* row and
  claims no line count on the *subagent* row. The contradiction was between the reviewer's
  paraphrase and the file. I withdrew that half.

**How to apply:** any change whose claim is "these are unedited copies" — snapshot bundles,
demo folders, vendored trees, docs mirrors. Verify every copy mechanically and say how many
you checked; the blind half usually cannot, and its list of "what I could not determine" is
your work queue. Related: [[a-record-is-checkable-only-where-its-evidence-survives]].
