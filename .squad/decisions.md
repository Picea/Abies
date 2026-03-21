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

---

## Picea.Abies.UI — Phase 2 Components (2026-03-21, issue #166)

### StackGap Enum: `Gap1`–`Gap6` with `None = 0`
Map to `--abies-ui-space-1` through `--abies-ui-space-6`. `ToStackGapCss()` returns `null` for `None` — `BuildClassName` already filters nulls, avoiding spurious `abies-ui-stack--gap-none` classes. Consistent with `SpinnerSize` naming.

### `FormatDouble()` Helper for ARIA Numeric Attributes
All `aria-valuemin`, `aria-valuemax`, `aria-valuenow` values use `value.ToString(CultureInfo.InvariantCulture)`. Centralised in a private helper rather than inlining at each call site.

### Labeled Divider: `<div>` Wrapper (Not Modified `<hr>`)
A labeled divider renders as `<div>` with two `<hr role="presentation" aria-hidden="true">` flanking a `<span>`. Plain divider remains `<hr role="separator">`. Matches ARIA authoring practices.

### `SkeletonOptions.Lines` Only Applies to `SkeletonShape.Text`
For non-Text shapes (Heading, Avatar, Rectangle, Circle) the `Lines` parameter is silently ignored. No `InvalidOperationException`. Document in XML doc if API is extended.

### Grid Gap: `int`, Not `StackGap` Enum
Grid component gap uses a direct `int` token index. Reusing `StackGap` would couple two layout primitives with different semantics. A `GridGap` enum can be added independently if needed.

### CSS Modifier Null Convention
When a modifier class is optional (no gap, no elevation), return `null` from the `ToXxxCss()` helper rather than empty string or `"none"`. `BuildClassName` filters nulls — spurious modifier classes are never emitted.

---

## Picea.Abies.UI — abies-ui.js (2026-03-21, issue #166)

### Focus Trap Teardown via `AbortController` (ES2024+ Idiomatic)
The focus trap `keydown` listener is registered with `{ signal: controller.signal }`. Teardown is `controller.abort()` — no stored listener reference needed. Single module-level `activeTrapController` enforces one active trap (Phase 1; nested support deferred).

### Module-Level `activeTrapController` — Single Trap (Phase 1)
Activating a new trap aborts the previous one first. Nested modal support (stack-based) is a known Phase 1 limitation.

### Load `abies-ui.js` as `defer`, Not `type="module"`
Pure side-effect file with no imports/exports. `defer` gives "run after parse" semantics without ES module machinery. ES2024+ syntax is compatible with module loading — this is a delivery decision, not a code style constraint.

### `MutationObserver` on `document.body` (Not `document`)
Abies renders into `document.body`. Scoping the observer to `body` avoids irrelevant `<head>` mutations (stylesheet injections, meta tag changes).

### Focusable Filter: Runtime `.filter()`, Not `:not([hidden])` Selector
`getFocusableElements()` filters via `.filter(el => !el.closest('[hidden]') && !el.closest('[inert]'))`. Embedding `:not([hidden])` in the CSS selector string cannot account for ancestor `[inert]` propagation or non-attribute hiding.

---

## Testing — Phase 2 (2026-03-21, issue #166)

### TUnit Boolean Assertions: `await Assert.That(value).IsTrue()`
Do NOT use NUnit's `Assert.That(x, Is.True)`. `Is` does not exist in the TUnit execution context. For `Page.EvaluateAsync<bool>(...)` results, use `await Assert.That(value).IsTrue()`.

### Tests Written Against Future DOM (Parallel Agent Pattern)
Tester-authored E2E tests may be written before the demo app update lands. This is intentional: tests express the accessibility contract. Tests go green when the parallel implementation agent completes — no modification needed.

### Focus Trap Test Layers on Smoke Test
`Modal_FocusShouldBeTrappedInsideWhenOpen` extends the existing smoke test without duplicating open/close assertions. Smoke test = navigation contract; Phase 2 test = accessibility contract.
