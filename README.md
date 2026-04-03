# Abies (/ˈa.bi.eːs/)

A full-stack **Model-View-Update (MVU)** framework for .NET — build interactive web applications with pure functions, from server-rendered HTML to client-side WebAssembly.

[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Picea.Abies)](https://www.nuget.org/packages/Picea.Abies)
[![CD](https://github.com/Picea/Abies/actions/workflows/cd.yml/badge.svg)](https://github.com/Picea/Abies/actions/workflows/cd.yml)
[![E2E Tests](https://github.com/Picea/Abies/actions/workflows/e2e.yml/badge.svg)](https://github.com/Picea/Abies/actions/workflows/e2e.yml)
[![Benchmarks](https://github.com/Picea/Abies/actions/workflows/benchmark.yml/badge.svg)](https://github.com/Picea/Abies/actions/workflows/benchmark.yml)

## Why Abies?

Abies brings the [Elm Architecture](https://guide.elm-lang.org/architecture/) to .NET with a twist: **one codebase, four render modes**. Write your UI as pure functions and deploy it however you need — static HTML, interactive server, client-side WASM, or auto (server-first with WASM handoff).

- **Pure functional architecture** — no side effects in your domain logic
- **Virtual DOM** with efficient keyed diffing and binary batch patching
- **Type-safe routing** with C# pattern matching on URL segments
- **Full-stack tracing** with OpenTelemetry (browser → server)
- **Built on [Picea](https://github.com/Picea/Picea)** — the Mealy machine kernel that powers MVU, Event Sourcing, and Actor runtimes

## Render Modes

Abies supports four render modes — the same spectrum as Blazor, but built on pure MVU:

| Mode | Initial HTML | Interactivity | Use Case |
| --- | --- | --- | --- |
| **Static** | Server | None | SEO pages, content, zero JS |
| **InteractiveServer** | Server | Server (WebSocket) | Instant interaction, no WASM download |
| **InteractiveWasm** | Server | Client (WASM) | Offline-capable, no persistent connection |
| **InteractiveAuto** | Server | Server → Client | Best UX: instant interaction + WASM handoff |

All four modes share the same `Program<TModel, TArgument>` interface. Your MVU code doesn't change — only the hosting configuration does.

```csharp
// Static — one-shot HTML, zero JavaScript
var html = Page.Render<MyApp, MyModel, Unit>(RenderMode.Static);

// InteractiveServer — patches over WebSocket
var html = Page.Render<MyApp, MyModel, Unit>(new RenderMode.InteractiveServer());

// InteractiveWasm — client-side .NET runtime
var html = Page.Render<MyApp, MyModel, Unit>(new RenderMode.InteractiveWasm());

// InteractiveAuto — server-first, transitions to WASM
var html = Page.Render<MyApp, MyModel, Unit>(new RenderMode.InteractiveAuto());
```

## Quick Start

### Using Templates (Recommended)

```bash
# Install the Abies templates
dotnet new install Picea.Abies.Templates

# Create a browser (WASM) app
dotnet new abies-browser -n MyApp

cd MyApp
dotnet run
```

### Counter Example

```csharp
using Picea.Abies;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Events;

await Runtime.Run<Counter, Arguments, Model>(new Arguments());

public record Arguments;
public record Model(int Count);

public record Increment : Message;
public record Decrement : Message;

public class Counter : Program<Model, Arguments>
{
    public static (Model, Command) Initialize(Arguments argument)
        => (new Model(0), Commands.None);

    public static (Model, Command) Transition(Model model, Message message)
        => message switch
        {
            Increment => (model with { Count = model.Count + 1 }, Commands.None),
            Decrement => (model with { Count = model.Count - 1 }, Commands.None),
            _ => (model, Commands.None)
        };

    public static Document View(Model model)
        => new("Counter",
            div([], [
                button([onclick(new Decrement())], [text("-")]),
                text(model.Count.ToString()),
                button([onclick(new Increment())], [text("+")])
            ]));

    public static Subscription Subscriptions(Model model) => SubscriptionModule.None;
}
```

## Architecture

Abies is built on the **[Picea](https://github.com/Picea/Picea)** kernel — a Mealy machine abstraction that provides the core `(State, Event) → (State, Effect)` transition function. Abies specializes this for MVU:

```text
Message
  → Transition(model, message) → (model', command)
    → Observer: View(model') → Document → Diff → Patches → Apply
    → Interpreter: command → Result<Message[], PipelineError>
        → Dispatch each feedback message (recurse)
```

The `Apply` delegate is the seam between pure Abies core and platform-specific rendering:

| Platform | Apply Implementation |
| --- | --- |
| **Browser** (`Picea.Abies.Browser`) | JS interop → mutate real DOM |
| **Server** (`Picea.Abies.Server`) | Binary batch → WebSocket → client-side JS |
| **Tests** | Capture patches for assertions |

## Subscriptions

Subscriptions let you react to external event sources without putting side effects in `Transition`:

```csharp
public record Tick : Message;
public record ViewportChanged(ViewportSize Size) : Message;
public record SocketEvent(WebSocketEvent Event) : Message;

public static Subscription Subscriptions(Model model) =>
    SubscriptionModule.Batch([
        SubscriptionModule.Every(TimeSpan.FromSeconds(1), _ => new Tick()),
        SubscriptionModule.OnResize(size => new ViewportChanged(size)),
        SubscriptionModule.WebSocket(
            new WebSocketOptions("wss://example.com/socket"),
            evt => new SocketEvent(evt))
    ]);
```

## Performance: Abies Browser vs Blazor WASM

Measured with [js-framework-benchmark](https://github.com/krausest/js-framework-benchmark) on the same machine, same session.

### Duration Benchmarks (lower is better)

Latest same-session validation (2026-04-02, AC power, local main baseline):

- Full 9-benchmark duration suite rerun against Blazor WASM (same machine/session)
- Geometric mean (total medians): 0.62x for Abies vs 1.62x for Blazor
- Duration, startup/size, and memory tables below reflect the fresh 2026-04-03 run

Details: [Render StringBuilder Pool Cap Validation (2026-04-02)](docs/investigations/render-stringbuilder-pool-cap-validation-2026-04-02.md)

<!-- BENCHMARK:DURATION:START -->
| Benchmark | Abies 2.0 | Blazor 10.0 | Delta |
| --- | --- | --- | --- |
| Create 1,000 rows | 119.5 ms | **89.5 ms** | +34% |
| Replace 1,000 rows | 124.5 ms | **106.7 ms** | +17% |
| Update every 10th row ×16 | **80.4 ms** | 93.6 ms | **−14%** |
| Select row | **13.3 ms** | 80.3 ms | **−83%** |
| Swap rows | **30.9 ms** | 93.5 ms | **−67%** |
| Remove row | **19.9 ms** | 93.9 ms | **−79%** |
| Create 10,000 rows | 1183.5 ms | **805.3 ms** | +47% |
| Append 1,000 rows | 133.0 ms | **113.0 ms** | +18% |
| Clear 1,000 rows ×8 | **18.9 ms** | 40.0 ms | **−53%** |
| **Geometric mean** | **0.62×** | 1.62× | **−38%** |
<!-- BENCHMARK:DURATION:END -->

### Startup & Size (lower is better)

| Metric | Abies 2.0 | Blazor 10.0 | Delta |
| --- | --- | --- | --- |
| First paint | **71.1 ms** | 79.4 ms | **−10%** |
| Bundle (compressed) | **116 KB** | 1,078 KB | **−89%** |
| Bundle (uncompressed) | **454 KB** | 3,400 KB | **−87%** |

### Memory (lower is better)

<!-- BENCHMARK:MEMORY:START -->
| Metric | Abies 2.0 | Blazor 10.0 | Delta |
| --- | --- | --- | --- |
| Ready memory | **35.1 MB** | 41.1 MB | **−15%** |
| Run memory | **37.2 MB** | 52.6 MB | **−29%** |
| Clear memory | 59.3 MB | **49.4 MB** | +20% |
<!-- BENCHMARK:MEMORY:END -->

> **Note:** Clear memory is higher in Abies due to lazy GC in the WASM runtime. All other metrics show Abies ahead.

📊 **[Interactive Benchmark Charts](https://picea.github.io/Abies/dev/bench/)** — Historical trends on GitHub Pages

See [docs/benchmarks.md](./docs/benchmarks.md) for details on running benchmarks locally.

## Example Application: Conduit

The repository includes **Conduit**, a full implementation of the [RealWorld](https://docs.realworld.show/) specification — a Medium.com clone demonstrating:

- User authentication (login/register)
- Article CRUD with Markdown rendering
- Comments and favorites
- User profiles and following
- Tag filtering and pagination
- **Both WASM and server-rendered** hosting modes
- **REST API** with PostgreSQL read store
- **E2E tests** with Playwright
- **.NET Aspire** orchestration for local development

```bash
# Run with .NET Aspire (recommended)
dotnet run --project Picea.Abies.Conduit.AppHost

# Or run individually
dotnet run --project Picea.Abies.Conduit.Api &
dotnet run --project Picea.Abies.Conduit.Wasm
```

## Project Structure

### Core Framework

| Project | Description |
| --- | --- |
| `Picea.Abies` | Core MVU library — virtual DOM, diffing, rendering, subscriptions |
| `Picea.Abies.Browser` | Browser runtime — WASM host, JS interop, real DOM patching |
| `Picea.Abies.Server` | Server runtime — SSR, websocket patch transport |
| `Picea.Abies.Server.Kestrel` | Kestrel integration — WebSocket endpoints, static files |
| `Picea.Abies.Templates` | `dotnet new` project templates (`abies-browser`, `abies-browser-empty`) |
| `Picea.Abies.Analyzers` | Roslyn analyzers for compile-time HTML checks |

### Sample Applications

| Project | Description |
| --- | --- |
| `Picea.Abies.Counter` | Minimal counter example (shared logic) |
| `Picea.Abies.Counter.Wasm` | Counter — WASM hosting |
| `Picea.Abies.Counter.Server` | Counter — server-side hosting |
| `Picea.Abies.Conduit` | RealWorld app — domain model |
| `Picea.Abies.Conduit.App` | RealWorld app — MVU frontend |
| `Picea.Abies.Conduit.Wasm` | RealWorld app — WASM hosting |
| `Picea.Abies.Conduit.Server` | RealWorld app — server hosting |
| `Picea.Abies.Conduit.Api` | RealWorld app — REST API |
| `Picea.Abies.Presentation` | Conference presentation app |

### Infrastructure

| Project | Description |
| --- | --- |
| `Picea.Abies.Conduit.AppHost` | .NET Aspire orchestration |
| `Picea.Abies.ServiceDefaults` | Shared defaults (OpenTelemetry, health checks) |
| `Picea.Abies.Benchmarks` | BenchmarkDotNet micro-benchmarks |
| `contrib/js-framework-benchmark` | js-framework-benchmark entry point |
| `Picea.Abies.Tests` | Unit tests |
| `Picea.Abies.Server.Tests` | Server runtime tests |
| `Picea.Abies.Server.Kestrel.Tests` | Kestrel integration tests |
| `Picea.Abies.Conduit.Testing.E2E` | End-to-end Playwright tests |
| `Picea.Abies.Counter.Testing.E2E` | Counter E2E tests |

## Observability

Abies provides full-stack OpenTelemetry tracing out of the box:

- **Browser** — DOM event spans, fetch request propagation (via `abies.js`)
- **Server** — Session lifecycle, page render, message dispatch spans
- **Runtime** — Message processing, command execution, model update spans
- **OTLP export** — Browser traces export to `/otlp/v1/traces` proxy endpoint

Configurable verbosity levels: `off`, `user` (default), `debug`.

```html
<meta name="otel-verbosity" content="user">
```

See [Tutorial 8: Tracing](./docs/tutorials/08-tracing.md) for a full walkthrough.

## Requirements

- .NET 10 SDK or later
- A modern browser with WebAssembly support (for WASM mode)

## Building

```bash
dotnet build
dotnet test
```

## Documentation

See the [docs](./docs/) folder for comprehensive documentation:

### Getting Started

- [Installation](./docs/getting-started/installation.md)
- [Project Templates](./docs/getting-started/templates.md)
- [Your First App](./docs/getting-started/your-first-app.md)
- [Project Structure](./docs/getting-started/project-structure.md)

### Concepts

- [MVU Architecture](./docs/concepts/mvu-architecture.md)
- [Render Modes](./docs/concepts/render-modes.md) — Static, Server, WASM, Auto
- [Virtual DOM](./docs/concepts/virtual-dom.md)
- [Commands & Effects](./docs/concepts/commands-effects.md)
- [Subscriptions](./docs/concepts/subscriptions.md)
- [Pure Functions](./docs/concepts/pure-functions.md)

### Tutorials

1. [Counter App](./docs/tutorials/01-counter-app.md) — Basic MVU
2. [Todo List](./docs/tutorials/02-todo-list.md) — Managing collections
3. [API Integration](./docs/tutorials/03-api-integration.md) — HTTP commands
4. [Routing](./docs/tutorials/04-routing.md) — Multi-page navigation
5. [Forms](./docs/tutorials/05-forms.md) — Input handling & validation
6. [Subscriptions](./docs/tutorials/06-subscriptions.md) — Timers, resize, WebSocket
7. [Real-World App](./docs/tutorials/07-real-world-app.md) — Conduit walkthrough
8. [Tracing](./docs/tutorials/08-tracing.md) — OpenTelemetry integration

### Guides

- [Testing](./docs/guides/testing.md)
- [Debugging](./docs/guides/debugging.md)
- [Performance](./docs/guides/performance.md)
- [Deployment](./docs/guides/deployment.md)
- [Head Management](./docs/guides/head-management.md)
- [Error Handling](./docs/guides/error-handling.md)

## Contributing

We welcome contributions! Abies follows **trunk-based development** with protected main branch.

1. **Fork and clone** the repository
2. **Create a feature branch** from `main`
3. **Make your changes** with tests
4. **Ensure CI passes**: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`
5. **Submit a pull request** following our [PR template](.github/pull_request_template.md)

For detailed guidelines, see [CONTRIBUTING.md](CONTRIBUTING.md).

## The Name

Abies is a Latin name meaning "fir tree" — a species in the genus [Picea](https://github.com/Picea/Picea).

### Pronunciation

- **A**: as in "father" [a]
- **bi**: as in "machine" [bi]
- **es**: as in "they" but shorter [eːs]

**Stress**: First syllable (A-bi-es) · **Phonetic**: AH-bee-ehs

## License

[Apache 2.0](LICENSE) · Copyright Maurice Peters
