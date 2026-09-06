# 00-scope.md — `undo-redo`

> Save to `.squad/design/undo-redo/00-scope.md`.
>
> **Before running:** fill the Knowledge Scan from `decisions.md` and architect `MEMORY.md`. Everything below it is written to stand alone for `dreamer-first-principles`, which reads this file and the codebase and nothing else.

---

## 📚 KNOWLEDGE SCAN

**Related patterns:** _[from architect MEMORY.md]_
**Related decisions:** _[ids from decisions.md that constrain or inform this]_
**Related sessions:** _[the time-travel debugging work — link the decision id]_
**Revisit triggers:** _[any that may have fired]_
**Implication:** _[how that prior work shapes this]_

---

## Problem Statement

Abies applications must support undo and redo across the whole application state.

The application runs on a single `Picea` kernel. All state changes flow through one transition function. Undo and redo are therefore application-wide, not per-component: one history for the whole running application, not a separate history owned by each part of the UI.

A time-travel debugging capability already exists in the codebase. It is the starting point for this work, not something to replace or duplicate. Any design must say how it relates to what is already there.

### Invariants

Any valid solution must satisfy all of these.

1. **The transition function remains the only place where application state changes.** No design may introduce a second path that mutates state directly.
2. **Undo may not produce a state that ordinary interaction could not have reached.** Every state the user can land on by undoing must be a state the application could have been in.
3. **Redo immediately following an undo returns the application to the state it was in before that undo.** The pair is symmetric with no observable difference.
4. **Effects are not part of the state machine.** The kernel is pure; effects are executed outside it, at the interpreter boundary. A design must state explicitly what happens at that boundary during undo — including what happens to effects that have already left the system and cannot be recalled. Ignoring, refusing, or compensating are all admissible answers; saying nothing is not.

### Degrees of freedom — deliberately open

These are the questions this pass exists to answer. Do not treat any of them as settled.

- **At what level does undo operate?** The history could be built from any of several things that pass through the system. Deriving which one, and why, is part of the design.
- **What is retained, and how much?** History has a memory cost that grows with session length. Whether it is bounded, and how, is open.
- **What happens when the user acts after undoing?** The forward history can be discarded, kept, or something else. Justify the choice.
- **Which parts of the application state, if any, are exempt from undo?** Not all state is necessarily user-owned.

### Done means

- A design exists that satisfies all four invariants and answers all four open questions with reasoning, not preference.
- The relationship to the existing time-travel debugging capability is explicit: reuse, extension, or separation, with a stated reason.
- The design states its memory characteristics for a long-running session.

---

## Phase Plan

Full pass. All phases run:

```
architect                → 00-scope.md          (this file)
dreamer-first-principles → 01-track-a.md ⎫ parallel,
dreamer-informed         → 02-track-b.md ⎭ isolated contexts
dreamer-convergence      → 03-convergence.md    🛑
realist                  → 04-realist-plan.md   🛑
critic                   → 05-critic.md         🛑
spec-author              → 06-spec.md           🛑
architect                → 07-handoff.md
```

No skips. `spec-author` runs: this is new behaviour, not a refactor.

---

## Dynamic Weighting

**Track A weighted heavily.** The problem has a well-known conventional answer and a codebase whose structural properties may make that answer unnecessary. This is exactly the case where layering the conventional approach risks accidental complexity on top of a foundation that already provides most of what is needed. Track A must derive from the invariants without reaching for the familiar shape.

**Track B is not a strawman.** The conventional answer is production-proven and widely deployed; argue it from evidence at full strength. Convergence is only meaningful if both tracks are genuinely good.

**Critic weighted normally.** No time pressure, no incident.

---

## Expert Rooms to Summon

- 🎨 **UX** — real subagent. Undo and redo are user-facing affordances with established expectations; what counts as one undoable step is a UX question before it is a technical one.
- 🧪 **Test Strategy** — in-room. Invariants 2 and 3 are property-shaped and should be testable as such.
- ⚡ **Performance** — real subagent if any candidate retains unbounded history. Memory growth over a long session is a budget question.
