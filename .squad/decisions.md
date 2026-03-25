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
- `feat/42-token-versioning`
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
- [ ] Unit tests cover new logic (smart constructors, workflows, edge cases)
- [ ] Integration/E2E tests run via Aspire AppHost
- [ ] Security regression tests added for any new threat mitigations
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
- [ ] API reference current
- [ ] ADR created for significant architectural decisions
- [ ] CHANGELOG updated

**Review:**
- [ ] Reviewer approved (no open 🔴 Must Fix findings)
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

### Undocumented Deviations Block
Any code that deviates from an established principle without a documented approval (decision log + code comment) is 🔴 Must Fix unconditionally.

### Observability Review
Reviewer checks for OTEL trace coverage, custom spans, error recording, cross-service propagation, AddServiceDefaults(), and E2E trace verification tests.

### Threat Model Review
If a change adds an entry point, alters a trust boundary, or changes auth — the threat model must be updated. Missing updates are 🔴 Must Fix.
