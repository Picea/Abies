# Changelog

All notable changes to Abies will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
  - `dotnet new abies` - Full counter example with MVU pattern
  - `dotnet new abies-empty` - Minimal empty application

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

- Targets .NET 9.0 and .NET 10.0
- Uses Praefixum source generator for compile-time unique IDs
- OpenTelemetry tracing support (browser and .NET)
- Apache 2.0 license

## [Unreleased]

### Planned

- Benchmark quality gates for virtual DOM performance
- Additional subscription types
- Enhanced debugging tools

[1.0.0-rc.1]: https://github.com/Picea/Abies/releases/tag/v1.0.0-rc.1
[Unreleased]: https://github.com/Picea/Abies/compare/v1.0.0-rc.1...HEAD
