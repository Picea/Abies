# Tutorial 8: Distributed Tracing

Learn how to use Abies's built-in distributed tracing to understand user journeys, debug issues, and monitor your application in production.

**Prerequisites:** A running Abies application, familiarity with [MVU architecture](../concepts/mvu-architecture.md)

**Time:** 20 minutes

**What you'll learn:**

- How Abies traces user interactions end-to-end
- Configuring verbosity levels
- Viewing traces in the .NET Aspire dashboard
- Correlating frontend and backend traces
- Adding custom spans to your backend

## Overview

Abies includes built-in OpenTelemetry instrumentation that provides:

- **User interaction tracing** — Every click, input, and form submission creates a trace span
- **API call correlation** — HTTP requests are automatically linked to the UI events that triggered them
- **End-to-end visibility** — Trace context propagates from browser → API → backend services
- **Verbosity control** — Configure how much detail to capture based on your needs

## Quick Start

### 1. Tracing Is Enabled by Default

Your Abies application automatically:

1. Creates spans for UI events (clicks, inputs, form submissions)
2. Propagates `traceparent` headers to API calls
3. Exports traces to `/otlp/v1/traces` (proxied to your collector)

### 2. View Traces in the Aspire Dashboard

If using .NET Aspire:

```bash
cd Picea.Abies.Conduit.AppHost
dotnet run
```

Open the Aspire dashboard (typically `https://localhost:17195`) and navigate to **Traces** to see:

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

Abies supports three verbosity levels:

| Level | What's Traced | Use Case |
| --- | --- | --- |
| `off` | Nothing | Disable tracing entirely |
| `user` | UI events + HTTP calls | **Default** — production monitoring |
| `debug` | Everything (DOM updates, attribute changes, etc.) | Framework debugging |

### Setting Verbosity

**Method 1: Meta tag (recommended)**

```html
<meta name="otel-verbosity" content="user">
```

**Method 2: URL parameter (development)**

```
https://localhost:5209/?otel_verbosity=debug
```

**Method 3: JavaScript global**

```javascript
// Before page load
window.__OTEL_VERBOSITY = 'debug';

// At runtime (in browser console)
window.__otel.setVerbosity('debug');
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

Everything — useful when debugging the framework itself:

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

## How Trace Correlation Works

1. **User clicks a button** → Abies creates a `UI Event` span with a unique `traceId`
2. **Event triggers API call** → The `traceparent` header carries the `traceId` to the backend
3. **Backend processes request** → Creates spans under the same `traceId`
4. **Trace collector assembles** → All spans with the same `traceId` form one distributed trace

### Trace Attributes

Each `UI Event` span includes context:

| Attribute | Example | Description |
| --- | --- | --- |
| `ui.event.type` | `click` | DOM event type |
| `ui.element.tag` | `button` | HTML element tag |
| `ui.element.id` | `submit-btn` | Element ID if present |
| `ui.element.text` | `Submit Article` | Button/link text (truncated) |
| `ui.action` | `Click Button: Submit Article` | Human-readable description |

## Adding Custom Backend Spans

For deeper visibility in your .NET backend:

```csharp
using System.Diagnostics;

public static class Instrumentation
{
    public static readonly ActivitySource ActivitySource =
        new("Picea.Abies.Conduit.Api");
}

// In your endpoint or service:
using var activity = Instrumentation.ActivitySource.StartActivity("ValidateArticle");
activity?.SetTag("article.title", article.Title);

// ... validation logic

if (errors.Any())
{
    activity?.SetTag("validation.error_count", errors.Count);
    activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
}
```

## Debugging with Traces

### Finding the Root Cause

When something goes wrong:

1. Open the Aspire Dashboard → **Traces**
2. Find the failing request by status code or error
3. Click to expand the trace waterfall
4. Follow the chain: UI Event → HTTP → Backend → Database
5. Identify the failing span (usually marked red)

### Example: Debugging a Failed Submit

```
UI Event: Click Button "Publish"           ✓ 2ms
├── HTTP POST /api/articles                ✗ 502ms (500 Error)
│   └── POST /api/articles                 ✗ 500ms
│       ├── Validate article               ✓ 5ms
│       └── Insert into database           ✗ 495ms (Timeout)
```

The trace immediately shows: database timeout caused the failure.

## Performance Overhead

| Verbosity | Overhead | When to Use |
| --- | --- | --- |
| `off` | 0% | When tracing causes issues |
| `user` | ~1–2% | **Production default** |
| `debug` | ~5–10% | Development only |

## CDN vs. Shim Mode

Abies automatically handles two modes for the OpenTelemetry SDK:

**CDN mode (full SDK):** When the CDN is reachable, loads the full OpenTelemetry SDK with `ZoneContextManager`, `FetchInstrumentation`, and `UserInteractionInstrumentation`.

**Shim mode (lightweight fallback):** When the CDN is blocked (corporate firewalls, offline), falls back to a minimal ~50-line implementation that manually patches `fetch()` for context propagation. Both modes provide identical user-facing behavior and trace correlation.

Force shim mode with:

```html
<meta name="otel-cdn" content="off">
```

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

### JavaScript Runtime API

```javascript
// Get current verbosity
window.__otel.getVerbosity();  // 'user'

// Change verbosity at runtime
window.__otel.setVerbosity('debug');

// Force flush pending spans
await window.__otel.provider.forceFlush();
```

## Best Practices

### For Production

1. **Use `user` verbosity** — Captures what matters without noise
2. **Set up alerts** on error traces in your observability platform
3. **Sample if needed** — 1–10% is usually sufficient for high-traffic apps
4. **Correlate with logs** — Use `traceId` in log messages for cross-referencing

### For Development

1. **Use `debug` verbosity** when investigating DOM issues
2. **Check the Aspire dashboard** for trace visualization
3. **Use browser DevTools** Network tab alongside traces
4. **Add `?otel_verbosity=debug`** to the URL for quick toggling

## Troubleshooting

### No Traces Appearing

1. Check if OTel is initialized: `console.log(window.__otel)` in browser console
2. Verify the endpoint is reachable: Network tab → filter by `/otlp`
3. Check verbosity isn't `off`: `window.__otel.getVerbosity()`

### Traces Not Correlated

1. Verify `traceparent` header is being sent (Network tab → Headers)
2. Check backend is extracting the header (look for `Activity` in logs)
3. Ensure the same collector receives both frontend and backend traces

### CDN Loading Failures

If you see "OTel CDN disabled" or timeout errors:

1. Check network connectivity to unpkg.com
2. Add `<meta name="otel-cdn" content="off">` to use the shim
3. The shim provides identical functionality

## Next Steps

- [Guide: Performance Optimization](../guides/performance.md) — Use traces to find bottlenecks
- [Guide: Testing](../guides/testing.md) — Test tracing in integration tests
- [Guide: Debugging](../guides/debugging.md) — Combine tracing with other debugging techniques
- [Concepts: MVU Architecture](../concepts/mvu-architecture.md) — Understand the message flow

## See Also

- [OpenTelemetry Documentation](https://opentelemetry.io/docs/)
- [.NET Aspire Overview](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)