---
name: decide
description: Capture an architectural or framework decision in the canonical format and write it to .squad/decisions/inbox/. Use when the user types `/decide [topic]` to record a deliberate choice with context, alternatives, and consequences. Pure scribe — does not dispatch any subagent.
disable-model-invocation: true
allowed-tools: Read, Write
---

# /decide — capture a decision

The lightweight version of an ADR. Writes a single decision drop to `.squad/decisions/inbox/<slug>.md` with verdict `INFO`, scope `decision`. The scribe-decision-merger hook will move it into `decisions.md` on the next `SubagentStop`.

## Arguments

- `topic` — short slug for the decision. Optional; if missing, derive from the body.

## Procedure

1. **Gather the decision content.** Ask the user (or the conversation up to this point) for these five fields. If any is unclear, ask before writing.
   - **Context.** Why is a decision needed? What's the situation? (1-3 sentences)
   - **Decision.** The choice being made. Active voice, present tense. (1-2 sentences)
   - **Alternatives considered.** What else was on the table? Why was each rejected? (1-3 bullets)
   - **Consequences.** What does this lock us into? What gets harder, what gets easier? (1-3 bullets)
   - **Owner.** Who is accountable for this decision? (Default: the user.)

2. **Slug-ify the topic.** Lowercase, hyphenate, ≤ 50 chars. If `topic` argument is missing, generate a slug from the decision text (verb + noun is usually best).

3. **Resolve references.** If the new decision supersedes or builds on an existing one, find the relevant IDs in `.claude/docs/decisions.md`. List them in the front-matter `references` field. If unclear, omit.

4. **Write the file** to `.squad/decisions/inbox/decide-<slug>.md`.

5. **Confirm to the user** with the slug and the inbox path. Tell them the next `SubagentStop` will merge it into `decisions.md`.

## File template

```markdown
---
id: lead-<utc-iso8601-compact>-decide-<slug>
agent: lead
verdict: INFO
scope: decision
created: <utc-iso8601>
targets: []
blockers: []
high: []
medium: []
good: []
references:
  - <ids of decisions this supersedes or refines>
---

# Decision: <human-readable title>

## Context
<1–3 sentences>

## Decision
<1–2 sentences, active voice, present tense>

## Alternatives considered
- **<alternative>** — <why rejected>

## Consequences
- <consequence>

**Owner:** <name or role>
```

## What this skill does NOT do

- Does not dispatch architect or any subagent. If a decision warrants architect-level analysis (Beast Mode), the user should invoke architect first and `/decide` only afterward to record the conclusion.
- Does not modify `decisions.md` directly. The hook merges; this skill writes to inbox.
- Does not push, commit, or notify external systems.

## Failure modes

- **Topic missing and conversation context insufficient to derive one:** refuse and prompt for a topic.
- **Decision contradicts an existing decision:** still write, but populate `references` and call out the contradiction in the body. The user adjudicates contradictions; the scribe doesn't.
- **Decision touches a load-bearing file** (`principles-enforcement.md`, charter frontmatter, hook scripts, settings.json, CLAUDE.md): write the decision, but flag in the body that this requires a human edit (the curator and the scribe are both forbidden from these files).
