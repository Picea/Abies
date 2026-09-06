---
name: review
description: Run a code review on the current diff (against the merge base) by dispatching the split review pair — `reviewer-blind` then `reviewer-reconcile` — and writing a verdict to .squad/decisions/inbox/. Use when the user types `/review` to gate a change before merge.
disable-model-invocation: true
allowed-tools: Read, Glob, Grep, Bash(git*)
---

# /review — gated code review

This is the executable form of Missing Review Lockout. The orchestrator runs
this skill on `/review` and dispatches the review pair — `reviewer-blind`, then
`reviewer-reconcile` — against the current diff. **Always both agents, always
in that order.**

## Procedure

1. **Determine the merge base.**
   - If the current branch is `main` or `master`, the user is likely reviewing committed changes — diff against `HEAD~1`.
   - Otherwise, find the upstream/base branch:
     - `git rev-parse --abbrev-ref @{u}` if upstream is set
     - else default to `origin/main` (or `origin/master`)
   - Compute the diff: `git diff --merge-base <base> -- :^node_modules :^bin :^obj`.
   - Files included: only those tracked and modified.

2. **Dispatch `reviewer-blind` first.** Build its brief from **code context
   only**: the diff, the list of changed files with line counts, the branch or
   commit range, and the slug (if this change has a design pass behind it).

   Do not include the PR description, the linked issue, the plan, the critic's
   assessment, or any other narrative — `reviewer-blind` forms its own reading
   before any of that exists. `.squad/design/` and raw `git log`/`git show`/
   `git blame`/`gh pr view`/`gh issue view` are denied to it by hook, and it has
   no `memory:`; the one thing the hooks cannot stop is you quoting narrative
   into its prompt, so don't. It writes `.squad/design/<slug>/08-review-blind.md`
   and stops — it issues no verdict.

3. **Wait for `08-review-blind.md` to exist**, then dispatch `reviewer-reconcile`.
   Its brief may now include everything: the plan, the critic's accepted risks,
   the spec, the PR description, the issues, and references to load —
   `.claude/agents/reviewer-blind.md`, `.claude/agents/reviewer-reconcile.md`,
   `.claude/skills/code-review/SKILL.md`, `.claude/docs/principles-enforcement.md`,
   `.claude/docs/decisions.md`, `.claude/docs/decision-schema.md`. It reads `08`
   first, then the narrative, treating all of it as claims to verify — not fact.
   `enforce-phase-order.sh` refuses `09-review-verdict.md` while `08` is absent,
   and `reviewer-reconcile` refuses to run a blind pass itself; it has already
   read this prompt, so it cannot be the independent reader.

   `reviewer-reconcile` writes:
   - `.squad/design/<slug>/09-review-verdict.md`
   - `.squad/decisions/inbox/review-<sha-or-pr-id>.md`, conforming to the schema
     in `.claude/docs/decision-schema.md`, carrying a `commit:` field with the
     full 40-hex `HEAD` of the checkout it reviewed

4. **Wait for the verdict.** Do not synthesize a verdict yourself — neither
   half of the pair is optional, and `reviewer-reconcile` is the terminal node.

5. **Surface the verdict** to the user with a short summary:
   - The verdict label (PASS / NEEDS-CHANGES / BLOCKED / INFO)
   - The count of blockers, high, medium findings
   - The path to the full review (`09-review-verdict.md`)

   The `scribe-decision-merger` hook moves the inbox file to `decisions.md` on
   `SubagentStop` and, once the drop's `commit:` field matches the destination's
   actual `HEAD`, derives `.squad/.last-review-verdict` from it for the
   statusline. That file has exactly one writer — the merger hook. Neither half
   of the review pair writes it, and no agent should attempt to.

## Inputs

This command takes no arguments. Reviewing a specific commit, range, or file is out of scope — it always reviews the working diff vs. merge base. (For arbitrary commits, the user should invoke the review pair directly with a custom brief.)

## Re-review after fixes

Dispatch `reviewer-reconcile` alone, with a targeted brief describing what
changed and which findings it addresses. **`reviewer-blind` does not re-run.**
Its value is that its reading was formed before the narrative existed; a second
pass, run after the first review's findings are already known, would not have
that property. `reviewer-reconcile` re-reads the original `08-review-blind.md`
to keep that independent reading in view alongside the fix.

## What this skill does NOT do

- Does not write or edit code.
- Does not bypass either half of the review pair, and does not let
  `reviewer-reconcile` substitute itself for a missing `reviewer-blind` pass.
- Does not produce a verdict in the orchestrator's voice.
- Does not auto-merge, push, or alter git state.
- Does not treat `08-review-blind.md` as an approval — it is a reading, not a
  verdict. Acting on it alone triggers the Missing Review Lockout.

## Failure modes

- **No diff** (working tree matches merge base): inform the user there is nothing to review and exit.
- **Detached HEAD**: refuse with a clear message ("merge base is ambiguous; check out a feature branch first").
- **More than 5000 changed lines**: warn the user, then summarise per-file rather than feeding the entire diff.
- **No upstream and no `origin/main` or `origin/master`**: ask the user which base to use.
- **`08-review-blind.md` missing when `reviewer-reconcile` is dispatched**: this is a sequencing bug in the orchestrator, not a reviewer decision — dispatch `reviewer-blind` and wait before retrying.

## Reference

The review pair's full procedures live in `.claude/agents/reviewer-blind.md` and
`.claude/agents/reviewer-reconcile.md`, with the pattern catalog in
`.claude/skills/code-review/SKILL.md`. This skill is just the dispatch wrapper.
