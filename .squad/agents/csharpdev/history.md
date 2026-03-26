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
