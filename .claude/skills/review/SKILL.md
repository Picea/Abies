---
name: review
description: Run a code review on the current diff (against the merge base) by dispatching the `reviewer` subagent and writing a verdict to .squad/decisions/inbox/. Use when the user types `/review` to gate a change before merge.
disable-model-invocation: true
allowed-tools: Read, Glob, Grep, Bash(git*)
---

# /review — gated code review

This is the executable form of Missing Review Lockout. The orchestrator runs this skill on `/review` and dispatches the `reviewer` subagent against the current diff.

## Procedure

1. **Determine the merge base.**
   - If the current branch is `main` or `master`, the user is likely reviewing committed changes — diff against `HEAD~1`.
   - Otherwise, find the upstream/base branch:
     - `git rev-parse --abbrev-ref @{u}` if upstream is set
     - else default to `origin/main` (or `origin/master`)
   - Compute the diff: `git diff --merge-base <base> -- :^node_modules :^bin :^obj`.
   - Files included: only those tracked and modified.

2. **Build the reviewer brief.** Prepare a context bundle that includes:
   - The diff (truncated to ~1500 lines; if larger, provide a file-level summary plus per-file diffs on demand).
   - The list of changed files with line counts.
   - References to load: `.claude/agents/reviewer.md`, `.claude/skills/code-review/SKILL.md`, `.claude/docs/principles-enforcement.md`, `.claude/docs/decisions.md`, `.claude/docs/decision-schema.md`.
   - The current git branch and recent commits.

3. **Dispatch the `reviewer` subagent** via the Agent tool with the brief above. The reviewer must produce a single decision drop at `.squad/decisions/inbox/review-<branch-or-sha>.md` conforming to the schema in `.claude/docs/decision-schema.md`.

4. **Wait for the verdict.** Do not synthesize a verdict yourself. The reviewer is the terminal node.

5. **Surface the verdict** to the user with a short summary:
   - The verdict label (PASS / NEEDS-CHANGES / BLOCKED / INFO)
   - The count of blockers, high, medium findings
   - The path to the full review

   The scribe-decision-merger hook will move the inbox file to `decisions.md` on `SubagentStop` and update `.squad/.last-review-verdict` for the statusline.

## Inputs

This command takes no arguments. Reviewing a specific commit, range, or file is out of scope — it always reviews the working diff vs. merge base. (For arbitrary commits, the user should invoke the reviewer subagent directly with a custom brief.)

## What this skill does NOT do

- Does not write or edit code.
- Does not bypass the reviewer subagent.
- Does not produce a verdict in the orchestrator's voice.
- Does not auto-merge, push, or alter git state.

## Failure modes

- **No diff** (working tree matches merge base): inform the user there is nothing to review and exit.
- **Detached HEAD**: refuse with a clear message ("merge base is ambiguous; check out a feature branch first").
- **More than 5000 changed lines**: warn the user, then summarise per-file rather than feeding the entire diff.
- **No upstream and no `origin/main` or `origin/master`**: ask the user which base to use.

## Reference

The reviewer's full procedure lives in `.claude/agents/reviewer.md` and `.claude/skills/code-review/SKILL.md`. This skill is just the dispatch wrapper.
