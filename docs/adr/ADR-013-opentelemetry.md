# ADR-013: OpenTelemetry Instrumentation

**Status:** Accepted  
**Date:** 2024-01-15 (Updated: 2026-01-30)  
**Decision Makers:** Abies Core Team  
**Supersedes:** None  
**Superseded by:** None

## Context

Modern applications need observability to understand runtime behavior:

- Tracing request flows through the system
- Measuring performance of operations
- Debugging issues in production
- Understanding user behavior

Traditional approaches include:

1. Proprietary APM solutions (Application Insights, DataDog, etc.)
2. Custom logging with structured data
3. OpenTelemetry (vendor-neutral observability standard)

Abies applications, running in WebAssembly, have unique observability challenges:

- Client-side execution (no traditional server metrics)
- Async message processing
- Virtual DOM operations
- Command execution timing

## Decision

We adopt **OpenTelemetry (OTEL)** as the observability standard for Abies applications, using the .NET OpenTelemetry SDK.

### Core Implementation

The runtime's `ActivitySource` is defined privately in `Picea.Abies/Runtime.cs`. There is no
public `Instrumentation` class; the source is a private static field used internally by the
runtime loop:

```csharp
// Picea.Abies/Runtime.cs
private static readonly ActivitySource _activitySource = new("Picea.Abies.Runtime");
```

