---
name: code-review
description: Pattern catalog for reviewing C#/.NET code in functional-DDD codebases. Use when reviewing PRs, checking code before submitting, or validating that a change follows the team's conventions. Patterns adapted from dotnet/runtime maintainer review corpus, filtered to what applies to .NET 10 / C# 14 / functional DDD / Aspire / TUnit.
---

# Code Review Pattern Catalog

This is a **reference catalog** the Reviewer consults during reviews. It is not the review process itself — that lives in the Reviewer charter (`.claude/agents/reviewer.md`), which defines the steps, dimensions, output format, and verdict consistency rules.

These patterns are distilled from real C#/.NET maintainer review feedback (originally from dotnet/runtime, adapted for the team's stack). They represent what experienced .NET reviewers actually flag in practice. Each pattern includes the principle, why it matters, and what to suggest when you find a violation.

## How to Use This Skill

The Reviewer's charter defines **how** to review (the process, dimensions, output format, verdict rules). This skill catalogs **what** to look for (the specific patterns). The charter is always loaded; this skill is consulted on demand.

Consult this skill when:
- Reviewing C# code
- You want to verify a specific concern is a real pattern, not just an opinion
- You need a reference link to cite in a finding
- The Architect or a specialist asks "is this pattern valid?"

Do NOT use this skill as a checklist that must be exhaustively applied to every review. It's a reference, not a runbook.

---

## Correctness & Safety

### Error Handling & Assertions

- **Use `Debug.Assert` for internal invariants, not exceptions.** For internal-only callers, assert assumptions rather than throwing `ArgumentException`. Prefer `Debug.Assert(value is not null)` over the null-forgiving operator (`!`).
- **Use `throw` for reachable error paths, `UnreachableException` for exhaustive switches.** When a code path might be hit at runtime, throw an exception rather than asserting. Use `throw new UnreachableException()` for default cases in exhaustive switches over closed type hierarchies. Use `PlatformNotSupportedException` (not `NotSupportedException`) for platform gaps.
- **Include actionable details in exception messages.** Use `nameof` for parameter names. Include the unsupported type or unexpected value. Never throw empty exceptions.
- **Initialize output parameters in all code paths.** When a method has `out` parameters, ensure they are initialized to a defined value in all error paths.
- **Use `ThrowIf` helpers over manual checks.** Use `ArgumentOutOfRangeException.ThrowIfNegative`, `ObjectDisposedException.ThrowIf`, `ArgumentException.ThrowIfNullOrEmpty`, etc. instead of manual `if`-then-throw patterns.
- **Challenge exception swallowing that masks unexpected errors.** When code adds `try/catch` blocks that silently discard exceptions (`catch { return null; }`, `catch { continue; }`), question whether the exception represents a truly expected, recoverable condition or an unexpected error signaling a deeper problem. Silently catching exceptions that "shouldn't happen" hides root causes. The default disposition should be to let unexpected exceptions propagate or fail fast so the real issue gets investigated.

### Functional Domain Modeling

These patterns enforce the functional DDD principles in `.claude/docs/decisions.md`. Deviations require explicit user approval per `principles-enforcement.md`.

- **Errors are values, not exceptions, in the domain.** Workflow functions return `Result<T, TError>` where `TError` is a domain error type. Exceptions are reserved for programmer bugs and unrecoverable infrastructure failures.
- **`Option<T>` over null for intentional absence.** A nullable reference type signals "the compiler can't prove this isn't null." `Option<T>` signals "intentional absence is a valid state of the domain."
- **Smart constructors on constrained types.** Domain primitives (`EmailAddress`, `Username`, `Slug`) have private type constructors and public factory methods returning `Result<T, DomainError>`. Validation happens once, at creation. Reviewer flags any direct instantiation that bypasses the smart constructor.
- **State machines with distinct types per state, not boolean flags.** `Order` should not have an `IsConfirmed` flag and a nullable `ConfirmedAt`; it should be `DraftOrder | ConfirmedOrder | ShippedOrder` where each state carries only the data valid in that state. Reviewer flags boolean-flag-as-state and nullable-field-as-state.
- **Pure functions in the domain.** Domain functions take values, return values, and have no side effects. IO and side effects live at the application/infrastructure layer. Reviewer flags any IO call (file system, database, HTTP, time, random) inside a domain function.
- **No OO patterns in the domain.** No inheritance hierarchies for behavior. No mutable classes. No `Manager`, `Helper`, `Util` types in the domain layer. Records, pure functions, and discriminated unions only.
- **No infrastructure attributes on domain types.** Domain records have no `[Table]`, `[Column]`, `[JsonPropertyName]`, EF Core `[Key]`, or similar. Mapping to/from infrastructure is the infrastructure layer's responsibility.

