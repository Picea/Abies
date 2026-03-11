# Changelog

All notable changes to Abies will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0-rc.1] - Unreleased

### ⚠️ Breaking Changes — Picea Ecosystem Migration

This release migrates Abies into the **Picea ecosystem**. The framework functionality is unchanged, but packages, namespaces, and project structure have been reorganized.

### Changed

- **Package rename**: All packages renamed from `Abies.*` to `Picea.Abies.*` (see [ADR-023](docs/adr/ADR-023-package-rename.md))
  - `Abies` → `Picea.Abies`
  - `Abies.Browser` → `Picea.Abies.Browser`
  - `Abies.Server` → `Picea.Abies.Server`
- **Namespace rename**: All namespaces follow the package rename (`using Abies;` → `using Picea.Abies;`)
- **Kernel dependency**: Now depends on `Picea` NuGet package (extracted from `Automaton`) for `Automaton<>`, `Result<>`, `Option<>`, `Decider<>`, Runtime, and Diagnostics
- **Solution structure**: Reorganized from flat `Abies.*` directories to `Picea.Abies.*` directories
- **Repository**: Moved from `MCGPPeters/Automaton` monorepo to `picea/abies` focused repository (see [ADR-022](docs/adr/ADR-022-picea-ecosystem-migration.md))

### Added

- **Four render modes**: Static, InteractiveServer, InteractiveWasm, InteractiveAuto (see [ADR-024](docs/adr/ADR-024-four-render-modes.md))
  - `Picea.Abies.Server` — SSR runtime with WebSocket-based MVU loop
  - `Picea.Abies.Server.Kestrel` — Kestrel integration with `MapAbies<>()` endpoint
  - Same `Program<TModel, TArgument>` interface works across all render modes
- **Metapackages** for backward compatibility:
  - `Abies` → forwards to `Picea.Abies` (with deprecation message)
  - `Abies.Browser` → forwards to `Picea.Abies.Browser`
  - `Abies.Server` → forwards to `Picea.Abies.Server`
- **Roslyn Analyzers**: `Picea.Abies.Analyzers` package for compile-time HTML validation
- **Routing documentation**: Pattern-matching routing guide (replaces deprecated parser combinators)
- **Architecture Decision Records**: ADR-022 (Picea Migration), ADR-023 (Package Rename), ADR-024 (Four Render Modes)

### Deprecated

- **ADR-004 (Parser Combinators)**: Routing now uses plain `url.Path switch` pattern matching
- **Old package names**: `Abies`, `Abies.Browser`, `Abies.Server` are now metapackages — use `Picea.Abies.*` instead

### Migration Guide (1.x → 2.0)

1. **Update package references**:
   ```xml
   <!-- Before -->
   <PackageReference Include="Abies" />
   <PackageReference Include="Abies.Browser" />

   <!-- After -->
   <PackageReference Include="Picea.Abies" />
   <PackageReference Include="Picea.Abies.Browser" />
   ```

2. **Update using directives**:
   ```csharp
   // Before
   using Abies;
   using Abies.Html;

   // After
   using Picea.Abies;
   using Picea.Abies.Html;
   ```

3. **Add Picea kernel dependency** (if you use `Result<>`, `Option<>`, or `Automaton<>` directly):
   ```xml
   <PackageReference Include="Picea" />
   ```

4. **Routing** (if using parser combinators): Replace `Parser<Page>` chains with `url.Path switch` expressions. See the [routing guide](docs/guides/routing.md).

> **Note:** If you prefer a gradual migration, the metapackages (`Abies`, `Abies.Browser`) will continue to work for 2 major versions.

## [1.0.0-rc.2] - 2026-02-23

### Added

