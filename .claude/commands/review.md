---
description: Run the split code review — reviewer-blind, then reviewer-reconcile
---

# /review

Review code-touching work. **Always both agents, always in this order.**

## 1. `reviewer-blind`

Dispatch first. It forms its own reading of what the code does and what is wrong
with it, writes `.squad/design/<slug>/08-review-blind.md`, and stops. It issues
no verdict.

`.squad/design/` and raw `git log`/`git show`/`git blame`/`gh pr view`/
`gh issue view` are denied to it by hook, and it has no `memory:`.

**Your one job here is not to undo that by quoting.** The hooks cannot stop you
pasting the PR description, the issue text, the plan, or "the author says this
fixes the N+1" into its prompt. Give it the scope, the branch or commit range,
and the slug. Nothing else. This is the residue at the edge of every context
boundary in this design, and it is yours.

## 2. `reviewer-reconcile`

Dispatch after `08-review-blind.md` exists. It reads that file **first**, then
the plan, the critic's accepted risks, the spec, the PR description and the
issues — all of it as **claims to verify**. It runs the eleven dimensions,
reconciles against the Critic's accepted risks, and writes:

- `.squad/design/<slug>/09-review-verdict.md`
- `.squad/decisions/inbox/review-<sha-or-pr-id>.md`, carrying a `commit:` field
  with the full 40-hex `HEAD` of the checkout it reviewed

It does **not** write `.squad/.last-review-verdict` itself — that file is
deny-by-default for every agent. It is currently in a CI-6 shadow period that
logs to `.squad/log/gate-shadow.md` and allows instead of refusing until
`expires:` in `.squad/.gate-shadow` (2026-09-19), after which no action is
required for it to enforce. `scribe-decision-merger.sh` writes it from the
drop's `commit:` field on `SubagentStop`, once that field matches the
destination's actual `HEAD`; see `.claude/agents/reviewer-reconcile.md`.

`enforce-phase-order.sh` refuses `09-review-verdict.md` when `08` is absent, and
`reviewer-reconcile` refuses to run a blind pass itself — it has already read
this prompt, so it cannot be the independent reader.

## 3. Surface the verdict

Relay it to the user. On 🔴 Must Fix, the author is locked out per the Reviewer
Rejection Protocol; route the fixes to a different specialist or escalate.

On re-review after fixes, dispatch `reviewer-reconcile` alone with a targeted
brief. **`reviewer-blind` does not re-run** — its value is that its reading was
formed before the narrative existed, and a second pass would not have that
property.

## What this command does not do

- Approve anything itself. `reviewer-reconcile` holds the verdict; the Lead's
  lightweight-review authority does not extend to code.
- Treat `08-review-blind.md` as an approval. It is a reading, not a verdict.
  Acting on it alone triggers the Missing Review Lockout.
