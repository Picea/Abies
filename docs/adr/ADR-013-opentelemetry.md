# ADR-013: OpenTelemetry Instrumentation

**Status:** Accepted  
**Date:** 2024-01-15  
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

Implementation in `Abies/Instrumentation.cs`:

```csharp
public static class Instrumentation
{
    public static readonly ActivitySource ActivitySource = new("Abies");
}
```

Instrumentation points in the runtime:

```csharp
// Runtime.Run - main loop
using var runActivity = Instrumentation.ActivitySource.StartActivity("Run");

// Message processing
using var messageActivity = Instrumentation.ActivitySource.StartActivity("Message");
messageActivity?.SetTag("message.type", message.GetType().FullName);

// Command handling
using (Instrumentation.ActivitySource.StartActivity("HandleCommand"))
{
    await TProgram.HandleCommand(command, Dispatch);
}
```

Guidelines from `csharp.instructions.md`:

> Always instrument the code base using OTEL using best practices

Recommended instrumentation points:

| Operation | Activity Name | Tags |
| --- | --- | --- |
| Runtime loop | `Run` | - |
| Message dispatch | `Message` | `message.type` |
| Command execution | `HandleCommand` | `command.type` |
| HTTP requests | `HttpRequest` | `http.method`, `http.url` |
| DOM diff | `DomDiff` | `patch.count` |

## Consequences

### Positive

- **Vendor neutral**: Export to any OTEL-compatible backend
- **Standardized**: Industry-standard semantics and protocols
- **Automatic propagation**: Context flows through async operations
- **Low overhead**: Sampling controls performance impact
- **Rich ecosystem**: Exporters for Jaeger, Zipkin, Azure Monitor, etc.
- **Distributed tracing**: Can correlate client and server spans

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
- Performance-sensitive operations
- Error paths

### Don't Instrument

- Trivial operations (getters, simple calculations)
- High-frequency loops (would flood traces)
- Pure functions without side effects (unless measuring)

### Naming Conventions

- Use PascalCase for activity names
- Use dot-notation for hierarchical names: `Abies.Runtime.Update`
- Keep names stable (they become part of the API)

## Alternatives Considered

### Alternative 1: Custom Logging Only

Use `ILogger` with structured logging:

```csharp
_logger.LogInformation("Processing message {MessageType}", msg.GetType());
```

- Simple to implement
- No distributed tracing
- No standardized export
- Less rich analysis

Rejected because OTEL provides more capabilities.

### Alternative 2: Proprietary APM

Use Application Insights or DataDog SDK directly:

- Rich features out of the box
- Vendor lock-in
- SDK size increases bundle
- Different APIs per vendor

Rejected because vendor neutrality is valuable.

### Alternative 3: No Instrumentation

Ship without telemetry:

- Smallest bundle size
- Zero overhead
- No production visibility
- Debugging is hard

Rejected because observability is essential for maintainability.

## Related Decisions

- [ADR-001: Model-View-Update Architecture](./ADR-001-mvu-architecture.md)
- [ADR-006: Command Pattern for Side Effects](./ADR-006-command-pattern.md)
- [ADR-012: Test Strategy](./ADR-012-test-strategy.md)

## References

- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [.NET ActivitySource](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs)
- [.NET Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [OTEL Semantic Conventions](https://opentelemetry.io/docs/concepts/semantic-conventions/)
- [`Abies/Instrumentation.cs`](../../Abies/Instrumentation.cs) - ActivitySource definition
