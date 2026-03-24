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
|---|---|---|
| *None yet* | | |

## Mistakes & Lessons
*None yet — and that's a good thing. But it won't last.*

## Learnings
- 2026-03-23: For issue 160, debugger architecture must be anchored to `Runtime.cs` seams (`_apply`, `InterpretCommand`, `SubscriptionManager.Start/Update`, `navigationExecutor`) so replay gating is enforceable in the core runtime, while JavaScript remains adapter-only and debugger domain stays in `Picea.Abies`.
