# Senior JavaScript Developer — History

## About This File

Project-specific learnings from JS/TS work. Read this before every session.

## Patterns Established

*None yet — this grows as the codebase takes shape.*

## Dependencies Added

| Package | Why | Date |
| ------- | --- | ---- |
| *None yet* | | |

## Performance Observations

*None yet.*

## Gotchas & Quirks

*None yet.*

## Conventions

*None yet — propose team-wide conventions via `.squad/decisions/inbox/`.*

## Learnings

- 2026-03-27: Keep runtime startup resilient by treating debugger bootstrap as optional and always wiring `Interop.Handlers` first; this isolates debugger failures from core WASM input handling.
- 2026-03-27: In WASM startup, wire `Interop.Handlers` before optional debugger bootstrap; if debugger import/mount throws first, UI can render but all input becomes non-interactive because `DispatchDomEvent` has no handler registry.
- 2026-03-27: Runtime debugger UI startup now defaults to enabled with JS-level opt-out controls in both browser and server startup paths. The resolved setting is unified as `window.__abiesDebugger.enabled` from query/meta/global sources.
- 2026-03-27: `debugger.js` mount is now config-gated and auto-invoked on module load; Release remains safe because `debugger.js` is excluded from Release bundles and server startup treats missing module as a no-op.
- 2026-03-27: Browser spans disappeared after switching to `resources.resourceFromAttributes(...)` because `@opentelemetry/resources@1.30.1/+esm` exports `Resource` but not `resourceFromAttributes`; use a compatibility fallback (`resourceFromAttributes` when present, otherwise `new Resource(...)`) to preserve `service.name` without breaking initialization.
- 2026-03-27: Set explicit browser OTel resource attributes in runtime `abies-otel.js` files so spans carry `service.name` (with optional meta override via `otel-service-name`/`abies-otel-service-name`); this prevents Aspire UI traces from collapsing into `unknown_service`.
- 2026-03-27: Completed OTEL browser export hardening by pinning CDN API/SDK/exporter versions together (not partially) to keep the protobuf export path deterministic and aligned with the live Conduit WASM decision.
- 2026-03-27: Live Conduit WASM validation showed browser spans were not exporting through `sdk.SimpleSpanProcessor` from the CDN ESM build, even though spans were created successfully.
- 2026-03-27: The AppHost-hosted `/otlp/v1/traces` path accepted `application/x-protobuf` but rejected browser JSON exports with HTTP 415 during live testing, so browser telemetry now needs the protobuf exporter rather than the JSON OTLP HTTP exporter.
- 2026-03-27: Explicitly calling `traceExporter.export([span], ...)` when browser spans end is a reliable fallback for the CDN-hosted browser SDK path; guard the fetch wrapper so `/otlp/v1/traces` does not trace or decorate its own export request.
- 2026-03-23: Added issue #160 Release asset contract gate in `Picea.Abies.Templates.Testing/TemplateBuildTests.cs`.
- Gate publishes `abies-browser` template with `dotnet publish -c Release`, locates published `abies.js` under the publish output, scans for debugger runtime marker strings, and intentionally fails pending implementation.
- TUnit filtering via `dotnet test --filter` is unsupported in this setup; targeted execution was validated through the TUnit host (`dotnet run --project ...`) and full-run output captured the expected failure message.
- 2026-03-26: Updated counter buttons in `abies-browser` and `abies-server` templates to visible `+` and `-` labels, with `ariaLabel("Increase")` and `ariaLabel("Decrease")` to keep accessible names descriptive.
- 2026-03-26: Template tests should query button role names by accessibility labels (`Increase`/`Decrease`) once symbolic labels are used, instead of relying on glyph names.
- 2026-03-26: InteractiveServer DOM event transport lives in `Picea.Abies.Server.Kestrel/wwwroot/_abies/abies-server.js`, not `Picea.Abies.Browser/wwwroot/abies.js`; WebSocket payload changes for server mode belong there.
- 2026-03-26: `abies-server.js` can opportunistically enable OTel by resolving `../abies-otel.js` relative to its own script URL; if the module is missing or disabled, tracing stays a no-op.
- 2026-03-26: The WebSocket event envelope tolerates additive top-level JSON properties; `WebSocketTransport` continues deserializing `commandId`, `eventName`, and `eventData` while ignoring optional `traceparent`.
- 2026-03-26: **OTEL trace propagation now complete for all render modes**. Browser SDK loads OTel from CDN and detects `<meta name="otel-verbosity" content="user">` in index.html to auto-initialize. Default user-level verbosity records DOM events and fetch calls; no-op if meta tag missing or set to `"off"`. All browser spans POST to `/otlp/v1/traces` on the app's origin, enabling seamless backend integration.
- 2026-03-26: **Consumer apps now default to user-level OTEL verbosity**. Counter.Wasm, UI.Demo, and SubscriptionsDemo now ship with otel-verbosity meta tag enabled. Users see immediate observable tracing without code changes; opt-in to `debug` verbosity for detailed DOM/attribute tracing during development or troubleshooting.
