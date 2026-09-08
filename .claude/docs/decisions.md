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


### 2026-09-02 — reviewer-20260902T185118Z-pr355-round3 [reviewer · NEEDS-CHANGES]

---
id: reviewer-20260902T185118Z-pr355-round3
agent: reviewer
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-02T18:51:18Z
targets:
  - path: .claude/hooks/scribe-decision-merger.sh
    lines: "100, 208, 227, 325"
  - path: .claude/skills/git-advanced/SKILL.md
    lines: "15, 228"
  - path: .github/workflows/pr-validation.yml
    lines: "49-69, 164-186"
  - path: .github/workflows/codeql.yml
    lines: "62-82"
  - path: .github/workflows/cd.yml
    lines: "47-67"
  - path: .github/workflows/claude-hooks-tests.yml
    lines: "16-36"
  - path: .claude/docs/tech-stack.md
    lines: "71-72"
blockers:
  - file: .claude/hooks/scribe-decision-merger.sh
    line: 100
    reason: "Blocker 4, third bypass. `open(path, \"r\", encoding=\"utf-8\")` uses universal-newline translation (newline=None), so every lone CR is rewritten to LF before the parser runs -- the new strict splitter `re.split(r\"\\r\\n|\\r|\\n\", fm)` can never see a CR and is a no-op for it. Indenting the nested block with a bare CR instead of a tab makes those lines genuinely column-0, the guard never fires. Verified end-to-end against the live hook: a drop whose grep-visible column-0 declaration is `agent: csharp-dev` / `verdict: NEEDS-CHANGES` with a real non-empty blockers list archived as `[reviewer - PASS]` and wrote PASS to .squad/.last-review-verdict. Also a grep/python line-model differential: `grep -n '^agent:'` on that file matches only the csharp-dev line. Candidate fix verified: `newline=\"\"` on the open plus `re.split(r\"\\r\\n|\\n\", fm)` closes it, leaves fixtures 19/20/23/24/26 correct and keeps the suite at 146/146. Needs a fixture 27."
  - file: .claude/skills/git-advanced/SKILL.md
    line: 15
    reason: "New false claim about an enforcement gate, in the sentence rewritten this round to fix a different false claim about the same hook. Documents the accepted Conventional Commit types as `feat fix chore docs refactor test perf build ci style revert`. Verified live against enforce-conventional-commits.sh line 160 for all twelve types: `style:` and `revert:` are REJECTED (exit 2) and `security:` is ACCEPTED but not listed -- and `security` is one of the ten types decisions.md's own table documents. Same wrong list repeated at line 228. Replace both with the hook's actual set: feat, fix, docs, refactor, test, perf, security, ci, build, chore."
high:
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "Closing the CR case will not make .squad/.last-review-verdict trustworthy, and the in-code comment at line 290 (\"the impersonation is closed\") reads stronger than the code supports. Verified: a plain, un-nested, entirely well-formed drop declaring `agent: reviewer` / `verdict: PASS` / `scope: review` / `blockers: []` at column 0 archives as `[reviewer - PASS]` and writes PASS to the cache. `agent` is self-asserted and the SubagentStop payload carries no authorship channel, so the guard can only close parsing-differential impersonation, not impersonation as such. Say so at the top of the file and in decision-schema.md, or round four re-derives it."
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "LEGACY fall-through still reachable with non-`\\s` leading characters, the same class the comment block at lines 106-141 claims closed for the single BOM. Verified: a doubled BOM, U+200B, U+180E, U+2060 or U+00AD before the opening fence makes a drop declaring `agent: bogus-not-a-real-agent` / `verdict: TOTALLY-FINE` archive silently under a `<!-- legacy -->` marker with the whitelist, verdict-enum, scope-enum and blockers-consistency checks all skipped, instead of quarantining. It cannot write the verdict cache, which is why this is high and not a blocker. Strip/reject leading format-control characters before the fence match."
medium:
  - file: .github/workflows/pr-validation.yml
    reason: "The shell filter uses `.*\\.gitkeep$` while the JS isMaintenancePath uses `/(^|\\/)\\.gitkeep$/`. The comment says to keep the two in step; the shell form also matches a file literally named e.g. `notreally.gitkeep`. Harmless today, but make them the same shape."
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "Carried from round 2 and still open: the verdict-cache `created` extraction uses `grep -E '^created:' | head -n1` (first column-0 match) while the python parser takes the last top-level write. Now largely moot -- `created` is self-declared, so the latest-wins tie-break is gameable with an honest future timestamp regardless -- but taking `created` from the validator's stdout instead of re-grepping removes the divergence for free."
good:
  - file: .claude/hooks/tests/run.sh
    reason: "146/146 verified independently. The devops claim that the strict line splitter is load-bearing is correct and I proved it: reverting only `re.split(...)` back to `fm.splitlines()` while keeping the whitespace-aware `lstrip()` still forges `[reviewer - PASS]` and writes PASS for fixtures 23 (vertical tab) and 24 (form feed), while fixture 20 (tab) stays closed. The two fixes are independently necessary and the fixtures pin both. Every whitespace fixture also has a working positive control against a vendored pre-fix hook."
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "The guard now holds against all 17 characters Python's str.isspace() recognises, including U+001C-U+001E, NEL, LS and PS, which have no fixture -- all archive under the true identity csharp-dev/NEEDS-CHANGES. The five non-space format-control characters (U+200B, U+FEFF, U+180E, U+2060, U+00AD) survive into the key name and so cannot shadow a guarded field either. The warning that adding a new schema field or consistency check without adding its key re-opens the bypass is accurate and is the right thing to have written down."
  - file: .github/workflows/pr-validation.yml
    reason: "The path-filter conclusion is right and the enumeration behind it holds. `.claude/` needs no entry: `.*\\.md$` / `path.endsWith('.md')` already cover all prose under it, and .claude/hooks/**, statusline.py, settings.json and skill-router.json correctly fall through as code. The bare `.squad/` prefix was the real defect. Verified at HEAD: .squad/ tracks exactly 41 .md plus 3 .gitkeep, and every non-.md runtime artefact (.last-review-verdict, .hooks-ok, .signing-health, .locks/, transcripts, snapshots, *.log) is gitignored, so it can never reach a changed-file list. Behaviour for the real contents is unchanged and a .squad/*.sh would now correctly get the full pipeline."
  - file: .claude/skills/gh-cli/SKILL.md
    reason: "The ruleset claim is accurate and attributed to the right ruleset, which is the part that could easily have been wrong. Verified against the live API: `main` has two active rulesets; `Protect main` (6177598) has bypass actors OrganizationAdmin and RepositoryRole 5 with bypass_mode always, but carries only deletion/non_fast_forward/pull_request. Every rule the claim rests on -- pull_request, required_linear_history and the exact five required checks build / Validate PR Title / Validate PR Description / e2e / Analyze C# Code -- lives in `protectmainbranch` (12483698), which has zero bypass actors and current_user_can_bypass: never. The hedge about a future ruleset change is the right caveat."
  - file: .claude/docs/tech-stack.md
    reason: "Dropping the hand-maintained count was the right call after two stale passes, and the replacement is exact: the 16 enumerated workflow filenames match `ls .github/workflows/` with zero difference in either direction, the list is framed as point-in-time, and the reader is pointed at `ls | wc -l` instead of a number. The squad-*.yml removal claim is also true -- those four were deleted in 7f5d7d2, the PR's own first commit."
  - file: .claude/skills/git-advanced/SKILL.md
    reason: "Every behavioural claim about enforce-conventional-commits.sh other than the type list is verified live: first -m/-F anchoring (two-flag form passes, bad first -m blocks even with a good later -m, -m before a bad -F wins, bad -F before a good -m blocks), and the `$(...)`/heredoc fail-open, which really does exit 0 with a bogus subject. Naming that gap in the same breath as the recommendation is the honest shape."
references:
  - "https://github.com/MCGPPeters/squad-template/issues/8"
---

## Findings

Third pass on PR #355. Four of the six original blockers are closed and stay
closed. Blocker 4 is partially closed: the whitespace-indent and
`blockers`-shadow bypasses from round 2 are genuinely fixed and well
fixtured, but a bare CR used as the indentation prefix still forges
`[reviewer · PASS]` — universal-newline translation at `open()` rewrites it
to LF before the new strict splitter can ever see it, so the splitter is a
no-op for the one line terminator that arrives for free. Blocker 6 is
partially closed: the branch-name and `--admin` claims are now correct and
verified against the live ruleset API, but the same skill file states a
Conventional Commit type list that the hook rejects in two entries and is
missing a third.

Confirmed still open, not re-derived: `enforce-no-secrets.sh`'s non-cd-able
`target_dir` reading as "leaks found", `dotnet-format-on-save.sh`'s GNU
`date -r`, `statusline.py`'s two paths outside the `try`, and the
`git_commit_detect.py` under-match on `env`/`sudo`/subshell prefixes.


### 2026-09-02 — reviewer-20260902T193000Z-pr355-round4 [reviewer · NEEDS-CHANGES]

---
id: reviewer-20260902T193000Z-pr355-round4
agent: reviewer
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-02T19:30:00Z
targets:
  - path: .claude/hooks/scribe-decision-merger.sh
    lines: "197-217, 525"
  - path: .claude/hooks/lib/git_commit_detect.py
    lines: "45-72, 199-210"
  - path: .github/workflows/claude-hooks-tests.yml
    lines: "17-35"
  - path: .claude/hooks/enforce-no-secrets.sh
    lines: "115-140"
  - path: .claude/hooks/dotnet-format-on-save.sh
    lines: "169-206"
  - path: .claude/statusline.py
    lines: "60-82, 205-228"
  - path: .claude/docs/decision-schema.md
    lines: "166"
  - path: .claude/skills/git-advanced/SKILL.md
    lines: "15, 228"
  - path: .github/workflows/pr-validation.yml
    lines: "76, 195, 203-225"
blockers:
  - file: .claude/hooks/scribe-decision-merger.sh
    line: 215
    reason: "LEGACY fall-through is still open, and the new comment claims it is closed. `while text and unicodedata.category(text[0]) == \"Cf\"` strips only a LEADING RUN of Cf, but the comment says it 'closes the whole class instead of pinning one more instance of it'. It pinned one more instance. Six live reproductions against the current hook, each a drop with plainly-present, grep-visible front matter declaring `agent: bogus-not-a-real-agent` / `verdict: TOTALLY-FINE` / `scope: nonsense-scope`, all archived silently under `<!-- legacy -->` with ALLOWED_AGENTS/ALLOWED_VERDICTS/ALLOWED_SCOPES and the blockers-consistency check skipped, and the body `cat`'d verbatim into decisions.md (a forged `### ... [reviewer · PASS]` heading in the body lands in the authoritative conventions file): (1) one ASCII space then a BOM before the fence -- defeats the Cf loop outright; (2) leading NUL U+0000 (Cc); (3) leading BEL U+0007 (Cc); (4) leading combining acute U+0301 (Mn); (5) BOM, newline, BOM; (6) a drop written entirely with CR-only line endings. Case (6) is a REGRESSION INTRODUCED BY THIS ROUND: the vendored pre-fix hook (newline=None) quarantines it correctly for 'unknown agent'; with newline=\"\" the fence regex's `\\r?\\n` no longer matches and it falls to LEGACY. Verified fix, applied and re-run: replace the bare `print(\"LEGACY\")` with a fence-presence check first -- `if re.search(r\"(?m)^---\", text): print(\"front-matter fence present but file does not start with it\"); sys.exit(1)`. That closes all six at the outermost layer instead of enumerating one more character class, and the suite stays at 173 passed / 0 failed. Cannot write .last-review-verdict, so this is narrower than blocker #4 was -- but it is a validation bypass on decisions.md reachable with a one-byte prefix, and shipping the 'closes the whole class' comment is what buys a fifth round."
high:
  - file: .github/workflows/claude-hooks-tests.yml
    reason: "New coverage gap of exactly the shape fixed in round 2 for `.claude/agents/**`. run.sh line 1759 now sets STATUSLINE_PY=\"$REPO_ROOT/.claude/statusline.py\" and four of this round's new assertions exercise it, but `.claude/statusline.py` is in neither the pull_request nor the push `paths:` filter. An edit to statusline.py that re-breaks either MUST-NOT-RAISE path goes green in CI and surfaces later on an unrelated hooks PR. Add `.claude/statusline.py` to both filters. Confirmed by grep: the only run.sh references outside `.claude/hooks/` are `.claude/agents` (filtered) and `.claude/statusline.py` (not filtered)."
  - file: .claude/hooks/lib/git_commit_detect.py
    reason: "The documented limits do not fully match behaviour, which is what was asked. The `else: # \"env\"` branch comment says '`env` accepts its own flags (-i, -0, -u NAME, ...)', but the loop only skips tokens starting with `-`, so `-u`'s value is left as argv[0]. Verified live: `env -u FOO git commit -m \"x\"` returns no match, while `env -i`, `env --unset=FOO`, `env X=1`, `sudo`, `sudo env X=1` and both subshell forms all match. `sudo -u user` is documented in the module docstring's scope-limits section; `env -u NAME` is not documented anywhere and the inline comment reads as if it is handled. Either consume the value (`if argv[0] in (\"-u\",) and len(argv) > 1: argv = argv[2:]`) or list `env -u NAME` alongside `sudo -u user` in the scope-limits block. Fail-open either way, so it is accuracy, not a security regression."
medium:
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "Carried from rounds 2 and 3, still open at line 525: the verdict-cache `created` extraction uses `grep -E '^created:' | head -n1` (first column-0 match) while the python parser takes the last top-level write. Taking `created` from the validator's stdout instead of re-grepping removes the divergence for free."
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "A drop whose `agent:` is a YAML block list (`agent:\\n  - reviewer`) fails closed, but by raising `TypeError: cannot use 'list' as a set element` -- the raw Python traceback is written verbatim into the .reason file. Correct outcome, unhelpful message. Coerce non-str field values to a rejection reason before the whitelist checks."
  - file: .claude/hooks/tests/fixtures-pre-fix/lib/git-commit-detect.sh
    reason: "Eight vendored pre-fix fixtures are sha256-pinned and all eight pins verify. This ninth vendored file is not pinned, so it can drift without the suite noticing. Pin it like the rest."
good:
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "Blocker #4 is closed for the parsing-differential class, and it holds under re-attack. All four rounds of exploits re-run against the current hook as regressions and every one now archives under the true identity `[csharp-dev · NEEDS-CHANGES]` with no verdict-cache write: space-indented nesting, tab, NBSP, vertical tab, form feed, the blockers-shadow (quarantined for 'verdict PASS but blockers list is non-empty'), and the round-3 bare-CR indent. Encoding-level attacks fail closed to quarantine, not to LEGACY: UTF-16 and invalid UTF-8 both hit `unreadable: 'utf-8' codec can't decode byte 0xff`. No NFC/NFKC differential exists -- nothing normalises, so a fullwidth `ａgent:` never becomes `agent`. A CR-only nested block inside an LF-fenced drop quarantines for 'missing required fields'."
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "The devops claim that `newline=\"\"` and the narrowed `re.split(r\"\\r\\n|\\n\", fm)` are BOTH necessary is correct, verified the same way as the round-3 splitter: reverting the splitter alone (keeping newline=\"\") re-forges `[reviewer · PASS]` and writes PASS for the CR fixture; reverting newline=\"\" alone (keeping the narrowed splitter) does the same. Neither is redundant. The vendored `scribe-decision-merger.pre-cr-newline-fix.sh` is a working positive control -- it reproduces the CR forgery end-to-end -- and all eight sha256 fixture pins verify."
  - file: .claude/docs/decision-schema.md
    reason: "The self-declaration limitation text is genuinely correct, not merely less wrong. It draws the right line -- parsing bugs closed does not equal identity authenticated -- names the reason (the SubagentStop payload has no authorship channel), tells the reader how to treat `.last-review-verdict` and the `[agent · verdict]` heading, and puts the real fix upstream of the parser rather than promising another validator patch. Verified against behaviour: an honest column-0 `agent: reviewer` / `verdict: PASS` / `blockers: []` drop archives as `[reviewer · PASS]` and writes PASS, exactly as documented. The matching in-code comments (file header and the parse-loop block) are scoped the same way."
  - file: .claude/hooks/dotnet-format-on-save.sh
    reason: "The stdout-contamination bug devops reports catching mid-fix is real and the fix is right. Reproduced on this Linux box: GNU `stat -f %m <path>` exits 1 but prints a six-line filesystem-info block to STDOUT, so a bare `cmd1 || cmd2 || echo 0` would have captured it into lock_mtime and made `$(( ))` a syntax error -- wedging the mkdir-lock fallback harder than the GNU-only `date -r` it replaces. Regex-validating each dialect's output as `^[0-9]+$` before accepting it closes that regardless of which way the wrong dialect fails. Confirmed the shipped sequence yields lock_age=0 on a real directory."
  - file: .claude/skills/git-advanced/SKILL.md
    reason: "Type-list alignment verified independently across all four sources, which now read the identical ten in the identical order -- feat fix docs refactor test perf security ci build chore: the hook's regex at line 160, the decisions.md table at 220-231, pr-validation.yml's `types:` block, and SKILL.md at 15 and 228. Exercised the hook live for all twelve candidate types: the ten exit 0, `style:` and `revert:` exit 2. The new Merge/Revert parenthetical is accurate rather than conflated -- `Merge branch x` and `Revert \"feat: y\"` exit 0 via the subject-prefix case, while `Mergefoo bar` and lowercase `merge branch x` exit 2, so it really is a prefix special case and really is distinct from a `revert:` type."
  - file: .github/workflows/pr-validation.yml
    reason: "`(.*/)?\\.gitkeep$` is equivalent to the JS `/(^|\\/)\\.gitkeep$/` and is applied identically in all three shell filters (pr-validation, codeql, cd). Checked both patterns over `.gitkeep`, `a/.gitkeep`, `a/b/.gitkeep`, `notreally.gitkeep`, `a/notreally.gitkeep`, `.gitkeeps`, `x.gitkeep` -- identical results on every one. No similar error remains in those filters: `docs/`, `\\.github/instructions/` and `LICENSE$` are all anchored by the group's leading `^`, and `.*\\.md$` matches the JS `path.endsWith('.md')`."
  - file: .claude/hooks/enforce-no-secrets.sh
    reason: "`[ ! -d \"$target_dir\" ] || ! (cd \"$target_dir\" 2>/dev/null)` is the right fix at the right layer -- it tests cd-ability directly, in its own subshell so it cannot move this script's cwd, and lands on the same fail-open posture as the plain `-d` case. The gitleaks-stub fixturing keeps the assertion CI-independent, and the paired vendored pre-fix hook proves the old form really did block on a phantom finding."
  - file: .claude/statusline.py
    reason: "Both MUST-NOT-RAISE paths are closed and the degradation is clean, not garbled. Under PYTHONIOENCODING=ascii the script exits 0 and emits a single well-formed line (`claude | ? | last-review:? | inbox=0 Q=0 | sign:? | hooks:never`) with no partial-write duplication from the failed first print. Both regression tests have working positive controls against the vendored pre-fix copy that assert the specific exception name (FileNotFoundError, UnicodeEncodeError) rather than merely a non-zero exit."
references:
  - "https://github.com/MCGPPeters/squad-template/issues/8"
---

## 👁️ CODE REVIEW — PR #355 round 4 (uncommitted working tree on b93b430)

### Holistic Assessment

**Motivation:** Justified. Every change traces to a named, reproduced finding from
rounds 1–3, and two of them (the `stat` stdout contamination, the generalised
leading-format-character strip) were found by devops rather than handed to it.

**Approach:** Right, with one exception. The merger fixes finally moved to the
correct layer — `open(newline="")` is upstream of the splitter, which is upstream
of the indent guard — and I confirmed both halves are independently load-bearing.
The leading-character fix did not: it enumerated one more character class (`Cf`)
where the layer above it (fence presence) was the actual boundary.

**Verdict:** 🔴 Changes Requested

Blocker #4 — the headline, four rounds running — is **closed**. I re-ran every
exploit from rounds 1, 2 and 3 plus new encoding-, normalisation- and
fence-level attacks; the parsing-differential forgery class holds, and the
residual self-declaration case is now documented accurately in three places. The
one blocker below is a *different, lower-severity* finding: the LEGACY
fall-through I rated `high` in round 3 is still open via six routes, one of them
newly introduced by this round's `newline=""`, and the in-code comment asserts it
is closed. I have a verified four-line fix that closes all six and keeps the
suite at 173/173.

### Test suite

`bash .claude/hooks/tests/run.sh` → **`Summary: 173 passed, 0 failed`**. Confirmed
independently, and again at 173/173 with the candidate fix applied.

### Findings

#### 🔴 Must Fix (blocks merge)

**`.claude/hooks/scribe-decision-merger.sh:215`** — see the `blockers` entry.
Six reproductions; case (6), CR-only line endings, is a regression this round
introduced. Verified fix:

```python
m = re.match(r"\s*---[ \t]*\r?\n", text)
if not m:
    if re.search(r"(?m)^---", text):
        print("front-matter fence present but file does not start with it")
        sys.exit(1)
    print("LEGACY")
    sys.exit(0)
```

It catches the CR-only case at position 0 and all five prefix cases via the
closing fence, and leaves genuinely front-matter-less legacy files on LEGACY.

#### ⚠️ Should Fix

- **`.github/workflows/claude-hooks-tests.yml:17-35`** — add `.claude/statusline.py`
  to both `paths:` filters; four new assertions exercise a file CI does not watch.
- **`.claude/hooks/lib/git_commit_detect.py:199-210`** — `env -u NAME git commit`
  under-matches and is undocumented; the inline comment implies `-u NAME` is handled.

#### 💡 Nitpicks

- `created` grep/parser tie-break divergence at line 525 — carried, still open.
- `agent:` as a block list quarantines with a raw Python traceback as the reason.
- `fixtures-pre-fix/lib/git-commit-detect.sh` is the one vendored fixture without
  a sha256 pin.

#### ✅ What's Good

The regression discipline is now the strongest thing in this PR: eight pinned
pre-fix fixtures, all verifying; positive controls that assert the *named*
failure, not just a non-zero exit; and a devops report that reproduced my exploit
against the live pre-fix hook before touching anything rather than applying my
candidate on faith. The load-bearing claim about the two merger changes is true
and I proved it both ways. The self-declaration write-up in `decision-schema.md`
is the first time in four rounds that a comment about this hook says exactly what
is true and stops there.

### Metrics

- Files reviewed: 13 modified, 5 new (excluding out-of-scope `.claude/agent-memory/**`
  and the `.squad/` decision-archive churn)
- Lines added/modified: ~721 added / 28 removed
- Test coverage of new code: high — 27 new assertions, every fix paired with a
  vendored pre-fix positive control
- Complexity: Medium
- Pattern catalog consulted: yes


### 2026-09-02 — reviewer-20260902T203000Z-pr355-round5 [reviewer · NEEDS-CHANGES]

---
id: reviewer-20260902T203000Z-pr355-round5
agent: reviewer
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-02T20:30:00Z
targets:
  - path: .claude/hooks/scribe-decision-merger.sh
    lines: "128-176, 285-292, 340-375"
  - path: .claude/hooks/lib/git_commit_detect.py
    lines: "62-72, 171-244"
  - path: .github/workflows/claude-hooks-tests.yml
    lines: "22-51"
  - path: .claude/hooks/tests/run.sh
    lines: "new legacy-bypass block"
  - path: .claude/skills/git-advanced/SKILL.md
    lines: "15, 228"
blockers:
  - file: .claude/hooks/scribe-decision-merger.sh
    line: 168
    reason: "_FENCE_SAFETY_CAP is a fail-OPEN cap, so it is itself the bypass the round-4 blocker asked to close. `_fence_hidden_behind_junk_prefix()` returns False when `i == n`, and False means LEGACY -- so any junk prefix LONGER than 256 characters routes straight back into the unvalidated LEGACY branch. Reproduced end-to-end against the real hook (CLAUDE_PROJECT_DIR sandbox, forged drop declaring `agent: bogus-not-a-real-agent` / `verdict: TOTALLY-FINE` / `scope: nonsense-scope`): 257 NUL bytes then the fence -> archived under `<!-- legacy -->`, forged body in decisions.md, 0 quarantined. 256 NUL bytes rejects, 257 does not -- an exact off-by-cap boundary. This is not only reachable with exotic bytes: `1 NUL + 300 ordinary spaces + fence` also falls through, i.e. ONE non-\\s character plus ordinary whitespace padding. The comment at line 141-145 calls the cap 'not load-bearing for correctness', which is the same over-claim class as rounds 3 and 4. Fix: the walk terminates at the first visible character anyway, so it is already O(prefix), and the file is already fully in memory -- delete the cap (`n = len(text)`). If a cap is wanted it must fail CLOSED (return True on cap-hit), never fall to LEGACY."
  - file: .claude/hooks/scribe-decision-merger.sh
    line: 140
    reason: "_JUNK_CATEGORIES = {Cc, Cf, Cs, Co, Mn, Me} still enumerates category MEMBERS, and the comment above it claims 'What is DELIBERATELY excluded: every visible category -- letters, digits, punctuation (including `#`), symbols.' That is false: it also excludes Cn (unassigned) and Mc (spacing mark), neither of which is a visible category. Cn contains the Unicode Default_Ignorable_Code_Point ranges, which renderers are required NOT to render -- so these are genuinely invisible one-character prefixes. Five reproductions, each a single leading character in front of a plainly-present forged fence, all archived to decisions.md unvalidated (verified end-to-end against the real hook for U+2065, U+E0002, U+FFF0): U+2065, U+FFF0, U+E0002, U+E0080 (all Cn + Default_Ignorable), U+0378 (plain Cn), U+0903 (Mc). Third consecutive round where the in-code comment asserts closure the code does not have. Verified fix, applied and re-run: `return c.isspace() or unicodedata.category(c)[0] in (\"C\", \"Z\", \"M\")` -- a closure over the category CLASSES (Other / Separator / Mark) instead of an enumeration of members, i.e. 'anything that is not a Letter, Number, Punctuation or Symbol'. `#` is Po, so fixture 35 still stops the walk. With that plus the cap removal the suite stays at 217 passed / 0 failed, all six of round 4's routes and all five of these stay rejected, and all three genuine-legacy controls (fixtures 34, 35, and a leading-blank-lines variant) still return LEGACY."
  - file: .claude/hooks/scribe-decision-merger.sh
    line: 375
    reason: "Most reachable instance of this exact bug class, and it needs no exotic bytes at all: a drop with a well-formed OPENING fence and no closing fence hits `if not close: print(\"LEGACY\")` and is appended verbatim to decisions.md with ALLOWED_AGENTS/ALLOWED_VERDICTS/ALLOWED_SCOPES and the verdict<->blockers consistency check all skipped. Reproduced end-to-end (forged drop, closing `---` deleted): legacy marker present, forged content in decisions.md, 0 quarantined. PRE-EXISTING -- present at b93b430 and earlier, not introduced by this round -- but it is the same invariant the round-5 walk exists to enforce, so shipping the walk while this stays open is incoherent: the hook now rejects a fence hidden behind one invisible byte and accepts one that is simply unterminated. Flagging rather than prescribing, because the naive fix (reject whenever the opening fence matches and no closing fence follows) would hard-reject a genuine legacy drop whose first line happens to be a `---` horizontal rule -- the same false-rejection trap that correctly killed my round-4 candidate. Fix now with that case considered, or split to a follow-up issue and say so; do not apply a one-line version on faith."
high:
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "The safety cap is on the wrong loop. The bounded walk got a cap for a hypothetical 'megabytes of combining marks'; the Cf-strip loop at line 290-291 -- which is genuinely quadratic, `text = text[1:]` re-copying the whole string every iteration -- got none. Measured on this box: 50k leading BOMs 0.05s, 200k 0.41s, 400k (1.2 MB) 1.68s, i.e. 4x per doubling; ~12 MB of leading BOMs is minutes of CPU inside a SubagentStop hook. Same threat surface as the forged drops (write access to the inbox). Replace the loop with a single scan-and-slice, e.g. advance an index while `unicodedata.category(text[i]) == \"Cf\"` then `text = text[i:]` once."
medium:
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "_FENCE_OPEN_WIDE_RE's widened `\\r\\n|\\r|\\n` terminator does not reopen anything from rounds 3 or 4 -- checked deliberately. It is used only inside `_fence_hidden_behind_junk_prefix()`, whose sole output is reject-vs-LEGACY; it never reaches `fm_lines`, which still splits on `\\r\\n|\\n` only, so the round-4 bare-CR indent forgery still archives as `[csharp-dev - NEEDS-CHANGES]`. Recording it as verified so round 6 does not re-derive it."
good:
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "devops is right and my round-4 candidate fix was wrong -- confirmed independently, not taken on report. `re.search(r\"(?m)^---\", text)` matches a body horizontal rule in a realistic front-matter-less legacy drop (`## Decision\\n\\n...\\n\\n---\\n\\nRationale: ...`), so my fix would have converted honest legacy content into a hard quarantine error. Rejecting a reviewer's prescribed fix after verifying it live, and shipping a narrower one, is the right call and the right order of operations."
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "Keeping BOTH the Cf strip and the walk is justified, not redundant complexity -- verified by deleting the strip loop and re-running: a benign valid drop with a single leading BOM goes from VALID|reviewer|PASS (archived normally) to quarantine. The two layers do genuinely different work: strip = tolerate an editor-inserted BOM, walk = reject a hidden fence. The stated ground holds."
  - file: .claude/hooks/lib/git_commit_detect.py
    reason: "The env value-flag fix is correct and the over-consumption bug devops reports catching mid-implementation is real and really fixed. 17 cases exercised live: `env -u FOO`, `env --unset FOO`, `env --unset=FOO`, `env -C /tmp`, `env --chdir=/tmp`, chained `-u FOO -u BAR`, `--unset=FOO --unset=BAR`, `env -i -u FOO X=1`, `sudo env -u FOO`, `(env -u FOO ...)` and `GIT_AUTHOR_DATE=x env -u FOO ...` all detect; the attached form does NOT eat `git`. Degenerate `env -u git commit` and `env -u FOO notgit commit` fail open (no match), never a false trigger, which is the documented posture. The `sep`-based attached-vs-separate discrimination is the right discriminator and the comment explains the failure mode of getting it backwards."
  - file: .claude/hooks/tests/run.sh
    reason: "Test construction is the strongest part of this round. Each of fixtures 28-33 gets three assertions -- vendored pre-fix hook REPRODUCES the fall-through, current hook quarantines, current hook rejects for the EXPECTED REASON -- and fixtures 34/35 are genuine-legacy negative controls that would have caught my round-4 candidate before it shipped. All nine vendored fixtures now sha256-pin and verify. The NUL fixture's positive control works despite decisions.md containing a NUL byte (grep -q still matches in binary), so the reported grep/NUL blind spot did not silently weaken that assertion."
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "Rounds 1-3 forgery regressions all hold under re-attack: nested-key promotion indented with space, tab, NBSP, vertical tab, form feed, file separator and bare CR all archive under the true identity `csharp-dev|NEEDS-CHANGES` with no verdict-cache write; the blockers-shadow quarantines for 'verdict PASS but blockers list is non-empty'; UTF-16 and invalid UTF-8 fail closed to 'unreadable'; CRLF and honest drops parse. The `created` sixth output field, the non-scalar rejection reason, and the ninth sha256 pin all do what the round-4 nits asked."
  - file: .claude/skills/git-advanced/SKILL.md
    reason: "Re-confirmed the four type-list sources still agree after tech-writer's parallel edit: `feat fix docs refactor test perf security ci build chore`, identical set AND identical order in the hook regex (enforce-conventional-commits.sh:160), the decisions.md table (221-230), pr-validation.yml's `types:` block (109-119), and SKILL.md lines 15 and 228. No drift."
  - file: .github/workflows/claude-hooks-tests.yml
    reason: "`.claude/statusline.py` added to both the pull_request and push `paths:` filters, closing the round-4 coverage gap; the comment correctly notes that decisions.md appears in run.sh only via per-test sandbox paths and needs no filter entry."
