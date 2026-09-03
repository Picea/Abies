---
name: playwright-cli
description: Playwright CLI reference for the squad — `pwsh playwright install` from the .NET test project, codegen for new flows, trace viewer for failure diagnosis, the squad's headed/headless conventions, retry strategy in CI vs local, and screenshot/video artefact discipline. Use when authoring or running E2E tests, diagnosing flaky tests via traces, or generating selectors via codegen. Do not use for unit-test concerns (use dotnet-cli) or for accessibility audits (use the ux-review skill).
---

# `playwright` — Playwright CLI for the squad

The squad uses Playwright for E2E browser tests against the Aspire-orchestrated dev composition. .NET projects use `Microsoft.Playwright` (the .NET binding); the underlying Playwright tooling is the same regardless of language binding.

This skill captures the squad's conventions: when to use which command, what to do with traces, and the failure modes that cost the most time.

## Installation

Playwright needs browsers downloaded separately from the NuGet package.

```bash
# After dotnet build, run the install script the package generated
pwsh tests/Articles.E2E/bin/Debug/net10.0/playwright.ps1 install

# On Linux without pwsh — use the raw script
tests/Articles.E2E/bin/Debug/net10.0/playwright.sh install

# Specific browsers only (CI default; see CI section)
pwsh tests/Articles.E2E/bin/Debug/net10.0/playwright.ps1 install chromium

# Install with system dependencies (CI / fresh machine)
pwsh tests/Articles.E2E/bin/Debug/net10.0/playwright.ps1 install --with-deps
```

The squad's CI installs only `chromium` to keep image size and run time down. Cross-browser runs (firefox, webkit) are explicit, label-gated, and run nightly — not on every PR.

## Running tests

E2E tests are TUnit/xUnit-shaped wrappers around Playwright's API. They run via `dotnet test` like any other test, but with environment expectations:

```bash
# Standard run — headless, against the local Aspire AppHost
dotnet test tests/Articles.E2E

# Headed (browser visible) for debugging
HEADED=1 dotnet test tests/Articles.E2E --filter "Name~PublishFlow"

# Slow-mo for visual debugging (ms between actions)
HEADED=1 PWDEBUG=1 dotnet test tests/Articles.E2E --filter "Name~PublishFlow"

# Specific browser
BROWSER=firefox dotnet test tests/Articles.E2E
```

The squad's E2E tests read `HEADED`, `PWDEBUG`, and `BROWSER` env vars in a shared test fixture. If you're not seeing them honored, the fixture might not be inherited — check the project's `BaseE2ETest.cs` (or equivalent).

**Squad rule:** E2E tests assume the AppHost is running. The convention is to start AppHost in one terminal (`dotnet run --project src/Articles.AppHost`) and run tests in another. Some projects auto-start the AppHost via TUnit fixtures; others don't. Check the README.

## Codegen

Codegen records browser interactions and emits Playwright code. Indispensable for selector discovery on a complex page.

```bash
# Open the target URL and start recording (uses the .NET binding's pwsh wrapper)
pwsh tests/Articles.E2E/bin/Debug/net10.0/playwright.ps1 codegen https://localhost:5001

# With a specific viewport / device
pwsh playwright.ps1 codegen --viewport-size=1280,720 https://localhost:5001
pwsh playwright.ps1 codegen --device="iPhone 15" https://localhost:5001

# Output to file
pwsh playwright.ps1 codegen -o test.cs --target=csharp https://localhost:5001
```

Codegen output is **never committed as-is**. It's a starting point. Squad post-processing checklist:
1. Replace flaky `getByRole('button', { name: 'Submit' })` chains with `getByTestId('submit-publish')` — production code should have stable test IDs.
2. Remove `waitFor(timeout: 5000)` calls; use `expect(locator).toBeVisible()` with default auto-wait.
3. Extract page interactions into a Page Object class.
4. Add the test to a fixture that handles auth setup.

## Traces — the diagnostic gold

