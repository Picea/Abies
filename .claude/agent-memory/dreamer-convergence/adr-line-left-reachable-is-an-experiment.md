---
name: adr-line-left-reachable-is-an-experiment
description: When a doc Track A can read pre-answers a scope question, the user leaves it in place deliberately — convergence must classify copied-vs-derived, not treat it as contamination to route around.
metadata:
  type: feedback
---

If a reachable file (an ADR, a README) already contains an answer to one of the pass's open
questions, do **not** treat it as a leak that invalidates Track A, and do not hedge the
classification. Classify it: did Track A copy the line, or reason past it?

**Why:** the user made this call explicitly on `undo-redo`
(`lead-20260906T150129Z-undo-redo-adr-008-left-as-is`), declining to edit
`docs/adr/ADR-008-immutable-state.md:85` for the pass. Their reason: *"Editing a production
ADR to make the blind track derive independently would be a constructed result. Whether
Track A copies the ADR or reasons past it is the experiment; convergence classifies it."*
The classification is the *point* of leaving the line in place.

**How to apply:** argue the classification from evidence in descending strength, and state
the counterweight rather than fudging it. What worked on `undo-redo`:

1. **Does the artifact contradict the line's substantive claim?** ADR-008:85 said undo was
   *trivial*; Track A's whole artifact argued it is not, and enumerated why. A copy
   propagates its source's framing.
2. **Does it adopt the line's vocabulary?** The line said *snapshots*; Track A never used
   the word for its own history and explicitly ruled the snapshot mechanism out.
3. **Does the conclusion arrive as the survivor of recorded rejections, or as a premise?**
   Three abandoned lines with distinct falsifying reasons is the shape of a derivation.
4. **Does it produce a claim the source does not contain?** "A history does not allocate, it
   defers collection" is not in ADR-008 and its Negative section points the other way.
5. **The artifact's own provenance header.** Weakest — it is self-report. List it last.

Then say the honest counterweight out loud: agreement existed, and agreement with a
one-clause claim the artifact goes on to contradict is not evidence of copying. Verdict on
`undo-redo`: **reasoned past it.**

Corollary worth remembering: when *both* tracks and the reachable line agree, that is what a
forced answer looks like, and it strengthens the recommendation rather than weakening it.

See [[calibration-dual-track]].
