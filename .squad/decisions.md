# Squad Decisions

## Active Decisions

### 2026-03-20T00:00:00Z: Phase 1 UI kit contract for issue #152
**By:** Galadriel (Frontend Dev)
**What:** Phase 1 components in `Picea.Abies.UI` use pure Node-returning functions with immutable record options, token-first CSS custom properties, and explicit keyboard/focus/ARIA contracts per component.
**Why:** Establishes a coherent, reusable frontend contract for issue #152 with accessibility and composability built in.

### 2026-03-20T00:00:00Z: Issue #152 Phase 1 test strategy and merge gates
**By:** Samwise (Tester)
**What:** Use a test pyramid (unit/integration/a11y + focused E2E), require WCAG 2.1 AA and keyboard coverage for all interactive Phase 1 components, and enforce deterministic CI test gates before merge.
**Why:** Provides release confidence for the initial component kit while minimizing flaky CI behavior.

### 2026-03-20T13:20:00Z: Issue #152 architecture direction for Picea.Abies.UI
**By:** Maurice Cornelius Gerardus Petrus Peters (via Gandalf)
**What:** Phase 1 ships as two packages (`Picea.Abies.UI` and `Picea.Abies.UI.Demo`) with seven baseline components, token-driven styling, explicit accessibility contracts, and zero hidden mutable state.
**Why:** Delivers a practical starter kit quickly while preserving MVU purity and ecosystem consistency.

### 2026-03-20T14:05:00Z: Issue #152 review gate — execution readiness constraints
**By:** Elrond (Reviewer)
**What:** Keep issue #152 in changes-requested until v1 table scope/deferred items, measurable accessibility gates (automated + manual), and explicit CI/release mapping are fully defined.
**Why:** Prevents scope drift and unverifiable completion criteria.

### 2026-03-20T15:30:00Z: Issue #152 design iteration 2 — execution contract locked
**By:** Gandalf (Architect)
**What:** Locked four-phase implementation, v1 include/defer boundaries for all Phase 1 components, required accessibility verification matrix, explicit merge/release CI gate mapping, and frozen non-goals.
**Why:** Resolves reviewer concerns and establishes a measurable completion contract before implementation.

### 2026-03-20T16:00:00Z: Issue #152 kickoff package baseline in-repo
**By:** Maurice Cornelius Gerardus Petrus Peters (via Faramir)
**What:** Start implementation with a new `Picea.Abies.UI` net10.0 class library in the main solution, exposing immutable-options + pure static Node component factories for `button`, `textInput`, `select`, `spinner`, `toast`, `modal`, and `table`, plus a token CSS contract file under `wwwroot`.
**Why:** Provides a compile-ready baseline that matches Phase 1 scope while deferring advanced behaviors to explicit follow-up phases.

### 2026-03-20T16:10:00Z: Issue #152 kickoff execution source-of-truth locked
**By:** Gandalf (Architect)
**What:** Established `docs/guides/abies-ui-issue-152-execution-plan.md` as the implementation kickoff source of truth, including phase ownership mapping, explicit v1 include/defer boundaries per component, and merge/release gates mapped to required CI checks.
**Why:** Converts approved design direction into an execution contract that enables implementation to start with measurable completion criteria and no scope ambiguity.

## Process Decisions

### 2026-03-19T16:00:37Z: Always use the PR template
**By:** Maurice Peters (via Copilot)
**What:** Always use the PR template when creating pull requests.
**Why:** User request — captured for team memory

### 2026-03-19T16:25:36Z: PR titles must use Conventional Commits with uppercase subject
**By:** Maurice Cornelius Gerardus Petrus Peters (via Copilot)
**What:** Follow the repository PR title naming scheme (Conventional Commits with uppercase subject) when creating/editing PR titles.
**Why:** User request — captured for team memory

### 2026-03-20T12:30:00Z: Use terminal-safe execution patterns for long-running commands
**By:** Squad (integrated from memory instructions)
**What:** Avoid risky shell constructs in VS Code terminal. Do not pipe long-running commands through `tail`/`head`/`tee`/`grep`; use redirection to log files and inspect logs after completion.
**Why:** Prevents silent command termination and shell hangs in integrated terminal sessions.

### 2026-03-20T12:31:00Z: Benchmark comparisons require consistent power state
**By:** Squad (integrated from memory instructions)
**What:** Never compare benchmark runs across different MacBook power states. Record plugged-in vs battery for every benchmark set and run A/B in same session.
**Why:** Power-state variance can dominate signal and produce misleading conclusions.

### 2026-03-20T12:32:00Z: Performance claims require E2E benchmark validation
**By:** Squad (integrated from memory instructions)
**What:** Treat js-framework-benchmark as source of truth for user-visible performance; do not ship based on micro-benchmark improvements alone.
**Why:** Micro-benchmarks can show wins that regress real-world behavior.

### 2026-03-20T12:50:00Z: PR branches must pass `dotnet format --verify-no-changes`
**By:** Squad (integrated from PR instructions)
**What:** Before PR submission, run `dotnet format --verify-no-changes`; when fixing formatting in PR branches, scope changes with `--include` to avoid unrelated reformatting.
**Why:** Keeps PRs reviewable and aligned with `.editorconfig` without introducing noisy style-only churn.

### 2026-03-20T12:51:00Z: PR descriptions must follow repository template sections
**By:** Squad (integrated from PR instructions)
**What:** PR bodies must include: Description (What/Why/How), Related Issues, Type of Change, Testing, Changes Made, and Code Review Checklist.
**Why:** Maintains consistent review context and traceability for maintainers.

### 2026-03-20T12:52:00Z: PR gate requires Conventional Commits title and core CI checks
**By:** Squad (integrated from PR instructions)
**What:** PR titles follow Conventional Commits format and CI must pass build, lint (`dotnet format`), tests, and E2E (when applicable) before merge.
**Why:** Enforces predictable automation, quality standards, and release safety.

### 2026-03-20T13:05:00Z: Apply Abies brand palette to UI work, except Conduit projects
**By:** Maurice Cornelius Gerardus Petrus Peters (via Copilot)
**What:** All UI work should follow the Abies conference brand palette guidance, except Conduit-related projects which keep their existing visual conventions.
**Why:** User directive for consistent branding with an explicit Conduit carve-out.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
