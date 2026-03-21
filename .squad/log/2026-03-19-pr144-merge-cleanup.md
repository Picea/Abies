# 2026-03-19 — PR #144 Merged: ci: Add required benchmark regression check

**Date:** 2026-03-19
**Branch:** `ci/benchmark-required-check`
**PR:** #144
**Author:** Maurice Peters

---

## Topic

PR #144 merged — adds the benchmark regression check as a required CI status check.

---

## Key Events

1. **PR created with template** — PR #144 opened from `ci/benchmark-required-check` following the project PR template (What / Why / How / Testing sections).

2. **Copilot review feedback addressed** — Copilot flagged that the benchmark required-check workflow needed a skip condition for non-PR triggers; the `if: github.event_name == 'pull_request'` guard was added to prevent CI failures on direct pushes.

3. **PR title fixed to uppercase subject convention** — Original title lacked the Conventional Commits uppercase subject format; corrected to `ci: Add required benchmark regression check` with uppercase first letter after the colon.

4. **Branch merged into main** — All required checks passed (build, e2e, codeql, cd, benchmark); branch merged.

---

## Directives Captured

- **Always use the PR template** when creating pull requests (recorded 2026-03-19T16:00:37Z).
- **PR titles must use Conventional Commits with uppercase subject** (e.g. `feat: Add X` not `feat: add x`) (recorded 2026-03-19T16:25:36Z).

Both directives have been merged into `.squad/decisions.md` under `## Process Decisions`.
