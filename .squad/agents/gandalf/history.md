# Gandalf — Architect

## Project Context
- User: Maurice Cornelius Gerardus Petrus Peters
- Project: Abies (Tolkien legendarium theme)
- Created: 2026-03-19

## About This File
This file captures project-specific learnings from the Architect's work. It grows over time as the Architect discovers patterns, makes decisions, and extracts generalizations. Read this before every session.

## Patterns Discovered
*None yet — this file will grow as the squad works together.*

- For Abies ecosystem features, split delivery into conventions-first and kit-second phases to reduce rework and keep APIs cohesive.
- For UI library initiatives, enforce package boundaries early: core component package plus separate demo/documentation package.
- For issue kickoffs, publish an execution-plan artifact that binds component-level include/defer boundaries to phase checklists before implementation begins.

## Generalizations Extracted
*None yet.*

- Pure-function `Node` component libraries scale better when API contracts include accessibility behavior as first-class requirements.
- Tokenized theming with CSS custom properties should be defined once per kit and consumed consistently by every component.

## Decisions Made
*Refer to `.squad/decisions.md` for team-wide decisions. This section tracks Architect-specific reasoning.*

- Issue #152 (`Picea.Abies.UI`) should use a phased roadmap that delivers seven baseline components and a demo app under explicit WCAG 2.1 AA gates.
- Issue #152 kickoff should treat `docs/guides/abies-ui-issue-152-execution-plan.md` as the implementation source-of-truth contract for owners, phases, boundaries, and CI gates.
- Issue #152 execution contract was locked with explicit v1 include/defer boundaries, accessibility verification matrix, and concrete merge/release CI gate mapping.

## Revisit Triggers
| Decision | Trigger Condition | Last Checked |
|---|---|---|
| *None yet* | | |

## Mistakes & Lessons
*None yet — and that's a good thing. But it won't last.*

## PR Workflow Learnings
- Enforce PR template completeness early (What/Why/How, testing evidence, checklist) before requesting review.
- Require Conventional Commits titles at creation time to avoid late-stage CI/title-check failures.
- Keep formatting fixes scoped with `dotnet format --include` when PRs are already in review.