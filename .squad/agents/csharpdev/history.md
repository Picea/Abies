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
- 2026-04-04: Program-level decider validation can now express user-facing failures via `Result<Message[], Message>.Err(...)`; runtime should dispatch `decision.Error` through the same transition pipeline to preserve UX consistency (e.g., Conduit form errors still land in `HandleApiError` without fabricating `Ok(ApiError)` events).
- 2026-04-04: Completed full strict decider migration in Abies runtime and Program contract surface. `Program<TModel, TArgument>` no longer supplies default `Decide`/`IsTerminal` shims. `Runtime.Dispatch` now runs decider-first command handling (`IsTerminal` guard + `Decide` + event dispatch) under a command-level gate, removing the old command-as-event compatibility path. Validation matrix passed for core/builds/server/tests/templates/conduit unit tests; Conduit E2E has one remaining failure (`DeleteArticle_AsAuthor_ShouldNavigateToHome`, `.article-page` visibility timeout).
- 2026-04-04: `Program<TModel, TArgument> : Decider<...>` migration is directionally correct, but explicit `Decide` implementations are still required in practice for some implementers. `dotnet build Picea.Abies.sln -c Debug` currently fails with CS0535 in `Picea.Abies.Server.Tests/PageTests.cs` and `Picea.Abies.Server.Kestrel.Tests/EndpointTests.cs` because `TestCounter` lacks `Decide` (and should also define `IsTerminal` for consistency). Treat this as a true breaking migration: update all `Program` implementers, templates, and docs together.
- 2026-04-04: Promoting `Program<TModel, TArgument>` to a `Decider<...>` requires explicit `Decide` implementations on concrete Program classes. Static interface defaults in `Program` do not satisfy inherited `Decider` static abstract members for implementers. The safe default for MVU programs is pass-through decisioning (`Ok([message])`) and non-terminal behavior (`IsTerminal => false`).
- 2026-03-29: **step-forward DOES call TryApplyDebuggerSnapshot — no fix needed.** Full trace: JS sends `step-forward` → `Interop.DispatchDebuggerMessage` → `DebuggerRuntimeBridge.Execute` (calls `DebuggerMachine.StepForward`, which moves cursor AND sets `_currentModelSnapshot` from `_timelineModelSnapshots`) → `ApplyDebuggerSnapshot(Debugger.CurrentModelSnapshot)` (wired to `runtime.TryApplyDebuggerSnapshot`) → `ApplySnapshot` → `TrySetCoreState + Render`. Same pattern on Server (Session.cs:321). All 9 message types (`jump-to-entry`, `step-forward`, `step-back`, `play`, `pause`, `clear-timeline`, `get-timeline`, `export-session`, `import-session`) unconditionally trigger `TryApplyDebuggerSnapshot` after bridge execution. If the app doesn't visually update during play, the root cause is the snapshot content (imported sessions store string previews not full models), NOT missing `ApplySnapshot` calls.
- 2026-03-29: **Debugger session import/StepForward bug — root cause: abstract DU serialization.** — root cause: abstract DU serialization.** `GenerateModelSnapshot` uses `JsonSerializer.Serialize(model)` with default options. For models with an abstract discriminated union (DU) such as `Page` in Conduit, the produced JSON has NO `$type` discriminator. On import, `_timelineModelSnapshots[i].Snapshot` holds that JSON string. `TryApplyDebuggerSnapshot(string)` calls `JsonSerializer.Deserialize<TModel>(json)` which throws `NotSupportedException` (cannot instantiate abstract `Page`). The catch block silently returns `false`, so `Render()` is never called — no DOM patches — **no UI change**. Fix: annotate abstract DU roots with `[JsonPolymorphic]` + `[JsonDerivedType]`. Default `JsonSerializer` then emits/reads `$type`, enabling round-trip. Test coverage was also missing: add a test specifically for StepForward after ImportSession.
- 2026-03-27: InteractiveServer and InteractiveAuto debugger bootstrap must resolve a server-owned sibling asset under `/_abies/` instead of assuming `/debugger.js` exists in the host app. Cover this with HTTP-level tests that fetch the live asset path from both `UseAbiesStaticFiles()` and a generated server template app.
- 2026-03-27: For deterministic Debug startup behavior in WASM apps/templates, set `DebuggerConfiguration.ConfigureDebugger(new DebuggerOptions { Enabled = !debugUiOptOut })` at the top of `Program.cs` with an `ABIES_DEBUG_UI=0` opt-out. This avoids cross-run/static-config drift and keeps default-enabled behavior explicit.
- 2026-03-26: WASM templates now ship with an additional `AbiesApp.Host` project that serves the WASM AppBundle and maps `MapOtlpProxy()` so browser OTel spans can flow to a backend endpoint by default.
- 2026-03-26: Keep `AbiesApp.Host/**` excluded from the root WASM project (`Compile/Content/EmbeddedResource/None Remove`) to avoid top-level statement collisions during normal `dotnet build` of the generated WASM project.
- 2026-03-26: Template defaults for browser tracing are now enabled via `<meta name="otel-verbosity" content="user">` in template `wwwroot/index.html` files.
- 2026-03-26: Server-side template tracing defaults use OpenTelemetry with `.AddConsoleExporter()` and `MapOtlpProxy()` to provide immediate observable end-to-end trace flow.
- 2026-03-26: InteractiveServer WebSocket events now accept optional top-level `traceparent` and `tracestate` fields; `Session.RunEventLoop` restores that parent on a per-event activity so downstream runtime spans stay on the browser event trace.
- 2026-03-26: **OTEL trace propagation now complete for all render modes**. Conduit.API and PostgreSQL activity sources inherit via ServiceDefaults to all hosts; Conduit.Wasm.Host, Conduit.Server, Counter.Wasm.Host, Counter.Server all register `/otlp/v1/traces` proxy endpoint for browser spans. Browser SDK auto-discovers OTEL via `otel-verbosity` meta tag. End-to-end distributed tracing now spans browser → WebSocket/HTTP → server activity → database queries.
- 2026-03-26: **Activity source configuration unlocks Conduit distributed traces**. Use `ActivitySource.CreateActivity()` with appropriate tags for API operations, database calls, and business logic boundaries. Wire parent/child relationships remain intact through W3C `traceparent` propagation in both browser and server contexts.
- 2026-03-26: **Consumer apps now default to user-level OTEL verbosity**. Counter, UI.Demo, and SubscriptionsDemo templates ship with `<meta name="otel-verbosity" content="user">` to enable DOM events and fetch tracing by default; developers can override via `window.__otel.setVerbosity('debug')` or completely disable with `otel-verbosity="off"`.
- 2026-03-27: Live AppHost validation for Conduit WASM showed the non-JS path is ready: the page served from `https://localhost:5201` initializes browser OTEL, `ConduitProgram` emits `<meta name="otel-verbosity" content="user">`, `Conduit.Wasm.Host` maps `MapOtlpProxy()`, and a direct `POST /otlp/v1/traces` against the running host returned HTTP 200 under AppHost. If spans still do not appear, the remaining fault is on the browser/exporter side rather than backend proxy acceptance.
