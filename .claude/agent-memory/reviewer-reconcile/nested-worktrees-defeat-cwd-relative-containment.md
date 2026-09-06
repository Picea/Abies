---
name: nested-worktrees-defeat-cwd-relative-containment
description: Probe deny-list hooks with a root pointing AT the worktree directory, not just at a path inside it — the descendant direction is usually handled, the ancestor direction usually is not.
metadata:
  type: feedback
---

Whenever a blindness/deny-list hook resolves paths relative to the payload
`cwd`, probe it with a scope rooted **at** a nested checkout directory
(`.claude/worktrees`), not only with a path **inside** one.

**Why:** PR #358. `path_containment.py`'s `reach_hit` retries every trailing
segment run of a resolved path, so
`Read .claude/worktrees/agent-x/.claude/docs/decisions.md` is correctly
refused — the denied pattern recurs at a deeper offset and the fallback finds
it. But `Grep(path=".claude/worktrees")` and
`Glob(".claude/worktrees/**/decisions.md")` both exit 0: the root's own
segments (`.claude`, `worktrees`) match no deny pattern, and trailing runs of a
two-segment root are shorter, not longer. Verified by execution against both
`enforce-track-blindness.sh` and `enforce-review-blindness.sh`, with a real
registered worktree present holding full copies of every deny-list target.

The module's docstring named the gap honestly ("does not attempt the ancestor
direction … stays open in both callers") — but naming is not registering, and
the same changeset made the directory a standing convention by gitignoring it
while three charters declared `isolation: worktree`.

**How to apply:** for every deny-list hook, run four probes — denied path
direct, denied path via the nested checkout, scope rooted at the nested
checkout, scope rooted at its parent. The third is the one that passes review
by accident. Related: [[shape-heuristics-are-defeated-by-one-line]],
[[prefix-stripping-fixes-are-unbounded]].
