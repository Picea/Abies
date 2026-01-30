# Abies Documentation

Abies (/ˈa.bi.eːs/) is a WebAssembly library for building MVU-style web applications with .NET. This documentation covers everything from your first application to advanced API usage.

> **Target audience**: Intermediate to senior programmers new to Abies. We assume familiarity with C# and basic web development concepts.

---

## 🚀 Getting Started

New to Abies? Start here.

| Guide | Description |
|-------|-------------|
| [Installation](./getting-started/installation.md) | Prerequisites, SDK setup, and project creation |
| [Your First App](./getting-started/your-first-app.md) | Build a counter app step-by-step |
| [Project Structure](./getting-started/project-structure.md) | Understanding the Abies project layout |
| [Next Steps](./getting-started/next-steps.md) | Where to go from here |

---

## 📚 Tutorials

Hands-on, progressive learning paths.

| Tutorial | What You'll Learn |
|----------|-------------------|
| [1. Counter App](./tutorials/01-counter-app.md) | Basic MVU: model, messages, update, view |
| [2. Todo List](./tutorials/02-todo-list.md) | List management, adding/removing items |
| [3. API Integration](./tutorials/03-api-integration.md) | Commands, async operations, HTTP clients |
| [4. Routing](./tutorials/04-routing.md) | Multi-page navigation, URL parsing |
| [5. Forms](./tutorials/05-forms.md) | Form handling, input binding, validation |
| [6. Subscriptions](./tutorials/06-subscriptions.md) | Timers, browser events, WebSockets |
| [7. Real-World App](./tutorials/07-real-world-app.md) | Conduit sample walkthrough |

---

## 💡 Concepts

Deep dives into core Abies concepts.

| Concept | Description |
|---------|-------------|
| [MVU Architecture](./concepts/mvu-architecture.md) | Model-View-Update pattern explained |
| [Pure Functions](./concepts/pure-functions.md) | Why purity matters in Abies |
| [Virtual DOM](./concepts/virtual-dom.md) | How rendering and diffing work |
| [Commands & Effects](./concepts/commands-effects.md) | Side effect model and async operations |
| [Subscriptions](./concepts/subscriptions.md) | External event sources |
| [Components](./concepts/components.md) | The Element pattern for reusable UI |

---

## 📖 API Reference

Comprehensive documentation for all public types.

### Core Types
| API | Description |
|-----|-------------|
| [Program](./api/program.md) | Main application interface |
| [Element](./api/element.md) | Reusable component interface |
| [Runtime](./api/runtime.md) | Application lifecycle and execution |
| [Message](./api/message.md) | Event types and patterns |
| [Command](./api/command.md) | Side effect descriptors |
| [Subscription](./api/subscription.md) | External event subscriptions |

### Navigation & Routing
| API | Description |
|-----|-------------|
| [Url](./api/url.md) | URL representation and parsing |
| [Navigation](./api/navigation.md) | Navigation commands |
| [Route](./api/route.md) | Parser combinators and template routing |

### HTML API
| API | Description |
|-----|-------------|
| [Elements](./api/html-elements.md) | HTML element functions |
| [Attributes](./api/html-attributes.md) | HTML attribute functions |
| [Events](./api/html-events.md) | Event handlers and data types |
| [DOM Types](./api/dom-types.md) | Node, Element, Attribute, Handler |

### Utilities
| API | Description |
|-----|-------------|
| [Option](./api/option.md) | Optional value handling |

---

## 🛠 Guides

Practical how-to guides for specific tasks.

| Guide | Description |
|-------|-------------|
| [Testing](./guides/testing.md) | Testing strategies for Abies apps |
| [Debugging](./guides/debugging.md) | Debugging techniques and tools |
| [Performance](./guides/performance.md) | Optimization tips |
| [Error Handling](./guides/error-handling.md) | Error patterns and recovery |
| [Deployment](./guides/deployment.md) | Building and deploying to production |

---

## 🔧 Reference

Deep technical documentation.

| Reference | Description |
|-----------|-------------|
| [Virtual DOM Algorithm](./reference/virtual-dom-algorithm.md) | Diff and patch implementation |
| [Runtime Internals](./reference/runtime-internals.md) | How the runtime works |
| [JavaScript Interop](./reference/js-interop.md) | The JavaScript bridge |

---

## 📐 Architecture Decision Records

Design decisions and their rationale.

| ADR | Title |
|-----|-------|
| [ADR-001](./adr/ADR-001-mvu-architecture.md) | MVU Architecture |
| [ADR-002](./adr/ADR-002-pure-functional-programming.md) | Pure Functional Programming |
| [ADR-003](./adr/ADR-003-virtual-dom.md) | Virtual DOM |
| [ADR-004](./adr/ADR-004-parser-combinators.md) | Parser Combinators |
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

[View all ADRs →](./adr/README.md)
