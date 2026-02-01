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

Implementation in `Abies/Instrumentation.cs`:

```csharp
public static class Instrumentation
{
    public const string SourceName = "Abies";
    public const string SourceVersion = "1.0.0";
    public static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
}
```

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

### Console Tracing for Development

For WebAssembly applications, the `Telemetry` class provides a development-friendly way to observe traces in the browser console:

```csharp
// Enable in Program.cs
#if DEBUG
Telemetry.EnableConsoleTracing(new ConsoleTraceOptions(
    IncludeTimestamp: true,
    IncludeDuration: true,
    IncludeTags: true,
    MinDurationMs: 0.5  // Filter out fast operations
));
#endif

await Runtime.Run<MyApp, Args, Model>(args);
```

Output appears in the browser's developer console:

```
[Abies Telemetry] Console tracing enabled for source 'Abies'
[10:30:45.123] ▶ Run
[10:30:45.130] ▶ Message
[10:30:45.135] ■ Message (5.12ms) [message.type=MyApp.ButtonClicked]
[10:30:45.136] ▶ Dom.Diff
[10:30:45.138] ■ Dom.Diff (1.89ms) [dom.patch_count=3, dom.is_initial_render=false]
```

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
a converter (`OtlpJsonToProtobuf.cs`) that:

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

- Entry points (Initialize, Update, View)
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

- [`Abies/Instrumentation.cs`](../../Abies/Instrumentation.cs) - ActivitySource definition
- [`Abies/Telemetry.cs`](../../Abies/Telemetry.cs) - Console tracing for development
- [`Abies/Runtime.cs`](../../Abies/Runtime.cs) - Runtime loop instrumentation
- [`Abies/DOM/Operations.cs`](../../Abies/DOM/Operations.cs) - Virtual DOM instrumentation
- [`Abies/Subscriptions/SubscriptionManager.cs`](../../Abies/Subscriptions/SubscriptionManager.cs) - Subscription instrumentation
- [`Abies/wwwroot/abies.js`](../../Abies/wwwroot/abies.js) - JavaScript instrumentation
- [`Abies.Conduit.Api/OtlpJsonToProtobuf.cs`](../../Abies.Conduit.Api/OtlpJsonToProtobuf.cs) - JSON to protobuf converter
- [`Abies.Conduit.Api/Program.cs`](../../Abies.Conduit.Api/Program.cs) - OTLP proxy endpoint