references:
  - ".squad/decisions/archive/2026-09/2026-09-02T19-26-47-review-pr355-round4.md"
  - ".squad/decisions/archive/2026-09/2026-09-02T18-52-26-review-b93b430.md"
---

## 👁️ CODE REVIEW — PR #355 round 5 (uncommitted working tree on b93b430)

### Holistic Assessment

**Motivation:** Justified. Every change traces to a named, reproduced round-4
finding, and devops independently falsified my prescribed fix before applying it
rather than applying it on faith.

**Approach:** Half right. The nine non-blocker items are all correct and verified.
The blocker fix moved to a better layer than round 4's, but it is still an
enumeration — of six category members instead of one — wrapped in a fail-open cap.

**Verdict:** 🔴 Changes Requested — not shippable.

### Test suite

`bash .claude/hooks/tests/run.sh` → **`Summary: 217 passed, 0 failed`**, confirmed
independently, and again at **217/217** with my candidate fix applied to a mirrored
tree.

### Findings

See the front-matter. Three blocker routes (cap fail-open, `Cn`/`Mc`, unterminated
fence), one high (quadratic Cf strip), one medium (verified non-regression), eight
positives.

### Metrics
- Files reviewed: 14 changed + 9 new fixtures + 4 new pre-fix fixtures
- Lines added/modified: ~1311 added / 34 removed
- Test coverage of new code: high (3 assertions per bypass fixture + 2 negative controls)
- Complexity: High
- Pattern catalog consulted: yes


### 2026-09-03 — reviewer-20260903T075500Z-pr355-round6 [reviewer · NEEDS-CHANGES]

---
id: reviewer-20260903T075500Z-pr355-round6
agent: reviewer
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-03T07:55:00Z
targets:
  - path: .claude/hooks/scribe-decision-merger.sh
    lines: "265-345, 503, 575-585"
  - path: .claude/hooks/tests/fixtures-pre-fix/scribe-decision-merger.pre-unterminated-fence-fix.sh
    lines: "1-20"
  - path: .claude/hooks/tests/run.sh
    lines: "926-972"
blockers:
  - file: .claude/hooks/scribe-decision-merger.sh
    line: 282
    reason: "Iteration 3's contiguous-run discriminator is bypassable and the bypass is undocumented. An attacker separates each required field with an UNINDENTED, COLON-FREE line -- e.g. a bare YAML block-sequence item (`- none`) -- which resets hits_in_run, so no run ever reaches 2 distinct hits. Verified live end-to-end against the real hook: a drop that is VALID YAML (PyYAML parses it to {agent: reviewer, verdict: PASS, scope: review, id: ..., created: ..., blockers: []}), opens with `---`, has no closing fence, and interleaves `references:/- none`, `targets:/- one`, `good:/- clean`, `medium:/- nit` between the five required fields, ARCHIVES under `<!-- legacy -->` with the whitelist, both enums and the verdict<->blockers check all skipped, body cat'd verbatim into decisions.md. The comment at 265-292 reasons only about an attacker padding BEFORE or BETWEEN blocks; it never considers an attacker deliberately interleaving separators INSIDE the block, so this failure mode is entirely unrecorded. Severity note: this grants strictly LESS than the already-accepted unclosable route (an honest column-0 `agent: reviewer`/`verdict: PASS` WITH a closing fence archives as `[reviewer PASS]` and writes PASS to .last-review-verdict -- re-verified this round), because LEGACY never sets the verdict cache or the [agent verdict] heading. Fix requested is TEXT ONLY: record this bypass in the RESIDUAL paragraph. Do NOT ship an iteration 4 of the heuristic -- the discriminator infers intent from shape and cannot be made sound; another enumeration round is the exact failure mode rounds 4 and 5 already paid for."
  - file: .claude/hooks/scribe-decision-merger.sh
    line: 293
    reason: "The RESIDUAL paragraph's characterisation of the false-positive surface is directionally wrong -- fourth consecutive round in which a comment in this file overclaims how narrow something is. It says the residual is 'two of the five required words used as ADJACENT bare shorthand labels with NO prose between them'. Only an unindented, COLON-FREE line breaks a run, so ordinary prose sentences that happen to contain a colon do not break it. Two verified genuine-legacy reproductions, both hard-QUARANTINED by the current hook and both correctly LEGACY under the vendored pre-fix hook: (1) `created: 2026-01-05` / blank / 'We decided this at the sync: everyone agreed on the direction.' / 'The rollout plan: staged over two weeks, starting Monday.' / blank / `scope: tooling`; (2) `id: OLD-11` / blank / 'Standup at 09:30 covered the migration.' / blank / `scope: infra`. Neither is adjacent and both have real prose between the signal lines. Indented lines (code blocks, blockquotes) also keep a run alive. Restate the residual accurately: the separator must be unindented AND colon-free, so any colon-bearing prose line, URL line, `Note:`/`Context:` line, colon-containing markdown heading, or indented block keeps the run alive and can carry two coincidental signal words into the same run."
high:
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "Quarantine reason string at ~578 is stale: 'found a required field name at column 0' describes discarded iteration 1 (one hit, bounded scan). Shipped logic requires TWO DISTINCT required field names within one contiguous mapping-shaped run. A human triaging a quarantined drop is told the wrong rule. Restate, e.g. '...found two schema-required field names in one unbroken block of mapping-shaped lines'."
  - file: .claude/hooks/tests/fixtures-pre-fix/scribe-decision-merger.pre-unterminated-fence-fix.sh
    reason: "Provenance not declared. This is the one vendored fixture that was hand-reconstructed rather than snapshotted, but its header is byte-identical to the current hook's preamble and says nothing about that, while run.sh's integrity block states the convention that these fixtures are 'identified by commit *message* in the header comment above' -- i.e. a future reader will take it as a captured historical artifact. I independently verified the reconstruction IS faithful: diff vs the current hook is exactly and only the route-3 addition (nothing else), and across a 49-fixture behavioural sweep it diverges from the current hook on the unterminated-fence cases alone, matching every rounds-1-5 outcome identically. The sha256 pin is meaningful for its actual job (guarding future drift) but attests nothing about historical accuracy. Add a header note: reconstructed, not snapshotted; how it was verified."
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "Line 503 still says the check looks for a fence 'just behind a SMALL run of characters'. There is no cap any more (blocker 1 of round 5 removed it); the run is unbounded. Stale qualifier in the same paragraph family that has now caused two blockers."
  - file: .claude/hooks/tests/run.sh
    reason: "Test gap for the residual being claimed. Fixtures 49 and 50 cover the HR-open case and prose-separated coincidence where the prose is colon-free. There is no genuine-legacy control for the colon-bearing-prose shape (the actual false-positive boundary). Add one fixture per the two reproductions in blocker 2 so the documented residual is pinned by a test rather than only by a comment."
medium:
  - file: .claude/statusline.py
    reason: "The UnicodeEncodeError fallback calls sys.stdout.reconfigure(errors='replace') then print(line); both sit outside any try. If stdout is not a TextIOWrapper (reconfigure -> AttributeError) or the second print fails otherwise, the module's own MUST-NOT-RAISE contract is violated at the last line. Wrap in a bare try/except and fall back to os.write of an ASCII-encoded line."
  - file: .claude/hooks/tests/run.sh
    reason: "The post-fix perf budget of <2000ms on 500k leading BOMs is loose relative to the measured post-fix cost (milliseconds). A partial regression could still pass. Consider tightening to a few hundred ms, or asserting a ratio against the pre-fix timing measured in the same run."
good:
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "Round-5 routes 1 and 2 are fixed exactly as prescribed and independently re-derived by devops before adoption. Re-verified all 39 rounds-1-5 exploits this round: nested-key promotion, six whitespace-indent bypasses, nested blockers shadow, CR-indent forgery, leading BOM, space+BOM, NUL, BEL, combining acute, BOM/newline/BOM, CR-only, cap boundary 256 AND 257, 1 NUL + 300 spaces, and all six Cn/Mc characters (U+2065, U+FFF0, U+E0002, U+E0080, U+0378, U+0903) -- every one rejected or archived under its true identity. All three genuine-legacy controls still take the LEGACY path."
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "The quadratic Cf-strip finding was measured independently (0.03/0.34/1.63s at 50k/200k/400k), rewritten to scan-index-then-slice-once, and pinned by a perf regression test that asserts the pre-fix fixture EXCEEDS the budget -- a positive control, not just a threshold."
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "Stale-comment sweep is complete and accurate. The duplicate _JUNK_CATEGORIES/_FENCE_SAFETY_CAP block is gone; the two surviving references (lines 188-197, 442) are unambiguously past-tense historical notes, and the call-site pointer at 546-556 explicitly names comment duplication as the reason the false claim survived one edit and not the other."
  - file: .github/workflows/pr-validation.yml
    reason: "Conventional-commit type list verified against enforce-conventional-commits.sh line 160 and decisions.md's table -- all three now carry the same ten types, style/revert correctly dropped and security added. The (.*/)?\\.gitkeep$ tightening verified by probe: notreally.gitkeep is no longer classified as docs-only, .gitkeep and foo/.gitkeep still are; applied consistently across cd.yml, codeql.yml and pr-validation.yml."
  - file: .claude/hooks/enforce-no-secrets.sh
    reason: "The cd-ability check is the correct fix and the reasoning about `cd` failure being indistinguishable from gitleaks exit 1 is verified. dotnet-format-on-save.sh's dual-dialect stat with per-candidate integer validation correctly anticipates GNU stat -f printing a multi-line block to stdout on the wrong dialect."
references:
  - .squad/decisions/archive/2026-09/2026-09-02T19-54-17-review-pr355-round5.md
---

## CODE REVIEW — PR #355 round 6 (uncommitted working tree on b93b430)

### Holistic Assessment

**Motivation:** Justified. Rounds 1-5 each found a live, reproducible validation bypass in the
`SubagentStop` decision merger; this round closes the last two of them plus route 3, which the
user elected to fix in-PR rather than defer.

**Approach:** Routes 1 and 2 are structurally right — the cap is deleted rather than resized, and
the junk test closes over category classes rather than enumerating members. Route 3 is different
in kind: it is a heuristic that infers authorial intent from line shape, and it cannot be made
sound. That is acceptable; what is not acceptable is shipping it with a residual description that
is wrong in both directions.

**Verdict:** 🔴 Changes Requested — **on comment accuracy only.**

The logic is fine and I am explicitly **not** asking for a fourth iteration of the discriminator.
Two blockers, both text-only, both in one file: an unrecorded bypass of iteration 3 (run-splitting
with unindented colon-free lines, verified live with a fully valid-YAML forged drop) and a
false-positive residual that is stated as "adjacent, no prose between them" when ordinary prose
containing a colon does not break the run either. Fix those two paragraphs and the change ships.
Everything else — all 39 rounds-1-5 exploits, the perf fix, the sweep, the fixture provenance,
the workflow changes — I verified and it holds.

---

### Findings

#### 🔴 Must Fix (blocks merge)

- **`scribe-decision-merger.sh:265-292`** — Iteration 3 is bypassable and the bypass is
  unrecorded. `hits_in_run` resets on any unindented, colon-free line. A bare YAML block-sequence
  item (`- none`) is exactly that, and is valid YAML. Verified live: a drop opening with `---`,
  no closing fence, interleaving `references:/- none`, `targets:/- one`, `good:/- clean`,
  `medium:/- nit` between the five required fields, declaring `agent: reviewer` / `verdict: PASS`
  / `blockers: []`, parses under PyYAML to a clean schema-shaped dict **and archives under
  `<!-- legacy -->`** with every validation skipped. The comment reasons only about padding before
  or between blocks, never about separators placed inside the block.
  **Severity, stated plainly:** this grants strictly less than the unclosable route already
  documented at the top of the file — an honest column-0 drop *with* a closing fence archives as
  `[reviewer · PASS]` and writes PASS to `.last-review-verdict` (re-verified this round), whereas
  the LEGACY path never touches the cache. So this is a documentation blocker, not a capability
  blocker. **Fix: record it in the RESIDUAL paragraph. Do not ship iteration 4.**

- **`scribe-decision-merger.sh:293-312`** — The RESIDUAL paragraph is directionally wrong. It
  claims the residual needs "adjacent bare shorthand labels with NO prose between them". Two
  verified genuine-legacy reproductions say otherwise, both hard-quarantined now and both correctly
  LEGACY under the vendored pre-fix hook:

  ```
  ---
  created: 2026-01-05

  We decided this at the sync: everyone agreed on the direction.
  The rollout plan: staged over two weeks, starting Monday.

  scope: tooling
  ```

  ```
  ---
  id: OLD-11

  Standup at 09:30 covered the migration.

  scope: infra
  ```

  Neither is adjacent; both have real prose between the signal lines. Indented lines (code blocks,
  blockquotes) also keep a run alive, as do colon-containing markdown headings (`# Decision: adopt
  the new layout`) and bare URL lines (`see: https://…`). Restate: the separator must be
  **unindented and colon-free**. This is the fourth consecutive round a comment in this file has
  understated a residual, which is why it blocks rather than being advisory.

#### ⚠️ Should Fix (recommended)

- **`scribe-decision-merger.sh:~578`** — quarantine reason says "found a required field name at
  column 0", which is discarded iteration 1's rule. The shipped rule is two distinct names in one
  contiguous run. Restate.
- **`fixtures-pre-fix/scribe-decision-merger.pre-unterminated-fence-fix.sh:1-20`** — the one
  reconstructed fixture doesn't say it was reconstructed, while `run.sh`'s integrity block states
  the convention that these are identified by their commit-message headers. I verified the
  reconstruction is faithful (see below); add a header note recording that it was authored, not
  captured, and how it was checked.
- **`scribe-decision-merger.sh:503`** — "SMALL run of characters" is stale; the cap is gone.
- **`tests/run.sh:926-972`** — no genuine-legacy control for the colon-bearing-prose shape. Add
  the two reproductions above as fixtures so the residual is pinned by a test.

#### 💡 Nitpicks

- **`statusline.py:~220`** — the `UnicodeEncodeError` fallback (`reconfigure` + second `print`) is
  itself outside any try; both can raise, against the module's MUST-NOT-RAISE contract.
- **`tests/run.sh:914`** — the `<2000ms` post-fix perf budget is loose against a measured
  millisecond-scale fix.

#### ✅ What's Good

- **Routes 1 and 2 closed exactly as prescribed, and independently re-derived first.** Re-ran all
  39 rounds-1-5 exploits: nested-key promotion, six whitespace-indent bypasses, nested `blockers`
  shadow, CR-indent forgery, BOM, space+BOM, NUL, BEL, combining acute, BOM/NL/BOM, CR-only, cap
  boundary at both 256 and 257, `1 NUL + 300 spaces`, and all six `Cn`/`Mc` characters. Every one
  rejected or archived under its true identity. All three genuine-legacy controls still LEGACY.
- **The quadratic `Cf` strip** was measured independently and pinned with a *positive-control*
  perf test that asserts the pre-fix fixture blows the budget — not just a one-sided threshold.
- **The stale-comment sweep is complete.** Only two references survive and both are unambiguously
  past-tense. The call-site pointer explicitly names comment duplication as the mechanism that let
  the false claim survive one edit and not the other — that is the right lesson written in the
  right place.
- **The reconstructed fixture is faithful.** `diff` against the current hook is exactly and only
  the route-3 addition; across a 49-fixture behavioural sweep it diverges from the current hook on
  the unterminated-fence cases *alone* and matches every rounds-1-5 outcome identically. That is
  stronger evidence than a snapshot's provenance claim would have been.
- **Workflow changes verified by probe**, not by reading: the commit-type list matches
  `enforce-conventional-commits.sh` line 160 and `decisions.md`'s table across all three files, and
  `(.*/)?\.gitkeep$` correctly stops classifying `notreally.gitkeep` as docs-only.

### Metrics
- Files reviewed: 14 changed + 5 new pre-fix fixtures + 24 new drop fixtures
- Lines added/modified: ~1,847 added / ~35 removed
- Test coverage of new code: high — route 3 has 2 exploit fixtures with pre-fix regression proof
  plus 2 genuine-legacy controls; gap is the colon-bearing-prose control
- Complexity: High (adversarial parser hardening, sixth round)
- Pattern catalog consulted: yes — `code-review` skill; charter Step 0-3 followed, design
  artifacts read only after forming the independent assessment


### 2026-09-03 — reviewer-20260903T083000Z-pr355-round7-confirm [reviewer · PASS]

---
id: reviewer-20260903T083000Z-pr355-round7-confirm
agent: reviewer
verdict: PASS
scope: review
created: 2026-09-03T08:30:00Z
targets:
  - path: .claude/hooks/scribe-decision-merger.sh
    lines: "294-368, 645-654"
  - path: .claude/hooks/tests/fixtures-pre-fix/scribe-decision-merger.pre-unterminated-fence-fix.sh
    lines: "1-40"
  - path: .claude/hooks/tests/run.sh
    lines: "235-237, 987-1008"
blockers: []
high: []
medium:
  - file: .claude/statusline.py
    reason: "Carried nitpick, not re-raised as blocking: the UnicodeEncodeError fallback (sys.stdout.reconfigure + second print) sits outside any try. Optional follow-up."
  - file: .claude/hooks/tests/run.sh
    reason: "Carried nitpick, not re-raised as blocking: the <2000ms post-fix perf budget is loose against a millisecond-scale fix. Optional follow-up."
good:
  - file: .claude/hooks/tests/run.sh
    reason: "sha256 pin recomputed correctly to fbd4c0007d284e6ac2f84560e6cf8b4b66d55d36fe1138d76f67db62a34d6825 and the guard PROVEN still live: I appended one byte to the pinned fixture and re-ran the suite -- it failed loudly with the expected drift message (275 passed, 1 failed), then restored and re-verified. Editing a pinned fixture did not disable the guard it exists to provide."
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "Both blockers closed accurately. The BYPASS paragraph carries the exact reproduction, states the PyYAML parse result, and rules out iteration 4 with the correct reason -- shape alone cannot distinguish an abandoned attempt whose author used block-sequence fields from a deliberate interleave, because the input is IDENTICAL. The RESIDUAL paragraph now states the true boundary (any two of the five words separated only by blank, indented or colon-bearing lines) and names the four concrete shapes that qualify. No overclaim remains in either."
  - file: .claude/hooks/scribe-decision-merger.sh
    reason: "No logic edits, proven rather than asserted: re-ran the full 46-fixture rounds-1-6 corpus end-to-end against the edited hook. Every outcome is identical to round 6 except the intentionally-changed quarantine reason string. The run-splitting bypass (c1/c2) still archives as LEGACY -- documented, not closed, as instructed."
  - file: .claude/hooks/tests/fixtures-pre-fix/scribe-decision-merger.pre-unterminated-fence-fix.sh
    reason: "Provenance header is better than requested: it states the reconstruction was mechanical, lists three independent faithfulness checks, and pre-empts the specific trap that the byte-identical preamble is NOT evidence of capture from git history. It also states explicitly that the sha256 pin guards future drift and cannot attest historical accuracy."
  - file: .claude/hooks/tests/run.sh
    reason: "Fixtures 51/52 as a distinct residual-pinning class is the right structural answer to the drift pattern, and the failure message gets the polarity right ('update the paragraph, not this test'). Correctly asymmetric: the false-positive residual is pinned (nobody wants honest docs quarantined, so no perverse incentive) while the BYPASS is deliberately not pinned -- a test asserting 'this forgery succeeds' would penalise anyone who closed it as a side effect. Honest limitation: two fixtures pin the residual's LOWER bound, so they catch narrowing loudly but cannot catch widening. That asymmetry is inherent to examples, not a defect in these two."
references:
  - .squad/decisions/archive/2026-09/2026-09-03T07-46-14-review-pr355-round6.md
---

## CODE REVIEW — PR #355 round 7 (confirmation pass)

### Holistic Assessment

**Motivation:** Confirmation of four scoped edits requested in round 6. Not a fresh hunt, as agreed.

**Approach:** All four landed, none introduced logic changes, and the one operation I flagged as
risky (editing a pinned fixture) was verified not to have broken its own guard.

**Verdict:** ✅ Approved — `276 passed, 0 failed`. **The PR is shippable.**

Both blockers are closed by accurate text, the two carried nitpicks remain non-blocking, and the
residual-pinning idea is a genuine structural improvement on prose-only documentation.

---

### Confirmation of the four items

- **Blocker 1 (bypass recorded)** — ✅ `scribe-decision-merger.sh:294`. Exact reproduction, PyYAML
  parse result stated, iteration 4 ruled out for the right reason.
- **Blocker 2 (residual restated)** — ✅ `scribe-decision-merger.sh:333`. True boundary stated;
  four concrete qualifying shapes named. Both of my reproductions verified independently.
- **Item 3 (quarantine reason)** — ✅ now "found two schema-required field names in one unbroken
  block of mapping-shaped lines", with a comment recording what the old string described.
- **Item 4** — ✅ "SMALL run" gone; provenance header added and stronger than requested; control
  fixtures 51/52 added.

### sha256 pin — specifically confirmed

Pin matches the file. **The guard is still live:** I appended one byte to the pinned fixture and
re-ran the suite — it failed loudly (`275 passed, 1 failed`) with the expected drift message, then
I restored it and re-verified. The pin edit did not disable the check.

### No logic edits — proven

Re-ran the full 46-fixture rounds-1-6 corpus against the edited hook. Every outcome identical to
round 6 except the intentionally-changed reason string. The run-splitting bypass still archives as
LEGACY: documented, not closed.

### Metrics
- Files reviewed: 4 (targeted re-review, not a full pass)
- Suite: 276 passed / 0 failed (was 270)
- Complexity: Low (text-only edits + two fixtures)
- Pattern catalog consulted: yes


### 2026-09-06 — reviewer-reconcile-20260906T124513Z-pr358-round2 [reviewer-reconcile · NEEDS-CHANGES]

---
id: reviewer-reconcile-20260906T124513Z-pr358-round2
agent: reviewer-reconcile
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-06T12:45:13Z
commit: 6e28e7403f79ed02b93d0d1f3324152e97c7b628
targets:
  - path: .claude/hooks/enforce-review-verdict.sh
    lines: "47-141,190-240"
  - path: .claude/hooks/block-direct-commits-to-main.sh
    lines: "36-130"
  - path: .claude/hooks/enforce-track-blindness.sh
    lines: "229-250"
  - path: .claude/hooks/enforce-review-blindness.sh
    lines: "185-225"
  - path: .claude/hooks/lib/path_containment.py
    lines: "150-180"
  - path: .claude/hooks/tests/invariant-chain.sh
    lines: "156-200"
  - path: .claude/enforcement/refutations.md
    lines: "80-330"
  - path: .claude/agents/reviewer-reconcile.md
    lines: "244-254"
  - path: CLAUDE.md
    lines: "165-180"
  - path: .gitignore
    lines: "485-497"
blockers:
  - file: .claude/hooks/block-direct-commits-to-main.sh
    line: 36
    reason: "The fix for round-1 finding 🔴-3 duplicates ~90 lines of security-critical argv parsing (split_simple_commands/push_destinations) verbatim into enforce-review-verdict.sh and block-direct-commits-to-main.sh, reintroducing round-1 finding ⚠️-7/R-10 in the same commit that closes it for path containment. The comment justifies this with 'no established sourcing convention between them', which is false twice over: lib/git-commit-detect.sh is already sourced by four hooks at HEAD, and lib/git_commit_detect.py already implements split_simple_commands with a stronger prefix stripper; the HOOKS_LIB_DIR python import convention is established by this very changeset. Nothing asserts the two copies agree (grep push_destinations .claude/hooks/tests/ -> no matches), while blindness.sh:1304 asserts exactly that for path_containment.py."
  - file: .claude/hooks/lib/path_containment.py
    line: 150
    reason: "Both blindness hooks are defeated by a read rooted at .claude/worktrees. Verified by execution against the live repo with a registered worktree present: for dreamer-first-principles, Grep(path='.claude/worktrees') and Glob('.claude/worktrees/**/decisions.md') both exit 0 while Grep(path='.claude') exits 2; identically for reviewer-blind against .squad/design/**. The worktree holds full copies of every deny-list target. reach_hit's docstring names the gap as open, but it is registered nowhere: no refutations.md entry and no threat-model row. This changeset normalises the directory (adds .claude/worktrees/ to .gitignore as a standing convention) while three charters declare isolation: worktree, so a nested checkout is now routine rather than accidental. Merge Criterion (b): unregistered residual on the most-argued invariant in the changeset."
  - file: .claude/hooks/tests/invariant-chain.sh
    line: 159
    reason: "Round-1 finding 🔴-4 is unfixed for four live blocking PreToolUse gates, and the new stub-interpreter loop excludes them citing a registration that does not exist. Verified by execution with a python3 stub exiting 127: enforce-no-secrets.sh, enforce-gpg-signing.sh and block-large-files.sh exit 0; enforce-conventional-commits.sh exits 127, which is fail-open by this changeset's own stated rule (only exit 2 blocks a PreToolUse hook). All four are wired in settings.json. The loop's guard comment calls this 'a KNOWN, separately registered gap', but grep over refutations.md and threat-model.md for all four hook names returns no matches. Merge Criterion (c): publishing above the computed level, in the file whose job is to catch it. All four share lib/git-commit-detect.sh, so one guard there closes it."
  - file: .claude/enforcement/refutations.md
    line: 80
    reason: "Seven of the fourteen status:open ledger entries state, in the present tense, a level consequence this same working tree falsifies: R-1 (CI paths all present plus a drift guard), R-2 (00-warden-scan.md and 00-warden.md are in Track A's DENY at enforce-track-blindness.sh:244-245), R-6 (all three hooks import resolve_artifact), R-7 (.gitignore is unanchored again with !.squad/log/), R-11 (both shape checks tightened), R-12 (header rewritten), R-13 (verified refused across 17 command forms). R-18 additionally describes reviewer-reconcile.md as still carrying the old numbering when it was reworded into a different false claim. This is not laundering -- I audited all eighteen and the two genuine deferrals (R-5, R-8) are honest -- but the file is append-only, is the instrument the next round grades against, and is about to become history in which 'open because unfixed' and 'open because unverified at registration time' are indistinguishable. Append status: closed lines the way R-3/R-4/R-9/R-17 already do."
high:
  - file: .claude/hooks/enforce-review-verdict.sh
    line: 213
    reason: "git add -A && git commit -m x still bypasses the commit gate for new files (verified by execution: exit 0 with only an untracked B.cs), and the fix's comment asserts the index-union is complete -- 'an untracked file cannot be committed without git add first, which would already show up staged' -- which is true across two Bash calls and false within one."
  - file: .claude/agents/reviewer-reconcile.md
    line: 248
    reason: "The reworded 🔴-6 paragraph now claims the threat model 'lists four trust boundaries, none of which cover decision-inbox forgery'. In this same tree security-expert added a fifth (Squad-flow enforcement boundary) and TM-013/OR-008 is decision-drop forgery exactly. Two specialists shipped contradictory statements about one file; the charter should cite TM-013 / Trust Boundary 5 / R-15."
  - file: .claude/hooks/enforce-track-blindness.sh
    line: 311
    reason: "Three citation families from round-1 🔴-6 still dangle: T-012 (~10 uses across both blindness hooks and blindness.sh, now colliding with the newly created and unrelated TM-012), 04-realist-plan.md:2054 (both blindness hooks), and 10-architect-ruling-classifier.md (scribe-decision-merger.sh:984,1093,1330 and run.sh:2756). principles-enforcement.md shows the correct upstream-template treatment; apply it or give T-012 a real row."
  - file: .claude/hooks/enforce-review-history-channel.sh
    line: 60
    reason: "Variable indirection defeats all three command-classifying gates (verified: 'g=git; $g log' exits 0 for reviewer-blind; a variable-indirected commit ran unrefused). Not a fixable regex -- it is the structural limit refutations.md's own type: bound category exists for, and the ledger has zero bound entries. R-13 registers only the -c half, which is now fixed."
  - file: CLAUDE.md
    line: 168
    reason: "The new sentence 'This is the same set enforce-review-verdict.sh's case list gates on' is false: the hook also carries *.ts, *.tsx, *.jsx, *.py, *.sh, */Migrations/*, package.json and Directory.Packages.props, none of which CLAUDE.md names. Safe direction (doc narrower than gate), but the sentence instructs the reader to report exactly this."
medium:
  - file: .claude/hooks/validate-phase-artifact.sh
    line: 134
    reason: "Comment says the Reasoning Trail section is measured 'up to the next heading of the same or higher level'; the regex stops at the next heading of any level, so a subheading truncates the measured body. Fail-closed, so harmless in effect, but the comment describes a different regex."
  - file: .claude/hooks/lib/artifact_attribution.py
    line: 139
    reason: "slug = os.path.relpath(...) is unbounded: a transcript Write to any path ending /00-scope.md outside .squad/design/ yields a slug like ../../tmp/foo, and scope-warden.sh then writes 00-warden-scan.md beside it. Guard with a ..-rejecting check."
