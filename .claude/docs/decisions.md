# Team Decisions

This file holds the squad's authoritative team-wide conventions. The framework section below is hand-written and stable. Project-specific decisions accumulate over time, merged from `.squad/decisions/inbox/` by the `scribe-decision-merger` hook on subagent completion.

---

## Framework

### Beast Mode × Disney Creative Strategy
All significant features and architectural changes go through the Dreamer → Realist → Critic cycle before implementation. All code changes go through the independent Reviewer before merge. The design agents do not write code. The Reviewer does not participate in design phases.

### Design Phases Run as Isolated Agents
Each Beast Mode phase is a separate subagent with its own context, tool grant, and memory: `dreamer-first-principles`, `dreamer-informed`, `dreamer-convergence`, `realist`, `critic`, `spec-author`. The `architect` scopes and closes the pass but does not perform its phases; the Lead sequences them. Phases hand work to each other through numbered artifacts in `.squad/design/<slug>/`, never through summaries.

The split is what makes the dual-track Dreamer real rather than aspirational: `dreamer-first-principles` has no retrieval tools and no persistent memory, so its independence from prior art is enforced by construction rather than by instruction. The two tracks run concurrently and neither may read the other's artifact. Small, unambiguous designs take the `architect`'s fast path instead, which skips the phase agents entirely.

> **Superseded 2026-09-02:** this section previously read *"All significant features and architectural changes go through the Architect's Dreamer → Realist → Critic cycle before implementation... The Architect does not write code"* — a single Architect agent performing every phase itself. That description is superseded as of this migration. The framework now splits those phases into six isolated subagents (`dreamer-first-principles`, `dreamer-informed`, `dreamer-convergence`, `realist`, `critic`, `spec-author`) that the Lead sequences one at a time, because subagents cannot spawn subagents. The Architect scopes the pass (`00-scope.md`) and closes it (`07-handoff.md`) but no longer performs the Dreamer/Realist/Critic work directly except on its fast path for small, unambiguous designs. This note is kept rather than deleted so the change in operating model is traceable; all decisions below that refer to "the Architect's Dreamer → Realist → Critic cycle" describe the old model and should be read as historical unless restated.

### Principles Enforcement
Every deviation from an established principle requires explicit user approval before proceeding. No agent may silently compromise. Undocumented deviations are 🔴 Must Fix during review. See `.claude/docs/principles-enforcement.md` for the full protocol.

---

## Language & Platform

### .NET 10 (LTS) with C# 14
Target `net10.0`. Use the latest stable C# features. Always prefer new language features and APIs. Replace deprecated APIs with recommended alternatives.

### Picea.Abies Namespace Root
`Picea.Abies` is the root namespace for the project ecosystem. All code lives under it.

---

## Functional DDD

### Pure Functional Programming
No object orientation in the domain. No mutable classes, no inheritance hierarchies for behavior, no Manager/Helper/Util types. Model with immutable records, pure functions, discriminated unions, Result/Option. Exception: performance-critical hot paths — use what's fastest and comment why.

### Make Illegal States Unrepresentable
Replace primitive obsession with constrained types (smart constructors). Private type constructors, public smart constructors — the only way to obtain a valid instance. Model mutually exclusive states as sum types. Model optional data as `Option<T>`, not null.

### State Machines, Not Flags
Never model entity lifecycle state as boolean flags or nullable fields. Each state is a distinct type carrying only its own data. Transitions are methods on the source state type. The compiler enforces valid transitions.

### Errors Are Values
Expected failures use `Result<T, TError>` — never exceptions. Keep error types domain-specific and discriminated. Exceptions only for programmer bugs and unrecoverable infrastructure failures.

### Push IO to the Edges
Functional core, imperative shell. Domain functions are pure. Effects (time, persistence, external services) are supplied as capability functions. Application layer wires real implementations.

### Persistence Boundaries
Domain types never leak into infrastructure. No JSON/ORM attributes on domain types. Map domain types to DTOs at the boundary. `ToDomain` returns `Result` when persisted data might be invalid.

### Anti-Corruption Layer
When integrating with external systems, map external DTOs into internal domain types at the boundary. Never let an external schema leak into the internal model.

---

## Naming Conventions

### No I-Prefix on Your Own Interfaces
`UserRepository` not `IUserRepository`. BCL interfaces (`IOptions<T>`, `IEntityTypeConfiguration<T>`) keep their Microsoft names.

### No Async Suffix
Never suffix async method names with `Async`.

### Namespaces Are Bounded Contexts
`Picea.Abies.Commanding.Handler` not `Picea.Abies.CommandHandler`. `Picea.Abies.Demos.Subscriptions` not `Picea.Abies.SubscriptionDemo`. Depth over width. Folder structure mirrors namespace declarations exactly.

### Domain Terms Only
No `Manager`, `Helper`, `Util`, `Service` in the domain layer. Use ubiquitous language names that match the business domain. Modules as `static class ...Module`.

---

## Code Style (C#)

### File-Scoped Namespaces
Always. Single-line using directives.

### Expression-Bodied Members by Default
Use expression-bodied members unless the method body requires multiple statements.

### Pattern Matching by Default
Prefer pattern matching and switch expressions over traditional control flow.

### No #region
Ever. If you need regions, your class is too big.

### Nullable Reference Types
Declare variables non-nullable. Check for null at entry points. Always `is null` / `is not null` — never `== null` / `!= null`. Trust C# null annotations.

---

## Code Style (JavaScript)

### Vanilla First
No frameworks (React, Vue, Angular) unless explicitly Architect-approved after a full design cycle. The platform is the framework.

### ES Modules Only
`import`/`export`. No CommonJS, AMD, UMD.

### No Build Step by Default
If code runs natively in a modern browser with `<script type="module">`, that's the preferred delivery. Build steps require justification.

### No Unnecessary Dependencies
If the browser or Node.js provides it natively, don't npm install a package for it.

---

## Testing

### TUnit Only
TUnit is the only test framework. No xUnit, no NUnit, no MSTest. Source-generated, parallel by default, async-first assertions.

### No Arrange/Act/Assert Comments
Do not emit "Arrange", "Act", or "Assert" comments in tests.

### Aspire AppHost Is the Test Fixture (Amended 2026-09-02 — Fast In-Memory Exception)

Two test levels apply. Which one a contributor reaches for depends on what's being verified:

- **End-to-end / cross-service integration tests** — verify a real user journey or a service's behavior against its real dependencies (event stores, databases, other services). These **must** start the SUT via `DistributedApplicationTestingBuilder` against the Aspire AppHost. No `WebApplicationFactory`, no Testcontainers, no manual process startup at this level.
- **Fast in-memory integration tests** — verify a single service's HTTP pipeline (routing, auth, serialization, validation, command handling) in isolation, with external dependencies replaced by in-memory fakes. These are permitted to use `Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<T>`, or the underlying `Microsoft.AspNetCore.TestHost.TestServer` directly (`WebApplication.CreateBuilder()` + `.UseTestServer()`), instead of the Aspire AppHost. No containers — the point of this level is startup speed, not full-topology fidelity.

**Named exception extent** (the only current usages of the fast in-memory level; this is a closed list, not an open-ended license — a new usage should be reviewed for whether it truly needs the fast in-memory level or whether it belongs at the Aspire level):
- `Picea.Abies.Conduit.Api.Tests/ConduitApiFactory.cs` — subclasses `WebApplicationFactory<Program>`; replaces KurrentDB and PostgreSQL with in-memory fakes (`InMemoryEventStore<T>`, in-memory query delegates) to test the Conduit API's HTTP pipeline without containers.
- `Picea.Abies.Conduit.Api/Program.cs` — exposes the partial `Program` class so `WebApplicationFactory<Program>` in the test project above can target it.
- `Picea.Abies.Server.Kestrel.Tests/EndpointTests.cs` — its `AbiesTestHost` helper wraps `WebApplication.CreateBuilder()` + `.UseTestServer()` for in-process Kestrel pipeline tests. (Note: this is the `TestServer` pattern, not literally a `WebApplicationFactory<T>` subclass — same in-memory-host family, different entry point.)
- `Picea.Abies.Server.Kestrel.Tests/OtlpProxyEndpointTests.cs` — same `.UseTestServer()` pattern, for OTLP proxy endpoint tests.

