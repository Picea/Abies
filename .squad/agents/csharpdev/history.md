# Senior C# Developer — History

## About This File
Project-specific learnings from C#/.NET functional domain modeling work. Read this before every session.

## Platform
- **.NET 10** (LTS), C# 14
- **TUnit** for all testing
- **Picea.Abies** namespace root

## Functional Patterns Established
*None yet — Result/Option usage, workflow signatures, capability patterns tracked here.*

## Constrained Types Created
| Type | Invariants | Module |
|---|---|---|
| *None yet* | | |

## Bounded Contexts
*None yet — context boundaries and their relationships tracked here.*

## NuGet Packages Added
| Package | Why | Version | Date |
|---|---|---|---|
| *None yet* | | | |

## Performance Observations
*None yet — benchmark results and allocation profiles tracked here.*

## EF Core Patterns & Gotchas
*None yet.*

## Domain Modeling Decisions
*None yet — aggregate boundaries, event designs, ACL patterns tracked here.*

## Conventions
*None yet — propose team-wide conventions via `.squad/decisions/inbox/`.*

## Learnings
- 2026-03-26: WASM templates now ship with an additional `AbiesApp.Host` project that serves the WASM AppBundle and maps `MapOtlpProxy()` so browser OTel spans can flow to a backend endpoint by default.
- 2026-03-26: Keep `AbiesApp.Host/**` excluded from the root WASM project (`Compile/Content/EmbeddedResource/None Remove`) to avoid top-level statement collisions during normal `dotnet build` of the generated WASM project.
- 2026-03-26: Template defaults for browser tracing are now enabled via `<meta name="otel-verbosity" content="user">` in template `wwwroot/index.html` files.
- 2026-03-26: Server-side template tracing defaults use OpenTelemetry with `.AddConsoleExporter()` and `MapOtlpProxy()` to provide immediate observable end-to-end trace flow.
- 2026-03-26: InteractiveServer WebSocket events now accept optional top-level `traceparent` and `tracestate` fields; `Session.RunEventLoop` restores that parent on a per-event activity so downstream runtime spans stay on the browser event trace.
- 2026-03-26: **OTEL trace propagation now complete for all render modes**. Conduit.API and PostgreSQL activity sources inherit via ServiceDefaults to all hosts; Conduit.Wasm.Host, Conduit.Server, Counter.Wasm.Host, Counter.Server all register `/otlp/v1/traces` proxy endpoint for browser spans. Browser SDK auto-discovers OTEL via `otel-verbosity` meta tag. End-to-end distributed tracing now spans browser → WebSocket/HTTP → server activity → database queries.
- 2026-03-26: **Activity source configuration unlocks Conduit distributed traces**. Use `ActivitySource.CreateActivity()` with appropriate tags for API operations, database calls, and business logic boundaries. Wire parent/child relationships remain intact through W3C `traceparent` propagation in both browser and server contexts.
- 2026-03-26: **Consumer apps now default to user-level OTEL verbosity**. Counter, UI.Demo, and SubscriptionsDemo templates ship with `<meta name="otel-verbosity" content="user">` to enable DOM events and fetch tracing by default; developers can override via `window.__otel.setVerbosity('debug')` or completely disable with `otel-verbosity="off"`.
