---
name: retro
description: Run a retrospective on the most recent session log and produce a draft of candidate learnings for the curator to review. Use when the user types `/retro` (or `/retro [N]` to span the last N session logs).
disable-model-invocation: true
allowed-tools: Read, Glob, Write
---

# /retro — session retrospective

Reads the latest session log (or the last N if specified) and produces a structured retrospective draft to `.squad/learnings/inbox/retro-<date>.md`. The curator subagent will review the draft on its next run and decide which entries earn promotion.

## Arguments

- `N` (optional) — integer, number of most-recent session logs to scan. Default: 1.

## Procedure

1. **Locate the session logs.** Glob `.squad/log/*-session.md`, sort by date (filename), take the last `N`.

2. **Read each log.** Each log has timestamped entries written by the `session-logger` hook on `SubagentStop`. Pay attention to:
   - Repeated agent invocations on the same area (signal of friction).
   - Reviewer NEEDS-CHANGES verdicts followed by re-reviews (signal of recurring issues).
   - Architect or security-expert decisions (signal of new conventions to record).
   - Quarantined drops (signal of schema drift or agent confusion).

3. **Identify candidates.** Categorise each into one of:
   - **Worked well** — patterns that delivered value; might warrant promotion to a skill.
   - **Hit friction** — recurring blockers that consumed time; might warrant a new convention or hook.
   - **One-off** — happened, doesn't repeat, no action needed.
   - **Open question** — something that needs deliberate decision, not retro material.

4. **Apply the curator's filter.** A candidate must satisfy *all three* before going into "promote":
   - Recurrent (≥ 2 distinct prior incidents in the scanned window — if only 1, mark "needs more evidence")
   - Specific (a reviewer can point at code/config and say pass/fail)
   - Generalisable (would have applied to the previous incidents, not just the latest)

   Reference: `.claude/skills/learnings-curation/SKILL.md`.

5. **Write the retro draft** to `.squad/learnings/inbox/retro-<YYYY-MM-DD>.md`.

6. **Confirm to the user** with the path and a one-line summary: "<n> candidates flagged: <m> recommend promotion, <k> need more evidence, <p> noise".

## File template

```markdown
---
id: lead-<utc-iso8601-compact>-retro-<YYYY-MM-DD>
agent: lead
verdict: INFO
scope: retro
created: <utc-iso8601>
targets: []
blockers: []
high: []
medium: []
good: []
references: []
---

# Retrospective — <window described>

Scanned <n> session log(s): <list of log filenames>.

## Worked well
- <observation>. Evidence: <log entry refs>. **Promote candidate?** yes | needs more evidence | noise.

## Hit friction
- <observation>. Evidence: <log entry refs>. **Promote candidate?** yes | needs more evidence | noise.

## Open questions
- <question>. (Routes to: architect | reviewer | user.)

## Promote candidates
For each "yes" above, propose a target file and change kind:
- **<observation>** — target: `.claude/docs/decisions.md` (Functional DDD section). change_kind: add. Evidence: <≥ 2 references>.
```

## What this skill does NOT do

- Does not directly edit `decisions.md`, skills, or charters.
- Does not invoke the curator subagent. The user invokes the curator separately when they want proposals to merge.
- Does not summarize the *current* (in-progress) session — it reads completed logs only.
- Does not delete or modify existing log files.

## Failure modes

- **No session logs found:** report empty and exit. Likely the session-logger hook has not yet fired in this project.
- **Window too narrow:** if only 1 log exists and N>1 is requested, scan what's available and note the discrepancy.
- **Log files corrupt or unreadable:** skip them with a warning; don't fail the whole retro.
