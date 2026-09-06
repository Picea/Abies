---
description: Start a Beast Mode design pass — a short scoping exchange, then the architect
---

# /design

Start a design pass. **This is not a bare command.** Before anything is
dispatched, hold a short scoping exchange with the user — two or three questions,
not a questionnaire.

## Why there is an exchange at all

**Knowledge is retrievable. Intent is not.**

The `architect` can harvest every constraint from the decision register, the
ADRs and the codebase. What it cannot read anywhere is *which questions this pass
exists to answer* — which parts of the solution the user wants **derived** rather
than **adopted**.

A stated preference for a particular unit of change is knowledge, and the
architect will find it. The decision to withhold that preference so the pass can
derive it is intent, and only the user has it. Skip the exchange and
`00-scope.md` gets a "Degrees of freedom" section that is empty or invented,
which is the same as running the dual track on a problem that already has its
answer written into the brief.

## The exchange

Ask two or three of these, whichever the user's opening message has not already
settled. Ask them in one message, conversationally, not as a form.

1. **What is the problem?** Not the solution they have in mind — the thing that
   is wrong now, and how they would know it was fixed.
2. **What must stay open?** Which parts of this do they want derived from the
   problem rather than adopted from what already exists? This is the question
   nothing else in the system can answer, and it is the one that becomes the
   "Degrees of freedom — deliberately open" section of `00-scope.md`.
3. **What is genuinely fixed?** Hard constraints, non-negotiables, things already
   decided. These belong in the scope stated as *requirements*, in plain
   language — never as decision ids or pattern names, which go in
   `00-knowledge.md` where Track A cannot read them.

If the user answers only the first, ask the second. Do not fill it in for them,
and do not treat "no preference" as an answer to it — "no preference" is about
the solution; the question is about the *process*.

## Then

Dispatch `architect` with the problem statement and the answers, and say
explicitly which answers came from the user rather than from your own reading.
The architect chooses the fast path or a deep pass and hands back a phase plan.

For a **deep pass**, the sequence is in `CLAUDE.md` § 3, and gate 1 comes first:
`scope-warden.sh` writes `00-warden-scan.md` when the architect stops, you
dispatch `scope-warden`, and the user answers gate 1 **before** either Dreamer
track is dispatched.

For a **fast path**, the architect returns a plan directly. Say so, and say why
it chose that path — the choice is auditable, not silent.

## What this command does not do

- Decide the direction. Five gates in the pass belong to the user.
- Pick the path. That is the architect's, stated with its reason.
- Skip the exchange because the request looks clear. A clear request is exactly
  the one where the unstated intent is expensive to lose.