### Thread Safety

- **Use `Volatile` or `Interlocked` for cross-thread field access.** Fields written on one thread and read on another must use `Volatile.Read/Write` or `Interlocked`. The `??=` operator is **not** thread-safe. `Nullable<T>` is **not** safe for caching across threads (two-field struct can tear).
- **Use `TickCount64` for timeout calculations.** Use `Environment.TickCount64` (long) instead of `Environment.TickCount` (int) to avoid integer overflow at ~24.8 days.
- **Don't use shared mutable arrays without synchronization.** If you need a shared collection, use a concurrent collection or wrap access in a lock.

### Security

- **Guard integer arithmetic against overflow.** Size computations involving multiplication (`newCapacity * sizeof(T)`) must be guarded against overflow. Use `checked { }` blocks or design APIs that are correct by construction.
- **Clean sensitive cryptographic data after use.** Always clear key material with `CryptographicOperations.ZeroMemory`. When using `PinAndClear` but copying to another buffer, clear the original too. Use non-short-circuit operators (`|`) in verification code to prevent timing leaks.
- **Don't proactively send credentials.** Never send authentication credentials before receiving a challenge.
- **Limit `stackalloc` to ~1KB and validate size.** Don't `stackalloc` based on user-controlled or large input sizes — stack overflow is a DoS vector.
- **Parameterize all SQL.** No string concatenation into queries. Reviewer flags any `ExecuteAsync($"SELECT ... WHERE id = {id}")` — that's still concatenation under the hood unless it's a `FormattableString` going through a parameterizing API.
- **Escape all output going to HTML/JS/URL contexts.** Reviewer flags any direct output of user data without an explicit escape.

### Correctness Patterns

