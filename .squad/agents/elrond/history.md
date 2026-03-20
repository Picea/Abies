# Elrond — History

## Project Context

**Abies** — A reactive .NET MVU framework for WASM, server, and auto-render modes.

**Key Tech Stack:**
- Framework: .NET 10, C#, Blazor WASM
- Frontend: Web Components, TypeScript, Fluent UI
- Testing: Playwright (E2E), xUnit (unit)
- Infrastructure: GitHub Actions, Azure/Docker

**Standards:**
- PR titles: Conventional Commits with uppercase subject (checked by amannn/action-semantic-pull-request@v5)
- Code quality: dotnet format with EditorConfig, StyleCop, Roslyn analyzers
- Tests: All user journeys must have E2E tests (from RealWorld Conduit spec)
- Performance: Measured via js-framework-benchmark, required for perf PRs

**Squad Members:**
- Gandalf (Lead) — architecture, decisions
- Galadriel (Frontend) — UI, components
- Aragorn (Backend) — APIs, services
- Samwise (Tester) — test & quality
- Gimli (DevOps) — CI/CD, deployment
- Elrond (Reviewer) — code quality gate

## Learnings

(To be populated as Elrond reviews work and learns project conventions)

- First review: PR #144 benchmark required-check (verified template compliance, title formatting)
- Reviewer gate checklist: verify Conventional Commits PR title, required template sections, and explicit testing details.
- Block merge when required CI checks fail (build, lint/format, tests, E2E when applicable).
