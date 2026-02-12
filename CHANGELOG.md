# Changelog

All notable changes to Abies will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

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

[1.0.0-rc.1]: https://github.com/Picea/Abies/releases/tag/v1.0.0-rc.1
[Unreleased]: https://github.com/Picea/Abies/compare/v1.0.0-rc.1...HEAD