- **Fix root cause, not symptoms.** Investigate and fix the root cause rather than adding workarounds or suppressing warnings. If a test is failing, find out why — don't just `[Skip]` it. If an assertion is firing, don't just delete it.
- **Prefer safe code over unsafe micro-optimizations.** Do not introduce `Unsafe.As`, `Unsafe.AsRef`, or raw pointers without a documented performance need backed by a benchmark. Prefer `Span<T>`-based APIs.
- **Use `Unsafe.BitCast` for same-size type punning.** Prefer `Unsafe.BitCast<TFrom, TTo>` over `Unsafe.As<TFrom, TTo>` for type punning between value types of the same size — `BitCast` avoids undeclared misaligned access.
- **Delete dead code and unnecessary wrappers.** When the only caller of a helper changes or disappears, remove the helper. Dead code rots and confuses future readers.
- **Handle `SafeHandle.IsInvalid` before `Dispose`.** Check `IsInvalid` (not null) on returned `SafeHandle`s. Get the exception **before** calling `Dispose`, since `Dispose` might clear the error state.
- **Seal classes when `Equals` uses exact type matching.** If a class implements `Equals` with `GetType() == other.GetType()` comparison, seal the class to prevent subtle inheritance bugs.
- **Use `Environment.ProcessPath` and `AppContext.BaseDirectory`.** Use these instead of `Process.GetCurrentProcess().MainModule?.FileName` and `Assembly.Location` for NativeAOT/single-file compatibility.
- **File name casing must match `.csproj` references exactly.** Linux is case-sensitive. Mismatched casing builds on Windows but breaks in CI.
- **Prefer correct-by-construction designs.** Prefer designs where invariants are enforced by the type system (smart constructors, state machines, parse-don't-validate) over runtime checks scattered across call sites.

---

## Performance & Allocations

### Measurement & Evidence

- **Performance changes require benchmark evidence.** Demand BenchmarkDotNet results before accepting any change framed as an optimization.
- **Validate with realistic inputs.** Trivial benchmarks with predictable inputs overstate gains from jump tables and branch elimination. Require evidence from realistic, varied inputs.
- **Justify binary size increases.** Changes that increase binary size require measured wall-clock improvements on real apps, not just instruction counts.
- **Avoid premature optimization with object pools and caches.** Do not introduce global caches or object pools without evidence they are needed. Pools have lifetime, contention, and correctness costs that often outweigh allocation costs.

### Allocation Avoidance

- **Avoid closures and allocations in hot paths.** When a lambda captures locals creating a closure, consider using a static delegate with a state parameter (value tuple). Avoid string concatenation; use span-based operations or interpolated string handlers.
- **Pre-allocate collections when size is known.** Pass capacity to `Dictionary`, `HashSet`, `List`, `StringBuilder` constructors when expected count is available.
- **Structs in dictionaries need `IEquatable<T>` and `GetHashCode`.** Without these, the runtime falls back to boxing for equality comparison — every lookup allocates.
- **Avoid the Pinned Object Heap for non-permanent objects.** POH is never compacted and effectively gen2. Only use for objects that will survive as long as the process.
- **Suppress `ExecutionContext` flow for infrastructure timers.** When allocating `Timer` or similar background infrastructure, suppress EC flow (`ExecutionContext.SuppressFlow()`) to avoid capturing unrelated `AsyncLocal`s that leak memory across components.

### Code Structure for Performance

- **Place cheap checks before expensive operations.**
- **Allocate resources lazily where possible.** Avoid forcing type initialization during startup — startup time is often the most important perf metric.
- **Extract throw helpers into `[DoesNotReturn]` methods.** The JIT can then inline the success path more aggressively.
- **Avoid O(n²) patterns in collections and hot paths.** Watch for linear scans inside loops, repeated `RemoveAt` in loops. Use `RemoveAll`, single-pass restructuring.
- **Cache repeated accessor calls in locals.**
- **Compute constant data at compile time, not execution time.** Use `const`, `static readonly`, source generators, or `[ConstantExpected]`.

### Specific API Choices

- **Use `AppContext.TryGetSwitch` with a static readonly property.** Cache `AppContext` switches in `static bool Prop { get; } = AppContext.TryGetSwitch(...)` so the JIT can dead-code-eliminate unreachable paths.
- **Do not cache `typeof` expressions.** `typeof(...)` is JITed into a constant; caching it in a field is a de-optimization. Similarly, don't store `ArrayPool<T>.Shared` in variables — it breaks devirtualization.
- **Use `CollectionsMarshal` for large value-type dictionary lookups.** Use `GetValueRefOrAddDefault` or `GetValueRefOrNullRef` to avoid copying large structs.
- **Use `sizeof` instead of `Marshal.SizeOf` for blittable structs.**
- **Use the idiomatic `(uint)index >= (uint)length` bounds check.** The JIT recognizes this pattern and elides redundant bounds checks.
- **Source generators must be properly incremental.** Do not store Roslyn symbols (`ISymbol`, `Compilation`) in incremental pipeline steps.
- **Use `BenchmarkDotNet` for benchmarks, not stopwatch loops.** Hand-written timing is almost always wrong (no warmup, no statistical analysis, no isolation).

---

## API Design & Contracts

- **Parameter names matter.** Renaming a public API parameter (including case changes) is a **source breaking change** — it affects named arguments. Treat parameter renames in public APIs as breaking changes requiring an ADR.
- **Align exception types and validation order.** Validate arguments first (`ArgumentNullException`, then `ArgumentException`), then state (`InvalidOperationException`), then `ObjectDisposedException`, then perform the operation. Same exception types on all platforms.
- **`Try` APIs return `false` only for the common expected failure.** Throw for everything else (corruption, permissions, invalid arguments).
- **Don't expose mutable options after construction.** If values are captured at construction time, don't expose a mutable options object — that misleads callers.
- **Don't reference private field names or internal types in user-facing error messages.** Error messages are part of the public API surface.
- **Use `PlatformNotSupportedException` for platform limitations.**
- **Avoid unsigned types for lengths in public APIs.** Prefer `int` or `long`. Unsigned types create interop friction.
- **Use named types instead of `ValueTuple` across file boundaries.** `ValueTuple` is fine inside a method; across module boundaries it's a refactoring tax.
- **Follow the obsoletion process for deprecated APIs.** Pick the next available diagnostic ID, add `[Obsolete]`, and use `[EditorBrowsable(Never)]` with `[OverloadResolutionPriority(-1)]` for overload fixes.
- **Start core component changes with an ADR.** Changes to bounded-context boundaries, the AppHost topology, or the squad's infrastructure should start with an ADR or Architect handoff before implementation.

---

## Code Style & Formatting

- **Use well-named constants instead of magic numbers.**
- **Use `var` only when the type is obvious from context.** **Never use `var` for numeric types** — `var x = 1` hides whether `x` is `int`, `long`, or something else.
- **Use PascalCase for constants; descriptive names for booleans.** Boolean fields should be **positive and descriptive** (`_hasCurrent`, not `_valid`; `IsEnabled`, not `Disabled == false`).
- **Name methods to accurately reflect their behavior.** `Get*` implies a return value; use `Print*`/`Display*` for void. `ThrowIf...` not `ThrowExceptionIf...`. `Try*` implies a boolean return.
- **Prefer early return to reduce nesting.**
- **Avoid `using static` and `#region` in new code.**
- **Place local functions at method end, fields first in types.**
- **Narrow warning suppression to smallest scope.** Avoid file-wide `#pragma` suppressions.
- **Use pattern matching and `is`/`or`/`and` patterns.** Use named parameters for boolean arguments at call sites (`SaveAsync(force: true)` not `SaveAsync(true)`).
- **Do not initialize fields to default values (CA1805).** The CLR zero-initializes fields.
- **Sealed classes do not need the full `Dispose` pattern.**

---

## Consistency with Codebase Patterns

### PR Hygiene

- **Keep PRs focused on their stated scope.** No accidental file modifications, no unrelated refactoring, no whitespace noise, no build artifacts.
- **Do large refactorings and renames in separate PRs.** Mechanical renames should be separate from logic changes — otherwise the reviewer has to mentally diff twice.
- **Merge to main first, then backport to release branches.** Performance fixes typically don't meet the backport bar unless they fix a significant regression.

### Code Reuse & Deduplication

- **Extract duplicated logic into shared helper methods.** When the same pattern appears in three or more places, extract it.
- **Use existing APIs instead of creating parallel ones.**
- **Delete dead code and unused declarations aggressively.**

### Established Conventions

- **Store error strings in `.resx`, not inline.** Reference via the `SR` class (or the team's equivalent).
- **Sort lists and entries alphabetically.** Lists of areas, configuration entries, resx entries, registration calls, and ref source members should be maintained in alphabetical order — it makes merge conflicts and audits easier.
- **Use `DOTNET_` prefix for environment variables.** New runtime environment variables must use `DOTNET_` exclusively. Legacy `COMPlus_` names should not be added in new features.
- **Match existing style in modified files.** The existing style in a file takes precedence over general guidelines. Do not change existing code for style alone.

---

## Testing

These patterns complement the Reviewer charter's testing dimension and the team's Definition of Done.

- **Always add regression tests for bug fixes and behavior changes.** Enforced by `principles-enforcement.md` and 🔴 Must Fix in the Reviewer charter. Prefer adding `[Arguments]` test cases to existing test files.
- **Use TUnit conditional/skip attributes correctly.** Use TUnit's conditional skip mechanisms (`[Skip]`, `[SkipWhen]`) rather than runtime if-checks inside test bodies.
- **Test edge cases, error paths, and all affected types.** Include empty strings, negative values, boundary conditions, Turkish 'i' (`İ`/`ı`), surrogate pairs, leap years, DST transitions.
- **Test assertions must be specific.** Assert exact expected values (exact `Result` variant, exact byte counts), not broad conditions like "is not null."
- **Ensure tests actually fail when the fix is reverted.** This is the most important property of a regression test.
- **Delete flaky and low-value tests rather than patching them.** A flaky test is worse than no test.
- **Make test data deterministic and culture-independent.** Don't rely on `CurrentCulture`. Don't rely on the wall clock.
- **Use `PLACEHOLDER` for test passwords.** Avoids false positives from credential scanning tools.
- **Use the Aspire AppHost for integration and E2E tests.** Per `principles-enforcement.md`, this is the default. No `WebApplicationFactory`, no Testcontainers, no manual process startup — **except** the named, closed exception in `.claude/docs/decisions.md`'s "Aspire AppHost Is the Test Fixture" entry, which grandfathers a small set of fast in-memory `WebApplicationFactory`/`TestServer`-based tests for single-service HTTP-pipeline verification. Check that exception list before flagging a `WebApplicationFactory` usage — it may be one of the four named files, not a new violation.
- **Use `RemoteExecutor` for tests with process-wide shared state.**
- **Don't add heavy dependencies to test assemblies.**
- **Catch only expected exceptions in fuzz/property tests.**
- **Use modern TUnit patterns.** `await Assert.That(...)` — not manual return-code-style success indicators.
- **Reduce test output volume.** Avoid megabytes of console output.

---

## Documentation & Comments

- **Comments should explain why, not restate code.** Delete comments like `// Get the user`. A useful comment explains the non-obvious: a tricky invariant, a workaround for a known issue (with a link), a performance trade-off.
- **Delete or update obsolete comments when code changes.** Stale comments are worse than no comments.
- **Track deferred work with GitHub issues and searchable TODOs.** Reference a tracking issue in `TODO` comments with a consistent prefix (e.g., `TODO(#123):`).
- **Don't duplicate comments on interface implementations.** Use `<inheritdoc/>` on implementations.
- **Add XML doc comments on all new public APIs.** Properties should start with "Gets the ..." or "Gets or sets the ...".
- **Use SHA-specific or commit-based links in documentation.** Don't use branch-relative GitHub links.
- **Use established terminology in user-facing text.** Do not expose internal type names, private field names, or codenames in public docs or error messages.
- **Retain copyright headers and license information.**

---

## Platform & Cross-Platform

- **Use `BinaryPrimitives` for endianness-safe reads.** Use `ReadInt32LittleEndian`/`BigEndian` rather than pointer casts.
- **Use cross-platform vector APIs over ISA-specific intrinsics.** Prefer `Vector128/256/512.IsHardwareAccelerated` over `Avx512BW`, `Sse2`, `AdvSimd`. Use `BitOperations` for portable bit manipulation.

---

## What This Skill Does NOT Cover

- **The review process itself** (Steps 0-3, Holistic Assessment, Verdict Consistency Rules) — see `.claude/agents/reviewer.md`
- **The squad's principles** (functional DDD, namespaces, observability, security) — see `.claude/docs/decisions.md` and `.claude/docs/principles-enforcement.md`
- **The Definition of Done checklist** — see `.claude/docs/decisions.md`
- **Threat model maintenance** — see the Security Expert charter and `security-toolchain` skill
- **Performance benchmarking infrastructure** — see the Performance Engineer charter and `performance-engineering` skill
- **Documentation conventions and Diátaxis structure** — see the Tech Writer charter and `technical-writing` skill

If a pattern in this skill conflicts with the team's `decisions.md` or `principles-enforcement.md`, **the team's decisions take precedence**.

---

## Source & Adaptation Notes

This skill was adapted from the [dotnet/runtime code-review skill](https://github.com/dotnet/runtime/blob/main/.github/skills/code-review/SKILL.md), extracted from 43,000+ maintainer review comments across 6,600+ PRs.

**What was kept:** Generally applicable C#/.NET patterns for correctness, performance, API design, testing, style, and consistency that apply to any modern .NET codebase using C# 14, async/await, `Span<T>`, source generators, and BenchmarkDotNet.

**What was dropped:** dotnet/runtime-internals-specific patterns (JIT lowering, GC-EE interface vtables, ECMA-335 metadata parsing, P/Invoke marshalling specifics, `eng/common` arcade sync, native C++ patterns).

**What was added:** Functional-DDD patterns from the team's principles — smart constructors, state machines, `Result<T, TError>`, `Option<T>`, pure domain functions, no infrastructure attributes on domain types, Aspire AppHost as the default test startup mechanism (with a documented, closed exception for fast in-memory `WebApplicationFactory`/`TestServer` tests — see `.claude/docs/decisions.md`), TUnit-specific testing patterns.

**Carried across from Abies's prior `.squad/skills/code-review/SKILL.md` (2026-09-02):** that file's content was, on inspection, already fully captured by this generalized version — the "Functional Domain Modeling (Picea.Abies-Specific)" section maps 1:1 onto the "Functional Domain Modeling" section above with no loss (Abies's domain modeling *is* the team's generic functional-DDD convention, not a variant of it). The only genuine drift found was this skill's Testing-section claim that the Aspire AppHost is used with no exceptions, which the codebase itself contradicted (`ConduitApiFactory.cs`, `EndpointTests.cs`) — resolved above by the decisions.md amendment rather than by adding new pattern text here. `Result<T, TError>` and `Option<T>` are supplied by the external `Picea` NuGet package (not defined in this repo); as of this note the repo has version drift on that package (`1.0.0` in `Picea.Abies`, `Picea.Abies.Conduit`, `Picea.Abies.Conduit.Tests` vs. `1.0.27-rc-0002` in `Picea.Abies.Conduit.Api*` and `Picea.Abies.Conduit.ReadStore.PostgreSQL*`) — **unverified whether this is intentional**, flagged here rather than asserted one way or the other.
