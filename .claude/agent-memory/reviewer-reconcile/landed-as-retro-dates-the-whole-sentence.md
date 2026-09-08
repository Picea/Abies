---
name: landed-as-retro-dates-the-whole-sentence
description: When a plan edit adds "Landed as PR #N, merge commit <sha>" to a passage of predictions, every surviving claim in that passage becomes an as-landed assertion — check the PR's real size and gh pr checks against them
metadata:
  type: feedback
---

A reconciliation edit that inserts an as-landed anchor — a PR number, a merge
commit — into a passage written as a *plan* converts the whole passage. Every
neighbouring prediction is now read as a record of what happened. **Check each
one against the PR, not against the plan's intent.**

The two that pay off, both one command:

```
gh pr view <N> --json additions,deletions,changedFiles
gh pr checks <N>          # the gate the passage claims to be under
```

**Why:** on `undo-redo-followups` the realist correctly fixed what review
finding ⚠️-14 asked for (one csproj item → two) and appended *"Landed as PR
#361, merge commit 70d9ae5…"* to the PR-cut row. The rest of that row still
read *"Three files, no framework code, well under the line gate"* and the
precondition block still read *"the approval commit is X + Y and nothing
else"*. PR #361 was **40 files / 7,669 changed lines**, and `gh pr checks 361`
reported **`Check PR Size  fail`** against `pr-validation.yml`'s
`hardLimit = 1500` — the `maintenanceOnly` exemption cannot apply, because
`files.every(isMaintenancePath)` is false the moment a `.cs` or `.csproj` is in
the set. The approval commit carries 37 files beyond the two named. The
reconciliation was scoped to the item count and stopped there.

**How to apply:** on any pass whose job is *make the record true of what
landed*, treat the scope you were given as the **entry point**, not the extent.
Read the full sentence and the full block around every edit site. State the
finding as a rule — *every claim in a passage this edit anchors to a merge
commit must be reconciled or explicitly marked as-planned* — because an
enumeration of instances is read as the criterion and the next round comes back
with the fourth instance unfixed.

Adjacent: the realist's own lesson from the same incident was that an exception
named by element has **four** sites and you must grep the element name to find
them. Mine is the complement — the sites are right, the *sentences around them*
are not.

Related: [[a-blocker-list-is-not-the-criterion]],
[[doc-inventory-counts-go-stale-within-the-same-pr]],
[[inherited-or-introduced-decides-the-verdict]].
