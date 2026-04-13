# Team Decisions

## Framework

### Beast Mode × Disney Creative Strategy
All significant features and architectural changes go through the Architect's Dreamer → Realist → Critic cycle before implementation. All code changes go through the independent Reviewer before merge. The Architect does not write code. The Reviewer does not participate in design phases.

### Principles Enforcement
Every deviation from an established principle requires explicit user approval before proceeding. No agent may silently compromise. Undocumented deviations are 🔴 Must Fix during review. See `.squad/principles-enforcement.md` for the full protocol.

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

### Aspire AppHost Is the Test Fixture
All integration and E2E tests start the SUT via `DistributedApplicationTestingBuilder` against the Aspire AppHost. No `WebApplicationFactory`, no Testcontainers, no manual process startup.

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
**What:** Removed default `Decide`/`IsTerminal` from `Program.cs`. Made `Runtime.cs` decider-native — `Dispatch` now runs the full decide→transition pipeline. Added `_dispatchGate` SemaphoreSlim for command serialization. All builds and test suite passed (unit tests, integration tests, WASM tests, template tests, E2E suite).
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
- [ ] Regression test added for every bug fix (reproduces the bug, verifies the fix)
- [ ] Tech Writer has verified all existing docs are still in sync after the change
**Why:** Current state is directionally correct but not release-safe without propagation and guardrails.
---

### 2026-04-04: Architect — Full Decider Cutover Migration Target (Breaking)
**By:** Architect
**Requested by:** Maurice CGP Peters
**What:**
- Abies runtime must be decider-native end-to-end. `Runtime<TProgram, TModel, TArgument>` and all hosting entry points compile against strict decider Program contract.
- Runtime message flow is canonical: `Decide(state, command) -> events`, `Transition(state, event) -> (state, effect)`, `Interpret(effect) -> commands/messages`.
- Remove Program-level compatibility defaults (`Decide` pass-through, `IsTerminal` default).
- Every concrete Program implementer must explicitly define `Decide` and `IsTerminal`.
- All Program implementers (Conduit, Counter, SubscriptionsDemo, Presentation, UI Demo, Benchmark, templates, tests) satisfy strict decider contract.
- Remove staged-migration compatibility posture from implementation surface.
- Acceptance criteria: core runtime, all hosts, all apps, template-generated projects build and pass tests with strict decider contract.
**Why:** Remove architectural debt; make illegal states unrepresentable at the runtime boundary.

### 2026-04-04: C# Dev — Full Decider Migration Completed
**By:** C# Dev
**What:**
- Removed Program-level compatibility defaults (`Decide` pass-through, `IsTerminal` default) from `Picea.Abies/Program.cs`.
- Made runtime command handling decider-native in `Picea.Abies/Runtime.cs`: `Dispatch` now runs canonical decider flow with atomic command gate (`SemaphoreSlim`).
- Updated docs/comments in `docs/reference/runtime-internals.md`, `docs/api/program.md`, `Picea.Abies.Presentation/Program.cs`.
- All builds and most tests pass. One failing E2E test: `DeleteArticle_AsAuthor_ShouldNavigateToHome` (Playwright timeout on `.article-page`).
**Follow-ups:**
- Investigate and stabilize `DeleteArticle_AsAuthor_ShouldNavigateToHome`.
- Add focused runtime tests for command rejection and terminal short-circuit paths.
**Why:** Implement breaking migration to strict decider contracts.

### 2026-04-04: C# Dev — Program Decider Err as Message
**By:** C# Dev
**What:**
- Use `Result<Message[], Message>` instead of `Result<Message[], Unit>` for MVU Program deciders.
- Updated Program contract, runtime dispatch (routes `decision.Error` through transition), Conduit validation, impacted program implementations/templates/tests.
- Added focused tests: `Runtime_DispatchesDecisionErr_ThroughProgramMessageFlow`, `ConduitDecideTests`.
**Why:** `Unit` as error type forced validation failures into synthetic success events, blurring decider semantics. Routing `decision.Error` through transition preserves user-visible behavior while restoring explicit command rejection semantics.

### 2026-04-04: Reviewer — Full Decider Runtime Migration Audit
**By:** Reviewer
**Verdict:** Changes requested before approval.
**Primary blocker:** Runtime dispatch gate scope in `Picea.Abies/Runtime.cs` causes likely behavioral regression in Conduit E2E navigation flow.
**Findings:**
1. 🔴 **Must Fix** — `_dispatchGate` serializes entire command lifecycle including async effect/HTTP waits. UrlChanged and UI commands queue behind in-flight network commands → stalled routing. Conduit `DeleteArticle_AsAuthor_ShouldNavigateToHome` failure aligns with this. Fix: narrow synchronization scope to decision/state-transition critical section only.
2. 🟠 **Should Fix** — No regression tests for new decider-first concurrency behavior. Add runtime-level tests asserting UrlChanged is not blocked while another command awaits interpreter IO.
3. 🟡 **Should Fix** — Copied browser runtime assets (Presentation, SubscriptionsDemo, UI Demo `wwwroot/abies.js`) edited directly instead of canonical source (`Picea.Abies.Browser/wwwroot/abies.js`). Apply JS changes in canonical source only, then sync via build target.
**Contract review:** Program decider migration correctly applied at interface and implementer level. Runtime enforces Decide/IsTerminal before transition. Docs updated.
