---
name: the-clock-field-is-a-toolchain-job
description: Decision-drop created:/id: timestamps are unreliable by construction — five roles have no Bash to read a clock, and I got it wrong myself despite having one
metadata:
  type: project
---

Decision drops declare `created:` and embed a timestamp in `id:`. Both are
**unreliable by construction**, and this is a framework defect rather than any
agent's carelessness.

**Why.** `critic`, `architect`, `realist`, `spec-author`, `scope-warden` and
`dreamer-convergence` declare no `Bash` tool, so they cannot run `date -u`. On
PR #359 the correlation across seven drops was exact: `lead` and
`reviewer-reconcile` (both hold `Bash`) were accurate to 39 s and 72 s; the three
`critic` and two `architect` drops carried `T000000Z`, `T120000Z`, `T184500Z` —
two of them dated *after* the commit containing them. The defect predates that PR
(`2026-09-02T19-54-17-review-pr355-round5.md` is future-dated by 36 min).
`validate()` checks format, never plausibility, so it always passes.

**Then I did it too.** Writing that review I put `created: 2026-09-07T12:15:00Z`
on my own drop from context; the merger archived it at `10-25-28`. I was out by
1 h 50 m *while holding `Bash`* — local time written where UTC was asked for, at
the one moment nothing prompts you to check. So the real root cause is that the
schema asks the author, at authoring time, for a value only reliably knowable at
merge time, which `scribe-decision-merger.sh` already computes (it names the
archive file from it, `:1202`/`:1237`).

**How to apply.**
1. **Read the clock before writing the drop, not after.** `date -u
   +%Y-%m-%dT%H:%M:%SZ` and `+%Y%m%dT%H%M%SZ` in the same call that reads
   `git rev-parse HEAD`. Never write a timestamp from conversational context —
   the session's displayed date is local, and `created:` is UTC.
2. **Cross-check before finishing:** the archive filename the merger produces is
   the truth; if it disagrees with your `created:`, say so in the verdict, because
   you cannot fix it afterwards (`enforce-reviewer-readonly.sh` denies you
   `.claude/docs/decisions.md` and the archive).
3. **When grading this in someone else's drop, do not recommend rewriting `id:`.**
   Ids are documented stable anchors (`memory-policy.md` § *Anchors and stable
   IDs*) and are cited from agent memories and other drops' `references:`.
   Recommend correcting `created:` only, and fixing the class at the merger.
4. Two harms that sound right and **do not** materialise — check before asserting
   them: `squad-rotate.py` ages files by mtime, not `created:`, so archival
   eligibility is unaffected; and `decisions.md` lists drops in *merge* order, so
   the register reads in true chronology. The real residual harm is `id`
   collision, since the schema requires global uniqueness and `T000000Z` repeats.

See [[a-record-is-checkable-only-where-its-evidence-survives]].