Traces are zip files containing the full browser session: every action, screenshot, network request, console log, and DOM snapshot per step. The squad's E2E tests are configured to **always record traces, retain on failure**.

```bash
# Open a trace
pwsh playwright.ps1 show-trace ./TestResults/trace.zip

# CI artifacts: if a CI workflow runs E2E tests and uploads trace
# artifacts on failure, download via gh CLI and view locally
gh run download <run-id> -n e2e-traces
pwsh playwright.ps1 show-trace ./trace-of-failed-test.zip
```

The trace viewer shows actions on the left, the page state on the right, network on the bottom. The most useful sub-views:
- **Locator click** → see the actual element selected and screenshot at that moment.
- **Network** → diff request/response between the failing run and a known-good one.
- **Console** → JS errors that the test ignored.
- **Source** → the line of test code that ran each action.

## Retries and flakiness

Squad policy on flakiness:

1. **Retries are an admission of failure, not a fix.** In CI, the squad sets `retries: 1` (one retry on failure), not `2` or `3`. A test that needs `retries: 3` to be reliable is broken.
2. **Local runs do not retry.** A test that fails locally but passes "after a few tries" is hiding a race.
3. **Auto-waits beat explicit waits.** `expect(locator).toBeVisible({ timeout: 10000 })` over `waitForTimeout(5000)`.
4. **Network mocking for external dependencies.** The squad's E2E suite mocks third-party APIs at the Playwright level (`page.route()`); it does not let tests hit live external services.

When a test does flake, the squad's process:
1. Capture the trace from the failing run.
2. File an issue with the trace attached.
3. Mark the test `[Skip("Flaky — issue #N")]` rather than upping retries.
4. Curator notices repeated flake-and-skip → proposes a hardening pattern.

## CI configuration

The squad's E2E job in CI:
- Installs only `chromium --with-deps`
- Runs against the Aspire AppHost started as a step (not via the test fixture, for predictable startup timing)
- Records traces with `mode: "retain-on-failure"`
- Uploads `TestResults/` on failure as `e2e-traces` artifact (downloadable via `gh run download`)
- Has a job-level `timeout-minutes: 20` (E2E should never exceed this; if it does, that's a bug)

Cross-browser runs (firefox, webkit) are a separate workflow, label-triggered with `e2e-cross-browser`, not on every PR.

## Visual regression (optional)

The squad uses Playwright's screenshot comparison for a small set of "this layout matters" pages. The pattern:

```csharp
await Expect(page).ToHaveScreenshotAsync("article-list.png");
```

Snapshots live in `tests/Articles.E2E/Snapshots/` per platform. Updating them:

```bash
# Regenerate snapshots after an intentional change
UPDATE_SNAPSHOTS=1 dotnet test tests/Articles.E2E --filter "Category=visual"

# Or, for the .NET binding, the env var is PLAYWRIGHT_UPDATE_SNAPSHOTS
PLAYWRIGHT_UPDATE_SNAPSHOTS=1 dotnet test tests/Articles.E2E --filter "Category=visual"
```

Updated snapshots are committed; the diff in the PR shows visual changes alongside code changes.

## Failure modes and footguns

- **`pwsh: command not found`** — install PowerShell Core (`apt install powershell` on debian-likes; `brew install powershell` on macOS) or use the `.sh` variant on Linux.
- **Browser binaries missing in CI** — must run `playwright install --with-deps` after `dotnet build`. The squad's workflows do this; if you forked a workflow, check.
- **Tests pass headed, fail headless** — usually animation timing. Squad fix: `await page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce })` in test setup.
- **`page.GotoAsync` hangs forever** — server isn't listening. Check the AppHost is running and the URL matches the launch profile.
- **Selector matches "more than one element"** — the test depends on incidental text. Add a `data-testid`. If the production code can't have test IDs (third-party component), use `.first()` explicitly and document why.
- **Codegen output checked in unchanged** — the reviewer subagent flags this. Always post-process.
- **Trace files in source control** — `.gitignore` should cover `**/TestResults/`. If they're tracked, that's a finding.