good:
  - file: .gitignore
    line: 42
    reason: "The ⚠️-4 fix rejected the reviewer's proposed rationale, kept [Ll]og/ unanchored for its real intent, and added !.squad/log/ above the file-level negations with a comment explaining why the directory negation must come first. Verified correct across 8 paths."
  - file: .github/workflows/claude-hooks-tests.yml
    line: 18
    reason: "🔴-1 fixed beyond what was asked: all six paths plus .squad/.gate-shadow added to both filters, and blindness.sh:1348 re-derives the same enumeration at test time so the list cannot silently drift again. Verified non-vacuous (15 refs found, 0 missing)."
  - file: .claude/hooks/lib/artifact_attribution.py
    line: 1
    reason: "Three-tier attribution -- transcript ground truth, then mtime bounded by the invocation's own start, then unbounded with the method named in the return value -- answers ⚠️-3 properly rather than papering it, and returns method so callers can report provenance."
  - file: .claude/agent-memory/reviewer-reconcile/MEMORY.md
    line: 1
    reason: "🔴-7 fixed exactly right: seventeen entries restored byte-identical under the new agent's name, with a provenance header explaining why historical entries keep the old name."
  - file: .claude/hooks/enforce-reviewer-readonly.sh
    line: 515
    reason: "Both user-facing refusal strings now resolve: Trust Boundary 5, TM-014 and refutations.md residual R-16 all exist. That was 🔴-6's sharpest edge and it is gone."
references:
  - .squad/design/chore-0-squad-flow-v2-1/08-review-blind.md
  - .squad/design/chore-0-squad-flow-v2-1/09-review-verdict.md
  - .claude/enforcement/refutations.md
  - docs/security/threat-model.md
  - .claude/docs/principles-enforcement.md
---

# Review verdict — PR #358 round 2 (re-review after fixes)

🔴 Changes Requested. 800/0 on the hook suite, verified. Five of nine round-1
🔴 are fully fixed and three of the fixes are better than what was asked for.
Four blockers remain: one new regression (`push_destinations` triplicated with
a rationale the tree falsifies), one unregistered residual on the blindness
invariant (`.claude/worktrees/**`), one partially-unfixed finding whose test
exclusion cites a registration that does not exist, and a residual ledger whose
`status:` fields are wrong for seven of fourteen open entries.

Full reconciliation, evidence and fix directions:
`.squad/design/chore-0-squad-flow-v2-1/09-review-verdict.md`.


### 2026-09-06 — reviewer-reconcile-20260906T133024Z-pr358-round3 [reviewer-reconcile · NEEDS-CHANGES]

---
id: reviewer-reconcile-20260906T133024Z-pr358-round3
agent: reviewer-reconcile
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-06T13:30:24Z
commit: 6e28e7403f79ed02b93d0d1f3324152e97c7b628
targets:
  - path: .claude/hooks/lib/git_commit_detect.py
    lines: "259-386"
  - path: .claude/hooks/lib/git-commit-detect.sh
    lines: "50-140"
  - path: .claude/hooks/enforce-review-verdict.sh
    lines: "62-135,195-235"
  - path: .claude/hooks/block-direct-commits-to-main.sh
    lines: "27-105"
  - path: .claude/hooks/enforce-review-blindness.sh
    lines: "56-124,222,250"
  - path: .claude/hooks/enforce-track-blindness.sh
    lines: "68-95,244-245"
  - path: .claude/hooks/tests/invariant-chain.sh
    lines: "378-414,472-560"
  - path: .claude/enforcement/refutations.md
    lines: "336-470"
  - path: docs/security/threat-model.md
    lines: "33-66,104-107,116-121"
  - path: .claude/agents/reviewer-blind.md
    lines: "60-75"
  - path: CLAUDE.md
    lines: "165-200"
blockers:
  - file: .claude/docs/principles-enforcement.md
    reason: "ROUND CAP REACHED — this is review round 3 of one changeset (evidence: one archived reviewer-reconcile drop for base 6e28e740, cross-checked against the Round 2 line in 09-review-verdict.md). The Merge Criterion says the changeset is now split: what passes under (a)-(c) ships, the rest is registered and becomes the next changeset. Criterion (a) is green (823/0, executed), all four round-2 blockers and all five round-2 warnings are fixed and verified, and zero regressions were introduced. Criterion (b) is not yet met because two pre-existing residuals are unregistered. NO CODE CHANGE IS REQUESTED. This blocker exists to put the split decision in front of the user, who decides whether the two registrations land before this merges or as the first act of the next changeset - both are consistent with the cap. A fourth fix round is not."
  - file: docs/security/threat-model.md
    line: 45
    reason: "Trust Boundary 6 (created this round) scopes itself to Grep/Glob/Read/NotebookRead, and enforce-review-blindness.sh:250 exits 0 for every other tool. reviewer-blind's tools: line is 'Read, Grep, Glob, Bash, Write'. Verified by execution: firing EVERY hook in .claude/hooks/ with a reviewer-blind Bash payload of `cat .squad/design/<slug>/04-realist-plan.md` produced exit 0 from all of them, including enforce-review-history-channel.sh. dreamer-first-principles is unaffected (no Bash), so its blindness is genuinely structural; reviewer-blind's is not. Trust Boundary 5, written by the same specialist in the same tree, names exactly this class of omission for itself (TM-012, 'it mediates none of those tools equivalent effect via Bash') - the disclosure pattern exists and was applied one boundary over. Register a TM row on boundary 6 in TM-012's shape plus an Open Risk with owner and expires, and reword reviewer-blind.md's 'the One Thing That Remains a Promise' section, which names one residue where a Bash-holding agent has two. Pre-existing, not a regression: blocking only until registered."
  - file: .claude/hooks/enforce-review-blindness.sh
    line: 56
    reason: "The sibling-worktree layout and symlinks are named 'open, deliberately' in both blindness hooks' headers and registered nowhere: `grep -n worktree .claude/enforcement/refutations.md` hits only R-5 (a session RUNNING FROM a worktree, not reading THROUGH one), and TM-014's symlink language is the verdict cache on boundary 5, not blindness on boundary 6. This is the same 'naming a gap in a header is not registering it' argument accepted for round 2's blocker B; the ancestor-direction half was closed, the sibling half kept the disclosure and never got the entry. Milder than B was - Claude Code creates worktrees nested under .claude/worktrees/ (both live ones are), and the header is right that there is no fixed path segment to deny for a true sibling - which is a reason to register rather than fix, not a reason to do neither. Pre-existing, not a regression."
high:
  - file: .claude/hooks/enforce-review-blindness.sh
    line: 63
    reason: "Both hook headers list `{a,b}`-style brace expansion as an open 'deny-closure completeness gap', while TM-015 - added by the same round, in the same tree - records it as mitigated by the D20 fail-closed escalation. Execution agrees with TM-015: Glob('{src,.claude/docs}/**/*.md') and the benign Glob('{a,b}/*.md') both exit 2 for dreamer-first-principles; Glob('src/**/*.cs') exits 0. The residual is real but has the opposite sign - over-approximation with a measured over-block cost, not a reachable gap. Reword to what the code does and cite TM-015; leave symlinks in the sentence where it is still accurate."
medium:
  - file: .claude/hooks/validate-phase-artifact.sh
    line: 134
    reason: "Carried unfixed from round 2. The comment says the Reasoning Trail section is measured 'up to the next heading of the same or higher level'; the regex (?=^\\s{0,3}#{1,6}\\s|\\Z) stops at the next heading of ANY level, so a ### subheading directly under the section truncates the measured body. Fail-closed, cosmetic in effect."
  - file: CLAUDE.md
    line: 168
    reason: "Nothing asserts CLAUDE.md section 4's code-shaped list still matches enforce-review-verdict.sh's `case` list. The claim is now true (compared entry by entry) and section 4 correctly names the hook authoritative on drift, but this repository has the drift-guard pattern for exactly this (blindness.sh:1348 re-derives the CI paths: enumeration at test time) and it was not applied here."
  - file: .claude/hooks/tests/invariant-chain.sh
    line: 507
    reason: "The broken-interpreter loop keys on `grep -q 'python3' \"$hookfile\"`. Coverage is correct today (only git-history-namestatus.sh is skipped, and it needs no interpreter), but the four commit-time gates now reach python3 only THROUGH `source lib/git-commit-detect.sh` and stay in the loop because their header comments happen to contain the word. Deleting a comment would drop a live gate out of the loop silently; keying on the source line as well would make it structural."
good:
  - file: .claude/hooks/lib/git_commit_detect.py
    reason: "Round 2's blocker A was fixed the hard way rather than by the fallback I offered. find_push/push_destinations were extracted into the module that already had the tokenizer, find_commit/find_push now share a _find_subcommand body, and the structural assertions were added anyway. The docstring states precisely which of the two env/sudo forms the change reaches and which it does not - including that `sudo -u user` is unchanged rather than regressed."
  - file: .claude/hooks/lib/git-commit-detect.sh
    reason: "Round 2's blocker C was solved one level up from where it was reported: a single guard in the shared library plus a new git_commit_detect_from_payload() so JSON extraction and argv detection happen in ONE guarded python3 call. That closed the subtler half I had not separated out - each of the four gates previously ran its own UNGUARDED extraction before the shared guard could run. Verified by execution under both the broken-interpreter (stub exiting 127) and absent-interpreter shapes: all four now exit 2."
  - file: .claude/hooks/enforce-review-verdict.sh
    line: 116
    reason: "The warning-A fix turned up a real bug on the way: adding a fifth field exposed that the \\t field separator was collapsing empty fields (bash treats TAB as IFS whitespace regardless of IFS), so HAS_ADD silently took the third field's value on every commit. Switched to \\x1e, with the reasoning written down where the next reader will find it."
  - file: .claude/enforcement/refutations.md
    line: 338
    reason: "The Round 2 closures section appends rather than editing, explains why R-3/R-4/R-9/R-17 could carry their closure inline while these eight could not, and names the verification method per entry. R-18's closure explicitly warns that it covers only the sentence R-18 described and not the different false claim in the same paragraph - 'recorded here only so nobody reads this closure as covering the whole paragraph.' The ledger used as an instrument rather than a formality."
  - file: .claude/enforcement/refutations.md
    line: 433
    reason: "B-1 is registered as the ledger's first type: bound rather than laundered as a residual: it argues why resolving $G would require running the shell's own semantics on an untrusted string, cites enforce-conventional-commits.sh's existing 'do not evaluate what you are trying to gate' posture, sets expires: N/A - structural, and says outright that command-text classification is detection, not prevention."
  - file: .claude/hooks/tests/blindness.sh
    line: 438
    reason: "Round 2's blocker B ships with its collateral measured, disclosed AND asserted in both directions: line 438 pins that Track B's coarse Grep of plain .claude is now refused (the accepted cost), and line 442 pins that the collateral is narrow - .claude/skills is a sibling of .claude/worktrees, not an ancestor, and stays allowed. Pinning the limit of your own over-block is rarer than pinning the fix."
references:
  - reviewer-reconcile-20260906T124513Z-pr358-round2
---

Round 3 of PR #358: every round-2 finding is fixed and verified, the stated property is green at 823/0, zero regressions - and the Merge Criterion's round cap of two is now reached, so this verdict is the split rather than a fourth iteration.

## Disposition

