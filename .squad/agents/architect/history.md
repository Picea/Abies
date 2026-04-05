# Architect — History

## About This File

This file captures project-specific learnings from the Architect's work. It grows over time as the Architect discovers patterns, makes decisions, and extracts generalizations. Read this before every session.

## Patterns Discovered

*None yet — this file will grow as the squad works together.*

## Generalizations Extracted

*None yet.*

## Decisions Made

*Refer to `.squad/decisions.md` for team-wide decisions. This section tracks Architect-specific reasoning.*

## Revisit Triggers

| Decision | Trigger Condition | Last Checked |
| -------- | ----------------- | ------------ |
| *None yet* | | |

## Mistakes & Lessons

*None yet — and that's a good thing. But it won't last.*

## Learnings

- 2026-03-23: For issue 160, debugger architecture must be anchored to `Runtime.cs` seams (`_apply`, `InterpretCommand`, `SubscriptionManager.Start/Update`, `navigationExecutor`) so replay gating is enforceable in the core runtime, while JavaScript remains adapter-only and debugger domain stays in `Picea.Abies`.
- 2026-04-04: `Program<TModel, TArgument>` in Abies is currently an Automaton-facing MVU contract (Initialize/Transition/View/Subscriptions), while explicit `Decider<...>` is already used in Conduit domain aggregates. Architecturally, "Program should be a decider" should be implemented as a staged bridge (decider semantics + compatibility adapter) rather than an immediate hard replacement, to avoid breaking runtime and documentation contracts.
- 2026-04-04: Architecture review confirms the correct direction is staged convergence, not direct replacement: keep Program compatibility while introducing decider-native seams behind runtime abstractions, then cut over only after parity tests, docs/templates migration, and performance gates pass.
- 2026-04-04: User directive supersedes staged convergence for Program-to-Decider: execute full decider migration now, allow breaking changes, and remove temporary compatibility behavior/shims. Migration target is recorded in `.squad/decisions/inbox/architect-full-decider-migration-target.md` and is the active architecture contract.
