---
name: code-review
description: A pattern catalog for reviewing C#/.NET code in the Picea.Abies ecosystem. Use when reviewing PRs, checking code before submitting, or validating that a change follows the team's conventions. Patterns adapted from dotnet/runtime maintainer review corpus, filtered to what applies to .NET 10 / C# 14 / functional DDD / Aspire / TUnit.
---

# Code Review Pattern Catalog

This is a **reference catalog** the Reviewer consults during reviews. It is not the review process itself — that lives in the Reviewer charter (`.squad/agents/reviewer/charter.md`), which defines the steps, dimensions, output format, and verdict consistency rules.

These patterns are distilled from real C#/.NET maintainer review feedback (originally from dotnet/runtime, adapted for the Picea.Abies stack). They represent what experienced .NET reviewers actually flag in practice. Each pattern includes the principle, why it matters, and what to suggest when you find a violation.

## How to Use This Skill

The Reviewer's charter defines **how** to review (the process, dimensions, output format, verdict rules). This skill catalogs **what** to look for (the specific patterns). The charter is always loaded; this skill is consulted on demand when the Reviewer needs a specific pattern reference.

Consult this skill when:
- Reviewing C# code in the Picea.Abies ecosystem
- You want to verify a specific concern is a real pattern, not just an opinion
- You need a reference link to cite in a finding
- The Architect or a specialist asks "is this pattern valid?"

Do NOT use this skill as a checklist that must be exhaustively applied to every review. It's a reference, not a runbook. The Review Dimensions in the Reviewer charter are the runbook.

---

## Correctness & Safety

### Error Handling & Assertions

- **Use `Debug.Assert` for internal invariants, not exceptions.** For internal-only callers, assert assumptions rather than throwing `ArgumentException`. Prefer `Debug.Assert(value is not null)` over the null-forgiving operator (`!`).
- **Use `throw` for reachable error paths, `UnreachableException` for exhaustive switches.** When a code path might be hit at runtime, throw an exception rather than asserting. Use `throw new UnreachableException()` for default cases in exhaustive switches over closed type hierarchies. Use `PlatformNotSupportedException` (not `NotSupportedException`) for platform gaps.
- **Include actionable details in exception messages.** Use `nameof` for parameter names. Include the unsupported type or unexpected value. Never throw empty exceptions.
- **Initialize output parameters in all code paths.** When a method has `out` parameters, ensure they are initialized to a defined value in all error paths.
- **Use `ThrowIf` helpers over manual checks.** Use `ArgumentOutOfRangeException.ThrowIfNegative`, `ObjectDisposedException.ThrowIf`, `ArgumentException.ThrowIfNullOrEmpty`, etc. instead of manual `if`-then-throw patterns.
- **Challenge exception swallowing that masks unexpected errors.** When code adds `try/catch` blocks that silently discard exceptions (`catch { return null; }`, `catch { continue; }`), question whether the exception represents a truly expected, recoverable condition or an unexpected error signaling a deeper problem (race conditions, corruption, environment issues). Silently catching exceptions that "shouldn't happen" hides root causes. The default disposition should be to let unexpected exceptions propagate or fail fast so the real issue gets investigated.

### Functional Domain Modeling (Picea.Abies-Specific)

