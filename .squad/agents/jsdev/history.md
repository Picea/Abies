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
