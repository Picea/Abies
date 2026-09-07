---
name: a-confirmation-pass-invalidates-its-own-cache
description: Committing a commit-boundary confirmation's own artifacts moves HEAD and staleness the verdict cache it just produced — say where to stop before the pass ends
metadata:
  type: project
---

A commit-boundary confirmation pass (review a working tree → commit it → confirm
the committed tree) produces artifacts of its own: the appended
`09-review-verdict.md`, the merged `decisions.md` entry, the archived decision
drop, and the session log. **Committing those artifacts moves HEAD, and
`.squad/.last-review-verdict` then names the parent, not HEAD — so the very
merge the pass existed to unblock is blocked again.**

**Why:** `enforce-review-verdict.sh` scopes its *commit* branch to code-shaped
paths (`.cs`, `.sh`, `*.yml`, `.claude/agents/*.md`, …), so a docs-only artifact
commit sails through. But its *push* and *merge* branches have no such scoping:
`want_sha="$head_sha"` unconditionally, and the cache's `commit:` must equal
`git rev-parse HEAD`. So the artifact commit is permitted and then strands the
push. Each confirmation pass generates the artifacts that invalidate it — run it
twice and you have run it forever.

**How to apply:** when a pass ends with a confirmed PASS at sha X, state
explicitly in the final message that **push/merge must happen at X**, and that
the confirmation artifacts should be left uncommitted (or folded into a later
housekeeping commit made *after* the merge). If the orchestrator does commit
them, that is not a bug and not a finding — it just costs one more confirmation
pass, and saying so up front is cheaper than discovering it at the merge.

Verify before relying on this: `sed -n '236,262p'
.claude/hooks/enforce-review-verdict.sh` for the `want_sha` / `what` block, and
the `if [ "$KIND" = "commit" ]` code-shaped `case` list just above it.

Related: [[verify-a-registration-pass-by-scope-not-substance]] (the `find -newer`
scope check that shows *which* post-verdict files are hook state),
[[last-review-verdict-is-forgeable]] (what the cache does and does not prove).