These patterns enforce the functional DDD principles in the team's `decisions.md`. Deviations require explicit user approval per `principles-enforcement.md`.

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
- **Delete dead code and unnecessary wrappers.** When the only caller of a helper changes or disappears, remove the helper. Dead code rots and confuses future readers. (See also: Boy Scout Rule.)
- **Handle `SafeHandle.IsInvalid` before `Dispose`.** Check `IsInvalid` (not null) on returned `SafeHandle`s. Get the exception **before** calling `Dispose`, since `Dispose` might clear the error state.
- **Seal classes when `Equals` uses exact type matching.** If a class implements `Equals` with `GetType() == other.GetType()` comparison, seal the class to prevent subtle inheritance bugs where a subclass equals a parent but not vice versa.
- **Use `Environment.ProcessPath` and `AppContext.BaseDirectory`.** Use these instead of `Process.GetCurrentProcess().MainModule?.FileName` and `Assembly.Location` for NativeAOT/single-file compatibility.
- **File name casing must match `.csproj` references exactly.** Linux is case-sensitive. Mismatched casing builds on Windows but breaks in CI.
- **Prefer correct-by-construction designs.** Prefer designs where invariants are enforced by the type system (smart constructors, state machines, parse-don't-validate) over runtime checks scattered across call sites. A missed validation at one call site is a silent bug; a type that can't be constructed wrong is a guarantee.

---

## Performance & Allocations

### Measurement & Evidence

- **Performance changes require benchmark evidence.** Demand BenchmarkDotNet results before accepting any change framed as an optimization. "I think this is faster" is not an engineering statement.
- **Validate with realistic inputs.** Trivial benchmarks with predictable inputs overstate gains from jump tables, branch elimination, and similar tricks. Require evidence from realistic, varied inputs that match production usage.
- **Justify binary size increases.** Changes that increase binary size require measured wall-clock improvements on real apps, not just instruction counts.
- **Avoid premature optimization with object pools and caches.** Do not introduce global caches or object pools without evidence they are needed. Prefer making the underlying operation faster first. Pools have lifetime, contention, and correctness costs that often outweigh allocation costs.

### Allocation Avoidance

- **Avoid closures and allocations in hot paths.** When a lambda captures locals creating a closure, consider using a static delegate with a state parameter (value tuple). Avoid string concatenation; use span-based operations or interpolated string handlers.
- **Pre-allocate collections when size is known.** Pass capacity to `Dictionary`, `HashSet`, `List`, `StringBuilder` constructors when the expected count is available. Avoids repeated resize allocations.
- **Structs in dictionaries need `IEquatable<T>` and `GetHashCode`.** Without these, the runtime falls back to boxing for equality comparison — every lookup allocates.
- **Avoid the Pinned Object Heap for non-permanent objects.** POH is never compacted and effectively gen2. Only use for objects that will survive as long as the process.
- **Suppress `ExecutionContext` flow for infrastructure timers.** When allocating `Timer` or similar background infrastructure, suppress EC flow (`ExecutionContext.SuppressFlow()`) to avoid capturing unrelated `AsyncLocal`s that leak memory across components.

### Code Structure for Performance

- **Place cheap checks before expensive operations.** Order conditionals so cheapest/most-common checks come first. Move expensive work after early-exit checks.
- **Allocate resources lazily where possible.** Allocate expensive resources on first use, not during initialization. Avoid forcing type initialization during startup — startup time is often the most important perf metric.
- **Extract throw helpers into `[DoesNotReturn]` methods.** Move throwing logic from error paths into separate static local functions. The JIT can then inline the success path more aggressively.
- **Avoid O(n²) patterns in collections and hot paths.** Watch for linear scans inside loops, repeated `RemoveAt` in loops. Use `RemoveAll`, single-pass restructuring, or appropriate data structures.
- **Cache repeated accessor calls in locals.** Store the result of repeated property/getter calls in a local variable when the call is non-trivial.
- **Compute constant data at compile time, not execution time.** Use `const`, `static readonly`, source generators, or `[ConstantExpected]` to push work to compile time.

### Specific API Choices

- **Use `AppContext.TryGetSwitch` with a static readonly property.** Cache `AppContext` switches in `static bool Prop { get; } = AppContext.TryGetSwitch(...)` so the JIT can dead-code-eliminate unreachable paths.
- **Do not cache `typeof` expressions.** `typeof(...)` is JITed into a constant; caching it in a field is a de-optimization. Similarly, don't store `ArrayPool<T>.Shared` in variables — it breaks devirtualization.
- **Use `CollectionsMarshal` for large value-type dictionary lookups.** Use `GetValueRefOrAddDefault` or `GetValueRefOrNullRef` to avoid copying large structs.
- **Use `sizeof` instead of `Marshal.SizeOf` for blittable structs.** `sizeof` is more correct (compile-time) and significantly faster when no marshalling is involved.
- **Use the idiomatic `(uint)index >= (uint)length` bounds check.** The JIT recognizes this pattern and elides redundant bounds checks. Slice spans before iterating to avoid per-element bounds checks.
- **Source generators must be properly incremental.** Do not store Roslyn symbols (`ISymbol`, `Compilation`) in incremental pipeline steps — they hold the entire compilation alive. Output must be deterministic with `Ordinal`-sorted lists.
- **Use `BenchmarkDotNet` for benchmarks, not stopwatch loops.** Hand-written timing is almost always wrong (no warmup, no statistical analysis, no isolation). The Performance Engineer owns benchmark suite design.

---

## API Design & Contracts

- **Parameter names matter.** Renaming a public API parameter (including case changes) is a **source breaking change** — it affects named arguments. Treat parameter renames in public APIs as breaking changes requiring an ADR.
- **Align exception types and validation order.** Validate arguments first (`ArgumentNullException`, then `ArgumentException`), then state (`InvalidOperationException`), then `ObjectDisposedException`, then perform the operation. Same exception types on all platforms.
- **`Try` APIs return `false` only for the common expected failure.** Throw for everything else (corruption, permissions, invalid arguments). `Try` methods must always throw on invalid arguments — the boolean return is for the "thing might not be there" case, not for "everything that could go wrong."
- **Don't expose mutable options after construction.** If values are captured at construction time, don't expose a mutable options object — that misleads callers into thinking they can change behavior post-construction.
- **Don't reference private field names or internal types in user-facing error messages.** Error messages are part of the public API surface.
- **Use `PlatformNotSupportedException` for platform limitations.** When an operation can't complete in the current environment but could on a different platform, throw `PNSE`. Don't impose artificial limits beyond the OS capabilities.
- **Avoid unsigned types for lengths in public APIs.** Prefer `int` or `long` for length parameters. Unsigned types create interop friction and don't actually prevent bugs.
- **Use named types instead of `ValueTuple` across file boundaries.** `ValueTuple` is fine inside a method; across module boundaries it's a readability and refactoring tax.
- **Follow the obsoletion process for deprecated APIs.** Pick the next available `SYSLIB`/project diagnostic ID, add `[Obsolete]`, and use `[EditorBrowsable(Never)]` with `[OverloadResolutionPriority(-1)]` for overload fixes.
- **Start core component changes with an ADR.** Changes to bounded-context boundaries, the AppHost topology, or the Reviewer/Architect/principles infrastructure should start with an ADR or Architect handoff before implementation.

---

## Code Style & Formatting

- **Use well-named constants instead of magic numbers.** No raw hex or decimal constants without explanation. A constant with a meaningful name and a comment is worth more than a magic number with a comment.
- **Use `var` only when the type is obvious from context.** Use explicit types for casts, method returns where the type isn't obvious from the right-hand side, and async infrastructure. **Never use `var` for numeric types** — `var x = 1` hides whether `x` is `int`, `long`, or something else.
- **Use PascalCase for constants; descriptive names for booleans.** All constant locals and fields use `PascalCase`. Boolean fields should be **positive and descriptive** (`_hasCurrent`, not `_valid`; `IsEnabled`, not `Disabled == false`).
- **Name methods to accurately reflect their behavior.** Update names when behavior changes. `Get*` implies a return value; use `Print*`/`Display*` for void. `ThrowIf...` not `ThrowExceptionIf...`. `Try*` implies a boolean return.
- **Prefer early return to reduce nesting.** Use early returns for short/error cases to avoid unnecessary nesting. Put the error case first (early return), success path last.
- **Avoid `using static` and `#region` in new code.** `using static` is costly when reading code outside an IDE (e.g., GitHub review). `#region` gets out of date quickly.
- **Place local functions at method end, fields first in types.** Local functions go at the end of the containing method. Fields are the first members declared in a type, then constructors, then public members, then private members.
- **Narrow warning suppression to smallest scope.** Avoid file-wide `#pragma` suppressions. Disable only around the specific line that triggers the warning, with a comment explaining why.
- **Use pattern matching and `is`/`or`/`and` patterns.** Prefer `is` patterns and C# pattern matching over manual type checks and comparisons. Use named parameters for boolean arguments at call sites (`SaveAsync(force: true)` not `SaveAsync(true)`).
- **Do not initialize fields to default values (CA1805).** The CLR zero-initializes fields. Explicit `= false`, `= 0`, `= null` is redundant noise.
- **Sealed classes do not need the full `Dispose` pattern.** A simple `Dispose()` is sufficient since no derived class can introduce a finalizer.

---

## Consistency with Codebase Patterns

### PR Hygiene

- **Keep PRs focused on their stated scope.** No accidental file modifications, no unrelated refactoring, no whitespace noise, no build artifacts. Each PR should serve a single purpose. Mixed concerns make review harder and increase regression risk.
- **Do large refactorings and renames in separate PRs.** Separate no-diff refactors from functional changes. Mechanical renames should be separate from logic changes — otherwise the reviewer has to mentally diff twice.
- **Merge to main first, then backport to release branches.** The squad's `decisions.md` defines the backport policy. Performance fixes typically don't meet the backport bar unless they fix a significant regression.

### Code Reuse & Deduplication

- **Extract duplicated logic into shared helper methods.** When the same pattern appears in three or more places, extract it. Two is a coincidence; three is a pattern.
- **Use existing APIs instead of creating parallel ones.** Before introducing new types, enums, or helpers, check if existing ones serve the same purpose. Fix existing utilities rather than introducing duplicates.
- **Delete dead code and unused declarations aggressively.** When removing code, also remove helper methods, enum values, function declarations, and resx strings that are no longer used. (See also: Boy Scout Rule.)

### Established Conventions

- **Store error strings in `.resx`, not inline.** Reference via the `SR` class (or the team's equivalent). When removing code that uses a resx string, delete the unused string entry.
- **Sort lists and entries alphabetically.** Lists of areas, configuration entries, resx entries, registration calls, and ref source members should be maintained in alphabetical order — it makes merge conflicts and audits easier.
- **Use `DOTNET_` prefix for environment variables.** New runtime environment variables must use `DOTNET_` exclusively. Legacy `COMPlus_` names should not be added in new features.
- **Match existing style in modified files.** The existing style in a file takes precedence over general guidelines. Do not change existing code for style alone — that's noise in the diff.

---

## Testing

These patterns complement the Reviewer charter's testing dimension and the team's Definition of Done.

- **Always add regression tests for bug fixes and behavior changes.** This is enforced by the team's `principles-enforcement.md` and is a 🔴 Must Fix in the Reviewer charter. Prefer adding `[Arguments]` test cases (TUnit's `[InlineData]` equivalent) to existing test files rather than creating new ones.
- **Use TUnit conditional/skip attributes correctly.** Use TUnit's conditional skip mechanisms (`[Skip]`, `[SkipWhen]`) rather than runtime if-checks inside test bodies.
- **Test edge cases, error paths, and all affected types.** Include empty strings, negative values, boundary conditions, Turkish 'i' (`İ`/`ı`), surrogate pairs, leap years, DST transitions. Test both `true` and `false` for boolean options. Choose inputs that can't accidentally pass if the output wasn't touched.
- **Test assertions must be specific.** Assert exact expected values (exact `Result` variant, exact byte counts), not broad conditions like "is not null." A test that passes when the production code returns garbage is worse than no test.
- **Ensure tests actually fail when the fix is reverted.** This is the most important property of a regression test. Before declaring a regression test done, revert the fix and confirm the test fails.
- **Delete flaky and low-value tests rather than patching them.** Do not add tests known to be flaky. If a test relies on fragile runtime details and cannot be made reliable, prefer deletion. A flaky test is worse than no test — it trains the team to ignore failures.
- **Make test data deterministic and culture-independent.** Create `CultureInfo` with explicit format settings. Don't rely on `CurrentCulture`. Don't rely on the wall clock — inject a clock or use a fake time provider.
- **Use `PLACEHOLDER` for test passwords.** Avoids false positives from credential scanning tools.
- **Use the Aspire AppHost for integration and E2E tests.** Per the team's `principles-enforcement.md`, this is non-negotiable. No `WebApplicationFactory`, no Testcontainers, no manual process startup. The AppHost **is** the test fixture.
- **Use `RemoteExecutor` for tests with process-wide shared state.** Tests that modify shared state (environment variables, current directory, static fields) should use `RemoteExecutor` for isolation. Avoid hardcoded paths; use `Path.GetTempFileName()` or test-specific temp directories.
- **Don't add heavy dependencies to test assemblies.** Keep test projects lean. A heavy dependency in a test project blocks that whole assembly from running on devices, WASM, or NativeAOT.
- **Catch only expected exceptions in fuzz/property tests.** Catching all exceptions masks bugs like undocumented exceptions escaping the API.
- **Use modern xUnit/TUnit patterns.** Use `Assert.*` (or TUnit's `await Assert.That(...)`) — not manual return-code-style success indicators. Use `[Test]`/`[Theory]` consistently. Prefer `ThrowsAnyAsync<OperationCanceledException>` for cancellation tests.
- **Reduce test output volume.** Avoid megabytes of console output. Use `Thread.Sleep` with fewer iterations instead of busy loops where possible.

---

## Documentation & Comments

- **Comments should explain why, not restate code.** Delete comments like `// Get the user` that just duplicate the code in English. A useful comment explains the non-obvious: a tricky invariant, a workaround for a known issue (with a link), a performance trade-off.
- **Delete or update obsolete comments when code changes.** Stale comments describing old behavior are worse than no comments — they actively mislead.
- **Track deferred work with GitHub issues and searchable TODOs.** Reference a tracking issue in `TODO` comments with a consistent prefix (e.g., `TODO(#123):`). Remove ancient `TODO`s that will never be addressed — they are clutter, not work items.
- **Don't duplicate comments on interface implementations.** Documentation comments belong on the interface definition. Use `<inheritdoc/>` on implementations. Duplicating leads to divergence over time.
- **Add XML doc comments on all new public APIs.** These seed the published API documentation. Properties should start with "Gets the ..." or "Gets or sets the ...". Do not add XML docs to test code.
- **Use SHA-specific or commit-based links in documentation.** Don't use branch-relative GitHub links — they break when files move or branches are renamed.
- **Use established terminology in user-facing text.** Do not expose internal type names, private field names, or codenames in public docs or error messages. The Tech Writer owns the terminology table in `.squad/agents/techwriter/history.md`.
- **Retain copyright headers and license information.** All source files must include the standard license header. When porting code from other projects, retain original copyright and update `THIRD-PARTY-NOTICES.TXT`.

---

## Platform & Cross-Platform

- **Use `BinaryPrimitives` for endianness-safe reads.** Use `ReadInt32LittleEndian`/`BigEndian` rather than pointer casts. Separate endianness-specific reads from target-endianness reads at the API boundary.
- **Use cross-platform vector APIs over ISA-specific intrinsics.** Prefer `Vector128/256/512.IsHardwareAccelerated` and cross-platform APIs (`.Shuffle`, `.Min`) over `Avx512BW`, `Sse2`, `AdvSimd`. Use `BitOperations` for portable bit manipulation. ISA-specific code is a maintenance burden and only justified by benchmarks showing the cross-platform version is insufficient.

---

## What This Skill Does NOT Cover

This skill is a pattern catalog, not a complete review process. The following are **out of scope** for this skill and live elsewhere:

- **The review process itself** (Steps 0-3, Holistic Assessment, Verdict Consistency Rules) — see `.squad/agents/reviewer/charter.md`
- **The squad's principles** (functional DDD, namespaces, observability, security) — see `.squad/decisions.md` and `.squad/principles-enforcement.md`
- **The Definition of Done checklist** — see `.squad/decisions.md`
- **Threat model maintenance** — see the Security Expert charter
- **Performance benchmarking infrastructure** — see the Performance Engineer charter
- **Documentation conventions and Diátaxis structure** — see the Tech Writer charter

If a pattern in this skill conflicts with the team's `decisions.md` or `principles-enforcement.md`, **the team's decisions take precedence**. This skill is reference material distilled from another codebase's conventions; the team's own decisions are the source of truth.

---

## Source & Adaptation Notes

This skill was adapted from the [dotnet/runtime code-review skill](https://github.com/dotnet/runtime/blob/main/.github/skills/code-review/SKILL.md), which was extracted from 43,000+ maintainer review comments across 6,600+ PRs.

**What was kept:** Generally applicable C#/.NET patterns for correctness, performance, API design, testing, style, and consistency that apply to any modern .NET codebase using C# 14, async/await, `Span<T>`, source generators, and BenchmarkDotNet.

**What was dropped:** dotnet/runtime-internals-specific patterns including JIT lowering, GC-EE interface vtables, ECMA-335 metadata parsing, ref/ assembly conventions, `eng/common` arcade sync, `COMPlus_` legacy variables, P/Invoke marshalling specifics, native C++ patterns, `BCL` source layout conventions, and the API approval process specific to `dotnet/runtime`.

**What was added:** Picea.Abies-specific patterns from the team's functional DDD principles — smart constructors, state machines, `Result<T, TError>`, `Option<T>`, pure domain functions, no infrastructure attributes on domain types, Aspire AppHost as the only test startup mechanism, and TUnit-specific testing patterns.

**What was reframed:** The dotnet/runtime skill embeds its review *process* directly in the SKILL file. In this squad, the review process lives in the Reviewer charter (which is always loaded), and this skill is the reference catalog that the Reviewer consults on demand. This split keeps the always-loaded context lean while making the deep pattern library available when needed.
