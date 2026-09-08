---
name: a-copy-outside-the-deny-list-defeats-blindness
description: enforce-review-blindness.sh denies paths, not content — a byte-identical copy of the design pass committed elsewhere in the repo is allowed
metadata:
  type: project
---

`enforce-review-blindness.sh` denies `reviewer-blind` reads at or above
`.squad/design/**`. It is a **path** deny list, so it says nothing about a copy
of the same bytes at another path.

**As of 2026-09-07 this repository has one.** `7d32cdb` (PR #360, merged) added
`Picea.Abies.Presentation/content/demo/full/` carrying `00-scope` …
`07-handoff`, `refutations.md`, `room-security.md`, and `decision-drops/`
including two **previous review verdicts** — byte-identical to
`.squad/design/undo-redo/` (md5-verified for six artifacts). Plus
`stops/5.6-review-headers.composite.md` and `stops/5.5-property.cs`, a
hand-copied test method the demo's `SHA256SUMS` checksums.

**Why:** proved by firing the hook directly rather than reasoning about it —
```
printf '{"tool_name":"Read","agent_type":"reviewer-blind","tool_input":{"file_path":"..."}}' \
  | CLAUDE_PROJECT_DIR=$PWD .claude/hooks/enforce-review-blindness.sh; echo $?
```
exit **0** for the demo copies of `06-spec.md`, `05-critic.md` and the
`review-pr359-round2` drop; exit **2** for `.squad/design/undo-redo/06-spec.md`.
This is a second *path* reachable by the **mediated** tools, so it is distinct
from residual **R-19** (`Bash` unmediated) and **R-20** (sibling worktrees,
symlinks) — closing R-19 entirely would leave it standing. It was unregistered
when I found it.

**How to apply:** the blind reviewer's isolation in this repo is held partly by
its own restraint, so treat `08-review-blind.md`'s account of what leaked as
evidence to check, not as a disclaimer. Before crediting a blind reading as
independent, ask where else in the tree the design pass lives —
`git log --diff-filter=A -- '*/00-scope.md' '*/07-handoff.md'` finds copies. And
when a conference/demo folder duplicates a locked artifact, expect drift: the
presentation copy of `INV-3` already disagreed with the committed one on
whitespace before anyone tried to change either. Related:
[[a-record-is-checkable-only-where-its-evidence-survives]],
[[audit-the-residual-ledger-in-both-directions]].
