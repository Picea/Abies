---
name: handoff
description: Generate a structured handoff bundle for delegating work to a specific subagent — captures current state, change summary, open questions, and the next concrete instruction. Use when the user types `/handoff [target-agent] [task-slug]` (or `/handoff` to be prompted for arguments).
disable-model-invocation: true
allowed-tools: Read, Glob, Grep, Bash(git status*), Bash(git log*), Bash(git diff --stat*), Write
---

# /handoff — explicit work handoff

Captures whatever the orchestrator session knows about a task and writes it to `.squad/decisions/inbox/handoff-<slug>.md` (verdict `INFO`, scope `handoff`). The receiving subagent reads the file as the first step of its work.

## Arguments

- `target-agent` — one of: `architect`, `reviewer`, `csharp-dev`, `js-dev`, `tech-writer`, `security-expert`, `performance-engineer`, `devops`, `ux-expert`, `curator`. Required.

  The design phase agents (`dreamer-first-principles`, `dreamer-informed`, `dreamer-convergence`, `realist`, `critic`, `spec-author`) are **not** valid targets. They take their context from `.squad/design/<slug>/` artifacts, not from handoff bundles — hand off to `architect` to open or resume a design pass instead.
- `task-slug` — short hyphenated identifier for the task (≤ 50 chars). Required.

If either is missing, ask the user.

## Procedure

1. **Snapshot state:**
   - Branch, head SHA, dirty-file list (`git status --porcelain`).
   - Recent commits relevant to the task (`git log --oneline -10`).
   - Diff summary (`git diff --stat <merge-base>..HEAD`) if applicable.

2. **Summarise current state.** In ≤ 5 bullets:
   - What's done.
   - What's in flight.
   - What's blocked or unclear.
   - Test status (pass/fail/unrun) — only if you can determine cheaply.

3. **List the changes.** Per modified file, one line: `<path> — <one-sentence what changed>`.

4. **Pose the open questions.** ≤ 3 questions, each pointing to a file or decision. If you have no real questions, omit this section — don't manufacture them.

5. **Write the next concrete instruction** for the target agent. One sentence. Action-oriented. References specific files when possible.

6. **Identify relevant decisions.** Grep `.claude/docs/decisions.md` for the slug or related keywords; list the matching decision IDs in the front-matter `references`.

7. **Write the file** to `.squad/decisions/inbox/handoff-<target>-<slug>.md` with the canonical front-matter (verdict `INFO`, scope `handoff`, agent `lead`).

## File template

```markdown
---
id: lead-<utc-iso8601-compact>-handoff-<target>-<slug>
agent: lead
verdict: INFO
scope: handoff
created: <utc-iso8601>
targets:
  - path: <file>
blockers: []
high: []
medium: []
good: []
references:
  - <related decision ids>
---

# Handoff to `<target-agent>` — <slug>

**State:** branch `<branch>` at `<sha>`, <n> uncommitted file(s).

## Summary
- <bullet 1>
- <bullet 2>

## Changes
- `<path>` — <what changed>

## Open questions
- <question 1>

## Next step
<one-sentence imperative for the target agent>
```

## What this skill does NOT do

- Does not invoke the target agent — it produces the brief, the orchestrator dispatches separately. (Reason: handoff bundles are durable context; agents may run later or in a different session.)
- Does not commit or push.
- Does not modify code.

## Failure modes

- **Target agent not in the roster:** refuse and list valid targets.
- **Slug already exists in inbox:** suffix with timestamp.
- **No diff or commits:** still write a handoff with state="no changes yet" — pre-work handoffs are valid.
