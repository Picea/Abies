---
id: lead-20260906T150129Z-undo-redo-adr-008-left-as-is
agent: lead
verdict: INFO
scope: decision
created: 2026-09-06T15:01:29Z
targets:
  - path: docs/adr/ADR-008-immutable-state.md
    lines: "85"
blockers: []
high: []
medium: []
good: []
references: []
---

User decided to leave `docs/adr/ADR-008-immutable-state.md:85` exactly as written, declining to edit it for the `undo-redo` design pass gate 1.

## Context

`docs/adr/ADR-008-immutable-state.md:85`, in the Consequences → Positive section, states: "Undo/redo: Trivial to implement by storing state snapshots." This line is reachable by `dreamer-first-principles` as ordinary codebase reading (see the companion `flow-changelog.md` entry on the `docs/adr/` deny-list gap) and pre-answers a central open question of the `undo-redo` scope: at what level undo operates, and what the unit of a single undoable step is.

## Decision

Leave the line as written. Do not edit the ADR to shape what Track A can or cannot derive.

## Reason (the user's words)

Editing a production ADR to make the blind track derive independently would be a constructed result. Whether Track A copies the ADR or reasons past it is the experiment; convergence classifies it.

## For dreamer-convergence

Track A's read of ADR-008 is unmodified from its accepted form. When classifying Track A's output, treat agreement with ADR-008:85 as either independent derivation or reliance on the reachable text — the classification is the point of leaving the line in place, not a defect to route around.