Genuine Aspire-based E2E tests exist alongside these (e.g. `Picea.Abies.Conduit.Testing.E2E`, which starts KurrentDB + PostgreSQL + the Conduit API via `DistributedApplicationTestingBuilder`) — the fast in-memory level supplements the Aspire level, it does not replace it.

> **Amended 2026-09-02:** this convention originally read *"All integration and E2E tests start the SUT via `DistributedApplicationTestingBuilder` against the Aspire AppHost. No `WebApplicationFactory`, no Testcontainers, no manual process startup"* with no distinction between test levels. That blanket rule is superseded as of this note for the four files listed above. This is an **explicit user decision made 2026-09-02** during the `.squad/`-to-`.claude/` framework migration, after the Tech Writer found and flagged the drift in `tech-stack.md` as a live contradiction between decisions.md and the actual test suite. The user chose to **grandfather the existing fast in-memory tests with this documented exception** — not to migrate them to Aspire, and not to leave the contradiction flagged and unresolved. Read as historical: any older entry in this file that says "no `WebApplicationFactory`" without qualification predates this amendment. Read as live: new integration tests default to the Aspire AppHost; the fast in-memory level is for the narrow case of testing one service's HTTP pipeline in isolation, and its extent is the four files named above unless this note is amended again.

### E2E Tests for User Journeys
Always write E2E tests for user journeys. TUnit + Playwright via TUnit.Playwright.

### Spec-by-Example for New Features
Every new feature and behavior change starts with a **Spec-by-Example test** — drafted before any production code is written, approved by the user, and immutable during implementation. The test is the executable specification: if it passes, the feature is done. The Architect runs a Spec-by-Example Phase between Critic approval and Handoff (see Architect charter). The Test Strategy Expert Room (🧪) decides which level the spec test lives at (E2E through UI, API/integration, or workflow-direct) per feature. Implementation makes the approved test pass without modifying it; if implementation reveals the test is wrong, work pauses and the test change is re-approved.

**Skip Spec-by-Example for:** pure refactoring with no behavior change, trivial changes (one-line config, doc-only, dependency bumps with no behavior change), and bug fixes (already covered by the bug-fix regression test rule).

### Playwright MCP for Browsing
When browsing, inspecting web pages, or running browser diagnostics — always prefer the **Playwright MCP server** over curl, wget, or raw HTTP clients. Playwright gives you a real browser context: JavaScript execution, rendered DOM, network interception, cookies, auth flows, screenshots. Use it for debugging UI issues, verifying rendered output, inspecting Aspire dashboard traces, and validating DAST targets. Fall back to curl/fetch only if Playwright MCP is unavailable.

---

## Aspire & Observability

### Aspire for All Runnable Apps
Every application with more than one process uses .NET Aspire for local orchestration. Every service calls `AddServiceDefaults()`.

### Full OTEL Trace Coverage
Every functional flow is instrumented end-to-end — from user action through all backend hops. Custom `ActivitySource` spans on workflow entry points with meaningful names. Errors record exception info on spans. Cross-service trace context propagates.

### No Dark Services
Every component in the Aspire AppHost must emit telemetry visible in the dashboard. Missing spans are bugs.

### Templates Ship with Observability
All `dotnet new` templates include AppHost, ServiceDefaults, OTEL instrumentation, at least one E2E test, and a README for the dashboard.

---

## Security

### Living Threat Model
`/docs/security/threat-model.md` is maintained and updated after every change that alters the attack surface. Every threat has a corresponding regression test.

### Automated Security Pipeline
SAST, SCA, secrets detection, DAST, and container scanning run locally AND in CI. Critical/high findings block merge.

### Secure Defaults
Every endpoint has an explicit authorization policy. Parameterized queries only. No hardcoded secrets. CSP configured. CORS explicit.

---

## Documentation

### Markdown Only
All project documentation in `.md` format. No Word, no Confluence, no Google Docs for anything that lives with code.

### Docs Ship with Code
If a feature lands without docs, it's not done. If an API changes without updating its reference, it's a bug.

### Diátaxis Framework
Every doc fits one mode: tutorial (learning), how-to (task), reference (information), explanation (understanding). Don't mix modes.

### ADR Template
All ADRs follow the template at `/docs/adr/` with Status, Date, Decision Makers, Supersedes, Context, Decision, Consequences (Positive/Negative/Neutral), Alternatives, Related Decisions, References.

### Verify Mechanism Claims Against the Source, Not Against Other Prose
When a doc states what triggers, authorizes, or changes something in an automated system — a CI trigger, an actor check, a hash comparison — check the actual trigger or logic declaration before writing the sentence, even when you're just restating a claim that already appears (correctly or not) somewhere else in the docs. Confident, specific-sounding prose about a mechanism is not evidence the mechanism does that; copying it forward propagates the error instead of catching it.

---

## Boy Scout Rule

### Always Leave the Code Better Than You Found It
Every time you touch a file, you improve it. Not a separate task — part of every task. If you're in a file to fix a bug, and you see a poorly named variable, a missing type annotation, a stale comment, an unclear error message, or a code smell — you fix it. Small improvements compound. Codebase quality is everyone's responsibility, not a dedicated "cleanup sprint."

This applies to every agent: C# Dev, JS Dev, Tech Writer (docs are code too), Security Expert (scanner configs), DevOps (pipeline configs), Performance Engineer (benchmark code). If you touched it, leave it better.

The Reviewer checks for this. If a file was modified and obvious improvements were ignored, that's a ⚠️ Should Fix finding.

---

## Git Workflow

### Never Commit to Main
No agent and no human commits directly to `main` — locally or remotely. All changes go through feature branches and pull requests. No exceptions. No `--force`, no "just this once," no "it's a tiny fix." Main is protected. PRs are the only way in.

No local hook catches a violation of this rule before it runs. A `block-direct-commits-to-main` hook was designed for that but is deliberately not installed — it has an open false-positive defect upstream (`squad-template#19`) that would block legitimate commits. The rule is enforced server-side regardless: `main` is protected by two active GitHub rulesets (`Protect main`, `protectmainbranch`), and `protectmainbranch` requires a pull request, linear history, and passing status checks, with no bypass actors configured. A direct push to `main` is rejected by GitHub — it just isn't caught on the developer's machine before the attempt.