- **Capability-Based Command Handlers** (Issue #85)
  - `Commanding.Handler` delegate: `Task<Option<Message>> Handler(Command)` — return-based, no imperative dispatch
  - `Commanding.Pipeline.Compose(params Handler[])` (namespace `Abies.Commanding`) — compose multiple handlers into one
  - `Pipeline.For<TCommand>()` — type-safe handler factory with pattern matching
  - `Pipeline.Empty` — no-op handler for programs without side effects
  - `Runtime.Run` now accepts `params Commanding.Handler[]` — zero handlers for pure apps, multiple for complex ones

- **Result and Option Types** (DDD alignment)
  - `Result<TSuccess, TError>` type with `Ok` and `Error` cases
  - `Result.Extensions`: `Map`, `MapError`, `Bind`, LINQ `Select`/`SelectMany`, async overloads
  - `Option.Extensions`: `DefaultValue`, `DefaultWith`, `ToResult`, async overloads

- **Conduit Capability Architecture**
  - `ConduitError` hierarchy: `ValidationError`, `Unauthorized`, `UnexpectedError`
  - 18+ capability delegates returning `Result<T, ConduitError>` or `Option<T>`
  - 19 focused handler functions using Railway-Oriented Programming
  - Anti-corruption layer (ACL) adapters converting exceptions to Result at the boundary

- **Benchmark Suite**
  - RenderingBenchmarks with 9 comprehensive HTML rendering benchmarks
  - EventHandlerBenchmarks with 8 event handler creation benchmarks
  - CI/CD quality gates for throughput (105%/110%) and allocations (110%/120%)
  - Automated baseline tracking via GitHub Actions
  - E2E benchmark workflow using js-framework-benchmark
  - Separate CPU and memory benchmark charts on GitHub Pages

- **Binary Batching Protocol**
  - Blazor-inspired binary batch format replacing JSON serialization
  - `RenderBatchWriter` with LEB128 string encoding and string table deduplication
  - `JSType.MemoryView` for zero-copy WASM → JS memory transfer
  - JavaScript binary reader using `DataView` API
  - Create 1000 rows now matches Blazor performance (89ms vs 88ms)

- **Keyed DOM Diffing Improvements**
  - LIS (Longest Increasing Subsequence) algorithm for optimal move operations
  - Head/tail skip optimization for common prefix/suffix matching
  - Generic `MemoKeyEquals` for boxing-free memo key comparison
  - View cache layer for `lazy()` enabling `ReferenceEquals` bailout
  - Clear fast path (O(1) early exit for clearing all children)

- **Architecture Decision Records**
  - ADR-018: PR Lint Check Only Changed Files
  - ADR-019: Trunk-Based Development
  - ADR-020: Benchmark Quality Gates

- **Documentation**
  - Tracing tutorial (Tutorial 8)
  - Benchmarking strategy investigation document
  - Blazor performance analysis investigation document

### Changed

- **Target Framework**: .NET 10 only (dropped .NET 9 support)
- **Code Style**: Adopted expression-bodied members throughout codebase
- **WASM Optimizations**: Replaced `ConcurrentQueue<T>` with `Stack<T>` for all object pools (single-threaded WASM)
- **WASM Optimizations**: Replaced `ConcurrentDictionary<TKey, TValue>` with `Dictionary<TKey, TValue>` for handler registries
- **Conduit Application**: Major refactoring across pages and services for expression-bodied style
- **Source-Generated JSON**: Switched to source-generated JSON serialization for .NET 10 WASM trim-safety

- **Performance Optimizations (Toub-inspired)**
  - Replaced `Guid.NewGuid().ToString()` with atomic counter for event handler CommandIds
    - Event handler creation is now **10.9x faster** (209.2 ns → 19.18 ns)
    - Memory allocation reduced by **21%** per handler (224 B → 176 B)
  - Added `SearchValues<char>` fast-path for HTML encoding in DOM rendering
    - Skips expensive `HtmlEncode` when strings have no special characters
    - Large page rendering is now **8% faster** (16.7 µs → 15.4 µs)
    - Complex form rendering is now **6% faster** with **13% less memory**
  - Added `FrozenDictionary` cache for event attribute names (`data-event-{name}`)
    - Eliminates string interpolation allocation for 100+ known event types
    - Event handler memory reduced by additional **32%** (176 B → 120 B per handler)
  - Pre-allocated index string cache (256 entries) eliminates string interpolation for non-keyed children
  - StringBuilder pooling for HTML rendering reduces GC pressure

### Fixed

- **LIS Algorithm Bug**: Fixed critical bug where `ComputeLISInto` computed LIS of length 1 instead of 998 for swap benchmark — swap is now **2.7x faster** (326ms → 121ms)
- **First Paint**: Added loading placeholder for 65x faster first contentful paint (4,843ms → 74ms), now matching Blazor

### Breaking Changes

- **`Program<TModel, TArgument>` interface**: Removed `HandleCommand` method. Command handling is now decoupled from the program interface — handlers are composed at the application boundary and passed to `Runtime.Run`.
- **`Runtime.Run` signature**: Changed from `Run<T,A,M>(args)` (no command handling) to `Run<T,A,M>(args, params Commanding.Handler[])`. Existing code calling `Run` with just arguments still compiles (the `params` array is empty).
- **Command handler pattern**: The old imperative `Task HandleCommand(Command, Dispatch)` pattern is replaced by the return-based `Task<Option<Message>> Handler(Command)` delegate (in `Abies.Commanding`). Handlers return `Some(message)` or `None` instead of calling dispatch directly.
- **Removed `Match` extension methods**: `Result.Extensions.Match` and `Option.Extensions.Match` have been removed. Use native C# `switch` expressions and `is` pattern matching instead — these are canonical C# and provide the same exhaustive matching with better IDE support.

### Migration Guide (rc.1 → rc.2)

1. **Remove `HandleCommand` from your Program class** — it's no longer part of the interface.
2. **Create handler functions** using `Pipeline.For<TCommand>(async cmd => ...)` for each command type.
3. **Return `Option<Message>`** instead of calling `dispatch()`:
   - `return new Some<Message>(new MyResult(...))` — when the command produces a result
   - `return new None<Message>()` — when the command was handled but produced no message
4. **Compose handlers** and pass to `Runtime.Run`:
   ```csharp
   var handler = Pipeline.Compose(loginHandler, loadArticlesHandler);
   await Runtime.Run<MyApp, Args, Model>(args, handler);
   // Or pass handlers directly:
   await Runtime.Run<MyApp, Args, Model>(args, loginHandler, loadArticlesHandler);
   ```
5. **Programs without side effects** — just remove the handler argument:
   ```csharp
   await Runtime.Run<Counter, Args, Model>(new Args());
   ```
6. **Replace `.Match()` calls** with native C# pattern matching:
   ```csharp
   // Before:
   result.Match(ok => HandleSuccess(ok), err => HandleError(err));

   // After:
   var message = result switch
   {
       Ok<User, Error>(var user) => HandleSuccess(user),
       Error<User, Error>(var err) => HandleError(err),
       _ => throw new UnreachableException()
   };
   ```

## [1.0.0-rc.1] - 2026-02-03

### Added

- **Core Framework**
  - Model-View-Update (MVU) architecture for Blazor WebAssembly
  - Virtual DOM with efficient diffing and patching algorithm
  - Type-safe routing with parser combinators
  - Command pattern for side effects (HTTP, navigation, etc.)
  - Subscription system for external events (timers, WebSockets, browser events)
  - Pure functional architecture with immutable state

- **HTML API**
  - Complete HTML5 element coverage
  - SVG element support
  - Type-safe attributes and event handlers
  - Compile-time unique ID generation via Praefixum

- **Project Templates**
  - `dotnet new abies` — Full counter example with MVU pattern
  - `dotnet new abies-empty` — Minimal empty application

- **Documentation**
  - Getting Started guide
  - 7 progressive tutorials (Counter → Real-World App)
  - Concept deep-dives (MVU, Virtual DOM, Commands, Subscriptions)
  - Complete API reference
  - 17 Architecture Decision Records (ADRs)

- **Example Application**
  - Full RealWorld Conduit implementation
  - User authentication (login/register)
  - Article CRUD operations
  - Comments and favorites
  - User profiles and following
  - Tag filtering and pagination

### Technical Details

- Targets .NET 10
- Uses Praefixum source generator for compile-time unique IDs
- OpenTelemetry tracing support (browser and .NET)
- Apache 2.0 license

[2.0.0-rc.1]: https://github.com/Picea/Abies/compare/v1.0.0-rc.2...HEAD
[1.0.0-rc.2]: https://github.com/Picea/Abies/compare/v1.0.0-rc.1...v1.0.0-rc.2
[1.0.0-rc.1]: https://github.com/Picea/Abies/releases/tag/v1.0.0-rc.1
