# Tutorial: Distributed Tracing with OpenTelemetry

Learn how to use Abies's built-in distributed tracing to understand user journeys, debug issues, and monitor your application in production.

## Overview

Abies includes built-in OpenTelemetry instrumentation that provides:

- **User Interaction Tracing**: Every click, input, and form submission is traced
- **API Call Correlation**: HTTP requests are automatically linked to the UI events that triggered them
- **End-to-End Visibility**: Trace context propagates from browser → API → backend services
- **Verbosity Control**: Configure how much detail to capture based on your needs

## Prerequisites

- A running Abies application
- .NET Aspire (optional, for dashboard visualization)
- Basic understanding of the MVU architecture

## Quick Start

### 1. Enable Tracing (Default: On)

Tracing is enabled by default. Your application automatically:

1. Creates spans for UI events (clicks, inputs, form submissions)
2. Propagates `traceparent` headers to API calls
3. Exports traces to `/otlp/v1/traces` (proxied to your collector)

### 2. View Traces in Aspire Dashboard

If using .NET Aspire:

```bash
cd Abies.Conduit.AppHost
dotnet run
```

Open the Aspire dashboard (typically https://localhost:17195) and navigate to **Traces** to see:

- User interactions as parent spans
- HTTP calls as child spans
- Backend processing linked to frontend actions

### 3. Understand Trace Structure

A typical user journey trace looks like:

```
UI Event: Click Button "Submit Article"          [Browser - Abies.Web]
├── HTTP POST /api/articles                      [Browser - Abies.Web]
│   └── POST /api/articles                       [API - Abies.Conduit.Api]
│       ├── Database: INSERT article             [API - Database]
│       └── Database: INSERT tags                [API - Database]
```

## Verbosity Levels

Abies supports three verbosity levels to control tracing detail:

| Level | What's Traced | Use Case |
|-------|---------------|----------|
| `off` | Nothing | Disable tracing entirely |
| `user` | UI Events + HTTP calls | **Default** - Production monitoring, user journey analysis |
| `debug` | Everything (DOM updates, attribute changes, etc.) | Framework debugging, performance analysis |

### Setting Verbosity

**Method 1: Meta Tag (Recommended)**

```html
<!-- In your index.html -->
<meta name="otel-verbosity" content="user">
```

**Method 2: URL Parameter (Development)**

```
https://localhost:5209/?otel_verbosity=debug
```

**Method 3: JavaScript Global**

```javascript
// Before page load
window.__OTEL_VERBOSITY = 'debug';

// At runtime (in browser console)
window.__otel.setVerbosity('debug');
```

**Method 4: Check Current Verbosity**

```javascript
console.log(window.__otel.getVerbosity()); // 'user'
```

## What Each Level Traces

### `user` Level (Default)

Traces that matter for understanding user behavior:

```
✓ UI Event: Click Button "Sign In"
✓ UI Event: Input change on #email
✓ UI Event: Form Submit
✓ HTTP POST /api/users/login
✓ HTTP GET /api/articles

✗ setAppContent (DOM update)
✗ updateAttribute (attribute change)
✗ addChildHtml (DOM insertion)
```

### `debug` Level

Everything - useful when debugging the framework itself:

```
✓ UI Event: Click Button "Sign In"
✓ HTTP POST /api/users/login
✓ setAppContent                    ← DOM operations
✓ updateAttribute                  ← Attribute changes
✓ addChildHtml                     ← Child element additions
✓ replaceChildHtml                 ← Element replacements
✓ updateTextContent                ← Text updates
✓ pushState                        ← Navigation
✓ setLocalStorage                  ← Storage operations
```

## Understanding Trace Context

### How Correlation Works

1. **User clicks a button** → Abies creates a `UI Event` span with a unique `traceId`
2. **Event triggers API call** → The `traceparent` header carries the `traceId` to the backend
3. **Backend processes request** → Creates spans under the same `traceId`
4. **Trace collector assembles** → All spans with same `traceId` form one distributed trace

### Trace Attributes

Each `UI Event` span includes rich context:

| Attribute | Example | Description |
|-----------|---------|-------------|
| `ui.event.type` | `click` | DOM event type |
| `ui.element.tag` | `button` | HTML element tag |
| `ui.element.id` | `submit-btn` | Element ID if present |
| `ui.element.text` | `Submit Article` | Button/link text (truncated) |
| `ui.action` | `Click Button: Submit Article` | Human-readable description |
| `abies.message_id` | `CreateArticle` | Abies message dispatched |

## Integration with .NET Backend

### Aspire AppHost Configuration

The Aspire AppHost configures OTLP proxy automatically:

```csharp
// In Program.cs of AppHost
var frontend = builder.AddProject<Abies_Conduit>("frontend")
    .WithOtlpExporter();  // Routes /otlp/v1/traces to Aspire collector

var api = builder.AddProject<Abies_Conduit_Api>("api")
    .WithOtlpExporter();
```

### Backend Span Correlation

The API automatically extracts `traceparent` from incoming requests:

```csharp
// Incoming request has:
// traceparent: 00-abc123...-def456...-01

// API creates spans under the same trace
using var activity = ActivitySource.StartActivity("ProcessArticle");
// This span has traceId: abc123... (same as browser)
```

## Debugging with Traces

### Finding the Root Cause

When something goes wrong:

1. **Open Aspire Dashboard** → Traces
2. **Find the failing request** by status code or error
3. **Click to expand** the trace waterfall
4. **Follow the chain** from UI Event → HTTP → Backend → Database
5. **Identify the failing span** (usually marked red)

### Example: Debugging a Failed Submit

```
UI Event: Click Button "Publish"           ✓ 2ms
├── HTTP POST /api/articles                ✗ 502ms (500 Error)
│   └── POST /api/articles                 ✗ 500ms
│       ├── Validate article               ✓ 5ms
│       └── Insert into database           ✗ 495ms (Timeout)
```

The trace immediately shows: database timeout caused the failure.

### Adding Custom Spans (C#)

For more detail in your backend:

```csharp
using System.Diagnostics;

public static class Instrumentation
{
    public static readonly ActivitySource ActivitySource = new("Abies.Conduit.Api");
}

// In your code:
using var activity = Instrumentation.ActivitySource.StartActivity("ValidateArticle");
activity?.SetTag("article.title", article.Title);

// ... validation logic

if (errors.Any())
{
    activity?.SetTag("validation.error_count", errors.Count);
    activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
}
```

## Performance Considerations

### Overhead

| Verbosity | Overhead | When to Use |
|-----------|----------|-------------|
| `off` | 0% | When tracing causes issues |
| `user` | ~1-2% | **Production default** |
| `debug` | ~5-10% | Development only |

### Sampling (High-Volume Apps)

For high-traffic applications, configure sampling in your collector:

```yaml
# otel-collector-config.yaml
processors:
  probabilistic_sampler:
    sampling_percentage: 10  # Sample 10% of traces
```

Or client-side with a custom sampler (advanced).

## CDN vs Shim Mode

Abies automatically handles two modes:

### CDN Mode (Full SDK)

When unpkg CDN is reachable:
- Loads full OpenTelemetry SDK
- Uses `ZoneContextManager` for async context
- Full `FetchInstrumentation` and `UserInteractionInstrumentation`

### Shim Mode (Lightweight Fallback)

When CDN is blocked (corporate firewalls, offline):
- Minimal ~50-line implementation
- Manually patches `fetch()` for context propagation
- Exports to same OTLP endpoint

Both modes provide the same user-facing behavior and trace correlation.

## Configuration Reference

### HTML Meta Tags

```html
<!-- Disable tracing entirely -->
<meta name="otel-enabled" content="off">

<!-- Set verbosity level -->
<meta name="otel-verbosity" content="user">

<!-- Disable CDN (use shim only) -->
<meta name="otel-cdn" content="off">

<!-- Custom OTLP endpoint -->
<meta name="otlp-endpoint" content="https://my-collector:4318/v1/traces">
```

### JavaScript Globals (Before Page Load)

```javascript
window.__OTEL_DISABLED = true;          // Disable entirely
window.__OTEL_VERBOSITY = 'debug';      // Set verbosity
window.__OTEL_USE_CDN = false;          // Force shim mode
window.__OTLP_ENDPOINT = 'https://...'; // Custom endpoint
```

### Runtime API

```javascript
// Get current verbosity
window.__otel.getVerbosity();  // 'user'

// Change verbosity at runtime
window.__otel.setVerbosity('debug');

// Force flush pending spans
await window.__otel.provider.forceFlush();

// Access OTLP endpoint
console.log(window.__otel.endpoint);
```

## Best Practices

### For Production

1. **Use `user` verbosity** - captures what matters without noise
2. **Set up alerts** on error traces in your observability platform
3. **Sample if needed** - 1-10% is usually sufficient for large apps
4. **Correlate with logs** - use `traceId` in log messages

### For Development

1. **Use `debug` verbosity** when investigating DOM issues
2. **Check Aspire dashboard** for trace visualization
3. **Use browser DevTools** Network tab alongside traces
4. **Add URL parameter** `?otel_verbosity=debug` for quick toggling

### For Framework Development

When debugging Abies itself:

```javascript
// Enable maximum verbosity
window.__OTEL_VERBOSITY = 'debug';

// Verify spans are being created
// Look for: setAppContent, updateAttribute, replaceChildHtml, etc.
```

## Troubleshooting

### No Traces Appearing

1. Check if OTel is disabled: `console.log(window.__otel)`
2. Verify endpoint is reachable: Network tab → filter by `/otlp`
3. Check verbosity isn't `off`: `window.__otel.getVerbosity()`

### Traces Not Correlated

1. Verify `traceparent` header is being sent (Network tab → Headers)
2. Check backend is extracting the header (look for `Activity` in logs)
3. Ensure same collector receives both frontend and backend traces

### CDN Loading Failures

If you see "OTel CDN disabled" or timeout errors:

1. Check network connectivity to unpkg.com
2. Add `<meta name="otel-cdn" content="off">` to use shim
3. The shim provides identical functionality

## Next Steps

- [Guide: Performance Optimization](../guides/performance.md) — Use traces to find bottlenecks
- [Guide: Testing](../guides/testing.md) — Test tracing in integration tests
- [Concepts: MVU Architecture](../concepts/mvu-architecture.md) — Understand the message flow
- [Reference: Runtime](../reference/runtime.md) — Backend tracing details

## See Also

- [OpenTelemetry Documentation](https://opentelemetry.io/docs/)
- [.NET Aspire Overview](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)
