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

