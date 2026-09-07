# 🛡️ Scope Warden — mechanical scan — undo-redo

Written by `.claude/hooks/scope-warden.sh` on `SubagentStop`. This is the
deterministic half of gate 1: what can be found by matching. The fourth
category — prior work presented as reference material — is a judgement call
and belongs to the `scope-warden` subagent, which reads this file and writes
`00-warden.md`. Nothing here rewrites the scope.

**Checked:** `.squad/design/undo-redo/00-scope.md` (254 lines)
**Lexicon:** `.claude/docs/pattern-lexicon.md` — 77 terms, 20 recall phrases
**Verdict:** CLEAN

## Decision ids

_None found._

## Pattern names and recall grammar

_None found._

## Paths Track A may not read

_None found._

## Not checked here

**Prior work presented as reference material.** No regex separates
*"transitions must be total"* from *"we solved this with a state machine
last time"*. Dispatch `scope-warden` for that category and for the gate
question.