The activity source name is therefore **`"Picea.Abies.Runtime"`**. To collect these traces a
consumer adds that source to its OpenTelemetry tracer provider (see
[Enabling Tracing](#enabling-tracing) below).

### Instrumentation Points

The framework instruments the following operations:

| Component | Activity Name | Tags | Purpose |
| --- | --- | --- | --- |
| **Runtime** | `Run` | - | Main loop lifecycle |
| **Runtime** | `Message: {Type}` | `message.type`, `message.name`, message properties | Message processing with type context |
| **Runtime** | `Update` | `message.type` | Model update processing |
| **Runtime** | `Command: {Type}` | `command.type`, `command.name`, command properties | Command execution with details |
| **Runtime** | `InitCommand: {Type}` | `command.type`, `command.name`, command properties | Initial command execution |
| **DOM** | `Dom.Diff` | `dom.patch_count`, `dom.is_initial_render` | Virtual DOM diffing |
| **DOM** | `Dom.Apply` | `dom.patch_type` | Patch application |
| **Subscriptions** | `Subscription.StartAll` | - | Initial subscription setup |
| **Subscriptions** | `Subscription.Update` | - | Subscription diffing |
| **Subscriptions** | `Subscription.StopAll` | - | Teardown |
| **Subscriptions** | `Subscription.Start` | `subscription.key` | Single subscription start |
| **Subscriptions** | `Subscription.Stop` | `subscription.key` | Single subscription stop |

### Application-Level Instrumentation

Applications can add detailed tracing for their commands. Example from Conduit:

| Command | Activity Name | Tags | Purpose |
| --- | --- | --- | --- |
| `LoginCommand` | `Auth: Login` | `auth.email`, `auth.success`, `auth.username`, `auth.error` | User authentication |
| `LoadArticlesCommand` | `Articles: Load` | `articles.tag`, `articles.author`, `articles.limit`, `articles.offset`, `articles.count`, `articles.total` | Article list loading |
| `ToggleFavoriteCommand` | `Article: Favorite/Unfavorite` | `article.slug`, `article.was_favorited`, `article.action`, `article.favorites_count` | Favorite toggle |

<a id="enabling-tracing"></a>

### Enabling Tracing

There is **no in-framework helper** for enabling tracing (no `Telemetry`/`EnableConsoleTracing`/
`ConsoleTraceOptions` API exists). Tracing is enabled through the standard .NET OpenTelemetry
configuration on whichever host process collects the traces.

**Server / host process** — the host (e.g. the WASM bundle host, or the Conduit API) configures
OpenTelemetry directly. The Abies project templates wire it up in `Program.cs`, adding the
relevant sources and a console exporter for local development:

```csharp
// Picea.Abies.Templates/templates/abies-browser/AbiesApp.Host/Program.cs
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
    .WithTracing(tracing => tracing
        .AddSource("Picea.Abies.Server.Kestrel.OtlpProxy")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());
```

For Aspire-hosted services, `Picea.Abies.Conduit.ServiceDefaults/Extensions.cs`
(`AddServiceDefaults`) configures tracing, metrics and logs and exports via OTLP using the
endpoint env vars that Aspire injects (`OTEL_EXPORTER_OTLP_*`). It registers the runtime/app
sources explicitly:

```csharp
// Picea.Abies.Conduit.ServiceDefaults/Extensions.cs (AddServiceDefaults)
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(builder.Environment.ApplicationName))
    .WithTracing(t => t
        .AddSource("Picea.Abies")
        .AddSource("Picea.Abies.Conduit.Api")
        .AddSource("Picea.Abies.Conduit.ReadStore.PostgreSQL")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
```

> **Note:** the framework's runtime `ActivitySource` is named `"Picea.Abies.Runtime"`. A tracer
> provider must `AddSource("Picea.Abies.Runtime")` to capture the runtime spans listed above.

**Browser / WebAssembly** — there is no C# console-tracing API. Browser-side tracing is provided
by an optional JavaScript OpenTelemetry module (`abies-otel.js`) that `abies.js` loads
dynamically when configured via a `<meta>` tag, a URL parameter, or `window.__otel`. When loaded,
it emits spans for UI events and DOM batches and forwards them over OTLP/HTTP to the proxy
endpoint (see [Browser OTLP Proxy](#browser-otlp-proxy)); when not configured it stays a no-op
shim and no traces are produced.

### JavaScript Tracing

The JavaScript runtime (`abies.js`) uses a selective tracing approach:

**Traced (high-value operations):**

- `setAppContent` - Initial render
- `addChildHtml` - Adding elements
- `replaceChildHtml` - Element replacement
- `pushState`, `replaceState`, `load` - Navigation
- `UI {action}` - User interaction events (click, input, submit, keydown)

**UI Interaction Span Attributes:**

When a user interacts with the UI (clicks, types, submits), spans include rich context:

| Attribute | Description | Example |
| --- | --- | --- |
| `ui.event.type` | DOM event type | `click`, `input`, `submit` |
| `ui.element.tag` | HTML element tag | `button`, `a`, `input` |
| `ui.element.id` | Element ID if present | `login-btn` |
| `ui.element.text` | Element text content (truncated) | `Sign In` |
| `ui.element.classes` | CSS classes | `btn btn-primary` |
| `ui.element.aria_label` | Accessibility label | `Submit form` |
| `ui.action` | Human-readable action | `Click Button: Sign In` |
| `abies.message_id` | Internal message handler ID | `handler-123` |

**Not traced (high-frequency/low-value):**

- Attribute updates (`updateAttribute`, `addAttribute`, `removeAttribute`)
- Text updates (`updateTextContent`)
- Document title changes (`setTitle`)
- LocalStorage operations

### Browser OTLP Proxy

Browsers cannot send traces directly to the Aspire dashboard due to CORS restrictions
and the inability to use gRPC from browser JavaScript. The Conduit API provides an
OTLP proxy endpoint at `/otlp/v1/traces` that:

1. Accepts OTLP/HTTP traces from the browser (JSON or protobuf)
2. **Converts JSON to protobuf** for Aspire 13.x compatibility (see below)
3. Adds CORS headers for cross-origin requests
4. Forwards traces to the Aspire dashboard's HTTP OTLP endpoint

#### JSON-to-Protobuf Conversion

Aspire 13.x OTLP/HTTP endpoint **only accepts `application/x-protobuf`** format, not JSON.
Since browser JavaScript cannot easily generate protobuf binary data, the API proxy includes
a converter (in `Picea.Abies.Server.Kestrel/OtlpProxyEndpoint.cs`) that:

1. Detects incoming `Content-Type: application/json` requests
2. Parses the OTLP JSON structure using `System.Text.Json`
3. Maps to protobuf message types from `IF.APM.OpenTelemetry.Proto` package
4. Serializes to protobuf binary using `Google.Protobuf`
5. Forwards with `Content-Type: application/x-protobuf`

This enables the lightweight browser shim (which sends JSON via `fetch()`) to work
seamlessly with Aspire's strict protobuf requirement.

**Note**: The official `OpenTelemetry.Exporter.OpenTelemetryProtocol` package keeps proto
types internal. We use `IF.APM.OpenTelemetry.Proto` (a third-party package) which exposes
the necessary `OpenTelemetry.Proto.*` namespaces.

#### Distributed Trace Context Propagation

To correlate browser spans with backend API spans in a single distributed trace:

1. **Browser UI Event Span** - When a user clicks a button, the `UI Event` span is created
2. **HTTP Client Span** - When the WebAssembly code makes a `fetch()` call, a child span is created
3. **traceparent Header** - The `traceparent` W3C header is injected with the trace ID and span ID
4. **Backend Span** - The API receives the header and creates spans under the same trace

**CDN Mode** (when OpenTelemetry CDN loads successfully):

- Uses `tracer.startActiveSpan()` to set the span as active in context
- `FetchInstrumentation` automatically creates child spans with correct parent
- `ZoneContextManager` propagates context through async operations

**Shim Mode** (when CDN is blocked or times out):

- Tracks `activeTraceContext` that persists briefly after span ends
- Patched `fetch()` uses this context to create child HTTP spans
- Manually injects `traceparent` header into outgoing requests

The result is a complete distributed trace showing:

```text
UI Event (Click Button: Sign In)
  └── HTTP POST /api/users/login
        └── POST /api/users/login (API server span)
              └── ... (database, other backend operations)
```

**Endpoint Resolution Priority** (for browser-compatible HTTP):

1. `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT` - Explicit traces endpoint
2. `ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL` - Aspire HTTP endpoint (preferred)
3. `DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL` - Alternative Aspire HTTP endpoint
4. `OTEL_EXPORTER_OTLP_ENDPOINT` - Only if protocol is `http/protobuf` or `http/json`
5. Default: `https://localhost:21203/v1/traces`

> **Important**: The generic `OTEL_EXPORTER_OTLP_ENDPOINT` is only used if the
> `OTEL_EXPORTER_OTLP_PROTOCOL` is explicitly set to an HTTP protocol. When Aspire
> sets this to gRPC (port 21202), the proxy correctly uses the HTTP endpoint instead.

## Consequences

### Positive

- **Vendor neutral**: Export to any OTEL-compatible backend
- **Standardized**: Industry-standard semantics and protocols
- **Automatic propagation**: Context flows through async operations
- **Low overhead**: Sampling controls performance impact
- **Rich ecosystem**: Exporters for Jaeger, Zipkin, Azure Monitor, etc.
- **Distributed tracing**: Can correlate client and server spans
- **Development friendly**: Console tracing for debugging

### Negative

- **Learning curve**: OTEL concepts (spans, baggage, context) are complex
- **WASM limitations**: Some exporters may not work in browser
- **Payload size**: Telemetry data adds to message size
- **Configuration complexity**: Sampling and export need tuning

### Neutral

- `ActivitySource` is the .NET native OTEL API
- Can be disabled in production for minimal overhead
- Integrates with .NET Aspire dashboard

## Instrumentation Guidelines

### Do Instrument

- Entry points (Initialize, Transition, View)
- Command handlers
- External calls (HTTP, storage)
- Performance-sensitive operations (DOM diff/apply)
- Error paths

### Don't Instrument

- Trivial operations (getters, simple calculations)
- High-frequency loops (would flood traces)
- Individual attribute updates
- Pure functions without side effects (unless measuring)

### Naming Conventions

- Use PascalCase for activity names
- Use dot-notation for hierarchical names: `Dom.Diff`, `Subscription.Start`
- Include component prefix for clarity
- Keep names stable (they become part of the API)

## Related Decisions

- [ADR-001: Model-View-Update Architecture](./ADR-001-mvu-architecture.md)
- [ADR-003: Virtual DOM Implementation](./ADR-003-virtual-dom.md)
- [ADR-006: Command Pattern for Side Effects](./ADR-006-command-pattern.md)
- [ADR-007: Subscriptions for External Events](./ADR-007-subscriptions.md)
- [ADR-012: Test Strategy](./ADR-012-test-strategy.md)

## References

- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [.NET ActivitySource](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs)
- [.NET Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [OTEL Semantic Conventions](https://opentelemetry.io/docs/concepts/semantic-conventions/)

### Implementation Files

- [`Picea.Abies/Runtime.cs`](../../Picea.Abies/Runtime.cs) - `ActivitySource` definition and runtime loop instrumentation (the former standalone `Instrumentation.cs`/`Telemetry.cs` files were folded into the runtime during the Picea migration)
- [`Picea.Abies/Diff.cs`](../../Picea.Abies/Diff.cs) - Virtual DOM instrumentation
- [`Picea.Abies/Subscriptions/Manager.cs`](../../Picea.Abies/Subscriptions/Manager.cs) - Subscription instrumentation
- [`Picea.Abies.Browser/wwwroot/abies.js`](../../Picea.Abies.Browser/wwwroot/abies.js) - JavaScript instrumentation
- [`Picea.Abies.Server.Kestrel/OtlpProxyEndpoint.cs`](../../Picea.Abies.Server.Kestrel/OtlpProxyEndpoint.cs) - OTLP proxy endpoint with JSON-to-protobuf conversion (the former `Picea.Abies.Conduit.Api/OtlpJsonToProtobuf.cs` converter now lives here)
- [`Picea.Abies.Conduit.Api/Program.cs`](../../Picea.Abies.Conduit.Api/Program.cs) - Conduit API OTLP wiring

## Changelog

- **2026-03 (v2 migration)**: Updated to reflect current state after Picea migration.
  - Updated all file references from `Abies/*` → `Picea.Abies/*` prefix
  - Updated `Abies/DOM/Operations.cs` → `Picea.Abies/Diff.cs` (file was renamed)
  - Updated `Abies/wwwroot/abies.js` → `Picea.Abies.Browser/wwwroot/abies.js`
  - Updated `Abies.Conduit.Api/*` → `Picea.Abies.Conduit.Api/*`
  - The former standalone `Abies/Instrumentation.cs` (and `Telemetry.cs`) were folded into
    `Picea.Abies/Runtime.cs`; there is no longer a separate `Instrumentation.cs` file, and the
    `ActivitySource` is now a private field named `"Picea.Abies.Runtime"`.
