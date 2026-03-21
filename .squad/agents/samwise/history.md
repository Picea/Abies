# Samwise — Tester

## Project Context
- User: Maurice Cornelius Gerardus Petrus Peters
- Project: Abies (Tolkien legendarium theme)
- Created: 2026-03-19

## Learnings

- Record and report MacBook power state (plugged in vs battery) with every benchmark run.
- Do not compare benchmark results across different power states; re-baseline in the same session if needed.
- Treat js-framework-benchmark as source of truth for user-visible performance validation.
- Issue #152 test gates: enforce component test pyramid with WCAG 2.1 AA + keyboard coverage and deterministic focused E2E before merge.
- For early `Picea.Abies.UI` component tests, lock in the current rendered HTML contract with deterministic attribute assertions and avoid testing deferred behavior from TODO follow-ups.
