---
description: Consolidate session learnings — curator, then curator-adversary
---

# /curate

Turn accumulated session evidence into framework proposals. **Both agents, in
this order.** Runs only when the user asks; never proactively.

## 0. Scope the window

The curator refuses "summarise everything we learned" — and it is right to.
Establish a window first: a date range, a set of sessions, a specific thread.
If the user has not given one, ask.

## 1. `curator`

Dispatch with the window. It reads `.squad/log/`,
`.squad/decisions/archive/<YYYY-MM>/`, `.squad/orchestration-log/` and
`.squad/log/lexicon-hits.md`, and writes proposals to
`.squad/learnings/inbox/`.

Three tests for a genuine learning: **recurrent** (two prior incidents is the
floor), **specific** enough to act on, and **generalisable** to the earlier
incidents rather than just the latest.

It never edits a live file, with one exception: `.claude/docs/pattern-lexicon.md`
is its own, because that file is data for a check rather than a rule for an
agent, and it has to move in both directions — narrowing an over-firing term on
the evidence of the hit log is not a proposal, it is maintenance.

The honest part of its output is what it noticed and deliberately did **not**
propose. Surface that too.

## 2. `curator-adversary`

Dispatch after the inbox is written. One batched pass over every proposal, one
counter-argument each: **when would this rule be wrong, what does it cost, who is
disadvantaged by it.**

It exists as a separate context because an agent arguing against its own proposal
argues weakly — the same self-assessment problem `dreamer-convergence` exists to
avoid. It has no `memory:`, deliberately: an adversary that remembers which
proposals it liked is no longer adversarial.

It is **not** folded into `critic`. That memory is the codebase's failure
dossier, and diluting it with framework-governance findings would cost more than
the agent saves.

## 3. Surface both, get explicit decisions

Present each proposal beside its counter-argument. The user accepts or rejects
each one **explicitly** — silence is not acceptance.

## 4. Apply and archive

For each accepted proposal: apply it **manually** to its target
(`decisions.md`, `tech-stack.md`, a skill), then move the proposal file to
`.squad/learnings/archive/<YYYY-MM>/`. Move rejected proposals there too, with
the rejection recorded — a rejected proposal is evidence about the framework's
boundaries and is worth as much as an accepted one.

The merge is a separate deliberate step. The curator is forbidden from applying
a proposal itself, and so are you until the user has said yes.

## Load-bearing files

If the curator flags a concern about `principles-enforcement.md`, a charter in
`.claude/agents/`, a hook script, `.claude/settings.json` or `CLAUDE.md`, it is
forbidden from proposing a change to it. That flag is for the user to consider —
surface it as a concern, not as a proposal, and do not draft the edit.

## Why this is gated so heavily

Frameworks rot by accretion. Every incident feels like it deserves a rule, and a
hundred rules is the same as none. The curator exists to say no.
