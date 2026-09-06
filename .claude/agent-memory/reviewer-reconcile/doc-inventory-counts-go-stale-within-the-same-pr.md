---
name: doc-inventory-counts-go-stale-within-the-same-pr
description: When a PR corrects a counted inventory in tech-stack.md/CLAUDE.md, re-count against the post-PR tree — the PR often adds an item to the very list it is fixing
metadata:
  type: feedback
---

Recount every "**Verified** — N workflows/projects/endpoints" claim against the
tree *as the PR leaves it*, not as it found it, and check the enumerated list
matches the count.

**Why:** on PR #355 the migration correctly dropped a stale "17 workflows"
claim in `.claude/docs/tech-stack.md` and `CLAUDE.md` down to 15 — while the
same PR added `.github/workflows/claude-hooks-tests.yml`, making the real
number 16. The correction was already false when it was written. The same file
class had, one pass earlier, produced a wrong branch-protection claim (a
`branches/main/protection` 404 read as "no protection", when two rulesets were
active — the legacy endpoint does not report rulesets). Two false claims in two
passes in the same two files.

**How to apply:** for any doc asserting a count or an enumeration, run the
`ls`/`grep` yourself against the working tree including untracked files
(`git status -s`), and diff the enumerated names against it — a count can be
right while the list is missing an entry, and vice versa. Applies equally to
"enforced by hook X" claims: check the hook is installed *and* that the defect
the doc describes wasn't fixed elsewhere in the same PR. #355 also left a
`git-advanced/SKILL.md` note documenting a greedy-`-m` defect as still open
that the same PR had fixed.
