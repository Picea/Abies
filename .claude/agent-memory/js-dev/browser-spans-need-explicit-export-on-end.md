---
name: browser-spans-need-explicit-export-on-end
description: SimpleSpanProcessor does not reliably flush in the CDN ESM SDK build, so abies-otel.js exports each span explicitly when it ends
metadata:
  type: feedback
---

`abies-otel.js` calls `traceExporter.export([span], ...)` directly when a span
ends, rather than relying on `sdk.SimpleSpanProcessor` to flush it. The fetch
wrapper is guarded so `/otlp/v1/traces` does not trace or decorate its own
export request.

**Why:** Live Conduit WASM validation on 2026-03-27 showed spans being created
successfully and never leaving the browser — `SimpleSpanProcessor` from the
CDN-hosted ESM build did not flush them. Explicit export-on-end is the reliable
path for this loading strategy. The self-instrumentation guard exists because
tracing the export request produces an infinite regress of spans.

**How to apply:** Do not "clean this up" by deleting the explicit export in
favour of the processor — it looks redundant and is not. If you change span
lifecycle handling, verify end-to-end against a running AppHost rather than
trusting that spans were constructed.

Verified 2026-09-02: the explicit `traceExporter.export([span], ...)` call and
the comment about `SimpleSpanProcessor` not flushing reliably are both still in
`abies-otel.js`.

**One correction.** The original note said the AppHost `/otlp/v1/traces` proxy
"rejected browser JSON exports with HTTP 415, so browser telemetry needs the
protobuf exporter". The proxy no longer rejects JSON:
`Picea.Abies.Server.Kestrel/OtlpProxyEndpoint.cs` accepts both
`application/x-protobuf` and `application/json` and returns 415 only when the
content type is neither. The protobuf exporter choice stands as a team decision,
but the 415 is no longer the reason for it. Related:
[[otel-cdn-api-drift-needs-fallbacks]].