Round 2's four blockers (A: duplicated push parsing; B: `.claude/worktrees/**` defeating both blindness hooks; C: four live gates fail-open under a broken python3 with a false "separately registered" claim; D: seven stale `level consequence:` fields in the ledger) are **all fixed**, each verified the way it was originally established - by execution where the finding came from execution. Round 2's five warnings (A: `git add -A && git commit` on an untracked file; B: the charter's "four trust boundaries" claim; C: three dangling citation families; D: variable indirection unregistered; E: the false "same set" claim in CLAUDE.md) are **all fixed**. One nitpick (unbounded `relpath` slug) is fixed; one (the Reasoning Trail comment/regex mismatch) is carried.

Three of the fixes are better than what was asked for, and two of them found defects the review had not separated out - see the `good:` entries.

## Why NEEDS-CHANGES rather than PASS

Not because anything in this changeset is wrong. Because criterion (b) requires every non-regression finding to be **registered** before the verdict, and two pre-existing residuals are not: the `Bash` read channel on the newly created Trust Boundary 6, and the sibling-worktree/symlink gap named in both hook headers.

Both are closable by an append. Neither asks for a code change. The reviewer classifies, the author registers, the reviewer verifies - and by design I cannot write the entries myself.

## What the user is being asked to decide

The round cap says the changeset is split: what passes ships, the rest becomes the next changeset. Here the "rest" is not a slice of files - it is two ledger entries about behaviour this changeset did not introduce. So the decision is simply whether those two registrations land before this merges, or as the first act of the next changeset. Both are consistent with the cap. A fourth fix round is not, and I am not requesting one.

The reason this is escalated rather than recorded as advisory: one of the two sits on the blindness invariant, which is this changeset's own centrepiece. `reviewer-blind` holds `Bash`; the deny list covers `Read`/`Grep`/`Glob`/`NotebookRead`; I fired every hook in the repository at a `reviewer-blind` `Bash` payload reading a denied artifact and none of them refused. `dreamer-first-principles` has no `Bash`, so Track A's blindness is genuinely structural. `reviewer-blind`'s is a gate with a fifth door, and its charter currently calls the prompt-quoting residue "the One Thing That Remains a Promise."


### 2026-09-06 — reviewer-reconcile-20260906T134435Z-pr358-round3-close [reviewer-reconcile · PASS]

---
id: reviewer-reconcile-20260906T134435Z-pr358-round3-close
agent: reviewer-reconcile
verdict: PASS
scope: review
created: 2026-09-06T13:44:35Z
commit: 6e28e7403f79ed02b93d0d1f3324152e97c7b628
targets:
  - path: .claude/enforcement/refutations.md
    lines: "472-544"
  - path: docs/security/threat-model.md
    lines: "33-77,116-120,132-136"
  - path: .claude/hooks/enforce-review-blindness.sh
    lines: "56-72"
  - path: .claude/hooks/enforce-track-blindness.sh
    lines: "68-84"
  - path: .claude/agents/reviewer-blind.md
    lines: "62-92"
blockers: []
high: []
medium: []
good:
  - file: .claude/enforcement/refutations.md
    note: "R-19 states the instruction-vs-invariant level split precisely -- invariant-level for dreamer-first-principles (the four mediated tools ARE its whole read surface), instruction-level over Bash for reviewer-blind. R-20 registers the sibling-worktree and symlink gaps the hook headers had only ever disclosed in a comment. Both carry owner, level consequence and expires. Appended under a new round-3 section; append-only rule honoured."
  - file: .claude/enforcement/refutations.md
    note: "The round-3 preamble explains why the third finding (brace expansion) is deliberately NOT registered -- devops corrected the headers to match TM-015's existing Mitigated row. An auditor reading 'three findings, two entries' gets the answer in the ledger instead of reconstructing it."
  - file: docs/security/threat-model.md
    note: "TM-016's Mitigation column reads 'None in the hooks' rather than laundering the charter instruction as partial mitigation. Trust Boundary 6's scope note concedes that its own tool list is accurate only because it is silent on the one channel differing between its two governed agents. R-19 / TM-016 / OR-010 cross-cite in all directions with agreeing expires dates."
  - file: .claude/hooks/enforce-review-blindness.sh
    note: "Reworded header states brace expansion is NOT open, explains the (grouped, None) -> D20 cwd escalation, cites TM-015, and discloses the over-block cost (a benign {a,b}/*.md is refused too). Documenting the cost of one's own fix is what stops a future reader 'correcting' the false positive and reopening the hole. Symlinks correctly left in the open sentence. Identical treatment in enforce-track-blindness.sh."
  - file: .claude/agents/reviewer-blind.md
    note: "Retitled to 'the Two Things That Remain a Promise' and gives the agent a concrete catch-yourself trigger (cat / sed -n / grep under .squad/design/) rather than an abstract prohibition. An instruction-level control is only as good as its salience."
references:
  - ".squad/design/chore-0-squad-flow-v2-1/09-review-verdict.md"
  - ".squad/design/chore-0-squad-flow-v2-1/08-review-blind.md"
  - ".squad/decisions/archive/2026-09/2026-09-06T13-33-43-review-pr358-round3.md"
  - ".claude/docs/principles-enforcement.md#the-merge-criterion--continuous-improvement"
---

# Review verdict — PR #358 round-3 split, closing pass

**Verdict: PASS.** Round 3 reached the Merge Criterion's round cap and returned
the split: criterion (a) green, all round-2 findings fixed, three findings that
asked for registration rather than code. The user chose *register now, then
merge*. This pass verifies only that the registration landed and is true of the
tree — it is not a fourth review round, and no dimension that passed in round 3
was reopened.

## The three claims, all verified

1. **`security-expert`** — R-19 and R-20 appended under a new
   `## Residuals and bounds, round 3` section at `refutations.md:472`, both with
   the full schema. Threat model gained a Trust Boundary 6 scope note, row
   TM-016 and open risk OR-010.
2. **`devops`** — brace-expansion sentence reworded in both blindness hook
   headers to state it is mitigated, citing TM-015; symlinks correctly left
   open.
3. **`tech-writer`** — `reviewer-blind.md` § 62 retitled to "the Two Things
   That Remain a Promise", naming the `Bash` channel and instructing the agent
   to hold the rule there.

## Verified by execution, not by report

- **Scope:** `find -newer` against the archived round-3 drop returns exactly the
  five claimed files plus hook-written state and my own notebook. **No scope
  creep** — the property that makes a register-then-merge disposition safe.
- **The `Bash` gap is still live:** all 19 hooks re-fired with a
  `reviewer-blind` `Bash` payload of `cat .squad/design/<slug>/04-realist-plan.md`
  — `refused: 0`. Correct: R-19 registers it, it does not fix it, and neither
  R-19 nor TM-016 overstates a fix.
- **The reworded headers' claim is true:** brace-grouped and climbing patterns
  refuse (exit 2) on both hooks for both governed agents; benign `{a,b}/*.md`
  also refuses (the disclosed cost); controls pass (exit 0). Headers, TM-015 and
  the code now agree three ways — round 3's finding was exactly that
  disagreement.
- **R-20 is not over-registration:** `grep realpath` across both hooks and
  `lib/path_containment.py` hits only the comment saying symlinks would need it,
  never a call site.
- **Suite:** `823 passed, 0 failed`, executed by me. Unchanged from round 3,
  which is the expected result for a documentation-and-ledger delta — a changed
  count would have been the finding.

## Merge Criterion

(a) satisfied — author-named property green at 823/0. (b) **satisfied, and this
is what changed** — every residual now carries an owner, a level consequence and
an `expires:`. (c) satisfied — 0 regressions. The round-cap split is complete
and the changeset is clear to merge.

## Nitpicks, non-blocking

- `refutations.md:529` — R-20's embedded `grep -n worktree` evidence command is
  self-stale: it says "finds only R-5" and now returns 8 hits including R-20's
  own title. Meaning recoverable; fold into whichever pass next touches the
  ledger.
- `reviewer-blind.md` cites the residual by file rather than by id (`R-19`),
  where the sibling charter cites "residual R-15" by number.
- Carried from round 3 and unchanged: `validate-phase-artifact.sh:133-139`
  heading-level comment; no assertion that CLAUDE.md § 4 matches the hook's
  `case` list; `invariant-chain.sh:507` stub loop keyed on an incidental grep.
- Two registered worktrees still on disk at `6e28e74`; `git worktree remove`
  when the pass ends.


### 2026-09-06 — reviewer-reconcile-20260906T134928Z-pr358-commit-confirm [reviewer-reconcile · PASS]

---
id: reviewer-reconcile-20260906T134928Z-pr358-commit-confirm
agent: reviewer-reconcile
verdict: PASS
scope: review
created: 2026-09-06T13:49:28Z
commit: 9b71b1813b11a412702ac14c0e7f5f2cedd67b29
targets:
  - path: .squad/design/chore-0-squad-flow-v2-1/09-review-verdict.md
    lines: "246-320"
  - path: .squad/log/2026-09-06-session.md
    lines: "361-367"
  - path: .claude/docs/decisions.md
    lines: "tail-64"
  - path: .gitignore
    lines: "473,496"
blockers: []
high: []
medium: []
good:
  - file: .squad/design/chore-0-squad-flow-v2-1/09-review-verdict.md
    note: "Commit-boundary confirmation only -- the eleven dimensions were NOT re-run and nothing is reopened. git rev-parse 9b71b18^ is exactly 6e28e7403f79ed02b93d0d1f3324152e97c7b628, so the commit is a single boundary over the passed working tree with no intervening history. Verdict carried forward unchanged: PASS."
  - file: .squad/design/chore-0-squad-flow-v2-1/09-review-verdict.md
    note: "git diff 6e28e74..9b71b18 --stat is 69 files, +5336/-730. Larger than the 55 files / +3330 recorded at the top of 09 because that figure was git diff HEAD -- working-tree-only, blind to the two then-untracked lib/ modules (artifact_attribution.py, path_containment.py) and to the artifacts the hooks wrote across rounds 2-4. The delta is accounted for entirely by those, not by new work. No .cs, .js, .mjs, .csproj, appsettings.* or Dockerfile in the diff: still squad-framework-only, as reviewed."
  - file: .squad/log/2026-09-06-session.md
    note: "git status --porcelain shows exactly one line, this file modified. The delta is four appended session-logger.sh SubagentStop lines at 13:46:41Z-13:48:01Z, after the 13:46:13Z commit -- append-only hook state, no content change. --untracked-files=all adds nothing further."
  - file: .gitignore
    note: ".claude/worktrees/ is ignored at line 496 and carries 0 tracked files, so the two live agent worktrees (each containing a full copy of the hook set) did not enter the commit. .squad/.last-review-verdict is ignored at line 473, so the verdict cache token was correctly kept out of the tree it certifies."
  - file: .claude/docs/decisions.md
    note: "Only governed document with a post-verdict mtime (15:45:14, vs the verdict at 15:44:32), so it was checked rather than assumed: its last 64 lines are byte-identical to the body of ...round3-close.md (diff clean modulo one leading/trailing blank line). The change is scribe-decision-merger.sh mechanically merging a drop I authored -- not new content, not a hand edit."
  - file: .claude/hooks/tests/run.sh
    note: "823 passed, 0 failed, executed by me at 9b71b18. Unchanged from rounds 3 and 4, which is the expected result for a commit that changed no file content -- a changed count would have been the finding."
  - file: .squad/design/chore-0-squad-flow-v2-1/09-review-verdict.md
    note: "Scope audit recorded honestly: six committed files carry a post-verdict mtime and all six are hook-written state or the reviewer notebook -- none is a hook script, charter, doc-under-review or CI config. The residual limit is named rather than papered over: the reviewed working tree was not snapshotted, so byte-for-byte identity is inferred from the single-parent commit, the mtime audit and the reproduced suite, not diffed."
references:
  - ".squad/design/chore-0-squad-flow-v2-1/09-review-verdict.md"
  - ".squad/design/chore-0-squad-flow-v2-1/08-review-blind.md"
  - ".squad/decisions/archive/2026-09/2026-09-06T13-45-14-review-pr358-round3-close.md"
  - ".claude/docs/principles-enforcement.md#the-merge-criterion--continuous-improvement"
---

# Review verdict — PR #358, commit-boundary confirmation at `9b71b18`

The round-4 pass approved a **working tree** on top of `6e28e740`. The merge gate
keys on a commit, so this pass exists for one question only: **is the committed
tree the tree that was passed?** It is. The eleven dimensions were not re-run,
and this drop reopens nothing.

## The three claims, all verified

1. **The diff matches.** `git diff 6e28e74..9b71b18 --stat` → 69 files,
   +5336/−730; `git rev-parse 9b71b18^` → `6e28e740` exactly. One commit, no
   intervening history. The file set is the reviewed control delta plus exactly
   the four categories the commit was expected to carry — review artifacts,
   session log, merged decision drops, reviewer notebook. No runtime-executed
   file of any kind appears.

2. **`git status` is clean apart from hook state.** One modified file, the
   session log, appended to by `session-logger.sh` *after* the commit.

3. **The suite is unchanged.** `823 passed, 0 failed`, run by me at this HEAD.

## Why the stat is bigger than the number in `09`

`09` records `55 files, +3330/−730`, which was `git diff HEAD` — working-tree
only. It could not see `artifact_attribution.py` and `path_containment.py`
(untracked at the time, tracked now) or the artifacts written during the review
rounds themselves. The difference is bookkeeping, not scope creep.

## The one thing worth checking rather than assuming

Six committed files have an mtime later than the verdict. Five are unambiguous
hook state or my own notebook. The sixth, `.claude/docs/decisions.md`, is a
governed document — so it was diffed: its tail is verbatim the body of the
round-3-close drop, merged by `scribe-decision-merger.sh` from a drop I wrote
myself.

## Residual, stated plainly

The reviewed working tree no longer exists and was not snapshotted. "Byte-for-byte
identical" is therefore **inferred** — from the single-parent commit, the mtime
audit, and the reproduced 823/0 — not diffed. That inference would miss an edit
made before `15:44:32` that I did not read, which is the exposure every review
carries and which the commit boundary neither widens nor narrows.

## Merge Criterion

(a) satisfied — author-named property green at 823/0. (b) satisfied — every
residual carries owner, level consequence and `expires:`; unchanged since
round 4. (c) satisfied — 0 regressions. **`9b71b18` is clear to merge.**

## Nitpicks, carried and non-blocking

- Carried unchanged from round 4: R-20's self-stale `grep -n worktree` evidence
  command; `reviewer-blind.md` citing its residual by file rather than by `R-19`;
  the three carried from round 3.
- Now checkable and still open: **both registered worktrees remain on disk**
  under the ignored, untracked `.claude/worktrees/`. `git worktree remove` when
  the pass ends. 💡, not a merge condition.


### 2026-09-06 — lead-20260906T150129Z-undo-redo-adr-008-left-as-is [lead · INFO]

---
id: lead-20260906T150129Z-undo-redo-adr-008-left-as-is
agent: lead
verdict: INFO
scope: decision
created: 2026-09-06T15:01:29Z
targets:
  - path: docs/adr/ADR-008-immutable-state.md
    lines: "85"
blockers: []
high: []
medium: []
good: []
references: []
---

User decided to leave `docs/adr/ADR-008-immutable-state.md:85` exactly as written, declining to edit it for the `undo-redo` design pass gate 1.

## Context

`docs/adr/ADR-008-immutable-state.md:85`, in the Consequences → Positive section, states: "Undo/redo: Trivial to implement by storing state snapshots." This line is reachable by `dreamer-first-principles` as ordinary codebase reading (see the companion `flow-changelog.md` entry on the `docs/adr/` deny-list gap) and pre-answers a central open question of the `undo-redo` scope: at what level undo operates, and what the unit of a single undoable step is.

## Decision

Leave the line as written. Do not edit the ADR to shape what Track A can or cannot derive.

## Reason (the user's words)

Editing a production ADR to make the blind track derive independently would be a constructed result. Whether Track A copies the ADR or reasons past it is the experiment; convergence classifies it.

## For dreamer-convergence

Track A's read of ADR-008 is unmodified from its accepted form. When classifying Track A's output, treat agreement with ADR-008:85 as either independent derivation or reliance on the reachable text — the classification is the point of leaving the line in place, not a defect to route around.


### 2026-09-06 — critic-20260906T184500Z-undo-redo [critic · NEEDS-CHANGES]

---
id: critic-20260906T184500Z-undo-redo
agent: critic
verdict: NEEDS-CHANGES
scope: architecture
created: 2026-09-06T18:45:00Z
targets:
  - path: .squad/design/undo-redo/04-realist-plan.md
  - path: .squad/design/undo-redo/00-scope.md
    lines: "182-190"
  - path: Picea.Abies/Runtime.cs
    lines: "212-220, 288-291, 465-500"
blockers:
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 284
    reason: "Bare-event drift: a model change arriving outside the Decide envelope (interpreter feedback, decided-error, later events of a multi-event Decide, or a policy-excluded message) moves Present without pushing a Step, so the next undo silently discards it. Conduit: a profile fetch landing mid-typing is erased by one undo press. INV-1, INV-2 and INV-3 all pass while the feature loses user data."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 291
    reason: "Redo is unreachable in any application with a live subscription: record and continue both clear Future, so a 250 ms tick (SubscriptionsDemo/Program.cs:110) caps redo's lifetime at 250 ms, and DefaultHistoryPolicy (IsUndoable => true) fills the 100-entry history with ticks in 25 s. The plan states this outcome at line 578 as an argument for the projection lens, then proceeds with the lens deferred and no substitute. Open question 4 is therefore unanswered against the scope's Done-means."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 396
    reason: "INV-7 claimed vacuous/by-construction but falsified by any undo sequence of length >= 2 over a subscription-toggling model: reconciling at a passed-through state starts a subscription (Subscriptions/Manager.cs:51-66) which a Subscription.Create can deliver from deterministically. The saving reading -- each undo press is its own destination -- is unstated and load-bearing, and it forecloses any future multi-step undo. INV-7 also has no test vehicle: it is a Runtime.Render property, not a WithHistory property, and step 7 places all seven properties in workflow-direct domain tests."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 279
    reason: "The late-NothingToUndo path delegates UndoRefused to TProgram.Transition, so undo with nothing left to undo delivers a message that may change state or emit a command -- INV-4's stated falsifier. Reachable by two fast clicks: dispatch is fire-and-forget (Runtime.cs:288-291) and _decisionGate is released at :474 before the transition is awaited at :494, so no threading is required. Fix is one branch: NothingToUndo returns (h, Command.None)."
high:
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 444
    reason: "HistoryStack 'amortised O(1)' is unachievable for a persistently-used bounded stack (Okasaki: amortisation and persistence are incompatible without laziness/scheduling), and the history IS used persistently. Restate as O(Depth) worst case and pick the array copy-on-write representation."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 88
    reason: "INV-2 'by construction' is by instruction: History<TModel> and Step<TModel> are constructible with arbitrary Past. Principles gate -- Make Illegal States Unrepresentable is cited as constraining. Needs internal constructors plus a test seam, or explicit user approval as a deviation."
  - file: .github/workflows/pr-validation.yml
    line: 211
    reason: "1500-line hard PR-size limit fails the build; docs are exempt, source and tests are not. The plan has no PR decomposition and § Parallelisable implies one branch."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 472
    reason: "No demo adopts WithHistory, so js-framework-benchmark measures a non-adopting application and its 5% gate is structurally silent on this pass. Step 10 must be an explicit BenchmarkDotNet A/B with a derived budget, and must run after step 6 and before steps 7-9."
  - file: Picea.Abies.Conduit.ServiceDefaults/Extensions.cs
    line: 32
    reason: "AddSource matches exact names, so Picea.Abies.History (and, already, Picea.Abies.Runtime and Picea.Abies.Subscriptions) is collected nowhere. Step 9's acceptance criterion is unverifiable, and fixing it touches files the plan declares unchanged."
medium: []
good:
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 297
    reason: "IsSilent must be a type test, not reference equality -- Commands.None allocates a fresh Command.None() per call (Command.cs:12). Caught at plan time; would otherwise have marked every edge committed and made the feature refuse everything."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 148
    reason: "INV-4 and INV-5 routed through two runtime paths that already exist and already behave differently (Runtime.cs:465-468 vs :482-489), verified rather than assumed."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 213
    reason: "Overrules 01-track-a.md and 03-convergence.md on RingBuffer<T> reuse after reading Debugger/RingBuffer.cs and finding a mutable class. Corrects inherited material in public rather than propagating it."
references: []
---

Critic verdict on the `undo-redo` design pass: LOOP BACK TO REALIST. Four blockers, all in the mechanism rather than the direction; H1/`WithHistory` remains the right shape.

## Why realist and not Dreamer

Nothing found reopens the H1 ranking. The two-track convergence holds, the kernel does force the zipper shape, and every alternative still pays in runtime seams or reflection. Three of the four blockers share one root — the wrapper has no story for model changes that did not arrive through the `Decide` envelope — and the instrument that was going to supply that story, A3's projection lens, was deferred at gate 3 without its concrete consequences having been put in front of the user. The fourth is a one-branch defect inside an otherwise correct rule.

## What the loop-back owes

1. A chosen resolution for bare-event drift: un-defer the lens, record bare changes as a refusing barrier reusing the INV-5/INV-6 machinery, or state plainly that this pass ships undo without usable redo for applications with subscriptions or asynchronous effects. Rebasing (re-applying the delta onto the restored model) is not admissible — it computes a model rather than moving to one and destroys INV-2-by-construction, which is H1's principal claim.
2. An explicit reading of INV-7's "state the user asked for" across a run of undo presses, plus a `Runtime`-level test vehicle with an instrumented subscription. The reading may be an `architect` amendment to `00-scope.md` rather than a `realist` fix.
3. `NothingToUndo` returning a true no-op from `Transition`, and the same recompute treatment extended to `Redo` and `Clear`.
4. The principles deviation on `History`/`Step` constructibility either fixed or approved.

## Spawns

`security-expert` — a change from the knowledge scan's "not summoned". `Step.Cause` retains raw messages for up to `Depth` entries; the DEBUG snapshot path now needs `JsonTypeInfo<History<TModel>>`, dragging the 2026-03-29 `JsonPolymorphic` obligation onto the application's whole message hierarchy, which in Conduit contains three credential-bearing message types; and `DebuggerMachine.ExportSession`/`ImportSession` is a reachable export surface. The scan's own re-summon trigger is met.

`performance-engineer` and `ux-expert` as the plan already asks, with re-sequencing and three added questions respectively.


### 2026-09-06 — critic-20260906T000000Z-undo-redo-pass-2 [critic · NEEDS-CHANGES]

---
id: critic-20260906T000000Z-undo-redo-pass-2
agent: critic
verdict: NEEDS-CHANGES
scope: architecture
created: 2026-09-06T00:00:00Z
targets:
  - path: .squad/design/undo-redo/04-realist-plan.md
  - path: .squad/design/undo-redo/00-scope.md
    lines: "183-194"
  - path: Picea.Abies/Runtime.cs
    lines: "200-220"
  - path: Picea.Abies.Conduit.App/Model.cs
    lines: "82-96"
blockers:
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 248
    reason: "B5 — the transit hold guarantees INV-7 only for undo presses closer together than SettleWindow (default 250 ms) and only while no held subscription delivers. Two presses 350 ms apart are two runs, so subscriptions reconcile against the intermediate state; a held anchor subscription that delivers ends the run mid-movement. Step 9's property runs with SettleWindow = null, an explicit terminal Settle and a non-autonomous source, so it cannot falsify any of it."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 512
    reason: "B6 — HistoryMessage.Settle carries no ordinal. DispatchFromSubscription is fire-and-forget (Runtime.cs:288-291), so a settle already dispatched by the window being cancelled ends the run that replaced it. Fix is one field plus a guard in Decide and Transition."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 472
    reason: "B7 — SEC-1/SEC-2 redact Step.Cause, but Conduit's passwords are model fields bound to controlled inputs (Model.cs:82-96, Pages/Settings.cs:49), so the retained secret arrives via Step.Model, which nothing scrubs. SEC-3 as worded in the plan is unsatisfiable for any model carrying the value, and SEC-6's threat row would claim a mitigation that does not cover the threat."
high:
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 538
    reason: "S9 — under the default policy redo still dies silently: barrier clears Future and Decide(Redo) returns the INV-4 no-op, so a lost redo is indistinguishable from nothing to redo."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 415
    reason: "S10 — the identity default blocks undo at the most recent world change permanently, not merely at the most recent tick; in Conduit that is every fetch and every navigation."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 569
    reason: "S11 — HistoryEvent.Continue is unconditionally transparent, so a projection change carried by the second event of a multi-event Decide is neither recorded nor barriered. Reachable shape, evidenced at Picea.Abies.Tests/RuntimeIsolationAndSubscriptionFaultTests.cs:218."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 542
    reason: "S12 — IsUndoable's entire remaining effect is to turn a record into a barrier; the name says the opposite. Remove it or rename it."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 148
    reason: "S13 — the erasure removed the guarantee that SameUndoable is an equivalence relation; L1-L4 do not restore symmetry."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 838
    reason: "S15 — a real 50 ms wall-clock settle test, five commits after ca2519d removed sleep-based subscription tests from this project. Use TPolicy.Time with a test-owned deterministic TimeProvider."
  - file: .squad/design/undo-redo/00-scope.md
    line: 128
    reason: "S16 — INV-1 is stated unconditionally and is falsified by the gate-2 refusal policy the pass settled; its property needs an availability precondition, which is an architect clause."
medium: []
good:
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 166
    reason: "The transit anchor is seam-free and every link verifies: Runtime.cs:214 asks the wrapper, Manager.cs:51-66 keys the diff, and Runtime.cs:212-220 reconciles outside the patch guard. Zero lines of Runtime.cs."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 716
    reason: "S2 fixed rather than deviated; InternalsVisibleTo Picea.Abies.Tests already exists at Picea.Abies.csproj:24, so the principles gate closed for free."
references: []
---

Second Critic pass on design `undo-redo`: LOOP BACK TO REALIST with three blockers — INV-7 is still falsifiable through the new transit machinery, and the folded-in security mitigation misses the field the secret actually travels in.

## Why this is a loop-back and not a mitigation list

Revision 2 closed B1, B2, B4 and all of S1–S8 with mechanisms I verified in the code, and the H1 direction is untouched. But B3 — the invariant that caused the first loop-back — is not closed. The transit hold is correct for the scenario it was designed against and is defeated by the shipped default: a settle window sized from editing-coalescence literature (250 ms) is shorter than a deliberate user's inter-press interval, a held anchor subscription delivering during a run collapses the run onto an intermediate state, and the settle signal carries no identity so a cancelled window's in-flight message ends the wrong run.

Compounding it, the planned property runs with `SettleWindow = null`, an explicit terminal `Settle` and a source that never dispatches on its own — a mode no application ships in and which excludes all three triggers by construction. That is the invariant-coverage gate: a property that excludes the clock from a clock-driven mechanism is not coverage of that mechanism.

B7 is independent. The security room's own worked example — Conduit's password fields — reaches the history through `Step.Model`, not `Step.Cause`, because a controlled input requires the value to be in the model. `SensitiveCause` cannot see it. The fix is small (`Scrub` on the policy, plus the law `Restore(Scrub(a), b) = Restore(a, b)`), but it must land before the threat-model row claims mitigation.

## Recurrence

Third pass in a row where a claim of the form "X is closed by construction" turned out to be closed by a mechanism with a precondition the artifact does not state. Revision 1: INV-2 by construction, actually by instruction. Revision 1: INV-7 vacuous, actually false for runs. Revision 2: INV-7 held by transit, actually held only within a window.


### 2026-09-07 — critic-20260907T000000Z-undo-redo-pass3 [critic · NEEDS-CHANGES]

---
id: critic-20260907T000000Z-undo-redo-pass3
agent: critic
verdict: NEEDS-CHANGES
scope: architecture
created: 2026-09-07T00:00:00Z
targets:
  - path: .squad/design/undo-redo/04-realist-plan.md
    lines: "605-665"
  - path: .squad/design/undo-redo/04-realist-plan.md
    lines: "342-365"
  - path: Picea.Abies/Runtime.cs
    lines: "288-291"
blockers:
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 618
    reason: "B8 — an application's Decide Err message reaches Transition bare (Runtime.cs:460,484; Picea.xml:1229-1239 shows AutomatonRuntime.Dispatch does not call Decide), so apply classifies it as seal/SealedByWorld. A validation rejection permanently blocks undo and redo and names the wrong category of reason, violating INV-5's identify-which-reason clause. Fix: Err(e) -> Err(HistoryEvent.Enveloped(m,[e]))."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 352
    reason: "B9 — subscription-delivered messages are indistinguishable from user actions at the wrapper (Runtime.cs:288-291 and :332 are the same delegate), so a timer tick is enveloped and records, clearing Future. The plan's default-policy narrative claims the opposite (a typed refusal naming FastTick). In SubscriptionsDemo redo is silently destroyed every 250 ms — S9's opacity and B2's harm on the record path. Requires a corrected narrative, a step-8 autonomous-source property, and a user decision on whether a discarded redo branch should refuse with a reason."
high:
  - file: .squad/design/undo-redo/04-realist-plan.md
    reason: "S18 — the Enveloped fold also changes error semantics (Runtime.cs:353-365 aborts the batch after the model is fully folded); the user approved the ordering change only."
  - file: .squad/design/undo-redo/04-realist-plan.md
    reason: "S19 — L5 forces Scrub to the identity under WholeModelHistoryPolicy; an adopter overriding Scrub alone silently deletes the field on undo."
  - file: .squad/design/undo-redo/04-realist-plan.md
    reason: "S20 — Movement.Held(Anchor) is a live unscrubbed model outside SEC-3(b)'s 'reachable via any Step.Model' set. Third occurrence of the wrong-field shape in this pass."
  - file: .squad/design/undo-redo/04-realist-plan.md
    reason: "S21 — step 9 assertion (4) describes a hold left open settling, which the design cannot do."
  - file: .squad/design/undo-redo/04-realist-plan.md
    reason: "S22/S23/S24 — INV-7's protection is confined to bracketed runs and no button chrome can bracket; the origin re-basing exception is stated far more narrowly than the rule behaves; the recommended keydown/keyup bracket loses its Settle on a lost key-up."
medium: []
good:
  - file: .squad/design/undo-redo/04-realist-plan.md
    reason: "Deleting the settle clock closed B5 and B6 by subtraction rather than by guard; the edge algebra is consistent under Pop; Scrub survives the INV-3 derivation via L5."
references: []
---

Third Critic pass on `undo-redo` revision 3: LOOP BACK TO REALIST with two blockers, both in the classification of who moved the model.

Revision 3 closes B5, B6, B7 and all seventeen 🟠/🟡 from pass 2, verified in the code rather than in
its own disposition table. What survives is the four-row record/pass/seal table's first question,
"came through the envelope?", whose real answer — interpreter feedback and `Decide` errors, and
nothing else — is never stated. B8 misclassifies a validation rejection as the world moving; B9
misclassifies a subscription delivery as a user action, which silently destroys redo in the
`SubscriptionsDemo` shape and contradicts the default-policy narrative the user approved.

Neither needs new mechanism. `05-critic.md` § *What revision 4 must contain* lists nine bounded
items; nothing goes back to the Dreamer and H1 is untouched.


### 2026-09-07 — architect-20260907T120000Z-undo-redo [architect · INFO]

---
id: architect-20260907T120000Z-undo-redo
agent: architect
verdict: INFO
scope: architecture
created: 2026-09-07T08:03:52Z
targets:
  - path: Picea.Abies/History/
  - path: Picea.Abies.Tests/History/
  - path: docs/adr/ADR-030-undo-redo-as-a-history-program.md
  - path: docs/adr/ADR-008-immutable-state.md
    lines: "85"
  - path: docs/security/threat-model.md
blockers: []
high:
  - reason: "Undo REFUSES at an effect boundary rather than crossing it. This is a product decision selecting between two defensible engineering positions, not a finding that the industry-standard cross-with-a-documented-contract answer is wrong. ADR-030 must say so in those words."
  - reason: "An application with a model-mutating subscription must declare a projection or redo will not function, and under the whole-model policy undo's reach also expires on a wall clock at Depth / the subscription's rate — 25 seconds in SubscriptionsDemo's shape. First-class behaviour, not a footnote."
  - reason: "Spec file Picea.Abies.Tests/History/UndoRedoSpec.cs cannot compile until the end of plan step 6 (PR 3), which collides with the lock protocol's 'lands before step 1' reading and with reviewer-reconcile's same-PR check. Needs a user answer on commit placement before the first push. See 07-handoff.md section 8 item 1."
  - reason: "04-realist-plan.md revision 4 still carries the two sentences Critic S25 found false and will not be revised again; 06-spec.md lines 4 and 15 are authoritative over plan :195-199 and :408-410. An implementer reading only the plan implements the wrong narrative."
medium:
  - reason: "The step-8 review brief must state that reviewer-blind reads Generators.cs and DocumentComparer with the same weight as the spec — they sit outside the lock and can make a locked property vacuous without the locked file changing."
  - reason: "This pass's INV-5 and INV-7 collide by name with the verdict-cache invariants in .claude/hooks/; disambiguation depends on a qualifier on the id line, because the artifact validator reads INV-<n> and nothing else."
good:
  - reason: "The withheld preference — one dispatch = one undoable step — was derived independently by both blind Dreamer tracks before the user revealed it. The strongest dual-track result this repository has produced."
  - reason: "ADR-025 was examined and found NOT to require superseding. Nothing moves out of #if DEBUG; zero lines of Runtime.cs change; the runtime-seams-anchor-replay-gating revisit trigger does not fire."
references:
  - lead-20260906T150129Z-undo-redo-adr-008-left-as-is
---

Undo/redo ships as `WithHistory` — a higher-order `Program` over a `(Past, Present, Future)` zipper — with undo refusing, by typed and pre-announced value, to cross an action that already spoke to the world.

## Decision

Adopt **H1**: `Picea.Abies.History`, a new bounded context inside `Picea.Abies`, opt-in by composition at an application's call site. `WithHistory<TProgram, TPolicy, TModel, TArgument>` wraps the application's model into a history value and forwards everything else, in the same static-forwarding shape `WithView` already uses. Undo and redo are ordinary `Message`s whose transitions return `Command.None`.

Full design at `.squad/design/undo-redo/` — `00-scope.md` (INV-1 … INV-7), `01-track-a.md`, `02-track-b.md`, `03-convergence.md`, `04-realist-plan.md` rev. 4, `05-critic.md` fourth pass, `06-spec.md`, `room-security.md`, `07-handoff.md`.

Load-bearing consequences of the shape:

- **Zero lines of `Runtime.cs`, `Program.cs`, any head adapter, or any `.js` file.** The wrapper only *answers* the question `Runtime.Render` already asks at `Runtime.cs:214`. No fifth seam; the `runtime-seams-anchor-replay-gating` revisit trigger does not fire.
- **No reflection, no serializer, no `JsonTypeInfo`, no `TrySetCoreState`.** Release-safe under trimming and AOT.
- **ADR-025 examined and found not to require superseding.** Nothing moves out of `#if DEBUG`, including `RingBuffer<T>`, which is mutable and cannot be a field of a model value under ADR-008.
- Heads: InteractiveServer, InteractiveWasm and Native, identical implementation. Static excluded — no MVU loop. Under InteractiveAuto the history *begins* at the client handoff.

## Convergence verdict

Both Dreamer tracks derived the same object without contact — a higher-order program over a past/present/future triple, undo as an ordinary message, the undo transition returning `Command.None`, a bounded past, and *both tracks independently named `WithView` as the local precedent*. The right reading is not that two tracks liked a zipper: the kernel's single dispatch funnel, effect-isolating `Transition`, model-derived subscriptions and existing `WithView` composition **jointly force** the shape. Track A shows it is forced here; Track B's citations show it works elsewhere.

Both tracks also killed event-sourced replay, from opposite directions — Track B on cost (constructor-time `replay` flag, serialization obligations, ADR-025 partially superseded), Track A on correctness (`Transition` reads the wall clock in a shipped app in this repository, so replay cannot reproduce the walk and INV-3 fails as a matter of fact). Both rejected computed inverses as the primary mechanism on INV-2.

**The withheld preference.** The user named "at what level undo operates" the central question and deliberately withheld their own answer so it would be derived. Both tracks landed independently on **one dispatch = one undoable step**, and the user then confirmed it. Optional 500 ms coalescing is off by default and separable.

**The ADR-008 experiment.** `ADR-008:85` — *"Undo/redo: Trivial to implement by storing state snapshots"* — was left reachable by Track A on the user's explicit decision, to test whether Track A would copy or reason past it. Convergence classified it: **reasoned past**, on five pieces of evidence — it contradicts the line's central adjective, declines its vocabulary ("snapshot", which it uses only to rule the mechanism out), arrives at storing as the survivor of three recorded rejections, and produces a cost model ("a history does not allocate — it defers collection") the ADR does not contain and arguably points away from. The deny-list gap that made the file reachable is registered as **R-21**, owned by `devops`.

## The effect boundary — a product call, recorded as one

Track A derived **refusal**: the observable state is `model ⊗ world`, the cursor moves over the first factor only, and INV-2 as written cannot catch the divergence because both models were literally visited. Concrete falsifiers in this codebase: undo across `LoginSubmitted` double-posts; undo across `FavoriteArticle` disagrees with the server.

Track B evidenced **crossing with a documented non-recall contract**: the industry's settled answer, mitigated by compensation and by deferral.

**The user chose refusal**, with the refusal typed, explained, and answerable before the press. Track B's position is excluded by product decision, **not** by analysis — a later reader must not mistake the one for the other.

Gate 2's "discard the forward branch" rule was subsequently **superseded** by *refuse across the superseded branch*: the branch is preserved, its first edge sealed with `SupersededByNewAction(cause)`, and `Forward(h)` refuses by name in advance. The original rule's reason — a retained branch makes redo a relation, which has no inverse, so INV-3 becomes unstatable — is about a branch that is retained *and crossable*, and does not reach a sealed one. The Critic verified independently that no edge ever returns to `Crossable`. Price: retention bounded at `2 × Depth`, and (S29) session-lifetime rather than `Depth`-dispatch-bounded. It buys diagnosability, not reach.

## Accepted risks and mitigations (Critic, fourth pass — APPROVED WITH MITIGATIONS)

Three user-approved Cleanness compromises: the projection lens trades INV-2's by-construction status for L1–L6 plus a reachability assumption the framework cannot test; a wrapped multi-event decision interleaves effects **and fails** differently from an unwrapped one ("complete model, partial effects" replacing "partial model, no later effects"); and a superseded branch is retained though it can never be crossed.

Five 🟠 accepted, none needing a revision 5 — S25 (two false sentences survive in the plan; `06-spec.md` lines 4 and 15 win), S26 (the missing superseded-branch line, now spec line 14), S27 (generator alphabet partition — the autonomous source belongs to property 8(k) alone, or INV-1 and INV-3 are falsified by correct behaviour), S28 (`TaskCompletionSource` ordering, no sleeps — `ca2519d`), S29 (retention *lifetime*, answered by `security-expert`'s third follow-up as Trust Boundary 7 wording). Five 🟡 carried in `07-handoff.md` § 6.1.

Two judgement calls upheld: **S23** — origin re-basing keeps its behaviour and corrects its description, because the one-line alternative would make pre-record navigations recorded stops that restore a model without its URL; **item 6** — SEC-3(b)'s reachability set is not widened to name the held anchor, because a clause discharged by `Scrub` cannot name a site `Scrub` must not reach. In both cases a specialist overruled the Critic and the record says so.

Security: SEC-1 … SEC-7, with ADR-030 landing as `tech-writer`'s plan step 13 deliverable. Trust Boundary 7 (three retention shapes, each with the bound that actually applies), two threat-model rows with the anchor and superseded-branch bullets, and two hardening-backlog fast-follows land as `security-expert`'s plan step 16 deliverables.

## Spec test

`Picea.Abies.Tests/History/UndoRedoSpec.cs` — workflow-direct through a real in-process `Runtime`, no AppHost, no Playwright, no new dependency. **Immutable for this feature from the approval commit; implementation passes when this test passes without modification.** Seven properties for seven invariant ids, each with a named falsifier that must be observed before its step closes. Amended at approval: spec line 12 (undo out of a terminal state) moved into the lock as A9. `Picea.Abies.Tests/SpecAttribute.cs` is an addition to the plan's file table, flagged by `spec-author` rather than slipped in.

The lock's known hole — `Generators.cs` and `DocumentComparer` sit outside it and can make a locked property vacuous — is closed at review, not by locking more files. The step-8 review brief must say so.

## Standing exclusions

A3(b) crossable-iff-inverse (owes its own invariant when it returns); deferral / hold windows (a fifth seam); `NavigationCommand.Replace`; durable undo; DOM-owned state; no demo or template adopts `WithHistory` this pass, which makes the CI benchmark gate structurally silent — step 7's report must say so, and a green integer-MB size gate is not evidence either.


### 2026-09-07 — architect-20260907T184500Z-undo-redo-navigation [architect · INFO]

---
id: architect-20260907T184500Z-undo-redo-navigation
agent: architect
verdict: INFO
scope: architecture
created: 2026-09-07T09:34:55Z
targets:
  - path: Picea.Abies.History
  - path: .squad/design/undo-redo/
blockers: []
high:
  - reason: "Adopter obligation (06-spec.md obligations line 17): incoming navigation reaches a WebAssembly program through an application-supplied converter (Picea.Abies/Navigation.cs:17-29), so the seal fires only for the framework's Picea.Abies.UrlChanged. An application composing WithHistory must pass `url => new UrlChanged(url)`; one that names its own navigation message gets no seal, its navigations become ordinary undo stops, and undo restores a model whose URL the browser is not showing — with every property in the spec still green. No framework property can quantify over this."
  - reason: "Accepted, documented limitation (room-security.md § Follow-up 2026-09-07 (III), S33): the seal makes the whole Url — path, query string and fragment — the stored EdgeState cause of every post-record navigation, returned by Backward/Forward and rendered by the chrome on every render the edge stays refused. UrlChanged is sealed and framework-owned, so no adopter can mark it SensitiveCause. A password-reset or magic-link token in the query string is displayed in plain text. Rendered-chrome exposure, not a retention one; disposition is accepted-and-documented rather than a framework-side SEC-2 rule."
  - file: README.md
    reason: "README.md:183-190 is this repository's own counter-example to the obligation above — it hands an adopter `url => new UrlChangedTo(url)`. tech-writer fixes it at plan step 13, noting that the README example is not itself a WithHistory adopter."
medium:
  - reason: "Cite 00-scope.md by invariant id and quoted clause, never by line range. The amendment lengthened INV-2 and staled four line-range citations across the plan and the spec at once. The scope is a file that moves; the ids exist to make it citable."
  - reason: "00-scope.md now reads 'at least one property per id' — INV-2 carries two properties, and validate-phase-artifact.sh reports only missing ids, with no count and no cap. Plan step 8(a) reads the same."
good:
  - reason: "The seal is derivable rather than agreed: of the four classification rows, only `seal` survives the amended INV-2. The Critic re-derived it independently instead of accepting the account, and confirmed the amendment with no revision 5, no gate reopened and no mechanism added."
references:
  - architect-20260907T120000Z-undo-redo
---

Incoming browser navigation is a commitment in both directions: INV-2's world now includes the browser's location, and a post-record `UrlChanged` seals the history on both edges instead of recording an undo stop.

Amends `architect-20260907T120000Z-undo-redo` (`WithHistory`, refusal at the effect boundary). The direction, the mechanism and every gate decision in that drop stand; this is one classification row and its consequences.

## What was wrong

Only navigation *before* anything was recorded had a rule (origin re-basing). After `Origin` becomes `Established`, a `UrlChanged` delivered by the browser on a **back or forward press** was enveloped like any other message and took the `record` row. Undo across it restored a model carrying the previous page's `Route` while the browser stayed where the user put it, and the browser's own back/forward stack walked away from the application's history. Raised by the user after close-out; no phase caught it.

The reason no phase caught it is worth more than the bug: `00-scope.md`'s INV-2 said *"application state"* and its falsifier said **model**, so every downstream property quantified over the model alone and was green for this whole class of violation. Track A had already named the shape — `model ⊗ world` — in `01-track-a.md`; the scope had not.

## Decision

**INV-2 amended** (the pass's third `00-scope.md` amendment): the state it quantifies over is the model **together with the location the user is at**, on any head where the application has one. A navigation is a commitment in both directions — one the application asked for, and one the browser delivered. The two move together or not at all. Falsifier extended with the navigation case and with the browser-stack divergence case; id and falsifier shape kept.

**One classification rule, a standing constraint on implementation:**

> `origin is UrlChanged && Origin is Established -> seal, SealedByWorld(UrlChanged)` — both incident edges. `Backward(h)` and `Forward(h)` refuse with the navigation as cause.

## Why the seal is forced, not chosen

Of the four classification rows against an incoming `UrlChanged` once `Origin` is `Established`: `record` mints an undo stop whose crossing produces a `(model, location)` pair no ordinary interaction produced — the amended INV-2's new falsifier verbatim; `pass` leaves the *earlier* stop crossable and hits the same falsifier one press later; `rebase` is `Fresh`-only by construction and unavailable in this window; `seal` satisfies the invariant exactly, because `Present.Route` moves with the location in the same transition and both edges refuse thereafter. The amended INV-2 both permits the seal and **forces** it.

**INV-3 needed no location clause, and that is a finding rather than an omission.** `Present.Route` moves only on a transition whose origin is `UrlChanged`; after the first `record` every such transition seals both incident edges, and before it there is nothing to cross — so the location is **constant across any window in which a movement is permitted**. A location conjunct on INV-3 would be one no property could ever turn red on, which is worse than leaving it out.

## Consequences

- **The reach statement gains a third bound** — the last incoming navigation — alongside the last command feedback and `Depth` ÷ subscription rate. In a routed application it is usually the binding one: in Conduit, undo reaches back to the current page and no further.
- ***"The world is a command's feedback and nothing else"* is no longer true unqualified.** It carries one exception in each of its four homes.
- **`ux-expert`'s q9/q13 premise changed** and that brief is dispatched first, so the corrected premise travels with wave 0.
- **Cost:** no new type, no new `EdgeState`, no new `MovementAvailability` case, no new `Scrub` site, no policy member, no dependency, no line of `Runtime.cs`. Doc-and-one-row.

## Alternatives considered

- **Qualifying INV-2 with the adopter's wiring condition** — rejected. The condition is a statement about the application's own composition, which no framework property can quantify over; it belongs in the spec's obligations table beside the lens laws, not inside an invariant that is true of the framework.
- **Mechanism for the converter hole** (policy predicate, framework-owned `UrlChanges` wrapper, analyzer rule) — all three are new mechanism and one reverses a gate-4 deletion; none is needed to state the truth. **An analyzer rule is named as a follow-on candidate**, since an obligation a compiler can check beats one a guide asserts, and `Picea.Abies.Analyzers` already exists.
- **A framework-side SEC-2 rule for `UrlChanged`'s URL** — declined by the user in favour of the documented limitation above.

Full artifacts: `.squad/design/undo-redo/`. The rule and its derivation are `04-realist-plan.md` § *The classification rule* `[R4-nav]`; the confirmation is `05-critic.md` § *confirmation pass, 2026-09-07* (CONFIRMED WITH A BOUNDED LIST — two 🔴, four 🟠, seven 🟡, all thirteen mitigations accepted by the user); the execution contract is `07-handoff.md` § 2.5, standing decision 7, § 5.3 and § 6.1.


### 2026-09-07 — reviewer-reconcile-20260907T121500Z-pr359-undo-redo-design-record [reviewer-reconcile · NEEDS-CHANGES]

---
id: reviewer-reconcile-20260907T121500Z-pr359-undo-redo-design-record
agent: reviewer-reconcile
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-07T10:25:28Z
commit: 66379e7e2f9e7e5cbd5c3554369f1d5177fca176
targets:
  - path: .claude/enforcement/refutations.md
    lines: "544-590"
  - path: .claude/docs/flow-changelog.md
    lines: "35-56"
  - path: .claude/docs/decisions.md
    lines: "2158-2290"
  - path: .squad/design/undo-redo/
  - path: Picea.Abies.Conduit.ServiceDefaults/Extensions.cs
    lines: "32"
blockers:
  - file: .claude/enforcement/refutations.md
    line: 550
    reason: "R-21's source: cites 00-warden.md section 'Findings on Adjacent Artifacts', which exists in no file in the tree. The committed 00-warden.md is the re-run, verdict CLEAN, 0 findings, and git log --all shows a single version, so no earlier copy is recoverable. The Merge Criterion makes refutations.md a gating document; an entry whose evidence pointer does not resolve cannot be audited. R-21's substance is independently sound (enforce-track-blindness.sh:277-290 has no docs/adr/** DENY glob) so the fix is the citation, not the entry."
  - file: .claude/docs/flow-changelog.md
    line: 40
    reason: "The entry certifying gate 1's effectiveness says scope-warden reads 00-warden.md. The warden writes 00-warden.md and reads 00-warden-scan.md, per both artifacts' own text, CLAUDE.md section 3 rule 2, and memory-policy.md. As written the judgement half of the gate reads its own output, in the one governance entry whose purpose is to grade that split. The claim it certifies is true (verified against the deleted 00-scope-undo-redo.md:25,74 at f0cb682), which is why the sentence describing it should be too."
  - file: .claude/enforcement/refutations.md
    reason: "Merge Criterion clause (b) unmet: four verified findings are registered nowhere with owner, level consequence and expires: — the decision-drop created: field that critic and architect cannot produce (no Bash tool; pre-existing since the 2026-09-02 pr355 drops), the six unregistered ActivitySources in ServiceDefaults, pass-cost.md's header stating units its cumulative data does not have, and session-logger.sh recording orchestrator intentions at 25:214 signal-to-noise for a second consecutive day. All four are pre-existing and therefore registrable rather than fixable here; registration is the remedy, and enforce-reviewer-readonly.sh forbids the reviewer from writing it."
high:
  - file: .claude/docs/decisions.md
    line: 2172
    reason: "The architect drop's Security paragraph lists Trust Boundary 7, two threat-model rows and two hardening-backlog fast-follows as produced. They are plan step 16 deliverables, unchecked, and docs/security/ is untouched by this commit. Settled against 04-realist-plan.md:1805,1865 and 07-handoff.md:273,406 and section 5.1 — these ARE deliverables and the record says so clearly, two documents away. Every other forward-looking item in the same drop is marked as such, so these read as accomplished by contrast. One clause fixes it. targets: listing ADR-030 is within schema (paths the drop pertains to) and is not flagged."
  - file: .claude/agent-memory/architect/MEMORY.md
    line: 6
    reason: "Index line presents ADR-030 as the settled authority ('ADR-030's decision') for a file that does not exist and is step 13's deliverable. A memory index is what a future session believes without checking."
  - file: Picea.Abies.Conduit.ServiceDefaults/Extensions.cs
    line: 32
    reason: "Six declared ActivitySources are collected nowhere — Picea.Abies.Runtime, Picea.Abies.Subscriptions, Picea.Abies.Server.Page, Picea.Abies.Server.Session, and the two Server.Kestrel sources — and no ActivitySource in the repository is named exactly 'Picea.Abies', so the one framework registration matches nothing at all. Wider than either the design record or the blind reading states. Pre-existing, out of scope for a docs-only PR, and correctly named in 07-handoff.md section 9 — but that section is headed 'named, not scheduled' and there is no issue, backlog entry or ledger entry. Violates the team's observability principle and dimension 8's no-dark-services rule."
  - file: .squad/decisions/archive/2026-09/2026-09-07T09-34-55-arch-undo-redo-navigation.md
    line: 6
    reason: "Five of seven new drops carry unreadable-clock created: values; two are dated after the commit that introduces them (architect claims 18:45:00Z against a 09:56:59Z commit). Root cause is structural, not dishonesty: critic and architect declare no Bash tool and cannot run date -u, and the correlation with clock access is exact across all seven drops. Pre-existing (2026-09-02 pr355-round5 is future-dated by 36 minutes). Two of the blind reading's three harms do not materialise — squad-rotate.py ages by mtime and decisions.md lists in merge order — but id collision does: two drops already use T000000Z and the schema requires global uniqueness. Do not rewrite the ids (documented stable anchors, cited from three places); correct the two impossible created: values and let scribe-decision-merger.sh stamp the field from the clock it already reads at :1202."
medium:
  - file: .claude/agent-memory/security-expert/state-retention-features-need-sensitivity-marker.md
    reason: "Recommends 'ISensitiveCause : Message' as cross-session guidance. Gate-4 decision 5 removed the I prefix (07-handoff.md:206); revision 4 uses the prefixed form nowhere. One token, plus half a sentence recording that the prefixed form was rejected."
  - file: .claude/agent-memory/critic/picea-xml-is-the-kernel-contract.md
    reason: "Sends future agents to Picea.xml without naming the version trap. Verified: 1.0.0's Picea.xml is 1369 lines and documents AutomatonRuntime's members; 1.0.27-rc-0002's is 477 lines and mentions AutomatonRuntime exactly once, the type declaration, with zero members. Four projects reference the rc. Same fix needed in realist/picea-package-xml-answers-kernel-questions.md."
  - file: .squad/log/pass-cost.md
    line: 3
    reason: "Header says 'wall-clock per design phase'; the column is cumulative and monotonic and does not reset across slugs. The correction lives only in dreamer-convergence's private notebook, and the header itself declares this file the denominator for whether the dual track earns its cost."
  - file: .squad/design/undo-redo/00-warden.md
    reason: "Framework observation: CLAUDE.md section 3 rule 2 prescribes a warden re-run on scope rework, and the warden writes to the same path, so the first report is overwritten. Both R-21's citation and flow-changelog's two-leaks claim point at a report that no longer exists. Both claims are nonetheless true — verified independently against f0cb682's deleted draft and the hook source — but a governance record whose evidence is destroyed by the documented happy path deserves registering."
good:
  - file: .squad/design/undo-redo/07-handoff.md
    lines: "452-453"
    reason: "Both blockers from the Critic's confirmation pass are discharged with the residue named rather than absorbed: B10's mitigation (1) landed as 06-spec.md obligations line 17, (2) is recorded as still owed to tech-writer at step 13, (3) is explicitly out of pass. B11 corrected in both places marked 'implement from this', with 'Do not implement from any copy that still carries it'."
  - file: .squad/design/undo-redo/07-handoff.md
    lines: "517-524"
    reason: "S25 registered as an unresolved risk in the author's own words with its failure mode stated: 'This is a documented precedence rule, not a resolution; if an implementer reads only the plan, they will implement the wrong narrative.' Authors rarely write that sentence about their own artifact."
  - file: .squad/design/chore-0-squad-flow-v2-1/09-review-verdict.md
    reason: "The commit-boundary confirmation is a pure append (zero deletions), labelled 'not a re-review', carries the verdict forward unchanged, and distinguishes what it diffed from what it inferred in an explicit 'Claims I could not verify' section. Correct shape for amending a closed verdict artifact."
  - file: .squad/design/undo-redo/
    reason: "Citation discipline holds inside the design directory as well as outside it. The blind reviewer found twenty of twenty drop citations accurate; B10, B11, S25, S29, S31 and S20 dispositions each check out against the artifact and line they name. Mechanically clean besides: seven drops archived and merged exactly once each, R-21 the next unused id in a gap-free ledger, zero orphaned memory notes, zero dangling wikilinks, all seven INV ids covered by the spec, Track A's Reasoning Trail and Track B's Known Failure Modes present, lexicon gate passed with no override."
references:
  - architect-20260907T120000Z-undo-redo
  - architect-20260907T184500Z-undo-redo-navigation
  - reviewer-reconcile-20260906T134928Z-pr358-commit-confirm
---

# Review — PR #359, `undo-redo` design record

🔴 **Changes Requested.** Docs-only, 67 files, all `.md`, +11738/−86; nothing the
runtime executes is in the range. The changeset's stated property is green
(`bash .claude/hooks/tests/run.sh` → 823 passed, 0 failed at this HEAD).

The engineering is not in question. The record's own metadata and provenance are.
Three blockers: a residual-ledger citation that resolves to nothing, a governance
entry that misstates the control it grades, and four verified findings that the
Merge Criterion requires registered before ✅ and which the reviewer is forbidden
to register itself.

Full reasoning, the reconciliation against `08-review-blind.md`, and the Critic's
accepted-risk audit are in
`.squad/design/undo-redo-design-record/09-review-verdict.md`.


### 2026-09-07 — reviewer-reconcile-20260907T104055Z-pr359-round2-undo-redo-design-record [reviewer-reconcile · NEEDS-CHANGES]

---
id: reviewer-reconcile-20260907T104055Z-pr359-round2-undo-redo-design-record
agent: reviewer-reconcile
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-07T10:40:55Z
commit: 730c7daf4039057335c432cf1a9c50f77114fef0
targets:
  - path: .claude/enforcement/refutations.md
    lines: "591-721"
  - path: .claude/docs/flow-changelog.md
    lines: "38-45"
  - path: .claude/docs/decisions.md
    lines: "2162-2394"
  - path: .squad/decisions/archive/2026-09/
  - path: .claude/agent-memory/architect/MEMORY.md
  - path: .claude/agent-memory/security-expert/state-retention-features-need-sensitivity-marker.md
  - path: .squad/design/undo-redo-design-record/09-review-verdict.md
blockers:
  - file: .claude/enforcement/refutations.md
    reason: "Merge Criterion (b) is met for the four findings my round-1 blocker enumerated and unmet for three it did not. Unregistered and unfixed at this HEAD - the framework half of round-1 W-4 (a warden re-run overwrites 00-warden.md and destroyed the evidence for two governance claims; my own round-1 text said 'register it' and my blocker's list then omitted it), W-7 (security-expert's own memory note still recommends ISensitiveCause, the name gate-4 decision 5 rejected), and W-8 (the critic and realist Picea.xml notes still omit the version trap). The binding clause is principles-enforcement.md section The Merge Criterion (b) - 'every finding that is not a regression of a stated property is registered' - not the reviewer's enumeration, and its own 'no longer blocking, once registered' line puts advisory findings inside that scope. Remedy is one ledger entry (R-26) that may absorb all three, plus two one-token memory-note edits by their owners. This is round 2 of 2 - a third round triggers the cap and the verdict there is the split, so the proportionality call between landing this residue and overriding is the user's, and this blocker records the uncertainty rather than forcing a fix round."
  - file: .claude/enforcement/refutations.md
    line: 649
    reason: "R-22's level consequence states in the present tense a condition the same commit removed - 'five of the seven drops in this pass carry unreadable-clock created: values, two of them dated after the commit that introduces them'. At this HEAD three do (critic-20260906T184500Z, critic-20260906T000000Z-undo-redo-pass-2, critic-20260907T000000Z-undo-redo-pass3) and none is dated after its commit, because round-1 W-1(a) landed in the same commit that registers R-22. Everything else in the entry is exact and verified: the root cause, the Bash-holding-reviewer counter-example, the id-collision residual, and the scribe-decision-merger.sh:1202 remedy pointer. refutations.md is append-only and is the instrument the next round grades against, so once this is history nobody can distinguish open-because-unfixed from open-because-written-before-the-fix-in-its-own-commit. Not structurally malformed - every required field is present - so the remedy is one appended line under R-22, never an in-place edit."
high: []
medium:
  - file: .claude/enforcement/refutations.md
    lines: "649-721"
    reason: "R-22 through R-25 all carry owner devops, which matches my round-1 suggestion, but R-22's and R-24's and R-25's remedies are hook scripts under .claude/hooks/ and CLAUDE.md's routing entry for devops names .github/workflows/** rather than .claude/hooks/**. The owner assignment is right; the routing table is what is silent. Not this PR's business."
  - file: .claude/enforcement/refutations.md
    line: 591
    reason: "The Correction to R-21 section has no forward pointer from R-21 itself, so it is discoverable only by adjacency or by grep. An in-place pointer would violate the append-only rule, so this is a property of the format rather than a defect in the fix."
good:
  - file: .claude/enforcement/refutations.md
    line: 591
    reason: "The R-21 correction chose a more durable citation than the one I asked for: it quotes both leaked sentences inline with their commit and line numbers (f0cb682:00-scope-undo-redo.md:25 and :74, both re-verified verbatim here) and re-derives the same conclusion from a second independent source (enforce-track-blindness.sh:277-290, re-checked - grep for docs/adr returns no output). A pointer into .squad/design/ is destroyed by the documented happy path; a commit-pinned quotation is not, and it is the only form reviewer-blind can check."
  - file: .claude/enforcement/refutations.md
    lines: "591-600"
    reason: "The append-only rule was reasoned about rather than obeyed by reflex. The correction's preamble argues why it is not the user-only Extensions case, cites the Round 2 closures precedent it follows, and states what it does not do - extend an expiry or widen what R-21 licenses. Verified as a pure insertion; R-21's body, status, expires and owner are byte-identical."
  - file: .squad/decisions/archive/2026-09/
    reason: "The id anchors were left alone under pressure to tidy them. Correcting created: while leaving five wrong-looking id: values in place is the uncomfortable half of W-1's fix and the half that keeps three external citations resolving."
  - file: .claude/docs/decisions.md
    line: 2232
    reason: "W-2 fully closed. The Security paragraph now marks Trust Boundary 7, the threat-model rows and the hardening-backlog fast-follows as security-expert's plan step 16 deliverables and ADR-030 as tech-writer's step 13; the architect memory index line and the note's description no longer present ADR-030 as written. grep -rn ADR-030 .claude/ finds no surviving sentence claiming the file exists."
  - file: .claude/docs/flow-changelog.md
    line: 40
    reason: "Round-1 blocker 3 closed. The entry now reads 'reading 00-warden-scan.md and adding the category no regex reaches', which is the flow CLAUDE.md section 3 rule 2 prescribes and both artifacts describe in their own text. The judgement half of gate 1 no longer reads its own output in the one governance entry whose purpose is to grade that split."
references:
  - reviewer-reconcile-20260907T121500Z-pr359-undo-redo-design-record
  - architect-20260907T120000Z-undo-redo
  - architect-20260907T184500Z-undo-redo-navigation
---

# Re-review — PR #359 round 2, `undo-redo` design record

⚠️ **Needs Human Review.** All three round-1 blockers are closed and verified
against the tree; the stated property is green (`bash .claude/hooks/tests/run.sh`
→ 823 passed, 0 failed at this HEAD, unchanged from `66379e7`, which is the
correct result for a documentation-and-ledger delta). Scope is exactly the
claimed remedies plus review artifacts and hook-written state — 15 files, no
creep, nothing code-shaped.

What keeps this off ✅ is narrower than round 1 and partly my own doing. Merge
Criterion (b) is satisfied for the four findings my round-1 blocker enumerated
and unsatisfied for three it did not, one of which my own round-1 finding text
asked to be registered. The residue is four one-line items: an appended
correction under R-22, one new ledger entry, and two one-token memory-note
edits.

This is round 2 of 2. A third round triggers the cap, and under the cap the
round-3 verdict is the split rather than another fix request — so the choice
between landing the residue now and overriding to merge is the user's, and I am
not converting the two ⚠️ findings into blockers to force the first.

Full reasoning, the ledger audit run in both directions, the ⚠️-4 reading
checked against the binding clause, and the stop instruction for the closing
pass are in `.squad/design/undo-redo-design-record/09-review-verdict.md`
§ *Re-review — round 2*.


### 2026-09-07 — reviewer-reconcile-20260907T105656Z-pr359-confirm-undo-redo-design-record [reviewer-reconcile · PASS]

---
id: reviewer-reconcile-20260907T105656Z-pr359-confirm-undo-redo-design-record
agent: reviewer-reconcile
verdict: PASS
scope: review
created: 2026-09-07T10:56:56Z
commit: 756bf4d04af987e70b4fc1e0f00eb08a99c3d5f8
targets:
  - path: .claude/enforcement/refutations.md
    lines: "668-770"
  - path: .claude/agent-memory/security-expert/state-retention-features-need-sensitivity-marker.md
    lines: "28-33"
  - path: .claude/docs/decisions.md
    lines: "2393-2474"
  - path: .squad/decisions/archive/2026-09/2026-09-07T10-41-14-review-pr359-round2.md
  - path: .squad/design/undo-redo-design-record/09-review-verdict.md
blockers: []
high: []
medium:
  - file: .claude/enforcement/refutations.md
    line: 737
    reason: "R-26 bundles three sub-items carrying two statuses - items 1 and 3 open, item 2 (round-1 W-7) reported closed in the same commit. An auditor grepping 'status: open' re-checks a sub-item the body already reports closed. The inline disclosure handles it and unbundling would cost two more ids, so this is a note on the shape rather than a requested change; it is also the handling this file's own preamble prescribes, applied to a sub-item."
  - file: .claude/enforcement/refutations.md
    line: 668
    reason: "The note appended under R-22 pins commit 730c7da, one commit behind the commit it lands in. Conservative rather than wrong - the three-drop count it states was re-derived at 756bf4d and is unchanged, because the one drop this commit adds (reviewer-reconcile-20260907T104055Z) carries a readable created: value 19 seconds off its archive stamp. A commit-pinned claim is the durable form."
good:
  - file: .claude/enforcement/refutations.md
    lines: "668-684"
    reason: "Item 1 confirmed. The note under R-22 is a pure insertion (hunk @@ -666,6 +666,23 @@, zero deleted lines), so R-22's level consequence, owner, expires and status are byte-identical to 730c7da. It sits inside Residuals and bounds, not the user-only Extensions section, extends no expiry and widens no scope, and argues its own permissibility against the append-only rule before using it. Substance re-derived: exactly three drops in the branch's archive delta carry unreadable-clock created: values (critic 18:45:00Z and two 00:00:00Z), and none is dated after its commit - the two architect drops now read 08:03:52Z and 09:34:55Z, matching their archive filenames. The entry correctly stays open; the process defect it registers is untouched by a wording correction."
  - file: .claude/enforcement/refutations.md
    lines: "737-770"
    reason: "Item 2 confirmed and audited in both directions. R-26 lands with type, source, files, owner, level consequence, expires 2026-09-21 and status open; ids R-1 through R-26 are gap-free with no duplicates. Sub-item 1 verified still open - CLAUDE.md:153 has the warden re-run and overwrite 00-warden.md at the same path, and .claude/agents/scope-warden.md is untouched across 66379e7~1..756bf4d. Sub-item 3 verified still open - neither the critic nor the realist Picea.xml note mentions 1.0.27 or the version trap, and the package's Picea.xml is 477 lines as stated. Sub-item 2 is reported closed inline rather than registered as open, so nothing already fixed is registered as a residual. R-26 also records the binding reading of Merge Criterion (b) in the ledger, which is where the next round grades from."
  - file: .claude/agent-memory/security-expert/state-retention-features-need-sensitivity-marker.md
    line: 30
    reason: "Item 3 confirmed. The note now recommends 'SensitiveCause : Message' with no I prefix and records that the prefixed form was proposed and rejected, citing 07-handoff.md:206. grep -rn ISensitiveCause across .claude/ returns no surviving recommendation - the remaining hits are this note's own record of rejection, R-26's body, and two verbatim quotations of the round-1 finding inside decision drops. The note's 'gate-4 decision 5' is not an off-by-one against the standing-decisions table row 6: it is the pass's own established vocabulary, used identically at 04-realist-plan.md:847 and :2189 and 07-handoff.md:527, and the cited line number resolves to the row stating the rule."
  - file: .squad/design/undo-redo-design-record/09-review-verdict.md
    reason: "Item 4 confirmed - nothing else changed. git diff --numstat 730c7da..756bf4d is 9 files, +567/-2, and the only deletion in the range is the two lines of the security-expert memory note that item 3 replaces. Every file is a named remedy, a round-2 review artifact, or session-logger.sh state. No .cs, .js, .csproj, .sh, workflow or appsettings file entered the range. The fixes and the round-2 review artifacts landed in one commit, which is what round 2's stop instruction asked for and the reason this pass has a single sha to confirm."
  - file: .claude/hooks/tests/run.sh
    reason: "Stated property re-executed at 756bf4d: 823 passed, 0 failed - unchanged from 730c7da and 66379e7, the correct result for a documentation-and-ledger delta. Stated for honesty: this establishes behaviour is unchanged and nothing more. invariant-chain.sh mentions refutations.md only inside a comment, so the suite contains no structural validator for the ledger; R-26's field completeness was checked by hand."
references:
  - reviewer-reconcile-20260907T104055Z-pr359-round2-undo-redo-design-record
  - reviewer-reconcile-20260907T121500Z-pr359-undo-redo-design-record
---

# Confirmation — PR #359 round-2 residue, commit 756bf4d

PASS. The four residue items landed, each is true of this tree, and nothing else rode along.

This is the confirmation the round cap prescribes after the user chose path 1 with no override — a scope-and-truth check, not a third round. No dimension was reopened, and no finding the cap ruled out of scope was re-derived.

**Merge Criterion at 756bf4d.** (a) green — `bash .claude/hooks/tests/run.sh` → 823 passed, 0 failed, unchanged. (b) met — round-1 W-1 through W-8 are each now fixed (W-1, W-2, W-4's evidence half, W-7) or registered with owner, level consequence and expiry (W-3 → R-23, W-5 → R-24, W-6 → R-25, W-4's framework half and W-8 → R-26); round-2 W-9 is fixed by the R-22 note and W-10 is discharged by R-26. The reading applied, stated so the next round need not guess: (b) binds on blocking and advisory findings; the two round-2 nitpicks are not registered and do not block, because neither asserts a defect requiring a remedy, both are findable in the merged round-2 drop, and a reading under which every nitpick must reach the ledger regresses without limit. (c) not in question — no level claim moved.

**Where to stop.** Push and merge at 756bf4d04af987e70b4fc1e0f00eb08a99c3d5f8. `.squad/.last-review-verdict` currently still reads NEEDS-CHANGES / 730c7da, so the gate is closed until this drop merges and `scribe-decision-merger.sh` writes the cache from it. Do not commit the confirmation section, this drop or the hook log churn before the merge: `enforce-review-verdict.sh` scopes its commit branch to code-shaped paths and would allow such a commit, but its push and merge branches compare the cache against `git rev-parse HEAD` unconditionally, so an artifact commit moves HEAD past 756bf4d and re-blocks the merge this pass exists to clear.

Full reasoning, the both-directions ledger audit and the per-item evidence are in `.squad/design/undo-redo-design-record/09-review-verdict.md` § *Confirmation — round-2 residue, commit `756bf4d`*.
### 2026-09-07 — reviewer-reconcile-20260907T120325Z-presentation-demo [reviewer-reconcile · NEEDS-CHANGES]

---
id: reviewer-reconcile-20260907T120325Z-presentation-demo
agent: reviewer-reconcile
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-07T12:03:25Z
commit: 07607bf71152ef8d224352e2d2a737d8312dcb2d
targets:
  - path: Picea.Abies.Presentation/Picea.Abies.Presentation.csproj
    lines: "12-19"
  - path: Picea.Abies.Presentation/content/demo/README.md
  - path: Picea.Abies.Presentation/content/demo/stops/5.1-warden-report.md
  - path: Picea.Abies.Presentation/content/demo/stops/5.2-hook-line.sh
blockers:
  - file: Picea.Abies.Presentation/Picea.Abies.Presentation.csproj
    line: 17
    reason: >-
      Merge-blocking. The <None Remove> item strips the 42 None items that populate the IDE
      file tree, making a folder meant to be presented from invisible in Visual Studio and
      Rider. Measured: with <Compile Remove> as the sole guard the build succeeds,
      ComputeFilesToPublish yields 0 entries under content/demo, and Content/EmbeddedResource
      match 0 items either way - so all three stated goals (never compiled, embedded, or
      published) are met by one line, and the other three Remove items are a no-op or a net
      cost. Fix: reduce the ItemGroup to the Compile Remove line alone.
  - file: Picea.Abies.Presentation/content/demo/README.md
    reason: >-
      Talk-blocking, per CLAUDE.md's rule that Picea.Abies.Presentation factual claims
      require reviewer-reconcile sign-off before a talk ships. Rule: every shipped fragment
      that contradicts another shipped file, and every README rationale that asserts more
      than its cited span contains, must be annotated to the standard the README already
      applies elsewhere. Four instances, non-exhaustive: (a) 5.1-warden-report.md:3 says the
      gate checked a 153-line scope while the shipped full/00-scope.md is 254 lines, and its
      "(line 99-101)" citation resolves to unrelated prose - the quoted sentence is at 221;
      (b) the same fragment is truncated one line before the R-21 conclusion while the README
      flags exactly that defect on 5.2-track-b-summary.md; (c) the 5.2-hook-line.sh rationale
      claims the span contains the refuse-for-everyone fallback when the span holds only the
      UNREADABLE signal - the fallback is at enforce-track-blindness.sh:422, outside it;
      (d) both 5.6 fragments cite undo-redo-design-record sources absent from full/ and
      unmentioned in the exclusions note.
high:
  - file: Picea.Abies.Presentation/content/demo/
    reason: >-
      No divergence detector for a snapshot of living files. full/refutations.md copies an
      appended-to ledger, and the full/* artifacts copy files the design process overwrites
      in place. The "unedited copies" claim is true today - I verified all 19 byte-identical
      at 07607bf - but degrades to asserted the moment anyone edits full/. A SHA256SUMS
      generated at snapshot time would keep the claim checkable forever, including by
      reviewer-blind, which is denied .squad/design/ by hook and could never otherwise
      verify it.
  - file: Picea.Abies.Presentation/content/demo/stops/5.4-critic-verdicts.md
    reason: >-
      Composite fragments carry no in-file provenance. Five spans from four files, ending in
      two bare "## Verdict" headings from different passes of the same document with no
      separator. All five spans verified exact with no authored text added, so the fragments
      are honest - but the attribution lives only in the README table, and slide content is
      precisely what gets separated from its index. An HTML comment naming the spans costs
      nothing.
medium:
  - file: Picea.Abies.Presentation/content/demo/stops/5.2-hook-line.sh
    reason: >-
      Extension misleads on a projector: the content is Python lifted from a heredoc, so any
      highlighter renders it as shell. Likewise timing/hooks-fired.log is markdown named
      .log, and it is the one authored (not copied) file in the bundle. Renaming
      5.5-property.cs would additionally dissolve the csproj blocker entirely, since that
      one extension is the only reason the build guard is needed.
  - file: Picea.Abies.Presentation/content/demo/README.md
    reason: >-
      Off-by-one: full/refutations.md:722 is cited for R-25, which is at 723 (722 is blank).
      The neighbouring R-24 citation at :707 is exact. Trivial except that line-precise
      citation is this artifact's entire premise.
  - file: .github/workflows/pr-validation.yml
    reason: >-
      Informational, no action required. check-pr-size hard-fails (10,151 lines,
      maintenanceOnly false due to six non-.md fragments plus the .csproj), but it is not a
      required status check - the PR shows UNSTABLE, not BLOCKED, and squash-merges normally.
      Renaming fragments would not help, since the .csproj alone keeps maintenanceOnly false.
      The real cost is detect-changes setting docs_only=false, running the full CI matrix on
      a documentation change. Worth a line in the PR description so the red X reads as
      priced-in rather than missed.
good:
  - file: Picea.Abies.Presentation/content/demo/full/
    reason: >-
      All 19 whole-file copies byte-identical to their sources, verified twice - against the
      working tree and against git show 07607bf. The blind reviewer could verify 2 of 19; the
      remaining 17 are now confirmed. The change's central claim holds completely.
  - file: Picea.Abies.Presentation/content/demo/README.md
    reason: >-
      Refuses to fabricate. Three requested fragments were not created because the text does
      not exist at 07607bf, each row recording what was searched and the nearest genuine
      quote; a PR #358 decision drop is excluded with a stated reason. Index integrity
      confirmed by comm - 20 files, 23 rows, the 3 orphan rows are exactly the not-created
      three, zero orphan files.
  - file: Picea.Abies.Presentation/content/demo/timing/hooks-fired.log
    reason: >-
      Declares itself compiled-not-verbatim in its own header, separates hook runs from
      subagent runs, and carries a "Not evidenced at all" section. Contrary to the blind
      reading, it does not overclaim the warden's line count - it attributes the 254-line
      figure to the mechanical scan only, which the surviving scan file supports.
references: []
---

Every verbatim claim this change makes is true - all 19 full/ copies and all 21 stops fragments verified byte-exact at the pinned commit - but the csproj guard hides the tree from the IDE it is meant to be presented from, and the index leaves four factual discrepancies unannotated to a standard it meets everywhere else.

## Reconciliation with the blind reading

The blind reviewer's headline finding is resolved and partly withdrawn. The 153-vs-254 line
discrepancy is in the source at 07607bf, not introduced here: `00-warden.md:3` says 153,
`00-scope.md` is 254, and `00-warden-scan.md:9` says 254 - the bundle copies all of them
faithfully. And `hooks-fired.log` does not make the contradictory claim attributed to it; it
attributes the 254-line figure to the mechanical scan only, which is correct.

What survives is a presentation hazard rather than an error: the warden report's
`(line 99-101)` citation resolves to unrelated prose in the shipped scope (the sentence is at
221), the 153-line scope was never committed so those citations are permanently
unverifiable, and the README is silent on both.

## Suggested fix

Reduce the csproj ItemGroup to the `<Compile Remove>` line alone - proven sufficient by
measurement - and add four annotations to README.md per the rule stated in the second
blocker. Neither fix touches the bundle's substance; the copies and excerpts are sound.

Full verdict: `.squad/design/presentation-demo/09-review-verdict.md`


### 2026-09-07 — reviewer-reconcile-20260907T122033Z-presentation-demo-round2 [reviewer-reconcile · NEEDS-CHANGES]

---
id: reviewer-reconcile-20260907T122033Z-presentation-demo-round2
agent: reviewer-reconcile
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-07T12:20:33Z
commit: 07607bf71152ef8d224352e2d2a737d8312dcb2d
targets:
  - path: Picea.Abies.Presentation/Picea.Abies.Presentation.csproj
    lines: "12-15"
  - path: Picea.Abies.Presentation/content/demo/README.md
    lines: "80,86,87,88,97,98"
  - path: Picea.Abies.Presentation/content/demo/stops/5.1-warden-report.md
  - path: Picea.Abies.Presentation/content/demo/SHA256SUMS
blockers:
  - file: Picea.Abies.Presentation/content/demo/README.md
    line: 97
    reason: >-
      Merge-blocking. The 5.6-review-headers row offers 2026-09-07T11:56:59+02:00 as "the
      commit's own author date", citable on a slide. That is the author date of 66379e7, the
      pre-squash branch commit 08-review-blind.md was reviewing; 07607bf, which README line 4
      defines as "the commit" and every column header pins to, is authored
      2026-09-07T13:00:00+02:00. The number is faithfully quoted from 08-review-blind.md:105 -
      the attribution to 07607bf is introduced here and is false as committed. Verified with
      git show -s --format=%aI on both commits. Fix is one clause naming 66379e7 and giving
      07607bf's real author date.
  - file: Picea.Abies.Presentation/content/demo/stops/5.2-track-a-summary.md
    reason: >-
      Talk-blocking, per CLAUDE.md's rule that Picea.Abies.Presentation factual claims require
      reviewer-reconcile sign-off before a talk ships. The fragment named for Track A contains
      the Lead's narration about Track B - "Track B has landed with three ranked candidates ...
      Waiting on Track A" - a line that explicitly states Track A had not finished. Its pair,
      5.2-track-b-summary.md, is about both tracks running, not about Track B's findings. Both
      spans are byte-exact and both agent labels are correct; the README's R-25 caveat explains
      whose voice the line is in but not that its subject is the other track. A slide captioned
      "Track A's summary" showing this text states something false. Fix is one sentence per row,
      to the standard the README already applies to the truncation flagged two clauses later.
high:
  - file: Picea.Abies.Presentation/content/demo/stops/
    reason: >-
      Carried unaddressed from round 1. The four composite fragments still carry no in-file
      provenance naming their spans; 5.4-critic-verdicts.md is five spans from four files ending
      in two bare "## Verdict" headings from different passes with only blank-line separators.
      All spans re-verified byte-exact with no authored text, so the fragments are honest - but
      the attribution lives only in the README table. Unregistered, so it prevents an approve
      under Verdict Consistency Rule 1. Intended resolutions are four HTML comments or an
      explicit user override, NOT registration in the enforcement residual ledger - a demo-folder
      annotation does not belong there.
medium:
  - file: Picea.Abies.Presentation/content/demo/stops/5.1-warden-report.md
    reason: >-
      No trailing newline. Now the only file in the bundle without one; round 1 verified every
      non-empty file newline-terminated, so this is a small regression from the hand-cut at
      refutations.md line 572.
  - file: Picea.Abies.Presentation/content/demo/README.md
    reason: >-
      Two +1 off-by-one citations. full/refutations.md:722 is cited for R-25, which is at 723
      (722 blank) - carried unfixed from round 1. full/05-critic.md:427 is cited for the
      navigation confirmation, whose heading is at 428 (427 blank). The other 25 numeric
      citations resolve exactly. Also, the corrected 5.2-hook-line.sh rationale says :308-315 is
      "too far to fold in without pulling in unrelated surrounding script" - true of :422, not
      true of :308-315, which is contiguous and directly above.
  - file: Picea.Abies.Presentation/Picea.Abies.Presentation.csproj
    reason: >-
      Residual on the closed blocker, not a reopen. 43 of the 44 bundle files are now None items;
      the exception is stops/5.5-property.cs, the file the comment is about - Compile Remove takes
      it out of Compile without putting it into None. Strictly better than round 1 (0 visible to
      43 visible) and not worth a second ItemGroup, but the comment's "the files stay visible as
      None items" is overstated for the one file it names.
good:
  - file: Picea.Abies.Presentation/Picea.Abies.Presentation.csproj
    reason: >-
      Blocker 1 closed and re-measured, not merely re-read. ItemGroup reduced to the single
      Compile Remove line. Build succeeded with 0 warnings and 0 errors while the invalid
      compilation unit stays in the tree; Compile, Content and EmbeddedResource all match 0 items
      under content/demo; ComputeFilesToPublish yields 3 entries with 0 under content/demo; None
      items went 0 to 43. All three stated goals met by one line, IDE visibility restored.
  - file: Picea.Abies.Presentation/content/demo/SHA256SUMS
    reason: >-
      Round 1 finding 3 closed and exceeded. 39 files (19 full/ + 20 stops/), sha256sum -c gives
      39 OK and 0 failures, and re-running the documented generation command reproduces the file
      byte-identically. The README indexes it as authored tooling output rather than an excerpt,
      and pre-states that a future mismatch may mean the source moved rather than the copy being
      edited - the caveat that keeps a snapshot manifest from becoming a false guarantee later.
  - file: Picea.Abies.Presentation/content/demo/stops/5.1-warden-report.md
    reason: >-
      The truncation fix chose the honest resolution. Span extended 546-570 to 546-572 so the
      fragment reaches the "instruction-level" classification the stop exists to show; lines
      546-571 byte-identical to refutations.md and line 572 cut mid-line, disclosed twice in the
      index including the clause deliberately left out. Pure prefix, no authored text, and it
      matches the precedent 5.5-inv-coverage.md already set. The warden half is byte-exact and is
      the whole 19-line file.
  - file: .claude/docs/decisions.md
    reason: >-
      Register cleanup complete and verifiable. No occurrence of the malformed drop's id anywhere
      outside .git/, its archive file is deleted, the valid 12-03-39 drop and its decisions.md
      entry are intact and well-formed, inbox and quarantine are both empty, and
      .squad/.last-review-verdict correctly records NEEDS-CHANGES for 07607bf - written by the
      merger from the valid drop, so the mechanism the cleanup protects is demonstrably working.
  - file: Picea.Abies.Presentation/content/demo/
    reason: >-
      The verbatim discipline survived a second full mechanical sweep. All 19 full/ copies
      re-verified byte-identical against git show 07607bf, all 20 stops/ fragments re-verified
      byte-exact against their cited spans, file count 43 to 44 with SHA256SUMS the only addition
      and nothing removed, timing/ and graphics/ untouched, .squad/ still unedited. The one file
      that changed changed exactly as described.
references: []
---

Blocker 1 is closed by measurement and the register cleanup is verified complete. Blocker 2's four named instances are closed - three of them well - but the rule the blocker stated is not yet met: two further instances, one an author-date attribution that is false as committed. Both predate round 1 and were missed by me, not introduced by these fixes.

## What changed and how it was scoped

Scope of change since round 1 established by mtime against the round-1 drop's merge time: exactly
three paths under `content/` - `stops/5.1-warden-report.md`, `SHA256SUMS` (new) and `README.md` -
plus the `.csproj` and the decision-register cleanup. All 40 other bundle files re-verified
untouched rather than assumed.

## The sweep is now exhaustive

Round 1 stated blocker 2 as a rule with four examples, warning that the enumeration was not the
criterion. The four were fixed; the sweep was not run. I have now run it: every numeric citation in
the index (27, of which 25 exact and 2 off-by-one), every claim the README makes about the pinned
commit's own metadata (one, wrong), and every "this fragment is X's summary" claim against the
fragment's actual content (one pair, mismatched). There is no third instance. Round 3 closes this.

## What round 3 needs

Two one-sentence README edits (the author date, the two track-summary rows), one trailing newline,
two `+1` citation corrections, and - for an approve rather than a needs-changes - either four HTML
provenance comments on the composite fragments or an explicit user override recorded with their
rationale. This is round 2 of 3.

Full verdict: `.squad/design/presentation-demo/09-review-verdict.md` § *Re-review — round 2*.


### 2026-09-07 — reviewer-reconcile-20260907T130805Z-presentation-demo-round3 [reviewer-reconcile · NEEDS-CHANGES]

---
id: reviewer-reconcile-20260907T130805Z-presentation-demo-round3
agent: reviewer-reconcile
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-07T13:08:05Z
commit: 07607bf71152ef8d224352e2d2a737d8312dcb2d
targets:
  - path: Picea.Abies.Presentation/content/demo/README.md
    lines: "1-124"
  - path: Picea.Abies.Presentation/content/demo/SHA256SUMS
    lines: "1-39"
  - path: Picea.Abies.Presentation/content/demo/stops/5.1-warden-report.composite.md
    lines: "1-47"
  - path: Picea.Abies.Presentation/content/demo/timing/hooks-fired.log
    lines: "whole file"
blockers:
  - file: Picea.Abies.Presentation/content/demo/timing/hooks-fired.log
    line: 1
    reason: >-
      Matched by .gitignore:120 (*.log) and silently dropped by git add - the only ignored
      file of the 44 in the bundle, and one of only two authored files. git status
      --untracked-files=all lists 43 paths under content/ against 44 on disk. SHA256SUMS
      covers full/ and stops/ only, so timing/ has no manifest and nothing would detect the
      loss: the bundle would ship with a top-level README section indexing a file absent from
      the repository, with no error and no failing check. Not a regression of these fixes -
      round 1 checked for a demo-specific ignore rule and missed the repo-wide one, and round 2
      repeated the omission. Two one-step resolutions, and the choice is the user's - force-add
      the file at commit time (zero file changes; verify 44 staged paths under content/), or
      rename it to a non-ignored extension such as hooks-fired.md (one README row edit, robust
      for anyone who re-adds the tree later). Not a residual-ledger item; it needs a decision,
      not an entry.
  - file: .squad/design/presentation-demo/09-review-verdict.md
    line: 1
    reason: >-
      Round 3 of the Merge Criterion's two-round cap, recorded as a blocker so the cap is
      visible rather than inferred. Round count evidenced two ways and in agreement - archived
      drops 2026-09-07T12-03-39 and 2026-09-07T12-23-39 both carry commit 07607bf, and the
      round-2 section carries "Round: 2 of 3". No round 4 is requested. Stated properties are
      green - copies only, every stops/ file a verbatim excerpt, nothing under .squad/ moved,
      edited or deleted, all verified mechanically for the third time - so the cap does not
      escalate to the architect. The remaining step is the user picking a resolution for the
      hooks-fired.log blocker and then either overriding this verdict under Review Rule 6 or
      dispatching one targeted confirmation whose entire scope is: staged paths under
      Picea.Abies.Presentation/content/ = 44. If the bundle is committed, commit it and the
      .csproj only - committing this verdict file, the drops or the agent-memory notes in the
      same operation moves HEAD past the commit this verdict is pinned to and re-blocks the
      gate on a changeset that already passed.
high: []
medium:
  - file: Picea.Abies.Presentation/content/demo/stops/5.2-hook-line.sh
    reason: >-
      Carried unfixed from round 2 (nitpick 8). The README rationale says both out-of-span
      items are too far from :316-330 to fold in; that is true of :422 but not of :308-315,
      which is contiguous and directly above. Advisory, not worth a round.
good:
  - file: Picea.Abies.Presentation/content/demo/README.md
    reason: >-
      Both round-2 blockers closed and improved on. (e) The 5.6-review-headers row now
      attributes 2026-09-07T11:56:59+02:00 to 66379e7, the pre-squash branch commit
      08-review-blind.md was reading, and gives 07607bf's own author date as
      2026-09-07T13:00:00+02:00 - re-verified independently with git show -s --format=%aI on
      both commits, and against 08-review-blind.md:105. It also takes the suggested framing, so
      the squash rewriting the blind reviewer's clock reading becomes stop material rather than
      an erratum. (f) Both 5.2 summary rows now state each line's subject in bold, not only its
      voice, and separate the two explicitly; both descriptions re-verified against session log
      lines :422 and :424, both fragments still byte-exact. The two off-by-one citations are
      corrected to refutations.md:723 and 05-critic.md:428, both re-resolved, and the
      neighbouring citations re-resolved with them.
  - file: Picea.Abies.Presentation/content/demo/README.md
    reason: >-
      The composite-provenance override is recorded to the standard Review Rule 6 asks for - a
      new Overrides section attributed to the user and dated, quoting them verbatim with the
      reasoning intact, and spelling out the applied consequence. It is also a better resolution
      than the four HTML comments the finding asked for: those would have added authored bytes
      inside verbatim fragments and broken the SHA256SUMS manifest introduced one round earlier,
      a collision the override caught and the reviewer did not. Moving the seam marker into the
      filename via a .composite.md suffix satisfies the finding with zero bytes added to any
      fragment.
  - file: Picea.Abies.Presentation/content/demo/SHA256SUMS
    reason: >-
      Renames verified content-neutral rather than accepted. All four composites still
      byte-exact against their cited spans at 07607bf, with only the two disclosed mid-line cuts
      as deltas and no authored text anywhere. sha256sum -c gives 39/39 OK, regeneration is
      byte-identical, and the manifest's path set equals the on-disk set exactly. No stale
      pre-rename name survives in the bundle; the only repo-wide occurrences are in historical
      review records, correctly left as-written.
  - file: Picea.Abies.Presentation/content/demo
    reason: >-
      Third consecutive clean mechanical sweep. All 19 full/ copies byte-identical to their
      sources at 07607bf and all 20 stops/ fragments byte-exact against their cited spans,
      across two rounds of edits and a set of renames. The trailing-newline fix was swept rather
      than spot-fixed - 44 of 44 files newline-terminated, 0 CRLF. File count 44 to 44, four
      renames, nothing added or removed. .csproj unchanged from round 2 and dotnet build clean
      at 0 warnings and 0 errors. .squad/ still unedited: log diffs are 124/0 and 3/0 pure
      appends, and decisions.md is +265/-0 at a single end-of-file hunk.
references: []
---

Every round-2 finding is closed and verified: both blockers, all three mechanical fixes, and the composite-provenance finding by an explicit user override recorded with the user's rationale verbatim. Nothing else in the bundle changed, re-proved mechanically rather than inferred from mtimes.

## Round 3 of a two-round cap

The round count was checked before the findings were assembled, not after. Two archived reviewer-reconcile drops carry commit 07607bf and the round-2 verdict section carries "Round: 2 of 3"; both sources agree. No round 4 is requested. Stated properties are green, so the cap does not escalate to the architect - the shape here is the split, with the proportionality call handed to the user.

## The one open item

The bundle's tree is correct and complete; the open item is that `git add` will not stage all of it. `timing/hooks-fired.log` is matched by the repo-wide `*.log` rule at `.gitignore:120` - the only ignored file of 44, and one of only two authored files. It resolves by force-adding at commit time or by renaming to a non-ignored extension. It is a decision, not a fix round, and not a residual-ledger entry.

## Where to stop

Commit the bundle and the `.csproj` only. Committing this verdict file, the decision drops or the agent-memory notes in the same operation moves HEAD past the commit this verdict is pinned to and re-blocks the gate on a changeset that already passed.


### 2026-09-07 — reviewer-reconcile-20260907T131514Z-review-07607bf-confirm [reviewer-reconcile · PASS]

---
id: reviewer-reconcile-20260907T131514Z-review-07607bf-confirm
agent: reviewer-reconcile
verdict: PASS
scope: review
created: 2026-09-07T13:15:14Z
commit: 07607bf71152ef8d224352e2d2a737d8312dcb2d
targets:
  - path: .gitignore
    lines: "531-533"
  - path: Picea.Abies.Presentation/Picea.Abies.Presentation.csproj
    lines: "12-15"
  - path: Picea.Abies.Presentation/content/demo/timing/hooks-fired.log
blockers: []
high: []
medium:
  - file: .gitignore
    reason: >-
      The new negation is scoped by path but open by kind — any future *.log written anywhere
      under Picea.Abies.Presentation/content/demo/ is now tracked by default. Correct trade for
      a curated content folder under a manifest discipline; a note, not a request.
good:
  - file: .gitignore
    reason: >-
      The chosen resolution beat both options the round-3 finding named. A force-add would have
      left the trap armed for the next person to re-add the folder; a rename would have edited
      README.md:44 and diverged from the user's runbook. The scoped negation fixes the folder
      once, keeps the filename, and needs no doc change.
  - file: Picea.Abies.Presentation/content/demo/SHA256SUMS
    reason: >-
      sha256sum -c passes 39/39 against the staged bytes, which re-proves the bundle content is
      round 3's content independently of mtimes or of trusting the fix pass.
references: []
---

The round-3 open item is closed: all 46 staged paths verify, and PASS is pinned to the tree as staged.

## Scope

The Lead scoped this to round 3's single named check and nothing else. No re-verification of the
19 `full/` copies or 20 `stops/` fragments was performed; the round-3 section of
`.squad/design/presentation-demo/09-review-verdict.md` is the evidence for those.

## Verified

- `git diff --cached --name-only` = **46** paths: **44** under `content/demo/`, plus the `.csproj`
  and `.gitignore`. Sorted staged set vs files on disk under `content/demo/`: identical.
- `git check-ignore -v` on `timing/hooks-fired.log` exits 1 with no output — un-ignored. The same
  over all 46 staged paths via `--stdin` also exits 1: none ignored.
- `git ls-files --others` under `content/demo/` **including ignored files** is empty — nothing
  left behind by the staging command.
- Staged blob equals disk bytes for all 46 (`git rev-parse :<path>` vs `git hash-object`),
  0 mismatches. No CR bytes in any staged blob. `git diff` on the two staged prefixes is empty.
- `sha256sum -c SHA256SUMS` → 39/39 OK. The five unmanifested files all predate the round-3
  verdict's mtime, so the bundle is byte-for-byte the tree round 3 reviewed.
- The negation sits at end of file, after `.gitignore:120` (`*.log`), so the later rule wins. It is
  path-anchored; `*.log` still bites elsewhere (`.squad/log/dotnet-format-2026-09-07.log` remains
  ignored). `hooks-fired.log` is now the only tracked `*.log` in the repository.
- `README.md:44` still indexes the file under its original name — runbook and tree agree with no
  doc edit.
- The `.squad/` log appends, the `decisions.md` merger output, the agent-memory files and the three
  archived drops are all correctly left unstaged.

## Where to stop

Commit the 46 staged paths and nothing else. Committing this drop, the verdict file or the
agent-memory notes in the same operation moves HEAD past the commit this PASS is pinned to and
re-blocks the gate on a changeset that already passed. If they are to be committed, that is a
separate commit afterwards.


### 2026-09-07 — reviewer-reconcile-20260907T132100Z-review-b181a15-commit-boundary [reviewer-reconcile · PASS]

---
id: reviewer-reconcile-20260907T132100Z-review-b181a15-commit-boundary
agent: reviewer-reconcile
verdict: PASS
scope: review
created: 2026-09-07T13:21:00Z
commit: b181a154a21db627b14d1618b0202a0260812401
targets:
  - path: .gitignore
    lines: "531-533"
  - path: Picea.Abies.Presentation/Picea.Abies.Presentation.csproj
    lines: "12-15"
  - path: Picea.Abies.Presentation/content/demo/SHA256SUMS
    lines: "1-39"
blockers: []
high: []
medium:
  - file: .gitignore
    reason: >-
      Carried unchanged from the 07607bf confirmation. The negation is scoped by path but open by
      kind: any future *.log written anywhere under Picea.Abies.Presentation/content/demo/ is now
      tracked by default. Correct trade for a curated content folder under a manifest discipline;
      a note, not a request.
good:
  - file: Picea.Abies.Presentation/content/demo/SHA256SUMS
    reason: >-
      The manifest makes the commit boundary checkable without trusting mtimes or the staging step.
      sha256sum -c passes 39/39 against the committed tree, which is the same evidence that passed
      against the staged tree, so the commit demonstrably carried round 3's bytes.
  - file: .gitignore
    reason: >-
      The negation survives the commit intact at end of file, after *.log at line 120. Verified by
      probe rather than by reading: content/demo/**/*.log is un-ignored while content/other/x.log
      and some/where/random.log remain ignored by line 120, and hooks-fired.log is the only tracked
      *.log in the whole tree.
references: []
---

The PASS pinned to `07607bf71152ef8d224352e2d2a737d8312dcb2d` carries unchanged to
`b181a154a21db627b14d1618b0202a0260812401`. No dimensions were re-run; this is a boundary check
only, and the round-3 plus confirmation sections of
`.squad/design/presentation-demo/09-review-verdict.md` remain the evidence for the bundle's substance.

## What was checked

- **Parent.** `git rev-list --parents -n1 b181a15` gives the single parent
  `07607bf71152ef8d224352e2d2a737d8312dcb2d`. No merge, no intervening commit.
- **Boundary.** `git diff --name-status 07607bf..b181a15` is exactly 46 paths — 44 under
  `Picea.Abies.Presentation/content/demo/` added, plus `.gitignore` and
  `Picea.Abies.Presentation.csproj` modified. Nothing outside the confirmed set rode along.
- **Blob identity.** For each of the 46, `git rev-parse b181a15:<path>` equals
  `git hash-object <path>` — 46 compared, 0 mismatches. `git status` reports those paths clean, so
  the committed tree, the index and the working tree are the same bytes.
- **Independent content proof.** `sha256sum -c SHA256SUMS` at the committed tree: 39 OK,
  0 FAILED, exit 0. This does not depend on the index, on mtimes, or on trusting the commit step.
- **Nothing ignored, nothing left behind.** `git check-ignore -v --stdin` over all 46 committed
  paths exits 1 with no output. `git ls-tree -r b181a15 -- content/demo` lists 44 files, matching
  disk, and `git ls-files --others --ignored` under that prefix is empty.
- **The .gitignore fix survived.** The negation is at line 533, end of file, after `*.log` at line
  120. Probed both directions: `content/demo/timing/hooks-fired.log` exits 1 (un-ignored) while
  `Picea.Abies.Presentation/content/other/x.log` and `some/where/random.log` are still caught by
  line 120. `hooks-fired.log` is the only tracked `*.log` at HEAD.
- **Timing.** Newest mtime under `content/demo/` is `15:01:18`; `.gitignore` is `15:12:17`; the
  commit is authored and committed at `15:16:39`. Nothing was edited between confirmation and commit.
- **Working tree.** Only hook-written state (`.squad/log/2026-09-07-session.md`,
  `.squad/log/pass-cost.md`, `.claude/docs/decisions.md`, four archived drops under
  `.squad/decisions/archive/2026-09/`) and review artifacts
  (`.squad/design/presentation-demo/`, `.claude/agent-memory/reviewer-reconcile/`). No code-shaped
  path is dirty.

## Where to stop

**Push and merge at `b181a154a21db627b14d1618b0202a0260812401`.** This drop, the appended
confirmation line in `09-review-verdict.md`, the merger's `decisions.md` entry and the session log
are this pass's own artifacts; committing them moves HEAD past the sha this PASS is pinned to and
re-blocks the push and merge branches of `enforce-review-verdict.sh`, which compare against
`git rev-parse HEAD` unconditionally. Leave them uncommitted until after the merge, or fold them
into a separate housekeeping commit made afterwards.


### 2026-09-07 — reviewer-reconcile-20260907T134423Z-undo-redo-pr0-locked-spec [reviewer-reconcile · NEEDS-CHANGES]

---
id: reviewer-reconcile-20260907T134423Z-undo-redo-pr0-locked-spec
agent: reviewer-reconcile
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-07T13:44:23Z
commit: 3bd7af3d7e5cc79bb194912c21d45de692557e6c
targets:
  - path: Picea.Abies.Tests/History/UndoRedoSpec.cs
    lines: "1-852"
  - path: Picea.Abies.Tests/SpecAttribute.cs
    lines: "1-7"
  - path: Picea.Abies.Tests/Picea.Abies.Tests.csproj
    lines: "23-28"
blockers:
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 787
    reason: >-
      INV-6's Available case does not compile on TUnit 1.19.57 -- verified by
      independent build: CS1061, OrContinuation<int> has no member That. TUnit's
      .Or continues on the same subject, so a disjunction across two values is
      not expressible this way at all. Origin is 06-spec.md:1186, the approved
      text, so the route is a spec-author amendment with user re-approval, not a
      csharp-dev fix.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 7
    reason: >-
      The nine-line header this changeset adds states that the file's
      non-compilation is "expected: the types it exercises ... do not exist
      yet". False -- line 787 will still not compile after step 6 delivers every
      named type and fixture. The header is NOT approved spec text (it appears
      zero times in 06-spec.md), so this sentence is the changeset's own and
      blocks on its own account, independently of what happens to line 787.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 197
    reason: >-
      A6's two IsEquivalentTo expectations are permutations of the same four
      strings, and IsEquivalentTo is order-insensitive -- verified by execution:
      the bare expectation passes against the wrapped actual. A6's entire stated
      purpose is to record an ORDERING difference between the wrapped and
      unwrapped programs, so it cannot fail for the reason it exists.
      CollectionOrdering does not exist on 1.19.57 (CS0103), so the remedy is a
      different assertion, not an option flag. Origin is 06-spec.md:505 and :509.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 800
    reason: >-
      INV-6's closing comment claims "the property asserts that coverage at the
      end of the loop rather than assuming the generator found them". It does
      not -- the body ends at the seed loop's closing brace. This coverage
      assertion IS the named falsifier that 06-spec.md's The Lock relies on to
      close the unlocked-support-file hole, so the Lock's own stated closure is
      unbuilt for this property. With the unguarded continue guards at :490,
      :492, :642, :649, :699 and :732, INV-1, INV-3, INV-4, INV-5 and INV-6 can
      all pass having asserted nothing.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 528
    reason: >-
      "INV-2 runs twice, over Editor (whole-model) and ProjectedEditor" is false
      against the code -- both INV-2 properties start Editor (:517, :560), and
      the second one's own comment at :555 says "Whole-model policy
      DELIBERATELY". ProjectedEditor appears once in the file, at :315, in A8.
      Verbatim from 06-spec.md:908. A third claim at :616-620 says the property
      "is not done until both branches have been observed taken at least once";
      neither the Past nor the Future branch is counted.
high:
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    reason: >-
      The lock imposes at least six obligations on the unlocked support files
      and the narrative's stated closure covers one. (a) a global using static
      for ~13 unqualified fixture calls, since the class is sealed, non-partial
      and has no base type; (b) EditorLog must be per-test-isolated; (c)
      EditorLog.Timeline must return a snapshot, not the backing list (:190-195
      captures, clears, captures); (d) Start<Editor>() must reset the static log
      (A1 asserts emptiness at :52 with no Clear()); (e) Gen.Corpus must reach
      all five availability cases in both directions, because INV-6's coverage
      assertion is missing; (f) EditorProgram.Transition(MovementRefused) must
      issue no command -- 04-realist-plan.md:1120's S7 transparency rule means a
      refusal records no step, but pass() at :1128-1131 still calls sealTops
      when the command is not silent, and INV-5 snapshots Both() once at :730
      while INV-6 re-reads inside its loop at :766.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    reason: >-
      No [NotInParallel] on a class of 25 tests built on a static EditorLog with
      twelve Clear() calls and emptiness assertions at :52, :149-151, :247-248,
      :708-710 and :773-775. Measured: no assembly-level parallel config and no
      .runsettings in this project; TUnit defaults to parallel; six sibling
      classes carry class-level [NotInParallel] and NavigationTests.cs uses the
      method-level form four times. The attribute site is inside the lock, so
      the escape is an AsyncLocal-backed EditorLog -- a step-6 choice to make
      deliberately rather than discover when the suite goes flaky.
  - file: .claude/hooks/dotnet-format-on-save.sh
    reason: >-
      PRE-EXISTING FRAMEWORK GAP, register for devops. The hook fires on every
      Write/Edit of any .cs file with no exclusion for a file the process
      declares immutable, and runs the analyzer-fix pass including IDE0005
      (.editorconfig:268, severity warning) -- which is how "using
      Picea.Abies.History;" was stripped on first write. The line is present now
      (:13). Every whitespace divergence between the committed file and
      06-spec.md is explained by .editorconfig:80,
      csharp_preserve_single_line_statements = false. Currently neutralised by
      the Compile Remove -- verified: dotnet format --include reports "Formatted
      0 of 27 files" and leaves the file byte-identical -- and that protection
      disappears when PR 3 removes the exclusion.
  - file: .claude/hooks/enforce-review-blindness.sh
    reason: >-
      PRE-EXISTING FRAMEWORK GAP, register for devops. Picea.Abies.Presentation/
      content/demo/full/ carries a byte-identical copy of the whole design pass
      (md5 verified for 00-scope, 03-convergence, 04-realist-plan, 05-critic,
      06-spec, 07-handoff) plus decision-drops/ including two previous review
      verdicts, outside the hook's .squad/design/** deny list. Verified by
      firing the hook with a reviewer-blind payload -- exit 0 for the demo
      copies of 06-spec.md, 05-critic.md and the review-pr359-round2 drop, exit
      2 for .squad/design/undo-redo/06-spec.md. This is a second PATH reachable
      by the MEDIATED tools, so it is distinct from R-19 (Bash unmediated) and
      R-20 (worktrees, symlinks); closing R-19 entirely would leave it standing.
      Committed at 7d32cdb (PR #360, merged) -- not this changeset's fault.
      Unregistered: grep for "Presentation" and "content/demo" in
      .claude/enforcement/refutations.md returns nothing.
medium:
  - file: Picea.Abies.Tests/Picea.Abies.Tests.csproj
    reason: >-
      History/UndoRedoSpec.cs is in no MSBuild item group -- measured, Compile
      has 20 items without it and None has zero items in this project. The SDK's
      default None glob subtracts the Compile glob PATTERN, not the resulting
      item list, so a Compile Remove of a .cs file lands it nowhere and IDEs
      hide it without Show All Files, for the two PRs during which its whole
      purpose is to be looked at.
  - file: Picea.Abies.Presentation/content/demo/stops/5.5-property.cs
    reason: >-
      INV-3 now exists in three copies -- 06-spec.md, this checksummed
      presentation stop (SHA256SUMS line 36), and the locked file -- and they
      already disagree: the presentation copy carries the pre-format spelling.
      A correction to INV-3 has three homes and a checksum to refresh.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    reason: >-
      PR-body precondition unmet because no PR exists yet -- the plan's PR 0 row
      and 07-handoff.md section 6 both require the body to state the deliberate
      exclusion. Separately, .squad/log/2026-09-07-session.md and
      .squad/log/pass-cost.md are tracked, not ignored (git check-ignore exits
      1) and modified, so staging everything at once would breach the stated
      "three files and nothing else" criterion.
good:
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    reason: >-
      The PR-0 shape delivers the property it was designed for -- git log --all
      on the path returns nothing, so the Lock's git-history check is satisfied
      without a waiver, which is exactly the argument the plan and handoff make
      for this shape over landing the spec as PR 3's first commit.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    reason: >-
      Every Critic mitigation owed to this file landed. S27's alphabet partition
      matches 06-spec.md's table property-for-property across all eight
      properties (:475, :511, :559, :637, :692, :725, :759, :814); S28's
      WhenApplied<T>().WaitAsync is used throughout with no sleeps anywhere,
      continuing ca2519d; S25(a) is A7's third test; S26 is A3's preserved
      forward branch; B10's scoping is stated twice; and 7-yellow is A10.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    reason: >-
      A9, A10 and A7's third test each name their falsifier in prose AND assert
      it -- A_terminated_program_with_no_history_left_is_terminal (:385-399)
      exists specifically to stop IsTerminal being hardcoded false. That is the
      discipline the INV-2 and INV-6 blockers find missing.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    reason: >-
      The stripped "using Picea.Abies.History;" was caught and restored rather
      than left to pass silently. That is the behaviour that makes the
      format-on-save gap registrable instead of a post-mortem.
references: []
---

PR 0's shape is correct and its two compile-verified defects are in the approved spec text, not the transcription -- so the fix needs a spec-author amendment and user re-approval, and this commit is the last moment at which that is cheap.

## What was settled

I extracted every csharp fence from `.squad/design/undo-redo/06-spec.md` and diffed the reassembly
against the committed file. `.Or.That(...)` is `06-spec.md:1186`; A6's two `IsEquivalentTo` calls are
`:505` and `:509`. Verbatim. The transcription is faithful; the approved text is not correct.

`07-handoff.md` section 6.1, item 4-yellow states the consequence: *"After the approval commit a move
is a `// SPEC CONFLICT:` hand-back and a re-approval, not a relocation."* PR 0 **is** the approval
commit.

## The route is the user's

Two options, and I am not choosing between them: amend and re-approve before committing, or commit
as-is and accept that PR 3 opens with a hand-back on the pass's most load-bearing file. The header
sentence at `:7-9` is the exception -- it is not approved text and is `csharp-dev`'s to correct
either way.

## Where the blind reading and the narrative diverged

08 upheld on the two compile findings and on the missing falsifiers, and extended -- I found a third
false coverage claim it did not (`:528`, "INV-2 runs twice ... and ProjectedEditor"). 08's P7 fear is
refuted by `04-realist-plan.md:1120`'s S7 transparency rule and replaced by a narrower unstated
fixture obligation. 08's P4 reading of the `Picea.Abies.Presentation` precedent is half-wrong -- that
project has 45 `None` items and its comment is true for the `.md` files it exists to keep
presentable. 08's P10 slug complaint is refuted: `undo-redo` is the design pass, `undo-redo-pr0` is
this review's slug. 08's test count is off by four in the acceptance layer -- 25 total, not 21.

Full verdict: `.squad/design/undo-redo-pr0/09-review-verdict.md`.


### 2026-09-07 — reviewer-reconcile-20260907T144534Z-undo-redo-pr0-round2 [reviewer-reconcile · NEEDS-CHANGES]

---
id: reviewer-reconcile-20260907T144534Z-undo-redo-pr0-round2
agent: reviewer-reconcile
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-07T14:45:34Z
commit: faafc8b179f3069473a4116d9232d1dce9eab8d7
targets:
  - path: Picea.Abies.Tests/History/UndoRedoSpec.cs
    lines: "1-996"
  - path: Picea.Abies.Tests/SpecAttribute.cs
    lines: "1-7"
  - path: Picea.Abies.Tests/Picea.Abies.Tests.csproj
    lines: "23-29"
  - path: .squad/design/undo-redo/06-spec.md
    lines: "40-56, 236-249, 314-319, 1696-1730, 1832-1960"
blockers:
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 243
    reason: >-
      Assert.That(<collection>).IsEqualTo([...]) compiles on the pinned TUnit
      1.19.57 but can never pass. The collection expression is target-typed to
      a compiler-generated <>z__ReadOnlyArray<string> and compared by Equals,
      which is reference equality, so the assertion fails on the MATCHING
      sequence as well as on a permutation. Verified with both controls in a
      1.19.57 scratch project, and swept across every plausible support-file
      return type for EditorLog.Timeline (string[], List<string>,
      IEnumerable<string>, IReadOnlyList<string>, ImmutableArray<string>,
      ImmutableList<string>) - all six fail on identical content. Also verified
      against a bespoke [CollectionBuilder] type with correct IEquatable<T>
      value equality, which fails while the subject's own Equals returns True,
      proving no support-file choice closes it. Amendment 4 replaced an
      assertion that passed for the wrong reason (order-insensitive
      IsEquivalentTo) with one that fails for no reason, so step 6's
      obligation - observe every test red for the right reason, then make it
      green - can never be discharged for A6. This is a regression the
      changeset introduces, not an inherited gap, so it is not registrable.
      Two shapes that do work on 1.19.57 were executed with passing and
      failing controls: IsEquivalentTo(expected, CollectionOrdering.Matching)
      (using TUnit.Assertions.Enums) and
      Assert.That(actual.SequenceEqual([...])).IsTrue(). Which one lands is
      the spec-author's and the approver's call.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 247
    reason: >-
      Second instance of the same defect, A6's bare-program expectation. Same
      evidence, same route. Named separately because a fix applied only at the
      wrapped assertion would leave the test still unable to pass.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 318
    reason: >-
      Third instance, A7's subscription-activity ordering claim
      (EditorLog.SubscriptionActivity IsEqualTo ["start:...", "stop:..."]).
      Same evidence, same route.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 239
    reason: >-
      The locked comment states two false claims about TUnit 1.19.57 and cites
      the round-1 review as their authority: that IsEqualTo on a collection is
      order-sensitive on this version, and that
      IsEquivalentTo(expected, CollectionOrdering.Matching) does not exist
      here. Measured now - IsEqualTo on a collection is not order-sensitive,
      it is unsatisfiable, and CollectionOrdering DOES exist on 1.19.57 in
      namespace TUnit.Assertions.Enums (round 1's CS0103 was a missing using
      directive misread as an absent type; strings on TUnit.Assertions.dll
      lists CollectionOrdering, IsEquivalentToAssertion`2 and
      CollectionIsInOrderAssertionExtensions). Decision 1 makes the file
      immutable in its assertions AND ITS CLAIMS from the approval commit, so
      correcting this paragraph after the commit is itself a hand-back. This
      is not a duplicate of the assertion blockers: the paragraph would still
      be present and still wrong if the three assertions were fixed alone.
      :314-317 carries the same claim in shorter form. Both round-1 errors
      were mine - the amendment cites my executed probe as its reason for
      choosing this shape - and both are recorded in the verdict's opening
      section.
high:
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 54
    reason: >-
      [Timeout(30_000)] emits warning TUnit0015 (Missing TimeoutAttribute
      cancellation token parameter) on all 25 parameterless test methods,
      verified by probe on 1.19.57. Not fatal - no TreatWarningsAsErrors in
      Directory.Build.props, the test csproj or any workflow - so step 6 gains
      25 warnings where round 1 recorded a clean build. Also measured: the
      timeout DOES fire on an await-shaped body without a CancellationToken
      parameter (a [Timeout(2_000)] test awaiting Task.Delay(6_000) failed at
      2 s), so Decision 3 buys what it was meant to buy for these await-dense
      seed loops; it does NOT fire on a synchronous spin (a 6 s spin passed);
      and the timed-out body keeps running after the failure is reported,
      which on a class built on the process-wide static EditorLog will corrupt
      whichever test runs next - [NotInParallel] does not prevent this because
      the orphan is a detached continuation, not a test. The remedy for the
      warning is a signature change, which is neither an assertion nor a
      claim, so on Decision 1's wording step 6 could make it without a
      hand-back; that reading is not obvious enough to leave implicit.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 986
    reason: >-
      A seventh support-file obligation, absent from amendment 4's table of
      six. INV-7's assertion (4),
      Assert.That(EditorLog.ReportedKeySetsDuringHold.Distinct())
      .IsEquivalentTo([anchorKeys]), compares ELEMENTS with Equals, so a
      sequence-of-sets fails whenever the elements are reference-equality
      types (HashSet<string> and string[] elements both fail on identical
      content) and passes when they have value equality (a record element -
      matching passes, non-matching fails, both controls run). Round 1 cleared
      this site as order-insensitive by intent, which was right about ordering
      and silent about element equality. This is an obligation on the UNLOCKED
      support files rather than a lock defect - Keys(...) and
      ReportedKeySetsDuringHold must yield a value-equality element type - so
      it belongs in 06-spec.md section The Lock as item (g), beside (c) and
      (d). SingleReconciliation(...) at :971 is safe if it returns a flat
      IEnumerable<string> and has the same problem if it returns a set of sets.
  - file: Picea.Abies.Tests/Picea.Abies.Tests.csproj
    line: 23
    reason: >-
      Round 1's warning-7, carried, unfixed and unregistered - the only
      round-1 finding in that state. Re-measured on this working tree rather
      than carried on trust - getItem:None returns {"None": []} and
      getItem:Compile returns 20 items, with History/UndoRedoSpec.cs in
      neither. The SDK's default None glob subtracts the Compile glob PATTERN,
      not the resulting item list, so a Compile Remove of a .cs file lands it
      in no item group and neither Visual Studio nor Rider shows it without
      Show All Files, for the two PRs during which being looked at is its
      entire purpose. 06-spec.md section Amendment 4 assigns it to csharp-dev.
      Merge criterion (b) is unmet on this item alone.
  - file: .squad/log/2026-09-07-session.md
    line: 1
    reason: >-
      Round 1's warning-12, still open. git status shows this tracked file
      modified alongside the three intended paths, and the PR's stated
      criterion is three files and nothing else, so the committer must stage
      explicitly rather than use git commit with the all flag. Process
      caution, not a code defect. Related, also still open - no PR exists for
      branch test/0-undo-redo-spec (gh pr list --head returns empty), so
      round 1's warning-11 precondition on the PR body stating that the spec
      is deliberately excluded until step 6 remains unmet by absence.
medium: []
good:
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 591
    reason: >-
      Round-1 blocker 3(b) fixed by making the code true to the claim rather
      than the claim true to the code. INV-2 now walks the script under both
      lenses against one reachable set computed once, with distinct Because
      text per lens, and the falsifier table gains a projection-specific
      mutation. The whole-model lens is the half that holds by construction,
      so adding the projection lens added the half that can actually fail.
      Decision 2 kept INV-6's feedback interpreter on exactly the same
      reasoning. Both were open invitations to take the cheaper route.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 931
    reason: >-
      Round-1 blocker 3(a) fixed, and implementing it immediately exposed that
      BlockedByWorld was unreachable in either direction under NoFeedback, so
      the ten-combination claim could never have been met. That is the case
      for named falsifiers being code rather than prose, made by the artifact
      itself. Verified: the coverage assertion compiles and passes on 1.19.57,
      the observed.Add call at :885 is unguarded, and GetType().Name matches
      nameof for nested record cases.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 531
    reason: >-
      All four reached floors (INV-1, INV-3, INV-4, INV-5) are placed after
      the last continue guard and before the first assertion. Getting this
      wrong is easy and silent, and would have made the floor decorative.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 1
    reason: >-
      The header is fixed rather than hedged - round 1's finding was a false
      sentence, and the response was to rewrite it as csharp-dev's own
      accurate text instead of appending a disclaimer. Every claim in it
      checks out, including the CS0234 behaviour, which I verified by
      temporarily deleting the Compile Remove and restoring the csproj
      byte-identical afterwards.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 344
    reason: >-
      The transcription is verifiable by construction. Re-extracting
      06-spec.md's six csharp fences (344-829, 917-1014, 1043-1133, 1174-1223,
      1234-1429, 1443-1489), reassembling and diffing against the working-tree
      file yields 87 diff lines containing no assertion, no claim and no
      whitespace reflow - the 18-line header, a 15-line bridging comment, five
      blank lines and the class's closing brace moved to the end of the file.
      The Gen stub is again correctly omitted. Zero formatter damage this
      round, because the exclusion was in the csproj before the file was
      written.
  - file: .claude/enforcement/refutations.md
    line: 777
    reason: >-
      R-27 and R-28 are real registrations - each carries an owner, an
      expires date of 2026-09-21, and a level consequence written from
      executed evidence, and R-27 argues explicitly why it is neither R-19
      (unmediated Bash) nor R-20 (worktree/symlink deny-closure), which is the
      part that usually gets skipped.
references:
  - .squad/design/undo-redo-pr0/09-review-verdict.md
  - .squad/design/undo-redo-pr0/08-review-blind.md
  - .squad/design/undo-redo/06-spec.md
  - .squad/design/undo-redo/05-critic.md
  - .squad/design/undo-redo/07-handoff.md
  - .claude/docs/principles-enforcement.md
  - .claude/enforcement/refutations.md
---

# Review verdict - undo/redo locked spec (PR 0), round 2

Working tree on `test/0-undo-redo-spec`, uncommitted, over HEAD `faafc8b`. Round 2 of 2
before the cap; a third round splits the changeset under
`principles-enforcement.md` section *The Merge Criterion*.

## What was settled

Amendment 4 fixed six of the seven round-1 findings that were the spec's to fix, and fixed
the two that mattered most in the harder direction - adding the projection lens to INV-2
rather than deleting the sentence that claimed it, and keeping INV-6's feedback interpreter
rather than narrowing the coverage claim to what the old fixture could reach. The
transcription is byte-faithful to the six approved fences and the build claims check out.

## What blocks

The remedy amendment 4 adopted for round-1 blocker 2 does not work.
`Assert.That(<collection>).IsEqualTo([...])` compiles on TUnit 1.19.57 and fails on the
matching sequence, across every plausible subject type and even against a bespoke
value-equality collection type. Three assertions in the locked text - A6 twice, A7 once -
went from passing for the wrong reason to failing for no reason, which is strictly worse
in this process because step 6 cannot make them green. The locked comment that explains
the change states two false claims about TUnit and cites the round-1 review as their
authority.

## The error was mine, and it is recorded

Round 1 told the spec-author that `IsEqualTo` on a collection is order-sensitive (I ran only
the negative control - a permutation failing is equally consistent with "always fails") and
that `CollectionOrdering` does not exist on this version (I read a `CS0103` missing-using
error as an absent type). The amendment states that it chose this shape because the reviewer
had already executed it. Every claim in this round carries a passing control and a failing
control.

## The route

Same as round 1: a `spec-author` amendment with user re-approval, because the defects are in
approved text and `csharp-dev` may not edit it. PR 0 is still the approval commit, so this
is still the cheap moment - and it is the last one, because round 3 hits the cap and the
split never ships a red stated property.


### 2026-09-08 — reviewer-reconcile-20260908T045002Z-undo-redo-pr0-round3 [reviewer-reconcile · NEEDS-CHANGES]

---
id: reviewer-reconcile-20260908T045002Z-undo-redo-pr0-round3
agent: reviewer-reconcile
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-08T04:50:02Z
commit: 3baa6015d3be3a4461ece23f87150100d63647cb
targets:
  - path: Picea.Abies.Tests/History/UndoRedoSpec.cs
    lines: "1-1068"
  - path: Picea.Abies.Tests/SpecAttribute.cs
    lines: "1-7"
  - path: Picea.Abies.Tests/Picea.Abies.Tests.csproj
    lines: "23-32"
blockers:
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 2
    reason: >-
      Round 3 is past the cap and the split is degenerate (PR 0 is three files
      the plan requires to land together), so this is an escalation, not a
      rejection. The header states amendment 5 was re-approved on 2026-09-07;
      06-spec.md:2137 records "answered 2026-09-08" and :66-67 agrees. A false
      claim introduced by this changeset into a file the process declares
      immutable, so it is not registrable - but unlike rounds 1 and 2 it costs
      one line either side of the approval commit, by csharp-dev, with no
      spec-author amendment and no re-approval, because the Lock's consequence 3
      assigns csharp-dev's transcription commentary to csharp-dev. The user's
      call is route 1 (correct the date in the working tree, then commit;
      recommended) or route 2 (commit as-is and correct after). What must not
      happen is a third spec-author amendment - no approved fence, assertion or
      fence claim is touched by this round.
high:
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 1659
    reason: >-
      Step 6's Done-when names one csproj item to remove; the tree now carries
      two after this changeset fixed round-2's warning-7 with
      <None Include="History\UndoRedoSpec.cs" />. Left as-is, step 6 removes the
      Compile Remove per its checklist and the None Include survives as an inert
      stale item (no NETSDK1022 - the default None glob excludes the file once
      the Compile glob claims it). Owner realist; inherited-shaped and the clean
      registration for the next changeset.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 1
    reason: >-
      Carried and unchanged since round 1, both satisfied at PR-open and commit
      time rather than registrable. No PR exists for test/0-undo-redo-spec
      (gh pr list --head returns empty) and 04-realist-plan.md:1909 requires the
      body to state the deliberate compile exclusion and the git-history check.
      Working tree also carries a modified .squad/log/2026-09-08-session.md
      alongside the three intended paths, so stage explicitly rather than
      git commit -a.
medium: []
good:
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 301
    reason: >-
      Round-2 blocker 5 closed by execution. The three assertions at :301, :306
      and :381 now use IsEquivalentTo(expected, CollectionOrdering.Matching)
      with using TUnit.Assertions.Enums; verified on the pinned TUnit 1.19.57
      with 17 executed cases and both controls - matching order passes on
      string[], List, IEnumerable, IReadOnlyList, ImmutableArray, ImmutableList
      and ICollection subjects; a permutation fails on all of them; A7's exact
      two-element shape passes forward and fails reversed; length mismatches
      fail. The sweep is wider than the amendment's stated three subject types,
      so no support-file choice for EditorLog.Timeline breaks it and no new Lock
      obligation is created. INV-7's deliberately bare IsEquivalentTo at :1043
      and :1059 is untouched and still order-insensitive - the change did not
      over-apply.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 275
    reason: >-
      Round-2 blocker 6 closed. The paragraph that recorded two falsehoods as
      fact is replaced by measurements beside the assertion, and every claim in
      it was re-executed rather than read - bare IsEquivalentTo order-insensitive
      but content-sensitive; IsEqualTo unsatisfiable on all six named subject
      types on matching content; the bespoke CollectionBuilder probe reproducing
      equals-new=True on the line before the failure; A7's stop-then-start
      passing under bare IsEquivalentTo. All true. The evidence now sits where
      step 6 reads it instead of in a review nobody re-opens.
  - file: Picea.Abies.Tests/Picea.Abies.Tests.csproj
    line: 30
    reason: >-
      Round-2 warning-7 fixed rather than registered, and in the codebase's own
      idiom. -getItem:None now returns one item, History\UndoRedoSpec.cs;
      -getItem:Compile still returns 20 without it. <None Include= matches five
      sibling csprojs, the comment explains the lifecycle, and [R4-spec-commit]
      is a real Realist tag (04-realist-plan.md:1508, :1531, :1909) rather than
      an unresolved placeholder. Adding a None item does not pull the file into
      dotnet format's Roslyn workspace, so R-28's registered analysis is
      unchanged.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 564
    reason: >-
      The transcription is verifiable by construction. Re-extracting the six
      approved csharp fences from 06-spec.md (361-897, 987-1082, 1113-1201,
      1244-1291, 1304-1497, 1522-1566 - re-derived, since amendment 5 moved every
      line number) and diffing gives 74 lines containing only csharp-dev's
      30-line header, the 24-line invariant-layer bridge and the relocated
      closing brace. No assertion, no fence claim, no whitespace reflow. The Gen
      stub is again correctly omitted. Build green with the exclusion (0
      warnings) and CS0234-only without it, with nothing at the new
      TUnit.Assertions.Enums using - which the GlobalUsings.g.cs inspection shows
      is required and therefore not at IDE0005 risk. 224/224 existing tests pass;
      git log --all over Picea.Abies.Tests/History/ is still empty, so the Lock's
      git-history check is never waived.
references:
  - .squad/design/undo-redo-pr0/09-review-verdict.md
  - .squad/design/undo-redo-pr0/08-review-blind.md
  - .squad/design/undo-redo/06-spec.md
  - .claude/docs/principles-enforcement.md
  - .claude/enforcement/refutations.md
---

# Review — undo/redo locked spec (PR 0), round 3

Round 3 is past the two-round cap. Two archived drops evidence the count:
`2026-09-07T13-46-42-review-undo-redo-pr0.md` (`commit: 3bd7af3d…`) and
`2026-09-07T14-49-50-review-undo-redo-pr0-round2.md` (`commit: faafc8b1…`),
cross-checked against the `**Round:** n` lines in `09-review-verdict.md`.

Criterion (a) is green — every stated property of the three files as § *The Lock*
defines them was measured this round, not carried. Criterion (c) is green.
Criterion (b) has one item the changeset introduced and therefore cannot register:
a false re-approval date in `csharp-dev`'s own file header.

The cap's remedy is *split, what passes ships*, and the split is degenerate here:
PR 0 is three files the plan requires to land together, and one sentence cannot be
carved out of a file. But the property that made rounds 1 and 2 cap-forcing is
absent — this correction costs one line before or after the approval commit, by
`csharp-dev`, with no `spec-author` amendment and no user re-approval, because
amendment 5 added Lock consequence 3 assigning transcription commentary to
`csharp-dev`. So the cap escalates as a question, not as a rejection.

Full findings, evidence and the two routes: `.squad/design/undo-redo-pr0/09-review-verdict.md`
§ *Re-review — round 3, the split round*.


### 2026-09-08 — reviewer-reconcile-20260908T050117Z-undo-redo-pr0-confirmation [reviewer-reconcile · PASS]

---
id: reviewer-reconcile-20260908T050117Z-undo-redo-pr0-confirmation
agent: reviewer-reconcile
verdict: PASS
scope: review
created: 2026-09-08T05:01:17Z
commit: 3baa6015d3be3a4461ece23f87150100d63647cb
targets:
  - path: Picea.Abies.Tests/History/UndoRedoSpec.cs
    lines: "1-3"
  - path: Picea.Abies.Tests/SpecAttribute.cs
    lines: "1-7"
  - path: Picea.Abies.Tests/Picea.Abies.Tests.csproj
    lines: "23-32"
blockers: []
high: []
medium: []
good:
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 1
    reason: >-
      Route 1 taken and the round-3 open item is closed. The header now records
      the original approval on 2026-09-07, amendment 4's re-approval on
      2026-09-07 and amendment 5's on 2026-09-08, matching 06-spec.md at
      :1831, :2023 and :2137 and the artifact summary at :66-67.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 31
    reason: >-
      The six approved fences are unaltered, proven against the spec rather
      than against prior review notes. Reassembling 06-spec.md's fences at
      361-897, 987-1082, 1113-1201, 1244-1291, 1304-1497 and 1522-1566 and
      diffing gives one removed line, the class's closing brace moved to the
      end of the file, and 60 added lines of which every one is a comment, a
      blank line or that brace. No added line is code, so no assertion and no
      claim inside a fence can have moved.
  - file: Picea.Abies.Tests/Picea.Abies.Tests.csproj
    line: 30
    reason: >-
      Unchanged since round 3 and still doing its job. Mtimes for the csproj
      and SpecAttribute.cs precede this reviewer's round-3 drop, and
      re-measured MSBuild items still place the spec under None and not under
      Compile. Build of the test project succeeds with zero warnings.
references:
  - .squad/design/undo-redo-pr0/09-review-verdict.md
  - .squad/design/undo-redo/06-spec.md
  - .squad/decisions/archive/2026-09/2026-09-08T04-53-06-review-undo-redo-pr0-round3.md
---

# Confirmation — PR 0 undo/redo spec, route 1

Targeted confirmation after round 3's escalation, not a fourth round. The single
open item was the wrong amendment-5 re-approval date in `csharp-dev`'s own
transcription commentary; route 1 corrected it in the working tree before the
approval commit.

Four scope claims verified and nothing else: the header date now agrees with the
spec; the six approved fences are byte-faithful; `SpecAttribute.cs` and the
csproj are untouched since round 3; the change set is still exactly three files
under `Picea.Abies.Tests/`. The build was re-run rather than accepted.

Also recorded in the verdict: round 3's own prose reported the fence diff as 74
lines and the bridge note at :564-587, where the correct figures are 69 and
:568-591. The artifact did not change — the line arithmetic (1,068 file lines
against 1,009 fence lines) forces 60 added and 1 removed in both rounds — but
the note about it was carried rather than re-derived, which is the same
stale-number error class the closed finding named.

Carried and not this changeset's to close: the PR-body precondition and the
`.squad/log/` staging discipline, both satisfied at PR-open, and the plan
step-6 item registered against `realist`.


### 2026-09-08 — reviewer-reconcile-20260908T050430Z-undo-redo-pr0-commit-boundary [reviewer-reconcile · PASS]

---
id: reviewer-reconcile-20260908T050430Z-undo-redo-pr0-commit-boundary
agent: reviewer-reconcile
verdict: PASS
scope: review
created: 2026-09-08T05:04:30Z
commit: b39ac585036b344428a2202b522138ff29a3c8a9
targets:
  - path: Picea.Abies.Tests/History/UndoRedoSpec.cs
    lines: "1-1068"
  - path: Picea.Abies.Tests/SpecAttribute.cs
    lines: "1-7"
  - path: Picea.Abies.Tests/Picea.Abies.Tests.csproj
    lines: "23-32"
blockers: []
high: []
medium: []
good:
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 1
    reason: >-
      The commit boundary is exactly the tree this reviewer passed. b39ac58 has
      the single parent 3baa601, and git diff --name-status 3baa601..b39ac58 is
      exactly three paths - A History/UndoRedoSpec.cs, M
      Picea.Abies.Tests.csproj, A SpecAttribute.cs, +1085/-0 - with no fourth
      file and no deletion.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 2
    reason: >-
      The committed blobs equal the working-tree files passed at 3baa601, so
      nothing was amended between confirmation and commit. git rev-parse
      b39ac58:<path> matches git hash-object <path> for all three -
      2d50430539174d070c40623ab5e1e0faed6e5ec8,
      42c878375e73623d4f8349d2b3def340305ba22a and
      7d6502fd0077104098707e0feb3d866faf7d64cb. In particular the header's
      amendment-5 re-approval date, route 1's correction, is inside the
      committed blob.
  - file: Picea.Abies.Tests/History/UndoRedoSpec.cs
    line: 3
    reason: >-
      The Lock's git-history baseline now exists and is unambiguous. git log --
      Picea.Abies.Tests/History/ shows exactly one commit, b39ac58
      "test(history) Lock the undo-redo executable specification", so any later
      touch of the spec is a second commit on that path and reads as a spec
      edit rather than as part of the approval commit - which is the property
      the Lock's git-history check depends on.
  - file: Picea.Abies.Tests/Picea.Abies.Tests.csproj
    line: 30
    reason: >-
      The working tree left behind holds only hook-written state and review
      artifacts, so nothing code-shaped was omitted from the commit.
      decisions.md and decisions/archive/2026-09/ are merger-written, the
      session log and pass-cost.md are session-logger-written, and
      09-review-verdict.md plus agent-memory/reviewer-reconcile/ are this
      reviewer's own. A git status --ignored sweep of Picea.Abies.Tests/ finds
      nothing outside bin/obj but TestResults/, so no spec content was left
      unstaged by an ignore rule.
references:
  - .squad/design/undo-redo-pr0/09-review-verdict.md
  - .squad/decisions/archive/2026-09/2026-09-08T05-01-58-review-undo-redo-pr0-confirmation.md
  - .squad/decisions/archive/2026-09/2026-09-08T04-53-06-review-undo-redo-pr0-round3.md
---

# Commit-boundary confirmation — PR 0 undo/redo spec

Not a review round. The targeted confirmation at 05:01Z passed the three-file
working tree at HEAD `3baa601` and said where to stop; this re-issues that PASS
against the commit that tree became, so the merge gate has a cached verdict for
the HEAD it will actually see.

Four claims verified and nothing else: the diff is exactly the three paths
against the stated parent; the committed blobs are byte-identical to the files
confirmed; the residual working tree is hook-written state and review artifacts
only; and `Picea.Abies.Tests/History/` now has exactly one commit in its history.
No dimension was re-run and no finding was re-derived.

Where to stop. The cache records PASS for `b39ac58`, so `b39ac58` is the head the
merge gate accepts. The artifacts still dirty in this tree — this verdict file,
the merged `decisions.md`, `.squad/log/`, this reviewer's memory — are not PR 0;
committing them onto this branch moves HEAD past `b39ac58` and re-blocks the
merge with no review left to run.

Carried and not this changeset's to close: ⚠️-11 (PR body precondition) and
⚠️-12 (`.squad/log/` staging discipline), both satisfied at PR-open, and ⚠️-14,
registered against `realist` for plan step 6.


### 2026-09-08 — reviewer-reconcile-20260908T053100Z-undo-redo-followups [reviewer-reconcile · NEEDS-CHANGES]

---
id: reviewer-reconcile-20260908T053100Z-undo-redo-followups
agent: reviewer-reconcile
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-08T05:31:00Z
commit: eace0087b1c78cfcda7bfaa5bd895e9dce76a52b
targets:
  - path: Picea.Abies.Presentation/content/demo/README.md
    lines: "3, 52-53, 66-69, 85, 72-79, 105"
  - path: Picea.Abies.Presentation/content/demo/SHA256SUMS
    lines: "40-42"
  - path: Picea.Abies.Presentation/content/demo/stops/5.5-property.70d9ae5.cs
    lines: "1-48"
  - path: .squad/design/undo-redo/04-realist-plan.md
    lines: "1508, 1533, 1544-1553, 1669-1680, 1927"
blockers:
  - file: Picea.Abies.Presentation/content/demo/README.md
    line: 105
    reason: "The new fragment-index row claims both versions pass on their own commit and that TUnit reports green. Neither version has ever compiled - at 07607bf the artifact is a fenced block in 06-spec.md, at 70d9ae5 the file is Compile-Removed (0 Compile items measured), and the namespace it imports, Picea.Abies.History, does not exist anywhere in the repo. The user's decision said true, not pass."
  - file: Picea.Abies.Presentation/content/demo/README.md
    line: 3
    reason: "Sentences whose truth rested on a single pin or on 39 files are falsified by this change and left unamended - the opening paragraph at 3, the fragment-index table header at 85, and the manifest paragraph at 52-53 which says 39 files (19 + 20) where the tree now holds 40 (19 + 21) and the manifest 40 checksum lines. Fails the Definition of Done docs-in-sync item."
  - file: Picea.Abies.Presentation/content/demo/SHA256SUMS
    line: 40
    reason: "The hand-appended second section makes the README's own regenerate-and-diff check report a permanent four-line false mismatch on an intact tree, because LC_ALL=C sort places 5.5-property.70d9ae5.cs before 5.5-property.cs and the blank line and comment header are not reproducible by the documented generator. The instruction at 66-69 that teaches the reader to read a mismatch as the source having moved on is unamended. sha256sum -c still exits 0."
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 1927
    reason: "The edit anchors the PR-0 record to merge commit 70d9ae58b142039f274d4f124902a9a60b6a6186 while leaving two as-planned claims that the commit falsifies - three files, no framework code, well under the line gate (PR 361 is 40 files and 7669 changed lines, and the repo's own Check PR Size job failed on it against a 1500-line hard limit), and at 1533 the approval commit is those two files and nothing else (it carries 37 more). This hunk had no independent pass."
high:
  - file: Picea.Abies.Presentation/content/demo/README.md
    reason: "Row 105 says plus the two lines amendment 4 added and same span through assertion (iii). Measured 3 statements over 7 added lines at method scope and +9 at fragment scope, and the span was extended past assertion (iii) to end-of-method because the older excerpt is brace-unbalanced."
  - file: .squad/design/undo-redo/04-realist-plan.md
    reason: "Line 1508 inverts the mechanism it cites - it calls the None Include inert once the Compile glob no longer claims the file, which is exactly the state in which the item is the only thing holding the file in any MSBuild item group. Measured with both controls in a scratch project. PR 0's review finding 14 states it the other way round, and the plan's own 1552 and 1677 are correct."
  - file: Picea.Abies.Presentation/Picea.Abies.Presentation.csproj
    line: 12
    reason: "The comment says content/demo contains one excerpted test method - this change makes it two - and says the files stay visible as None items, which is false of exactly the two .cs fragments it describes. The second clause is pre-existing; the count is introduced here."
medium:
  - file: .squad/design/undo-redo/04-realist-plan.md
    reason: "The XML snippet at 1544-1550 drops one sentence present in the landed csproj comment, and step 6's Done-when instructs removal of that comment."
  - file: Picea.Abies.Presentation/content/demo/README.md
    reason: "Same test joins a design-doc source to a source-file source. The inference is sound - verified that the two full methods are character-identical apart from amendment 4's floor - but the join is unmarked. Also, the short-SHA filename convention has no stated rule, and step 6 will produce a third version of this property."
good:
  - file: Picea.Abies.Presentation/content/demo/stops/5.5-property.70d9ae5.cs
    reason: "Byte-verbatim against UndoRedoSpec.cs at 70d9ae5 lines 779-826, and the older fragment is byte-verbatim against 06-spec.md at 07607bf lines 1034-1072. Pairing rather than replacing is the correct answer to 06-spec.md finding 10, and the review-found-it-before-the-lock claim verifies against amendment 4."
  - file: .squad/design/undo-redo/04-realist-plan.md
    reason: "Every as-landed claim about the csproj verifies - PR 361, the merge commit, both items present at 70d9ae5 and absent at parent 7d32cdb, the ItemGroup holding nothing else, no NETSDK1022, and not a build error for the post-step-6 stale item. The four-site sweep is complete and internally consistent."
references: []
---

🔴 Changes Requested on the staged `undo-redo-followups` tree — the demo bundle's new row asserts test execution that never happened, three pre-existing README claims are falsified and unamended, the manifest's documented regeneration check now yields a permanent false mismatch, and the plan's PR-0 record anchors an as-landed commit to two as-planned claims that commit falsifies.

## Context

Round 1. `reviewer-blind` correctly refused the dispatching prompt's instruction to read
`04-realist-plan.md` through `git diff`, so blocker 4 and the two `high` items against the plan are
unwitnessed by an independent pass — flagged as such in the verdict rather than presented as
corroborated.

The narrative sharpened blocker 1 rather than softening it: the user's decision was that both
versions are **true** on their own commit; "and both pass" was added in the drafting, and it is the
half that is false.

Two of `08`'s findings were resolved in the author's favour with evidence `08` could not reach — the
spec-text/test-text join is verifiable and holds, and the four "out of scope" staged files are
declared scope and are version-controlled project-scope memory by `CLAUDE.md`'s own account.

Full reasoning, evidence and measurements:
`.squad/design/undo-redo-followups/09-review-verdict.md`.


### 2026-09-08 — reviewer-reconcile-20260908T054715Z-undo-redo-followups-round2 [reviewer-reconcile · NEEDS-CHANGES]

---
id: reviewer-reconcile-20260908T054715Z-undo-redo-followups-round2
agent: reviewer-reconcile
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-08T05:47:15Z
commit: eace0087b1c78cfcda7bfaa5bd895e9dce76a52b
targets:
  - path: Picea.Abies.Presentation/content/demo/README.md
    lines: "3-8, 54-55, 71-91, 119"
  - path: .squad/design/undo-redo/04-realist-plan.md
    lines: "1508, 1538-1548, 1938"
  - path: Picea.Abies.Presentation/Picea.Abies.Presentation.csproj
    lines: "12"
  - path: .claude/enforcement/refutations.md
blockers:
  - file: .claude/enforcement/refutations.md
    reason: >-
      Criterion (b) of the Merge Criterion is unmet. The ledger is byte-unchanged against HEAD,
      so neither of the two outstanding warnings is registered. Both are registrable (neither is
      a regression of a stated property) and either route closes this - fix them, or register
      each with an owner, a level consequence and an expires date for the reviewer to verify.
      Criterion (b) binds on the red and warning grades here; the nitpick items do not need
      registration. Criterion (a) is green (sha256sum -c SHA256SUMS exits 0, 40 of 40) and
      criterion (c) is not applicable. This is round 2 of the cap of two; a third round on this
      base commit is the split.
high:
  - file: Picea.Abies.Presentation/content/demo/README.md
    line: 71
    reason: >-
      The newly scoped first-section check still reports a permanent one-line mismatch on an
      intact tree. The printed command emits 40 lines; the manifest's first section holds 39, so
      diffing them always yields "36d35 < ... stops/5.5-property.70d9ae5.cs". The interpretation
      offered beside it ("the source moved on") is not what that line means. This is the
      remainder of round 1's blocker, reduced from four spurious lines to one and no longer
      blocking on its own - the whole-file trap is now documented with its exact size and cause,
      and the authoritative sha256sum -c path is named and verified green.
  - file: Picea.Abies.Presentation/Picea.Abies.Presentation.csproj
    line: 12
    reason: >-
      Carried unfixed from round 1 and unregistered. The comment still says "one excerpted test
      method" where this changeset makes it two, and its second clause ("the files stay visible
      as None items") remains false of exactly the .cs files it describes. Doc-sync item of the
      Definition of Done. Graded warning in round 1 and not escalated here.
medium:
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 1543
    reason: >-
      The hard-limit citation reads pr-validation.yml:210; grep puts "const hardLimit = 1500" at
      211, and 210 is the comment above it. Round 1's verdict said 210 and the author transcribed
      it faithfully - this one is the reviewer's error, not the author's.
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 1539
    reason: >-
      "those three files" at :1539 refers forward - the sentence above names two, and the third
      (the csproj) is named at :1550. The correction belongs where it sits; only the referring
      phrase is ahead of its referent.
  - file: Picea.Abies.Presentation/content/demo/README.md
    line: 119
    reason: >-
      "every seed takes an unguarded continue" is the one indicative behavioural clause left in a
      row that is subjunctive everywhere else. Textually supported and its conclusion is true;
      cheap to bring into line with its neighbours.
  - file: .squad/design/undo-redo/04-realist-plan.md
    line: 1553
    reason: >-
      Carried from round 1. The XML snippet still drops the landed comment's cross-reference
      sentence, so following step 6's Done-when produces a needless diff.
good:
  - file: Picea.Abies.Presentation/content/demo/README.md
    reason: >-
      All four round-1 blockers answered at the level of the stated rule rather than the quoted
      instances. Nine counts re-derived from scratch and all nine hold; after two rounds in which
      counts were the recurring defect, this round introduced none. The user's decision survives
      verbatim, without the addition that falsified it.
  - file: .squad/design/undo-redo/04-realist-plan.md
    reason: >-
      "nothing else held for the commit and not for the PR" is a sharper reading than the finding
      asked for, and it is the one that keeps the Lock's check intact - that check is PR-scoped
      and PR #361 does not bring the spec to passing, so the squash does not weaken it. The size
      gate failure is recorded without being adjudicated, and the adjudication is handed to the
      user explicitly.
references:
  - reviewer-reconcile-20260908T053100Z-undo-redo-followups
---

Round 2 on the undo-redo-followups changeset: all four round-1 blockers are closed and every claim added this round verifies, but the Merge Criterion's registration requirement is unmet, so the verdict is NEEDS-CHANGES rather than PASS.

## What was verified

The round-1 index blobs were recovered as unreachable git objects (`a7355f5` for the README, `48b7846` for the plan) so the round-2 delta could be isolated at the same base commit. Only two content files changed - `content/demo/README.md` (+34/-20, four regions) and `.squad/design/undo-redo/04-realist-plan.md` (+13/-2, three hunks). Everything else with a newer mtime is hook-written; `00-warden-scan.md` is byte-unchanged; `SHA256SUMS`, both `.cs` fragments and the Presentation csproj are untouched.

Closed: the false execution claims in the fragment row, now replaced by an explicit never-ran statement whose three legs all re-derive; the one-pin and 39-file sentences, now defaulted-and-excepted with all four counts measured correct against a 40-file tree; the as-landed anchor, now separating the three-file approval commit `b39ac58` from PR #361's 40-file, 7,669-line squash merge whose non-required size check failed; the miscounted amendment delta, now stated at both scopes and correct at both; the inverted `None Include` mechanism, now restored to the polarity the original finding stated; and the unmarked cross-source join, now named and independently re-checkable.

## What remains

Two warning-grade findings, neither registered. Round 1's regenerate-and-diff blocker is reduced from a four-line to a one-line false positive but the interpretation sentence beside the scoped instruction still does not cover that line. The Presentation csproj comment is unfixed from round 1 and its count is now wrong by this changeset's own addition.

Either fixing both or registering both closes criterion (b). The reviewer cannot write the ledger entries - `enforce-reviewer-readonly.sh` confines it to its own outputs, and that separation is what stops registration becoming laundering.

## Where to stop

The verdict and this drop are pinned to `eace0087b1c78cfcda7bfaa5bd895e9dce76a52b`. Committing moves HEAD, at which point `.squad/.last-review-verdict` no longer matches and the commit gate blocks again - including for a commit carrying nothing but the fix. Stage whatever must ride with the changeset before the commit the next verdict will name, not after.


### 2026-09-08 — reviewer-reconcile-20260908T060136Z-undo-redo-followups-round3 [reviewer-reconcile · PASS]

---
id: reviewer-reconcile-20260908T060136Z-undo-redo-followups-round3
agent: reviewer-reconcile
verdict: PASS
scope: review
created: 2026-09-08T06:01:36Z
commit: eace0087b1c78cfcda7bfaa5bd895e9dce76a52b
targets:
  - path: Picea.Abies.Presentation/content/demo/README.md
    lines: "71-89"
  - path: Picea.Abies.Presentation/Picea.Abies.Presentation.csproj
    lines: "12"
  - path: .squad/design/undo-redo/04-realist-plan.md
    lines: "1543"
blockers: []
high: []
medium: []
good:
  - file: Picea.Abies.Presentation/content/demo/README.md
    reason: >-
      The scoped first-section check now runs clean. Executed verbatim as printed, from
      content/demo/ on the untouched tree, it produces no output and exits 0. The head -n 39
      boundary is exactly the first section (line 39 is the last checksum entry, 40 is blank, 41
      is the comment header, 42 is the second-section entry). Every count in the surrounding
      paragraph re-derived independently - 40 files under full and stops (19 plus 21), 40
      checksum lines, 39 in the first section - and the whole-file trap re-reproduced at exactly
      4 lines. The interpretation sentence now names the extra line as the second-section file
      rather than offering the source-moved-on reading, which is what round 2 said had to become
      true.
  - file: Picea.Abies.Presentation/Picea.Abies.Presentation.csproj
    reason: >-
      The comment reads "excerpted test methods (not compilation units)" and the plural is
      correct - there are exactly two .cs fragments under content/. The claim that they are not
      compilation units is verified - the Presentation project has 1 Compile item in total and 0
      under content/, and dotnet build is clean at 0 warnings and 0 errors. The MSBuild item was
      deliberately left untouched, which is a defensible scope call in a documentation changeset
      that ships no executable change.
  - file: .squad/design/undo-redo/04-realist-plan.md
    reason: >-
      The hard-limit citation at 1543 now reads pr-validation.yml 211 and 1930 already did.
      Re-derived rather than quoted - grep puts "const hardLimit = 1500" at 211 and 210 is the
      comment above it. This was the reviewer's own error from round 1, transcribed faithfully by
      the author, and it is now closed. The plan delta this round is one line and only one.
references:
  - .squad/design/undo-redo-followups/09-review-verdict.md
  - .squad/design/undo-redo-followups/08-review-blind.md
  - .claude/docs/principles-enforcement.md
---

## Context

Round 3 of the review of the `undo-redo-followups` changeset on branch
`docs/0-undo-redo-followups`, at base commit `eace0087b1c78cfcda7bfaa5bd895e9dce76a52b`. Rounds
1 and 2 both returned NEEDS-CHANGES at this same commit; their drops are archived at
`2026-09-08T05-31-09-review-eace0087.md` and `2026-09-08T05-48-17-review-eace0087-round2.md`.
Round 2's four blockers were all closed; it blocked only on criterion (b) of the Merge
Criterion, naming two unregistered warning-grade findings and one reviewer citation error.

This round verified three fixes, all staged over the same HEAD. The round-2 baseline was
recovered from unreachable git objects - blob `7bb9d343` for the README and `c217e360` for the
realist plan - each confirmed to be neither the HEAD blob nor the current blob before use, so
every delta was measured rather than inferred from a cumulative diff.

## What was verified

The scoped regeneration check was run verbatim and exits 0 with no output on the untouched
tree, and a two-arm scratch-copy probe confirmed both the positive control and the one case the
new interpretation sentence does not cover. `sha256sum -c SHA256SUMS` from `content/demo/`
exits 0 with 40 of 40 OK and no warnings. `dotnet build` is clean at 0 warnings and 0 errors on
both `Picea.Abies.Presentation` and `Picea.Abies.Tests`. MSBuild item state is unchanged - 0
Compile items under `content/`, 43 None items out of 45 files, the two absentees being exactly
the two .cs fragments.

Nothing outside the three named files changed since round 2. A `find -newer` sweep returns only
those three plus hook-written paths and this reviewer's own notebook entries;
`00-warden-scan.md` has a newer mtime but is byte-unchanged against HEAD. One newer path sits
in a nested scratch worktree under `.claude/worktrees/`, which is gitignored and untracked and
cannot enter the changeset.

## The one grading call

Round 1's warning on the Presentation csproj comment was a compound - a count this changeset
made wrong, and a second clause that was already wrong. The introduced half is fixed. The
inherited half - that the .cs files stay visible as None items - remains false and is
downgraded to a nitpick, on the record, for five stated reasons - the regression half is
closed, the Boy Scout escalation was load-bearing only while the same sentence carried an
introduced error, it matches the grade given to two structurally identical findings elsewhere
in this same review, it has no verified consequence, and the fix costs the same after the merge
as before it so it is not cap-forcing. The clause is not claimed to be true and stays open as a
nitpick owned by `csharp-dev` on a future changeset.

## Criterion and disposition

Criterion (a) is green, criterion (c) is not applicable, and criterion (b) is met - there is no
open finding at the red or warning grade left to register, so the ledger append round 2 offered
as an alternative route is no longer owed. `.claude/enforcement/refutations.md` is byte-unchanged
against HEAD, which is the correct state rather than a gap.

This is round 3, past the cap of two, so the disposition is the split. The split is degenerate
in the good direction - the whole changeset passes (a) to (c) and all of it ships. Nothing is
carved out and no further round is requested. Five nitpicks are carried forward as advisory
notes, not as conditions on this merge.

## Where to stop

This verdict and this drop are pinned to `eace0087b1c78cfcda7bfaa5bd895e9dce76a52b`. Committing
moves HEAD, at which point `.squad/.last-review-verdict` no longer matches and the commit gate
blocks again, including for a commit carrying nothing but this verdict. All 19 paths are
already staged and `git diff` is empty. Commit once, from here.


### 2026-09-08 — reviewer-reconcile-20260908T060604Z-undo-redo-followups-commit-boundary [reviewer-reconcile · PASS]

---
id: reviewer-reconcile-20260908T060604Z-undo-redo-followups-commit-boundary
agent: reviewer-reconcile
verdict: PASS
scope: review
created: 2026-09-08T06:06:04Z
commit: 0d6b208ca012e9cbf6710fba64ffd44f62cc6c60
targets:
  - path: Picea.Abies.Presentation/content/demo/README.md
  - path: Picea.Abies.Presentation/content/demo/SHA256SUMS
  - path: Picea.Abies.Presentation/content/demo/stops/5.5-property.70d9ae5.cs
  - path: Picea.Abies.Presentation/Picea.Abies.Presentation.csproj
  - path: .squad/design/undo-redo/04-realist-plan.md
  - path: .claude/agent-memory/realist/MEMORY.md
  - path: .claude/agent-memory/realist/an-exception-named-by-element-has-four-sites.md
blockers: []
high: []
medium:
  - file: .squad/design/undo-redo-followups/09-review-verdict.md
    reason: >-
      This confirmation is itself uncommitted. Committing it moves HEAD off
      0d6b208ca012e9cbf6710fba64ffd44f62cc6c60 and re-blocks the commit gate on a
      commit carrying nothing but a confirmation. Merge from this HEAD; do not
      commit the appended verdict line or this drop first.
good:
  - file: Picea.Abies.Presentation/content/demo/SHA256SUMS
    reason: >-
      The manifest survived the commit boundary intact - sha256sum -c re-run inside a
      git archive export of the committed tree is 40/40 OK, exit 0, no warnings.
references:
  - reviewer-reconcile-20260908T060136Z-undo-redo-followups-round3
---

The round-3 PASS carries unchanged to `0d6b208ca012e9cbf6710fba64ffd44f62cc6c60`; the commit boundary added no reviewable content.

## What this drop is

Not a fourth review round and not a re-run of the eleven dimensions. The round-3
verdict (`09-review-verdict.md`, ✅ Approved, drop
`reviewer-reconcile-20260908T060136Z-undo-redo-followups-round3`) graded the tree at
`eace0087b1c78cfcda7bfaa5bd895e9dce76a52b` and explicitly noted that committing would
move HEAD and re-block `enforce-review-verdict.sh`. This drop re-pins that same PASS to
the commit that boundary produced, on evidence that the commit changed no graded content.

## What was verified

- **Commit shape.** `0d6b208ca012e9cbf6710fba64ffd44f62cc6c60`, single parent
  `eace0087b1c78cfcda7bfaa5bd895e9dce76a52b`, 21 files, `+2266/−18`. `commit:` above read
  live via `git rev-parse HEAD` in this checkout at the moment of writing, not copied.
- **The 21 paths partition exactly, with no remainder.** 7 graded content paths (the
  `targets` list) + 2 review artifacts (`08-review-blind.md`, `09-review-verdict.md`) + 6
  `reviewer-reconcile` notebook paths (`MEMORY.md` and five entries) + 3 merged drops +
  3 hook-written rows = 21.
- **The three merged drops are mine and are the review's own history** - all three carry
  `agent: reviewer-reconcile` and `commit: eace0087b1c78cfcda7bfaa5bd895e9dce76a52b`
  (`NEEDS-CHANGES`, `NEEDS-CHANGES`, `PASS` for rounds 1-3).
- **`decisions.md` is a hook append, not a hand edit.** One hunk, `@@ -3823,3 +3823,296 @@`,
  zero deleted lines, containing only the three scribe-merged entries whose ids match those
  same three drops.
- **Every graded path's committed blob equals what round 3 passed.** `README.md`
  `f72871e6`, `04-realist-plan.md` `a590177f`, `.csproj` `624ca60a` - the round-3 baseline
  table's "current" column verbatim - plus `SHA256SUMS` `a036722b`,
  `stops/5.5-property.70d9ae5.cs` `6775d948`, `realist/MEMORY.md` `81c0d186`,
  `realist/an-exception-named-by-element-has-four-sites.md` `db25b0f9`. Each also equals
  `git hash-object` on the working-tree copy, so index, commit and working tree agree.
- **The manifest is green in the committed tree, not merely in the working tree.**
  `sha256sum -c SHA256SUMS` run inside a `git archive 0d6b208` export from
  `content/demo/` - exit 0, 40 lines `OK`, zero non-OK lines, no warnings. The manifest's
  40 entries cover every file under `content/demo/` except the 5 documented non-payload
  paths (`README.md`, `SHA256SUMS`, `graphics/.gitkeep`, `timing/hooks-fired.log`,
  `timing/pass-cost.md`), and no manifest entry lacks a file. The `SHA256SUMS` delta is
  `+3/−0`: a blank line, a dated provenance comment, and the entry for the new excerpt.
- **The working tree holds only hook-written state.** `git status --porcelain
  --untracked-files=all` was empty when this pass opened; it now shows one path,
  `.squad/log/2026-09-08-session.md` `+3/−0`, three `session-logger.sh` rows. No untracked
  non-ignored file anywhere in the repository; no ignored file under `content/`; the
  nested `.claude/worktrees/` checkout remains ignored and unindexed as recorded in
  round 3.

## Where to stop

`.squad/.last-review-verdict` currently reads `PASS` / `eace0087b1c78cfcda7bfaa5bd895e9dce76a52b`
and so does **not** match HEAD - the commit gate is blocked right now. This drop, consumed
by `scribe-decision-merger.sh` on `SubagentStop`, is what rewrites the cache to
`0d6b208ca012e9cbf6710fba64ffd44f62cc6c60` and unblocks it. Merge from this HEAD. The
appended verdict line and this drop are uncommitted, and committing them would move HEAD
again and re-block the gate on a commit carrying nothing but a confirmation.


### 2026-09-08 — reviewer-reconcile-20260908T120946Z-undo-redo-pr1 [reviewer-reconcile · NEEDS-CHANGES]

---
id: reviewer-reconcile-20260908T120946Z-undo-redo-pr1
agent: reviewer-reconcile
verdict: NEEDS-CHANGES
scope: review
created: 2026-09-08T12:09:46Z
commit: 77aa2ba5e80130b5fbfcbd8ddae0b618e77c49fe
targets:
  - path: Picea.Abies/History/History.cs
    lines: "1-220"
  - path: Picea.Abies/History/HistoryStack.cs
    lines: "1-102"
  - path: Picea.Abies.Tests/History/HistoryTests.cs
    lines: "1-243"
  - path: Picea.Abies.Tests/History/HistoryStackTests.cs
    lines: "1-167"
blockers:
  - file: Picea.Abies/History/History.cs
    line: 118
    reason: "Present and Movement are public get/init on a public sealed record, so `with` is a public mutation channel that the internal constructor does not close. A probe compiled from an assembly outside InternalsVisibleTo, with EnablePreviewFeatures set as every adopter template sets it, moved Present while Past and Future stayed put. That is exactly the state the type's own XML doc at 107-112 says cannot be built, and it falsifies the plan's principles-gate resolution at 04-realist-plan.md 1470-1473 that all movement goes through the wrapper and that no deviation from Make Illegal States Unrepresentable is requested. Introduced by this changeset, so it is a regression and not registrable. The criterion is the rule, not the site list. Every public construction or mutation channel into History and Movement is closed to assemblies outside InternalsVisibleTo, demonstrated by a probe compiled from such an assembly with both controls. The locked UndoRedoSpec.cs contains no `with` expression at all, so narrowing these accessors cannot touch the lock."
  - file: Picea.Abies/History/History.cs
    line: 85
    reason: "Movement.Held's constructor is public and its object-typed anchor is unvalidated, while the XML doc at 63-69 rests the whole soundness argument for the type erasure on the framework being the only thing that ever constructs one. The same external probe built Held with a System.String anchor. When step 6 reads the anchor back and casts it to TModel, a wrong anchor is an InvalidCastException inside framework code with no context about its origin. The plan specifies Held with a TModel anchor at 157, 982 and 1060, where a public constructor would be type-safe by construction; the shipped object erasure is what turns the public constructor into a defect. Anchor is also a public getter on a public type, and per R4-6 and Trust Boundary 7 the anchor is a deliberately unscrubbed live model whose exposure the security room bounded to the DEBUG snapshot path. Same criterion as the blocker above."
high:
  - file: Picea.Abies/History/HistoryStack.cs
    reason: "History is a public record whose value equality and GetHashCode are reference-based on Past and Future, because HistoryStack overrides neither Equals nor GetHashCode and every operation returns a fresh instance. Verified by probe. Three narrative claims sit on top of this. The plan calls the history a value and cites ADR-008 for it, step 2's Done-when (a) says the round trip is value-equal end to end, and the locked UndoRedoSpec.cs compares whole History values at 226 and 851. Those spec assertions pass today only because the generated Equals opens with a reference check and the plan's Nothing branches return h itself, which is a property of step 6's implementation rather than of this type. A step-6 author who writes `h with { }` on a no-op path turns two locked properties red for a reason that looks like a wrapper bug. Not graded as a blocker because the plan's phrase is genuinely ambiguous between content equality and operator equality, and the change is forward-compatible. What should not ship is a public record with neither a structural equality nor a stated identity contract, and no test pinning either."
  - file: Picea.Abies/History/History.cs
    reason: "Step.Cause and Step.AtTicks are documented at 14 and 20-21 as belonging to this step, but StepBack at 161 and StepForward at 179 stamp the newly created step with the crossed step's cause and tick. The code follows the plan verbatim, so the code is right and both doc comments are wrong. The blind reviewer's reading, that these two fields name the edge whose identity survives being crossed in either direction, is the one the plan implements. It matters more than a stale comment usually would because Cause is SEC-2's redaction target and INV-6's refusal name, and step 6's author will read these two sentences to decide which message to redact and which to name. HistoryTests.cs 102-104 asserts the surprising behaviour and calls it correct, so no test will catch the doc."
  - file: Picea.Abies/History/History.cs
    reason: "Movement.Held with an object anchor is a shape change from the plan's Held with a TModel anchor, correctly forced by the locked spec and shipped without a flag. csharp-dev's stated reason verifies. UndoRedoSpec.cs 399 asserts IsTypeOf of Movement.Held non-generically, which cannot resolve if Movement is generic, and the lock outranks the plan. The erasure is the right call and is not being asked back. What is owed is the flag, because R4-6 at 04-realist-plan.md 980-1046, Trust Boundary 7's wording, the DEBUG-row bullet and step 8(m)'s Anchor_never_reaches_a_release_path_surface were all drafted against the un-erased shape. The erased shape changes at least three things none of them considered. A wrong-typed anchor is now constructible, the DEBUG JsonTypeInfo obligation for an object-typed property is not the obligation for a TModel-typed one, and the read surface is object rather than the model type. security-expert has not seen the erased shape. Owner architect at close-out or security-expert at step 16."
  - file: Picea.Abies.Tests/History/HistoryStackTests.cs
    reason: "Six sites across HistoryStackTests.cs 51-92 and HistoryTests.cs 152-171 assert exceptions with a hand-rolled try/catch/bool where TUnit's own idiom is available. Picea.Abies.Testing.Tests uses Assert.That(act).Throws at TestHarnessTests.cs 57, 71, 80, 89, 103 and 113 and TestHarnessVisualTests.cs 109 and 165, including for both InvalidOperationException and ArgumentOutOfRangeException, and both projects pin TUnit 1.19.57, so the remedy compiles as-is. This idiom appears nowhere else in Picea.Abies.Tests; these two files introduce it. Beyond consistency the hand-rolled form is weaker in three ways. It catches the base type, so an ObjectDisposedException would satisfy the InvalidOperationException test; it asserts nothing about the messages, which are deliberately written, good, and currently untested; and on failure it reports Expected True rather than naming the exception."
  - file: Picea.Abies.Tests/History/HistoryTests.cs
    reason: "Design-pass structure is embedded in shipped source at HistoryStackTests.cs 7 and HistoryTests.cs 11, 14-15 and 80 as Plan step 1, Plan step 2 and plan step 6's record. The new files contain no .squad paths; those are only in Picea.Abies.Tests.csproj 24-28, inherited from PR 0 and out of scope. Two costs. The plan-step references stop resolving the moment the pass closes and a contributor outside the squad cannot resolve them at all. More importantly, 08-review-blind.md names these lines as the reason its independence was weaker than the design intends, and there are five more PRs in this series whose blind half these headers will reach the same way. Stating the finding rather than the remedy, because this is a call the user may want to make once for the series. The long C# name-resolution comment at HistoryTests.cs 16-32 is a different thing entirely, is accurate, and should stay."
medium:
  - file: Picea.Abies/History/HistoryStack.cs
    reason: "The summary at 8-18 rests its worst-case O(Depth) argument on the stacks being kept trimmed to a caller-supplied depth, in the present indicative, but Trim has no caller anywhere in Picea.Abies. Step 6's record is the intended one. Either the tense changes or Trim's contract names who is obliged to call it. Critic 🟡 b's why-the-Future-trim-is-safe sentence, which the Critic assigned to step 1 or step 6 and the author took to step 6, is the natural companion and is still owed."
  - file: Picea.Abies/History/History.cs
    reason: "The Crossable precondition on StepBack at 155 and StepForward at 173 is real, correct and written down nowhere. The plan puts the check in step 6's Transition, which is the right split, but these are internal methods with a safety-critical precondition documented in neither the exception block nor a Debug.Assert. Debug.Assert on the crossed edge being Crossable is what the pattern catalog asks for on internal invariants, costs nothing in release, and fails loudly in exactly the scenario the feature exists to prevent. Separately, SealTops at 202-207 has a deliberate last-writer-wins semantics settled by Critic 🟡 c and 🟡 6 and accepted by the user, documented nowhere and untested in either direction."
  - file: Picea.Abies/History/History.cs
    reason: "Step.AtTicks has no consumer in the code, the tests, the locked spec or the plan. Step 14's coalescing uses TPolicy.CoalesceWindow and TPolicy.Time at 04-realist-plan.md 1946-1951, not AtTicks. Implementing it is not a deviation, since the plan carries it in the type shape at 1057, which is why this is graded medium. But it is a field threaded through seven PRs with no stated reader, and the honest options are a one-line scaffolding note or a question to the architect at close-out."
  - file: Picea.Abies.Tests/History/HistoryStackTests.cs
    reason: "Coverage gaps, none of them Done-when items. Trim's documented identity no-op is unasserted, so a future edit to return a new instance would pass every test while breaking the documented contract. Trim(0) on a non-empty stack is untested; I verified the arithmetic is correct, so this is coverage rather than risk. SealTops with both stacks empty, Cleared on an already-empty history, and every exception message are untested. HistoryStackTests.cs 127-131 asserts inside a 10,000-iteration loop, which on failure will fail about 9,900 times."
good:
  - file: Picea.Abies/History/HistoryStack.cs
    reason: "The summary argues its own O(n) rather than asserting it, and the argument is correct. A persistent linked list does not serve trim-from-the-far-end either, and the repo has System.Collections.Immutable available in the analyzers project, so ImmutableStack was passed on for a stated reason."
  - file: Picea.Abies.Tests/History/HistoryTests.cs
    reason: "StepBack_applies_restore_and_scrub_rather_than_assuming_whole_model_replacement is the best test in the changeset. Its restore delegate throws unless it sees both arguments correctly, so a StepBack that silently used one or the other cannot pass. A test built to fail for the right reason. Every Done-when for steps 1 and 2 is met and green, and the C# name-resolution comment at 16-32 correctly diagnoses a non-obvious constraint, correctly identifies it as a step-6 decision, and correctly declines to make that decision now."
references: []
---

🔴 Changes Requested on undo-redo PR 1 — two introduced regressions against Make Illegal States Unrepresentable, both proven with an external-assembly probe carrying both controls, plus five registrable findings.

## Verification performed

- `dotnet build Picea.Abies.Tests` — succeeded, 0 warnings.
- Full suite — 249/249 passed. csharp-dev's claim verified.
- `dotnet format --verify-no-changes` on both new directories — clean, exit 0.
- `git log` on `Picea.Abies.Tests/History/UndoRedoSpec.cs` — last touched by `70d9ae5` (PR #361). Unmodified by this changeset, so the Lock's git-history check passes.
- `git diff HEAD --stat` — no csproj, no `Runtime.cs`, no `.js`. The plan's slip-signal row is honoured.
- External-assembly probe `ExternalProbe`, outside `InternalsVisibleTo`. Positive control reproduced both blind findings; negative control (`h with { Past = … }`, `h.Origin`) returned CS0117 and CS1061, so internals really are out of reach and the positive results are not an artefact of probe privilege.
- `Trim(0)` on a non-empty stack — arithmetic correct, no exception. Coverage gap, not a bug.

## Reconciliation summary

The blind reading held up: all three of its compiled probes reproduce independently. The narrative settled two of its findings **against** it — the hard-coded `Crossable` is step 2 Done-when (c), required, and the missing edge check is a step-6 obligation the plan already assigns; and `SealTops`' last-writer-wins is Critic-reviewed and user-accepted behaviour. The narrative made two of its findings **worse** — the plan's principles-gate resolution explicitly claims the invariant that the `with` channel breaks, and the plan specifies `Held(TModel Anchor)` where `Held(object Anchor)` shipped. The namespace collision is inherited from the plan's Namespace Plan and PR 0's locked spec, and both remedies the blind reviewer proposed are unavailable, because the shadowing is caused by the test namespace inside the locked file and because the locked spec also calls `History.Backward` and `History.Forward` on the static class.

## Merge Criterion

Criterion (a) holds — every step 1 and step 2 Done-when executes and passes. The verdict turns on (b): the two blockers are deviations this changeset **introduces**, which are regressions and are not registrable. Criterion (b) binds on the blocker and high grades only; the medium entries are advisory. Full detail in `.squad/design/undo-redo-pr1/09-review-verdict.md`.

