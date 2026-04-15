# Abies Documentation

Abies (/ˈa.bi.eːs/) is a full-stack **Model-View-Update (MVU)** framework for .NET. Write your UI as pure functions and deploy it in four different render modes — static HTML, interactive server, client-side WASM, or auto (server-first with WASM handoff).

Built on the **[Picea](https://github.com/Picea/Picea)** kernel — a Mealy machine abstraction that powers MVU, Event Sourcing, and Actor runtimes.

> **Target audience**: Intermediate to senior .NET developers. We assume familiarity with C# and basic web development concepts.

---

## 🚀 Getting Started

New to Abies? Start here.

| Guide | Description |
| --- | --- |
| [Installation](./getting-started/installation.md) | Prerequisites, SDK setup, and package installation |
| [Project Templates](./getting-started/templates.md) | `dotnet new abies-browser` and `dotnet new abies-browser-empty` for quick scaffolding |
| [Your First App](./getting-started/your-first-app.md) | Build a counter app step-by-step |
| [Project Structure](./getting-started/project-structure.md) | Understanding the Abies project layout |
| [Next Steps](./getting-started/next-steps.md) | Where to go from here |

---

## 📚 Tutorials

Hands-on, progressive learning paths.

| Tutorial | What You'll Learn |
| --- | --- |
| [1. Counter App](./tutorials/01-counter-app.md) | Basic MVU: model, messages, transition, view |
| [2. Todo List](./tutorials/02-todo-list.md) | List management, adding/removing items |
| [3. API Integration](./tutorials/03-api-integration.md) | Commands, async operations, HTTP clients |
| [4. Routing](./tutorials/04-routing.md) | Multi-page navigation, URL parsing |
| [5. Forms](./tutorials/05-forms.md) | Form handling, input binding, validation |
| [6. Subscriptions](./tutorials/06-subscriptions.md) | Timers, browser events, WebSockets |
| [7. Real-World App](./tutorials/07-real-world-app.md) | Conduit sample walkthrough |
| [8. Tracing](./tutorials/08-tracing.md) | OpenTelemetry integration (browser + server) |

---

## 💡 Concepts

Deep dives into core Abies concepts.

| Concept | Description |
| --- | --- |
| [MVU Architecture](./concepts/mvu-architecture.md) | Model-View-Update pattern explained |
| [Render Modes](./concepts/render-modes.md) | The four render modes: Static, InteractiveServer, InteractiveWasm, InteractiveAuto |
| [Pure Functions](./concepts/pure-functions.md) | Why purity matters in Abies |
| [Virtual DOM](./concepts/virtual-dom.md) | How rendering, diffing, and binary batch patching work |
| [Commands & Effects](./concepts/commands-effects.md) | Side effect model and async operations |
| [Subscriptions](./concepts/subscriptions.md) | External event sources |
| [Components](./concepts/components.md) | The Element pattern for reusable UI |
| [The Picea Kernel](https://github.com/Picea/Picea) | The Mealy machine abstraction underneath Abies |

---

## 📖 API Reference

Comprehensive documentation for all public types.

### Core Types

| API | Description |
| --- | --- |
| [Program](./api/program.md) | Main application interface |
| [Element](./api/element.md) | Reusable component interface |
| [Runtime](./api/runtime.md) | Application lifecycle and execution |
| [Message](./api/message.md) | Event types and patterns |
| [Command](./api/command.md) | Side effect descriptors |
| [Subscription](./api/subscription.md) | External event subscriptions |

### Render Modes & Hosting

| API | Description |
| --- | --- |
| [Render Modes](./concepts/render-modes.md) | Static, InteractiveServer, InteractiveWasm, InteractiveAuto |
| [Runtime](./api/runtime.md) | Browser and server runtime entry points |
| [Deployment](./guides/deployment.md) | Hosting and production deployment guidance |

### Navigation & Routing

| API | Description |
| --- | --- |
| [Url](./api/url.md) | URL representation and parsing |
| [Navigation](./api/navigation.md) | Navigation commands |
| [Route](./api/route.md) | URL pattern matching and navigation |

### HTML API

| API | Description |
| --- | --- |
| [Elements](./api/html-elements.md) | HTML element functions |
| [Attributes](./api/html-attributes.md) | HTML attribute functions |
| [Events](./api/html-events.md) | Event handlers and data types |
| [DOM Types](./api/dom-types.md) | Node, Element, Attribute, Handler |

---

## 🛠 Guides

Practical how-to guides for specific tasks.

| Guide | Description |
| --- | --- |
| [Choosing a Render Mode](./guides/render-mode-selection.md) | When to use Static, Server, WASM, or Auto |
| [Testing](./guides/testing.md) | Testing strategies for Abies apps |
| [Debugging](./guides/debugging.md) | Debugging techniques and tools |
| [Abies DevTools: Time Travel Debugger](./guides/devtools.md) | Step through message history, replay sequences, inspect model state |
| [Performance](./guides/performance.md) | Optimization tips and benchmark methodology |
| [Error Handling](./guides/error-handling.md) | Error patterns and recovery |
| [Head Management](./guides/head-management.md) | Dynamic `<head>` content (title, meta, stylesheets) |
| [Deployment](./guides/deployment.md) | Building and deploying to production (WASM + server) |

---

## 🔧 Reference

Deep technical documentation.

| Reference | Description |
| --- | --- |
| [Browser Runtime API](./reference/browser-runtime-api.md) | `abies.js` public exports, callback contracts, and stability guarantees |
| [Virtual DOM Algorithm](./reference/virtual-dom-algorithm.md) | Diff and patch implementation |
| [JavaScript Interop](./reference/js-interop.md) | JavaScript bridge, including patch transport details |
| [Runtime Internals](./reference/runtime-internals.md) | How the MVU runtime works |

---

## 📐 Architecture Decision Records

Design decisions and their rationale.

| ADR | Title |
| --- | --- |
| [ADR-001](./adr/ADR-001-mvu-architecture.md) | MVU Architecture |
| [ADR-002](./adr/ADR-002-pure-functional-programming.md) | Pure Functional Programming |
| [ADR-003](./adr/ADR-003-virtual-dom.md) | Virtual DOM |
| [ADR-004](./adr/ADR-004-parser-combinators.md) | Parser Combinators *(Superseded)* |
| [ADR-005](./adr/ADR-005-webassembly-runtime.md) | WebAssembly Runtime |
| [ADR-006](./adr/ADR-006-command-pattern.md) | Command Pattern |
| [ADR-007](./adr/ADR-007-subscriptions.md) | Subscriptions |
| [ADR-008](./adr/ADR-008-immutable-state.md) | Immutable State |
| [ADR-009](./adr/ADR-009-sum-types.md) | Sum Types |
| [ADR-010](./adr/ADR-010-option-type.md) | Option Type |
| [ADR-011](./adr/ADR-011-javascript-interop.md) | JavaScript Interop |
| [ADR-012](./adr/ADR-012-test-strategy.md) | Test Strategy |
| [ADR-013](./adr/ADR-013-opentelemetry.md) | OpenTelemetry |
| [ADR-014](./adr/ADR-014-compile-time-ids.md) | Compile-Time IDs |
| [ADR-015](./adr/ADR-015-tracing-verbosity.md) | Tracing Verbosity |
| [ADR-016](./adr/ADR-016-keyed-dom-diffing.md) | Keyed DOM Diffing |
| [ADR-017](./adr/ADR-017-dotnet-new-templates.md) | dotnet new Templates |
| [ADR-018](./adr/ADR-018-pr-lint-only-changed-files.md) | PR Lint Check Only Changed Files |
| [ADR-019](./adr/ADR-019-trunk-based-development.md) | Trunk-Based Development |
| [ADR-020](./adr/ADR-020-benchmark-quality-gates.md) | Benchmark Quality Gates |

[View all ADRs →](./adr/README.md)

---

## 📊 Benchmarks

| Resource | Description |
| --- | --- |
| [Benchmark Guide](./benchmarks.md) | Running benchmarks locally |
| [Interactive Charts](https://picea.github.io/Abies/dev/bench/) | Historical trends on GitHub Pages |
| [Abies vs Blazor](../README.md#performance-abies-browser-vs-blazor-wasm) | Latest comparison in the README |
