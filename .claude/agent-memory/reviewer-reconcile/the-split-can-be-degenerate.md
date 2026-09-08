---
name: the-split-can-be-degenerate
description: At the round cap, when the changeset is indivisible the split has no carve line — grade by whether the remaining fix is cap-forcing, and escalate with named routes rather than manufacturing a fourth blocker
metadata:
  type: feedback
---

At round 3 the cap says *split — what passes ships, the rest becomes the next
changeset*. **That assumes a carve line exists.** When the changeset is a small
set of files the plan requires to land together (undo-redo PR 0: the locked spec,
its attribute, one csproj), and the one open item is a single sentence inside one
of them, there is nothing to carve. Do not resolve that by inventing a 🔴 —
that is exactly the fourth adversarial round the cap forbids — and do not resolve
it by writing PASS over a claim you measured to be false.

**Why:** at PR 0 round 3 the only finding left was a false re-approval date in
`csharp-dev`'s own file header (`2026-09-07`; `06-spec.md:2137` says
`answered 2026-09-08`). Round 1 had graded a header defect 🔴 in the same file,
so precedent alone said 🔴. But the earlier one was **cap-forcing** — the fix
required a `spec-author` amendment plus a user re-approval, which is why *"the
last moment at which this is cheap"* was a real argument — and this one was not:
amendment 5 had, at my own round-2 request, added a Lock consequence assigning
transcription commentary to `csharp-dev` to correct directly. Same class, one
line, same cost before and after the approval commit.

**How to apply:** at the cap, before grading, ask three questions in order —
(1) what is the *consequence* of the defect, not its class; (2) *who owns* the
fix under the artifact's own rules; (3) does the fix cost more **after** the
merge than before it? If the answer to (3) is no, the defect is not cap-forcing
and 🔴 buys nothing. Verdict ⚠️ Needs Human Review, blocker states the escalation
itself, and offer **numbered routes with a stated recommendation** — the user
decides in one line instead of dispatching another round. Say explicitly what
must *not* happen (here: a third `spec-author` amendment), because the author
reads an open verdict as an invitation to reopen the expensive path.

Related: [[the-round-cap-is-a-verdict-shape]] (count the rounds before grading),
[[state-the-finding-not-the-remedy]] (routes, not prescriptions),
[[a-blocker-list-is-not-the-criterion]].
