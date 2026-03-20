# Squad Decisions

## Active Decisions

No decisions recorded yet.

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
