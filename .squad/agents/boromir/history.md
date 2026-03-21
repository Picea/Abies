# Boromir History

## Project Context
- Project: Abies
- User: Maurice Cornelius Gerardus Petrus Peters
- Added: 2026-03-20
- Focus: Documentation quality, information architecture, docs-as-code lifecycle.

## Learnings
- Initial assignment created.
- Performance documentation must always include benchmark power state (plugged in vs battery) and same-session comparability notes.
- Benchmark summaries should treat js-framework-benchmark results as source of truth for user-visible performance claims.
- Terminal/runbook docs should prefer log-redirection patterns for long-running commands and avoid fragile pipe-based examples.
- PR-related docs must mirror repository standards: Conventional Commits examples, PR template structure, and required CI/test evidence expectations.
- Changelog and release notes should describe user-visible outcomes from PRs, not internal process details.
- UI documentation rule: reference Abies brand palette for non-Conduit UI guidance, with explicit notes that Conduit docs keep existing visual conventions.
- 2026-03-20: Issue #152 docs must keep explicit v1 include/defer boundaries, per-component keyboard/ARIA behavior tables, and CI/release gate mapping in sync with implementation criteria.
- 2026-03-20: Added `docs/guides/abies-ui-component-library-conventions.md` with package split guidance (`Picea.Abies.UI` + `Picea.Abies.UI.Demo`), immutable options + pure Node API contract, token-first theming, WCAG 2.1 AA verification expectations, SemVer/release gate mapping, and contributor checklist aligned to issue #152 decisions.