### Conventional Commits
All commit messages follow the [Conventional Commits](https://www.conventionalcommits.org/) specification. No free-form messages.

Format: `<type>(<scope>): <description>`

| Type | When |
|---|---|
| `feat` | New feature or capability |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `test` | Adding or updating tests |
| `perf` | Performance improvement |
| `security` | Security fix or hardening |
| `ci` | CI/CD pipeline changes |
| `build` | Build system or dependency changes |
| `chore` | Maintenance (no production code change) |

Scope is the bounded context or component: `feat(authentication): add token versioning`, `fix(articles): handle empty slug`, `docs(api): update endpoint reference`.

Breaking changes use `!` after the type: `feat(api)!: remove deprecated v1 endpoints`.

This format is mechanically enforced by the `enforce-conventional-commits` hook in `.claude/hooks/`.

### Branch Naming
All branches follow this convention:

`<type>/<issue-number>-<short-slug>`

Examples:
- `feature/42-token-versioning`
- `fix/87-empty-slug-crash`
- `docs/91-api-reference-update`
- `security/103-xss-sanitization`
- `refactor/110-extract-workflow-module`
- `test/115-e2e-article-publishing`
- `perf/120-cache-token-lookup`

Types match Conventional Commits. Always include the issue number. Slug is lowercase, hyphen-separated, max ~5 words.

This format is a squad convention, not currently mechanically enforced. A `validate-branch-name` hook was designed for this but is deliberately not installed — it has an open false-positive defect upstream (`squad-template#19`) that would block legitimately named branches. Until the hook ships a fix, follow the pattern by convention; reviewers should flag branches that don't match it.

---

## Dependency Approval Policy

### Every New Dependency Requires Review
No NuGet package or npm module is added without explicit review. Dependencies are liabilities — they add attack surface, maintenance burden, transitive risk, and upgrade obligations.

### Approval Flow

1. **Specialist proposes.** The C# Dev or JS Dev identifies a need and proposes a specific package.
2. **Security Expert reviews.** SCA scan for known CVEs, license compatibility check, transitive dependency audit. This is mandatory — no exceptions.
3. **Architect approves** (for framework-level dependencies). If the dependency introduces a new architectural pattern, affects multiple bounded contexts, or creates a significant coupling — the Architect reviews. For leaf dependencies used in one module, the Security Expert's approval is sufficient.
4. **Document the decision.** Every dependency addition gets a brief entry in `.squad/decisions/inbox/`: what package, why it's needed, what alternatives were considered, what the Security Expert found.

### Criteria for Approval
- **Is it necessary?** Does the BCL/platform already provide this? (`crypto.randomUUID()` over `uuid`, `System.Text.Json` over `Newtonsoft`, `fetch` over `axios`). If yes — rejected.
- **Is it maintained?** Active commits in the last 6 months. Responsive to security issues. Not a single-maintainer abandoned project.
- **Is it safe?** No known critical/high CVEs. Acceptable license (MIT, Apache 2.0, BSD). Reasonable transitive dependency tree (not pulling in 200 packages).
- **Is the scope right?** Prefer small, focused packages over kitchen-sink frameworks. Don't add a library for one function.

### Removal
Unused dependencies are removed. The Security Expert audits the dependency tree periodically. If a package is no longer imported anywhere — it's gone.

---

## Definition of Done

### A Task Is Not Done Until All of These Are True

Every task — feature, bug fix, refactoring, or any code change — must satisfy all applicable items before it can be considered complete. This is the squad's shared understanding of "done."

**Code:**
- [ ] Implementation follows all established principles (functional DDD, state machines, smart constructors, namespaces, etc.)
- [ ] No principle deviations without documented user approval
- [ ] Boy Scout Rule applied — touched files left better than found

**Testing:**
- [ ] Spec-by-Example test was approved by the user before implementation began (for new features and behavior changes)
- [ ] The originally-approved Spec-by-Example test still passes — unmodified — at merge time
- [ ] Unit tests cover new logic (smart constructors, workflows, edge cases)
- [ ] Integration/E2E tests run via Aspire AppHost
- [ ] Security regression tests added for any new threat mitigations
- [ ] Regression test added for every bug fix (reproduces the bug, verifies the fix)
- [ ] All tests pass (`dotnet test`)

**Observability:**
- [ ] OTEL traces cover the full functional flow (visible in Aspire dashboard)
- [ ] Custom `ActivitySource` spans on workflow entry points
- [ ] Error spans include exception info

**Security:**
- [ ] Threat model updated if attack surface changed
- [ ] Security scanning (SAST/SCA) passes with no critical/high findings
- [ ] No hardcoded secrets

**Documentation:**
- [ ] Docs updated or created for any user-facing change (Tech Writer involved)
- [ ] Tech Writer has verified all existing docs are still in sync after the change
- [ ] API reference current
- [ ] ADR created for significant architectural decisions
- [ ] CHANGELOG updated

**Review:**
- [ ] Reviewer approved (no open 🔴 Must Fix findings) — **mandatory for every code-touching work item; no work item closes without an explicit Reviewer approval recorded**
- [ ] No specialist self-approved their own work (Reviewer is the only authority that declares code complete)
- [ ] No code-shaped change was Lead-approved (Lead's lightweight authority is limited to README/CONTRIBUTING/CHANGELOG prose, decisions inbox, code comments, and `.md` docs only)
- [ ] UX Expert approved (for user-facing changes)
- [ ] No undocumented principle deviations

**Git:**
- [ ] Commit messages follow Conventional Commits
- [ ] Branch follows naming convention
- [ ] PR targets `main` (never direct commit)
- [ ] Pre-Push Quality Gate passes

---

## Review

### Independent Reviewer
The Reviewer approaches code with fresh eyes — no prior context from design phases. Evaluates what was written, not what was intended.

### Reviewer Lockout Authority
🔴 Must Fix findings block merge. Original author locked out on rejection — coordinator reassigns.

### Reviewer Is Mandatory for Every Code-Touching Work Item
No code-touching work item closes without an explicit Reviewer approval. This includes "trivial" changes — there is no such thing as too small to review. Trivial changes get reviewed faster, not skipped. Specialists do not self-approve their own code. The Lead does not approve code-shaped changes (`.cs`/`.js`/`.ts`/`.mjs`, Dockerfiles, GitHub Actions workflows, `appsettings.*`, `.csproj`/`Directory.Build.props`/`Directory.Packages.props`, `package.json`, EF migrations). The Lead's lightweight authority is limited to true non-code: README/CONTRIBUTING/CHANGELOG prose, decisions inbox, code comments, and `.md` docs. An attempt to declare code work complete without Reviewer approval triggers the **Missing Review Lockout** in `principles-enforcement.md` — the agent is locked out and the Lead reassigns to the Reviewer or escalates.

### Undocumented Deviations Block
Any code that deviates from an established principle without a documented approval (decision log + code comment) is 🔴 Must Fix unconditionally.

### Observability Review
Reviewer checks for OTEL trace coverage, custom spans, error recording, cross-service propagation, AddServiceDefaults(), and E2E trace verification tests.

### Threat Model Review
If a change adds an entry point, alters a trust boundary, or changes auth — the threat model must be updated. Missing updates are 🔴 Must Fix.

---

## Issue Prioritization

### Open Issue Priority Label Rule (2026-03-25)
All open issues in Picea/Abies must have exactly one priority label at all times.

Current normalized distribution:
- `priority:p0`: #127, #79, #81, #153, #158
- `priority:p1`: #151, #154, #155, #156, #157, #161, #162, #163, #164, #83
- `priority:p2`: #159, #165

Verification performed on 2026-03-25: every open issue has exactly one priority label.

### Issue #127 Hardening Baseline (2026-03-25)
- WebSocket transport must reassemble fragmented inbound frames, enforce a max inbound payload size, and serialize outbound sends.
- Conduit article list/feed must validate `limit`/`offset` and return `422` for invalid values.
- Conduit create/update endpoints must not return null-success; when unavailable they return explicit `503` Conduit error responses.
- Required regression coverage: `WebSocketTransportTests` and `ArticleEndpointTests`.

---

## Session Decisions

> **Note on paths in decisions below:** entries created before this migration (2026-09-02) may reference the old layout — `.squad/principles-enforcement.md`, `.squad/routing.md`, `.squad/agents/<name>/charter.md`. The framework now lives under `.claude/` (`.claude/docs/principles-enforcement.md`, the routing table in `CLAUDE.md` §2, agent charters in `.claude/agents/<name>.md`). Historical entries are left as originally written — they are a record of what happened, not a live pointer — except where a decision is still an active, load-bearing convention, in which case the reference below has been updated in place.

### 2026-03-26T00:00:00Z: Template defaults enable debugger + OTEL with WASM host proxy
**By:** Maurice Cornelius Gerardus Petrus Peters (via C# Dev)
**What:** Browser templates (`abies-browser`, `abies-browser-empty`) now default OTEL on (`otel-verbosity=user`) and include an `AbiesApp.Host` project that serves the WASM AppBundle and maps `/otlp/v1/*` via `MapOtlpProxy()`. Server template defaults now map `MapOtlpProxy()` and configure OpenTelemetry tracing with `AddConsoleExporter()`.
**Why:** Ensure generated templates are observable by default, support browser-to-backend tracing flow out of the box, and use console exporter as the default trace sink.

### 2026-03-26T00:00:00Z: Template counter buttons use symbol labels with accessible names
**By:** JS Dev
**What:** In template counter UIs (`abies-browser` and `abies-server`), render visible button labels as `+` and `-` while setting explicit ARIA labels (`Increase`/`Decrease`) for accessible button names.
**Why:** User requested plain plus/minus buttons in templates, and accessibility should remain descriptive rather than symbol-only.

### 2026-03-26T13:33:43Z: Always engage the squad for work in this repo
**By:** Maurice Cornelius Gerardus Petrus Peters (via Copilot)
**What:** All work in this repo routes through squad coordination. No direct commits, no solo agent work outside the team structure.
**Why:** User directive — enforcing squad discipline and coordination for all contributions.

### 2026-03-27T07:38:22Z: Browser OTLP export uses protobuf exporter with pinned CDN versions
**By:** JS Dev
**What:** Browser OTLP export now uses the protobuf trace exporter path, pins CDN API/SDK/exporter package versions to a known-compatible set, performs explicit export-on-span-end in the browser path, and excludes `/otlp/v1/traces` from self-instrumentation.
**Why:** Live Conduit WASM verification showed the backend proxy path accepted OTLP posts (HTTP 200) while browser-side export behavior required a browser-focused exporter strategy and deterministic CDN versioning to restore reliable end-to-end browser trace export.

### 2026-03-27T08:03:52Z: Browser OTEL sets explicit service.name to avoid unknown_service
**By:** Maurice Cornelius Gerardus Petrus Peters (via JS Dev)
**What:** Browser OTEL runtime now sets a stable resource `service.name` and allows per-app override via `<meta name="otel-service-name" content="...">` (with legacy `abies-otel-service-name` compatibility), preventing browser traces from falling back to `unknown_service`.
**Why:** Aspire trace grouping becomes reliable and identifiable for UI-originated spans when service naming is explicit instead of implicit.

### 2026-03-27T00:00:00Z: InteractiveServer debugger asset is package-owned under /_abies/
**By:** C# Dev
**What:** InteractiveServer and InteractiveAuto bootstrap resolve debugger startup from sibling `/_abies/debugger.js` shipped by `Picea.Abies.Server.Kestrel`, and that debug-only asset is excluded from Release builds.
**Why:** Relative import to `/debugger.js` depended on host-app static files that were not guaranteed, so default-on debugger startup could silently no-op even when bootstrap executed.

### 2026-03-27T00:00:00Z: Explicit debug UI default in WASM startups and browser templates
**By:** Maurice Cornelius Gerardus Petrus Peters (via C# Dev)
**What:** WASM startup files and browser templates set debugger defaults explicitly using `DebuggerConfiguration.ConfigureDebugger(new DebuggerOptions { Enabled = !debugUiOptOut })` with `ABIES_DEBUG_UI=0` opt-out.
**Why:** Ensures normal Debug starts keep debug UI enabled by default while preserving a clear opt-out path.

### 2026-03-27T00:00:00Z: Runtime debugger UI defaults on with JS-level opt-out
**By:** JS Dev
**What:** Browser and server runtime startup resolve debugger enablement from query/meta/global config, default to enabled, and expose unified state via `window.__abiesDebugger.enabled`; startup import remains best-effort when assets are absent.
**Why:** Keeps debugger visible by default in Debug startup while preserving a non-breaking opt-out path.

### 2026-03-27T00:00:00Z: WASM input handling must not depend on debugger bootstrap success
**By:** JS Dev
**What:** Browser runtime startup now wires event handler registry immediately after runtime start and before optional debugger bootstrap. Debugger bootstrap is treated as best-effort in Debug builds.
**Why:** If debugger bootstrap throws first, UI can render but never process input events when handler wiring is skipped.

### 2026-03-27T00:00:00Z: Browser OTEL export uses protobuf and explicit export-on-end fallback
**By:** JS Dev
**What:** Browser OTEL export uses `@opentelemetry/exporter-trace-otlp-proto`, pins compatible CDN package versions, exports spans explicitly on end in the browser path, and skips self-instrumentation for `/otlp/v1/traces`.
**Why:** Live Conduit WASM validation showed JSON export produced HTTP 415 and CDN ESM runtime behavior required deterministic browser-focused exporter handling.

### 2026-03-27T00:00:00Z: InteractiveServer debugger startup requires runtime browser coverage
**By:** Lead
**What:** Add browser-executed verification that waits for successful `/_abies/debugger.js`, then asserts `#abies-debugger-timeline[data-abies-debugger-adapter-initialized="1"]` and `window.__abiesDebugger.enabled === true`.
**Why:** Static asset checks alone do not prove dynamic sibling import path execution in `abies-server.js`.

### 2026-03-27T00:00:00Z: Debugger bridge handoff is explicit in browser runtime bootstrap
**By:** Maurice Cornelius Gerardus Petrus Peters (via Beast Mode)
**What:** After runtime debugger initialization in browser runtime bootstrap, assign the runtime debugger instance to interop (`Interop.Debugger = runtime.Debugger`) so debugger bridge dispatch always has a concrete machine instance.
**Why:** Prevent debug command responses like `unavailable|-1|0` caused by missing runtime-to-interop debugger handoff.

### 2026-03-27T00:00:00Z: Core abies.js remains debugger-free in release contract
**By:** Maurice Cornelius Gerardus Petrus Peters (via Beast Mode)
**What:** Keep debugger bootstrap/remount/fallback logic in `debugger.js` only and remove debugger-specific logic from core `abies.js` runtime path.
**Why:** Enforce the release strip contract that `abies.js` must not retain debugger references.

### 2026-03-27T00:00:00Z: Browser debugger adapter contract tests are transport-focused
**By:** C# Dev
**What:** Browser debugger adapter tests validate serialize/deserialize transport behavior instead of removed adapter internals.
**Why:** Production API no longer exposes prior internal state members.

### 2026-03-27T00:00:00Z: Template browser host resolves AppBundle from existing build output
**By:** C# Dev
**What:** Template browser host startup probes both Debug and Release AppBundle locations and uses the first existing path.
**Why:** Avoid startup failures when generated templates are built in one configuration and launched in another.

### 2026-03-27T00:00:00Z: Template restore isolates NuGet cache per generated app
**By:** C# Dev
**What:** Generated template `nuget.config` sets `globalPackagesFolder` to local `.nuget/packages`.
**Why:** Prevent stale global package cache shadowing locally packed debug artifacts.

### 2026-03-27T00:00:00Z: Browser package ships debugger.js in debug package artifacts
**By:** C# Dev
**What:** `Picea.Abies.Browser` packaging includes `wwwroot/debugger.js`, and targets copy it conditionally for debug flows while remaining release-safe.
**Why:** Browser runtime debug bootstrap imports `../debugger.js`; missing artifact prevents debugger shell mount in generated template apps.

### 2026-03-27T00:00:00Z: WASM debug bootstrap wires runtime bridge before mount
**By:** JS Dev
**What:** Browser startup calls `Interop.SetRuntimeBridge(Interop.DispatchDebuggerMessage)` after `runtime.UseDebugger()` and before mount.
**Why:** Debugger UI can mount without functional commands if the bridge callback is not wired.

### 2026-03-27T00:00:00Z: Debugger adapter bridge invocation is async-safe
**By:** JS Dev
**What:** Browser and server debugger adapters await bridge callback via `Promise.resolve(runtimeBridge(...))` before parsing response.
**Why:** Prevent `[object Promise]` timeline/status artifacts when callback returns a Promise.

### 2026-03-27T00:00:00Z: Browser debugger module resolution tries sibling then root fallback
**By:** JS Dev
**What:** Debug bootstrap module loader first tries `./debugger.js`, then `/debugger.js`, and caches the successful URL.
**Why:** Host/static-web-asset path differences can break debugger module loading in debug builds.

### 2026-03-29T00:00:00Z: App polymorphic DU roots must declare JsonPolymorphic metadata
**By:** C# Dev
**What:** Abstract application-layer DU roots participating in debugger snapshot serialization must use `[JsonPolymorphic]` with explicit `[JsonDerivedType]` registrations for all concrete variants.
**Why:** Imported timeline replay relies on JSON round-trip; missing type discriminators causes abstract type deserialization failures and no-op snapshot application.

### 2026-03-29T00:00:00Z: Step-forward path already applies debugger snapshot and render
**By:** C# Dev
**What:** No runtime C# fix required for `step-forward`; bridge execution already flows through `TryApplyDebuggerSnapshot` and render path in browser and server runtime.
**Why:** Investigation confirmed unconditional snapshot apply after debugger bridge execute for supported message types.

### 2026-04-01T00:00:00Z: CI Runtime Policy — staged fast/full/nightly lanes
**By:** Maurice Cornelius Gerardus Petrus Peters (via Performance Engineer)
**What:** Adopt staged CI lanes: fast PR feedback lane, full push/main confidence lane, and nightly deep validation. Keep js-framework-benchmark as the authoritative performance gate with a 5% regression threshold.
**Why:** Improve PR feedback time and runner efficiency without reducing production confidence signal.

### 2026-04-01T00:00:00Z: Security PR gating matrix realignment for speed and coverage
**By:** Maurice Cornelius Gerardus Petrus Peters (via Security Expert)
**What:** Keep exploit-critical security gates on PR (secrets, SCA high/critical, one mandatory SAST gate, relevant Trivy high/critical), move heavy DAST/template scans to push-main and nightly with path-filtered PR exceptions, and remove duplicate SCA gate overlap.
**Why:** Preserve pre-merge security blocking while reducing PR latency and maintaining defense-in-depth through scheduled full scans.

### 2026-04-04T20:43:47Z: Program contract should be decider-shaped
**By:** Maurice Cornelius Gerardus Petrus Peters (via Copilot, Architect, and C# Dev)
**What:** Record the directive that Program should be a decider, while preserving MVU/runtime compatibility through a staged migration. The canonical shape is decider-first semantics with explicit decide/evolve behavior and value-based errors; migration remains constrained by current `AutomatonRuntime` contracts.
**Why:** Align app-level program flow with existing decider usage in the domain while avoiding a one-step breaking API/runtime transition.

### 2026-04-04T20:43:47Z: Program-as-Decider migration guardrails
**By:** Architect
**What:** Adopt a two-phase approach: immediate semantic/contract alignment toward decider behavior, followed by a later runtime-native decider path after explicit ADR and migration cost acceptance.
**Why:** A direct hard replacement is high risk given public API blast radius and runtime coupling to `AutomatonRuntime`.

### 2026-04-04T20:58:02Z: Breaking-change directive (deduplicated)
**By:** Maurice Cornelius Gerardus Petrus Peters (via Copilot)
**What:**
- Always ask about breaking changes before making potentially breaking edits.
- Breaking changes are explicitly allowed for Program-to-Decider migration work.
**Why:** User directives captured and deduplicated with latest wording.

### 2026-04-04T21:10:00Z: Program-to-Decider evaluation round outcome
**By:** Architect, C# Dev, Reviewer
**Requested by:** Maurice Cornelius Gerardus Petrus Peters
**Verdict:** Proceed with staged migration, blocked for release until conditions are met.
**What:**
- Adopt staged convergence: keep runtime compatibility now, plan runtime-native decider cutover later via ADR and explicit gates.
- Treat this as a breaking migration path requiring explicit `Decide`/`IsTerminal` coverage across all Program implementers.
- Require migration updates in lockstep for templates, docs, tests, and runtime seams.
**Why:** Direction is architecturally correct, but runtime coupling and migration blast radius make hard one-step replacement too risky.

### 2026-04-04T21:10:00Z: Program-to-Decider merge/release gate conditions
**By:** Architect, C# Dev, Reviewer
**What:**
- Fix compile regressions in server test projects and template-generated projects (missing Program decider members).
- Clarify/enforce Program compatibility contract so implementers satisfy decider requirements consistently.
- Add migration guard tests that fail early when Program contract changes are not propagated.
- Keep `AutomatonRuntime` coupling until a replacement path is benchmarked, verified, and documented in ADR.
**Why:** Current state is directionally correct but not release-safe without propagation and guardrails.

### 2026-04-04T00:00:00Z: Full decider migration — breaking contract target
**By:** Architect
**What:** Runtime must be decider-native end-to-end. Remove all Program compat shims (default `Decide`/`IsTerminal`). All Program implementors must explicitly declare both members. Remove AutomatonRuntime-first language from docs. This is a breaking migration contract.
**Why:** Compat shims allow partial implementors to pass the compiler while violating the decider contract. Explicit declaration enforces correct behavior without silent no-ops.

### 2026-04-04T00:00:00Z: Full decider migration — implementation complete
**By:** C# Dev
**What:** Removed default `Decide`/`IsTerminal` from `Program.cs`. Made `Runtime.cs` decider-native — `Dispatch` now runs the full decide→transition pipeline. Added `_dispatchGate` SemaphoreSlim for command serialization. All builds and most tests pass. One failing E2E: `DeleteArticle_AsAuthor_ShouldNavigateToHome` (Playwright timeout on `.article-page`) — root cause identified as dispatch gate scope holding the lock over async HTTP effect awaits; tracked in #245.
**Why:** Completing the migration target set by the Architect. Runtime now enforces decider contract uniformly across all Program implementors.

### 2026-04-04T00:00:00Z: Program.Decide return type is Result<Message[], Message>
**By:** C# Dev
**What:** `Decide` returns `Result<Message[], Message>` (not `Result<Message[], Unit>`). Command validation failures are `Err<Message>` values dispatched through the runtime as error messages. Applied to `Program.cs`, `Runtime.cs`, `Conduit.cs`, all affected programs, templates, and tests.
**Why:** Errors must flow through the standard message pipeline rather than being silently discarded; typed `Err` enables explicit error handling in the update loop.

### 2026-04-04T00:00:00Z: Full decider migration audit — 🔴 dispatch gate scope regression tracked as #245
**By:** Reviewer
**What:** `_dispatchGate` is held for the entire command lifecycle including async HTTP effect awaits. This blocks navigation/subscription messages behind in-flight effects (head-of-line blocking). `DeleteArticle_AsAuthor_ShouldNavigateToHome` E2E times out as a result. Fix: narrow gate scope to the decide/transition critical section only, release before awaiting effects. 🟠 No concurrency fairness tests added for the new gate behavior. Tracked in GitHub issue #245.
**Why:** Gate scope being too wide re-introduces blocking on concurrent messages; E2E failure is a production correctness regression that must be fixed before release.

### 2026-04-15T09:16:51Z: User directive — Issue #243 scope reduction (superseded)
**By:** Maurice Peters (via Copilot)
**What:** Scope reduced for issue #243 to a simplified one-image-on-last-slide implementation only (no generic feature machinery).
**Why:** User directive captured; immediately superseded by the following scope correction.

### 2026-04-15T09:23:29Z: User directive — Issue #243 scope correction to generic image support feature
**By:** Maurice Peters (via Copilot)
**What:** Issue #243 is the full generic image support feature (framework-level image embedding in slides) plus a demo slide, not just the simplified one-image variant. Prior scope reduction directive is superseded.
**Why:** Clarification from user to restore the intended scope of the feature work.
### 2026-04-28T00:00:00Z: Express presentation dry run — narrative review (Technical Writer)
**By:** Senior Technical Writer (squad agent)
**Requested by:** Maurice Peters
**What:** Narrative and Dutch language review of `_expressSlides` ("Coderen met AI in 2026", 19 slides). 4 must-fix items: tools slide format conflict (pick table or bullets, not both), missing Deel 2→3 transition bridge, "compoundt over sessies" Dunglish (replace with "bouwt op over sessies"), and `metr-followup` delivery protection. 5 should-improve items flagged. Strong lines catalogued and preserved.
**Why:** Pre-conference dry run to validate structural soundness before delivery.
**Verdict:** Structurally sound. Ship it after applying must-fix items.

### 2026-04-28T00:00:00Z: Express presentation dry run — factual accuracy audit (Reviewer)
**By:** Reviewer (independent quality authority)
**Requested by:** Maurice Peters
**Status:** 🔴 Blocking until two must-fix items are resolved.
**What:**
- BLOCKING 1: Benchmark claim "vrijwel alle" on `picea-abies` slide overstates Abies's position (wins ~5/9, not almost all; 09_clear1k is 2.5× slower than Blazor). Required fix: replace with honest framing ("competitive with Blazor, beats on key creation benchmarks, gaps remain on clear and swap").
- BLOCKING 2: "51% dagelijks" attributed to both JetBrains AI Pulse January 2026 (`adoption` slide) and Stack Overflow 2025 (`productivity` slide) — one citation is wrong. Required fix: determine correct source and remove from the other.
- 10 non-blocking verify-before-presenting findings logged in orchestration log.
**Why:** Factual errors in a conference talk attributed to published surveys are verifiable live by the audience.

### 2026-04-28T00:00:00Z: Express presentation dry run — audience journey and visual design review (UX Expert)
**By:** UX Expert (squad agent)
**Requested by:** Maurice Peters
**What:** Three critical friction points: (1) `tools` slide density is projection-breaking — markdown table must be replaced with a visual chart or split across slides; (2) `trust` slide ASCII charts are the primary missed live demo opportunity — the usage/trust divergence (84% use, 29% trust) should be a real rendered chart as the demo payoff; (3) `picea-abies` at slide 18 introduces new complexity too late — reduce to 3 lines max. CTA "Begin bij de spec, niet bij de code" should appear in intro, not only on final slide.
**Verdict:** Talk lands despite the slides. Friction is in projection-hostile density and one missed visual payoff.
**Why:** Conference projector rendering and audience energy curve require different density decisions than document-mode slides.

### 2026-04-15T00:00:00Z: Express slides issue #2 number verification resolved
**By:** Reviewer
**Requested by:** Maurice Cornelius Gerardus Petrus Peters
**What:** Validated the blocking issue #2 percentages and source attribution. Stack Overflow 2025 is confirmed for both lines: "84% gebruikt of plant gebruik" and "51% van professionele developers gebruikt AI-tools dagelijks." The JetBrains AI Pulse URL in use (`https://www.jetbrains.com/lp/devecosystem-2025/ai-pulse/`) returned HTTP 404 at review time, so exact JetBrains percentages were removed from adoption framing.
**Why:** Prevent a live source-attribution contradiction in conference delivery and keep claims tied to a currently verifiable source.

### 2026-04-15T00:00:00Z: Express slide text updated to safe source-accurate wording
**By:** C# Dev
**Requested by:** Maurice Cornelius Gerardus Petrus Peters
**What:** Applied the requested express slide updates in `Picea.Abies.Presentation/Program.cs` under `_expressSlides`: adoption wording moved to safe non-numeric phrasing for JetBrains, Stack Overflow daily-use wording scoped to professional developers, and benchmark framing adjusted from overclaim language to balanced competitive wording.
**Why:** Incorporate reviewer fact-check corrections directly into the deck while preserving the narrative flow.

### 2026-09-02T00:00:00Z: Migration from `.squad/`-based framework layout to `.claude/`-based layout
**By:** Tech Writer (migration task)
**What:** Migrated the squad framework from the old layout (`.squad/agents/<name>/{charter.md,history.md}`, `.squad/routing.md`, `.squad/team.md`, `.squad/principles-enforcement.md`, `.squad/decisions.md`, `.squad/skills/`, `.squad/casting/`) to the current layout (`.claude/agents/`, `.claude/skills/`, `.claude/hooks/`, `.claude/docs/*.md`, `CLAUDE.md` as the Lead's charter). `.squad/` is now runtime state only (design artifacts, decisions inbox, session/orchestration logs). Old agent names map to new subagent names: `lead` → the orchestrator (`CLAUDE.md`, no separate agent file), `architect` → `architect`, `csharpdev` → `csharp-dev`, `jsdev` → `js-dev`, `techwriter` → `tech-writer`, `securitydev` → `security-expert`, `perfeng` → `performance-engineer`, `uxdev` → `ux-expert`, `devops` → `devops`, `reviewer` → `reviewer`. New agents with no old counterpart: `dreamer-first-principles`, `dreamer-informed`, `dreamer-convergence`, `realist`, `critic`, `spec-author`, `curator`.
**Why:** Bring Abies onto the current squad-template framework version so it benefits from the isolated-phase-agent design pass, the profiles system, and the current hook set. Old-layout teardown (`.squad/agents/`, `.squad/routing.md`, `.squad/team.md`, `.squad/principles-enforcement.md`) and conversion of per-agent `history.md` files into `.claude/agent-memory/` are handled separately.

### 2026-09-02T00:00:00Z: Abies diverges from squad-template on the Aspire-AppHost-only test rule (deliberate)
**By:** Tech Writer
**What:** As part of this migration, Abies's copy of the squad-template framework files now differs from the template on one specific point: the "Aspire AppHost Is the Test Fixture" convention (this file, Testing section) carries a named, closed exception permitting `WebApplicationFactory`/`TestServer` for four pre-existing fast in-memory test fixtures (`ConduitApiFactory.cs`, `Picea.Abies.Conduit.Api/Program.cs`, `EndpointTests.cs`, `OtlpProxyEndpointTests.cs`) — see that entry above for the full policy. Three framework files copied verbatim from squad-template (`.claude/agents/spec-author.md`, `.claude/skills/beast-mode-design/SKILL.md`, `.claude/skills/functional-ddd/SKILL.md`) still stated the template's unqualified "never `WebApplicationFactory`" rule for *new* work; they were updated to point at this file as authoritative rather than to restate the exception, so a project-specific carve-out doesn't have to be kept in sync across four copies. A fourth file, `.claude/docs/principles-enforcement.md`, also states the blanket rule (as an example of what counts as a deviation requiring approval) and was deliberately left completely unedited: this exception is itself a recorded instance of that protocol being followed, not a contradiction of it, so the enforcement text remains accurate as written.
**Why:** A future person diffing Abies against squad-template on this rule needs to know the divergence was deliberate — an explicit, documented user decision — so they don't "fix" it back to the template's blanket rule. See the amended entry above for the decision itself; this entry exists so the *fact of the divergence* is discoverable from the migration record, not just from the rule text.

<!-- legacy -->
### 2026-09-02 — csharpdev-auth-integration-slice

## Decision

For the first E2E-to-integration authentication migration slice, port invalid login by driving the real login message path:

- dispatch `LoginEmailChanged`
- dispatch `LoginPasswordChanged`
- dispatch and drain `LoginSubmitted`
- mock `LoginUser` to return `ApiError`

Do not inject `ApiError` directly in the migrated test. The value of this slice is proving that the reducer issues the login command with the entered credentials and that command failure flows back into the login page state and rendered error UI.

## Scope

Keep the slice local to `Picea.Abies.Conduit.Tests` and avoid expanding into additional auth scenarios until this pattern is established.

## Update 2026-05-05

For the next adjacent auth slice, valid login should use the same harness-first pattern:

- dispatch `LoginEmailChanged`
- dispatch `LoginPasswordChanged`
- dispatch and drain `LoginSubmitted`
- mock `LoginUser` to return `UserAuthenticated(session)`
- capture downstream `PersistSession`, authenticated-home feed fetch, and `NavigationCommand.Push` through `MockCommand<T>` side effects

Do not set authenticated model state directly in the migrated test. The useful assertion is that the success path flows through the same command batch the runtime uses after real authentication.

<!-- legacy -->
### 2026-09-02 — csharpdev-picea-1.0.0-migration

# csharpdev-picea-1.0.0-migration

Date: 2026-05-06
Owner: C# Dev

## Context

The requested migration updates Conduit project references from prerelease `Picea` to stable `1.0.0`.

Projects with direct `Picea` references were updated accordingly, but several Conduit projects also depend on `Picea.Glauca`.

## Observed Constraint

Published `Picea.Glauca` versions (`0.1.12`, `0.1.13`, `0.1.14`) depend on prerelease `Picea`:

- `0.1.12` -> `Picea >= 1.0.22-rc-0001`
- `0.1.13` -> `Picea >= 1.0.22-rc-0001`
- `0.1.14` -> `Picea >= 1.0.27-rc-0002`

With direct `Picea` pinned to `1.0.0`, restore fails (`NU1605`) for:

- `Picea.Abies.Conduit.Api`
- `Picea.Abies.Conduit.Api.Tests`
- `Picea.Abies.Conduit.ReadStore.PostgreSQL.Tests`

## Decision

Record this as a hard compatibility boundary:

- Keep direct `Picea` updates to `1.0.0` in scope.
- Do not perform broad event-store architecture rewrites in this migration step.
- Track Glauca compatibility as the gating item for full Conduit stabilization on `Picea 1.0.0`.

## Next Options

1. Publish a `Picea.Glauca` version compatible with stable `Picea 1.0.0`.
2. Replace Glauca usage in Conduit API/tests with alternative in-repo abstractions.


<!-- legacy -->
### 2026-09-02 — csharpdev-picea-glauca-temporary-pin

# C# Dev Decision Note: Temporary Picea Pin for Glauca-Coupled Conduit Projects

Date: 2026-05-06
Requested by: Maurice Cornelius Gerardus Petrus Peters

## Context

Conduit migration target is direct `Picea` `1.0.0`.

Current `Picea.Glauca` package line requires prerelease `Picea` versions (`>= 1.0.22-rc-0001` and currently `>= 1.0.27-rc-0002`), which triggers `NU1605` downgrade errors when Glauca-coupled projects pin direct `Picea` to `1.0.0`.

## Decision

Adopt migration option 1:

1. Keep all non-Glauca projects on direct `Picea` `1.0.0`.
2. For Glauca-coupled projects only, align direct `Picea` to the current Glauca compatibility floor (`>= 1.0.27-rc-0002`) as a temporary compatibility bridge.

Applied to:

- `Picea.Abies.Conduit.Api`
- `Picea.Abies.Conduit.Api.Tests`
- `Picea.Abies.Conduit.ReadStore.PostgreSQL`
- `Picea.Abies.Conduit.ReadStore.PostgreSQL.Tests`

## Rationale

This is the smallest targeted change that unblocks restore/build while preserving stable `Picea` `1.0.0` for all projects not coupled to Glauca.

## Exit Criteria

Remove temporary prerelease pins and return all Conduit projects to direct `Picea` `1.0.0` when either:

1. `Picea.Glauca` releases a version compatible with stable `Picea` `1.0.0`, or
2. Conduit removes/replaces Glauca coupling in the affected API/read-store/test paths.


<!-- legacy -->
### 2026-09-02 — reviewer-picea-1.0.0-review

# reviewer-picea-1.0.0-review

Date: 2026-05-06
Owner: Reviewer

## Decision

The current working-tree migration for `Picea` `1.0.0` is **not shippable** and must not merge as-is.

## Blocking Facts

1. Conduit restore fails with `NU1605` downgrade errors because Glauca requires prerelease `Picea` floors while direct references are pinned to `1.0.0`.
2. The change set mixes migration concerns with unrelated CI policy and large visual-regression infrastructure additions, making risk and rollback scope unclear.
3. New dependencies (`Microsoft.Playwright`, `SixLabors.ImageSharp`) were introduced without recorded dependency-approval evidence required by principles enforcement.

## Required Next Step

Split into focused deliverables:

1. **Migration-only branch/PR** that contains package/docs/changelog/version updates and restores cleanly.
2. **CI policy branch/PR** for E2E trigger/gating changes, with explicit approval for PR-gate removal.
3. **Visual regression branch/PR** for test harness + snapshots + workflow, with dependency approval and baseline maintenance policy.

## Compatibility Remediation Options

1. Publish `Picea.Glauca` compatible with stable `Picea 1.0.0` and keep direct pins at `1.0.0`.
2. Temporarily align direct pins to the Glauca transitive floor (`>= 1.0.27-rc-0002`) until compatible Glauca ships.
3. Remove/replace Glauca usage in affected Conduit API/test paths with in-repo abstractions.


<!-- legacy -->
### 2026-09-02 — reviewer-picea-glauca-option1-ship-readiness

# reviewer-picea-glauca-option1-ship-readiness

Date: 2026-05-06
Owner: Reviewer

## Decision

Option 1 (temporary Glauca compatibility pin) is shippable for the migration objective.

## Why

1. Prerelease direct `Picea` pin is scoped only to Glauca-coupled projects.
2. Non-Glauca projects remain pinned to stable `Picea` `1.0.0`.
3. Solution restore succeeds and no `NU1605` downgrade blocker remains.
4. Migration documentation explicitly labels the strategy as temporary and includes concrete exit criteria.

## Guardrails

1. Treat prerelease `Picea` pins required by Glauca (`>= 1.0.27-rc-0002`) as temporary compatibility debt.
2. Remove temporary pins once a Glauca release supports stable `Picea` `1.0.0` or Glauca coupling is removed from Conduit API/read-store/test paths.
3. Keep this migration slice focused; unrelated CI/workflow/test-infra changes should ship in separate PRs.


### 2026-09-02 — reviewer-20260902T144500Z-pr355-hooks [reviewer · NEEDS-CHANGES]

---
id: reviewer-20260902T144500Z-pr355-hooks
agent: reviewer
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-02T14:45:00Z
targets:
  - path: .claude/hooks/
    lines: "all 11 scripts"
  - path: .claude/settings.json
    lines: "1-111"
  - path: .claude/skill-router.json
    lines: "1-74"
  - path: .claude/hooks/tests/
    lines: "run.sh, agent_identity.py, 20 fixtures"
blockers:
  - file: .claude/settings.json
    line: 16
    reason: "statusLine command points at .claude/statusline.py, which does not exist in the repo. With refreshInterval 10 this fails every 10s, and the .squad/.last-review-verdict and .squad/.hooks-ok writes in three hooks have no consumer."
  - file: .claude/hooks/enforce-conventional-commits.sh
    line: 47
    reason: "Greedy sed extraction takes the LAST -m, so the standard two-flag form git commit -m <subject> -m <body> validates the BODY against the subject pattern and is blocked. Verified live."
  - file: .claude/hooks/enforce-conventional-commits.sh
    line: 29
    reason: "All four PreToolUse hooks gate on a bare substring match for git commit in raw command text. Verified false positive (a non-commit command containing that text was blocked) and false negative (git -C <path> commit bypasses all four gates). This is the exact defect class the PR says it avoided by not installing block-direct-commits-to-main.sh and validate-branch-name.sh."
  - file: .claude/hooks/scribe-decision-merger.sh
    line: 217
    reason: "Nested-key promotion lets any drop forge agent reviewer and verdict PASS. Verified end-to-end: a csharp-dev NEEDS-CHANGES drop with a nested meta block was archived as reviewer PASS and wrote PASS to .last-review-verdict, also bypassing the verdict-vs-blockers consistency check. Known bug per the in-code TODO; needs explicit user sign-off or a fix before shipping."
  - file: .github/agents/squad.agent.md
    line: 73
    reason: "Workflow removal is partial. This live Copilot agent definition still directs a coordinator to .squad/team.md, .squad/routing.md, .squad/agents/*/charter.md and the four deleted workflows, all removed by this PR. .gitattributes line 3 also still references .squad/agents/*/history.md."
  - file: .claude/docs/decisions.md
    line: 254
    reason: "States branch naming is mechanically enforced by the validate-branch-name hook in .claude/hooks/, which this PR deliberately does not install. git-advanced and gh-cli SKILL.md make the same claim for block-direct-commits-to-main.sh. A false claim about an enforcement gate in the authoritative conventions doc."
high:
  - file: .github/workflows/
    reason: "No CI job runs .claude/hooks/tests/run.sh, despite the suite's own header assuming a paths-scoped CI job. 67 regression assertions with zero automated execution."
  - file: .claude/hooks/enforce-no-secrets.sh
    reason: "Uses the deprecated gitleaks protect --staged while .githooks/pre-commit in the same repo already uses the current gitleaks git --staged. On a gitleaks major bump the unknown-subcommand exit lands in the catch-all branch and blocks every commit with a misleading malformed .gitleaks.toml message."
  - file: .claude/hooks/enforce-gpg-signing.sh
    reason: "Lines 215-219 hardcode one contributor's GPG key id and personal email as the reference setup in a tracked file in a public repo. Any other contributor copy-pasting it configures a key they do not hold, and check 5 then blocks all their commits."
  - file: .claude/hooks/dotnet-format-on-save.sh
    reason: "Claims .csproj/.props/.targets are in scope; dotnet format does not touch MSBuild XML (verified). Each such edit pays a full 48-project workspace load (12.8s warm) for zero effect, and rapid successive edits stack concurrent workspace loads."
  - file: .claude/settings.json
    reason: "Enables CLAUDE_CODE_ENABLE_TELEMETRY and the enhanced-telemetry beta for every contributor via a tracked file, with OTLP gRPC and no endpoint, defaulting to localhost:4317 - the port Aspire's dashboard binds. Belongs in the gitignored settings.local.json."
  - file: .squad/decisions/inbox/
    reason: "Five pre-schema drops carried over from main are inlined wholesale (171 lines) into decisions.md by the merger's LEGACY path on the first SubagentStop, unreviewed. Observed live in this working tree during the review."
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "Header says entries land under the Session Decisions anchor; the code appends at EOF and drop bodies carry their own H2 headings, so the last H2 in decisions.md is now an unrelated Guardrails from a prior drop."
medium:
  - file: .claude/hooks/enforce-no-secrets.sh
    reason: "Missing --redact (the sibling .githooks/pre-commit passes it); config_arg is unquoted at line 94 so a project path with a space breaks the flag; duplicates coverage the git pre-commit hook already provides for all commits."
  - file: .claude/hooks/squad-rotate.py
    reason: "Dead code: unused mode at line 87, a .gitkeep guard inside a *.md glob at line 172. precompact-snapshot.py line 130 assigns an unused today."
  - file: .github/workflows/pr-validation.yml
    reason: "isMaintenancePath and the NON_DOCS filters still allowlist .squad/ but not .claude/, where framework prose now lives."
good:
  - file: .claude/hooks/tests/run.sh
    reason: "67 of 67 pass in 1.1s. Coverage is real, not decorative: the pre-fix fixtures are sha256-pinned and asserted to quarantine for their NAMED reason rather than any reason, the roster invariant has a working negative control, and a decisions.md content grep closes the archived-but-never-appended gap."
  - file: .claude/hooks/session-context-loader.py
    reason: "All five python hooks verified to exit 0 on empty and malformed stdin; none can wedge a session."
  - file: .gitignore
    reason: "Correctly ignores the precompact transcript copies, snapshots and per-machine caches, which would otherwise commit full conversation transcripts."
references: []
---

## Findings

See the reviewer's detailed report for the full write-up. Verdict is
NEEDS-CHANGES on six blockers, the most serious being a verified forgery
path through the decision-drop validator that lets any agent record a
Reviewer PASS it never earned.


### 2026-09-02 — reviewer-20260902T160000Z-pr355-blocker-refix [reviewer · NEEDS-CHANGES]

---
id: reviewer-20260902T160000Z-pr355-blocker-refix
agent: reviewer
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-02T16:00:00Z
targets:
  - path: .claude/hooks/scribe-decision-merger.sh
    lines: "220-280"
  - path: .claude/hooks/lib/
    lines: "git_commit_detect.py, git-commit-detect.sh"
  - path: .claude/statusline.py
    lines: "1-197"
  - path: .claude/skills/git-advanced/SKILL.md
    lines: "15"
  - path: .claude/docs/tech-stack.md
    lines: "71"
  - path: .github/workflows/claude-hooks-tests.yml
    lines: "16-26"
blockers:
  - file: .claude/hooks/scribe-decision-merger.sh
    line: 275
    reason: "Blocker 4 NOT closed. The new guard computes indent = len(line) - len(line.lstrip(' ')) -- spaces only. Indenting the nested agent:/verdict: with a tab, NBSP, ideographic space, vertical tab, form feed or em space yields indent 0, the guard never fires, and the forgery lands unchanged. Verified live against the guarded hook: a drop declaring agent: csharp-dev / verdict: INFO at column 0 archived as '[reviewer . PASS]' and wrote PASS to .squad/.last-review-verdict. All six whitespace variants reproduce."
  - file: .claude/hooks/scribe-decision-merger.sh
    line: 280
    reason: "Second, space-only exploit using the deliberately-unguarded blockers key, answering the adversarial question directly: agent: reviewer / verdict: PASS at column 0 with a real non-empty top-level blockers list plus a nested 'notes:\\n  blockers: []' is archived as [reviewer . PASS] and writes the verdict cache. This defeats the verdict-vs-blockers consistency check, which is the only thing currently rejecting the space-indented fixture -- so devops's 'closed regardless' argument rests on a check that the same unguarded mechanism disables."
  - file: .claude/skills/git-advanced/SKILL.md
    line: 15
    reason: "Newly introduced false claim. States as a 'Known defect' that enforce-conventional-commits.sh 'takes the last -m, so the body gets validated against the subject pattern', and tells contributors to avoid the two-flag form 'until this is fixed' -- but Blocker 2 fixed exactly that in this PR. Verified: git commit -m 'fix(auth): reject expired tokens' -m 'body' now exits 0. The advice steers contributors away from the form the repo's own attribution requirement uses."
  - file: .claude/docs/tech-stack.md
    line: 71
    reason: "Corrected count is already wrong. Says '**Verified** -- 15 workflows' and enumerates 15, but the tree has 16 because this same PR adds .github/workflows/claude-hooks-tests.yml, which is absent from the list. CLAUDE.md:101 repeats '15 workflows'. This is the second false claim from this file pair in two passes (the first being branch protection)."
high:
  - file: .claude/hooks/lib/git_commit_detect.py
    reason: "Coverage regression vs. the substring match it replaces, not in the header's documented scope limits: an env-assignment prefix (GIT_AUTHOR_DATE=... git commit), sudo git commit, env X=1 git commit, and a parenthesised subshell (git commit ...) all now return GIT_COMMIT_MATCH=0 where the old code matched. Skipping leading NAME=VALUE tokens and sudo/env wrappers is a few lines; at minimum document the class."
  - file: .github/workflows/claude-hooks-tests.yml
    reason: "paths: omits .claude/agents/** and .claude/docs/decision-schema.md. run.sh line 114 asserts ALLOWED_AGENTS == roster(.claude/agents/) union {lead} against the real directory -- proved empirically: dropping one agent file in takes the suite to 95/96. run.sh's own header (line 530) names decision-schema.md as an intended trigger. Adding an agent will go green in CI and surface later on an unrelated hooks PR."
  - file: .claude/statusline.py
    reason: "Violates its own 'MUST NOT raise' constraint in two paths outside the try: resolve_project_dir() calls Path.cwd(), which raises FileNotFoundError on a deleted cwd when CLAUDE_PROJECT_DIR is unset; and print(line) emits U+2013 and the EU flag emoji unconditionally, raising UnicodeEncodeError under an ASCII stdout. Both reproduced. Real-world probability is low (PEP 538/540 covers the plain C locale), but the fix is moving two lines inside the existing try plus a reconfigure(errors='replace')."
  - file: .claude/hooks/dotnet-format-on-save.sh
    reason: "The mkdir-lock fallback exists for macOS, but its stale-lock check uses `date -r \"$lock_mkdir\"`, which is GNU semantics; BSD/macOS date -r takes epoch seconds, so it fails, `|| echo 0` makes lock_age enormous, and every waiter immediately reclaims the lock -- no serialization on the one platform the branch is for. Use stat -f %m / stat -c %Y with a fallback."
medium:
  - file: .claude/hooks/enforce-no-secrets.sh
    reason: "gitleaks_out=\"$(cd \"$target_dir\" ... && gitleaks ...)\" -- a directory that exists but is not cd-able makes the subshell exit 1, which the dispatch reads as 'leaks found' and blocks with an empty report. The [ ! -d ] guard above does not cover it."
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "The verdict-cache created: extraction uses grep -E '^created:' | head -n1 (first column-0 match) while the python parser takes the last top-level write. Divergent tie-breaking if a drop carries two created: lines; only affects latest-wins ordering."
good:
  - file: .claude/hooks/lib/git-commit-detect.sh
    reason: "Both cited defects genuinely closed, and closed at the right layer. Independently verified all four hooks: the false positive (echo see: git commit -m msg) no longer fires on any of them, and git -C <other-repo> commit is not merely parsed but actually acted on -- block-large-files reported the other repo's 6.7MB blob while cwd was Abies and the Abies control passed, and enforce-no-secrets caught a github-pat planted in the -C target. --git-dir/--work-tree resolves identically."
  - file: .claude/hooks/enforce-conventional-commits.sh
    reason: "First--m anchoring is correct and has a working negative control: the two-flag form passes, a bad first -m still blocks even when a later -m looks conventional, and --message=/-F/compound-&& forms all behave. The heredoc fail-open is argued honestly in the header -- it names the tradeoff, names why (evaluating extracted shell inside a security hook is worse), and admits it is the repo's own mandated form rather than burying that."
  - file: .claude/hooks/tests/run.sh
    reason: "96/96 verified independently, and 96/96 again with gitleaks, gpg and dotnet shimmed to exit 127 -- the CI job will pass on a fresh runner, and the suite is explicit at lines 1155-1160 about scoping the gpg/gitleaks tests to argv-parsing only. check_forgery_regression asserts the exact quarantine reason string, so the test itself records that the rejection comes from the consistency check rather than an identity check."
  - file: .github/agents/squad.agent.md
    reason: "Retirement is clean. Confirmed independently: no references remain anywhere outside the archived record of the original finding, beast-mode.agent.md has zero dependence on the deleted layout, and .gitattributes' surviving .squad/agents mention is explanatory comment only."
references:
  - "https://github.com/MCGPPeters/squad-template/issues/8"
---

## Findings

Four of six blockers close. Blocker 4 does not: the indent guard counts
spaces only, so a tab- or NBSP-indented nested key still forges
`[reviewer · PASS]` into `decisions.md` and `.squad/.last-review-verdict`,
and a second space-only exploit through the unguarded `blockers` key
disables the consistency check that is currently the sole thing rejecting
the fixture. Blockers 5 and 6 each acquired one new false doc claim
introduced by this PR.

