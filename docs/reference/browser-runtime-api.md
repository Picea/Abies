---
description: 'Browser Runtime API Reference — window.__otel public surface for OpenTelemetry control'
---

# Browser Runtime API Reference

The Abies framework exposes a browser-level OpenTelemetry API via `window.__otel` for runtime observability control. This guide documents the public surface and common usage patterns.

## Overview

`window.__otel` is initialized when `abies.js` loads (or when the OTel shim is active). It provides methods to:
- **Control tracing verbosity** — switch between `off`, `user` (default), and `debug` modes at runtime
- **Query current state** — inspect verbosity level and provider health
- **Force flush** — ensure pending spans are exported before page unload
- **Access the provider** — for advanced instrumentation scenarios

**Availability:** Always present after `abies.js` loads. In browsers without OTel support, the shim provides a no-op implementation.

## API Reference

### `setVerbosity(level: string)`

Set the OpenTelemetry tracing verbosity level at runtime.

**Parameters:**
- `level` (string): One of `"off"`, `"user"`, `"debug"`

**Behavior:**

| Level | Behavior |
|-------|----------|
| `"off"` | Tracing disabled. No spans created, no exports. Lowest overhead. |
| `"user"` (default) | Production mode. Traces UI events (click, change, submit) and HTTP calls. Recommended for reporting. |
| `"debug"` | Developer mode. Traces all DOM operations (mount, attribute change, text update). High verbosity — use only during development. |

**Example:**

```javascript
// Switch to debug mode for a specific user interaction
window.__otel.setVerbosity('debug');

// Reproduce the issue...

// Back to user mode
window.__otel.setVerbosity('user');
```

**Interop with meta tag:**
If the page includes `<meta name="otel-verbosity" content="user">`, the initial level is set from that tag. Runtime `setVerbosity()` calls override it.

### `getVerbosity(): string`

Query the current OpenTelemetry verbosity level.

**Returns:** `"off"`, `"user"`, or `"debug"`

**Example:**

```javascript
const level = window.__otel.getVerbosity();
console.log(`OTel verbosity: ${level}`);
```

### `provider`

Access the active OpenTelemetry tracer provider (if available).

**Type:** `TracerProvider | null`

**Use cases:**
- Advanced instrumentation — create custom spans for non-standard events
- Integration with server-side tracing — ensure client spans are correlated
- Provider health checks — verify the provider is initialized and exporting

**Example (advanced):**

```javascript
const provider = window.__otel.provider;
if (provider) {
  const tracer = provider.getTracer('my-app');
  const span = tracer.startSpan('custom-operation');
  
  // Do work...
  
  span.end();
}
```

### `forceFlush(timeout?: number): Promise<boolean>`

Force a flush of all pending spans to the configured export endpoint. Useful before page unload or navigation.

**Parameters:**
- `timeout` (optional, number): Timeout in milliseconds. Default: 5000 (5 seconds).

**Returns:** Promise<boolean>
- `true` if flush succeeded within the timeout
- `false` if timeout or export failed

**Example:**

```javascript
// Before page unload, ensure spans are sent
window.addEventListener('beforeunload', async () => {
  await window.__otel.forceFlush();
});
```

**Tracing behavior:**
The `forceFlush()` call itself creates a span (verbosity dependent). In `user` mode, it's traced; in `off` mode, no span is created.

## Configuration Meta Tags

Runtime API values can be initialized via `<meta>` tags in the HTML `<head>`. Runtime calls to `setVerbosity()` override meta tag values.

### `otel-verbosity`

```html
<meta name="otel-verbosity" content="user">
```

Sets the initial verbosity level. Valid values: `off`, `user`, `debug`. Default: `user`.

**When to use:**
- `off` — for performance-critical pages where tracing overhead is unacceptable
- `user` — standard production setting (recommended)
- `debug` — development/staging environments for detailed tracing

### `otel-enabled`

```html
<meta name="otel-enabled" content="true">
```

Enable or disable OpenTelemetry entirely. If `false`, the OTel shim is loaded but no spans are created. Equivalent to `setVerbosity('off')`.

Valid values: `true`, `false`. Default: `true`.

### `otel-cdn`

```html
<meta name="otel-cdn" content="https://unpkg.com/@opentelemetry/sdk-trace-web@0.51.0/dist/documents.js">
```

Specify a custom CDN URL for the OpenTelemetry SDK. If not set, `abies.js` uses a bundled fallback or default CDN.

**When to use:**
- Use a specific OTel version for compatibility
- Load from your own CDN for air-gapped environments
- Pin to a specific release for reproducibility

### `otlp-endpoint`

```html
<meta name="otlp-endpoint" content="http://localhost:4318/v1/traces">
```

Specify the OTLP (OpenTelemetry Protocol) HTTP export endpoint. Spans are sent to this URL via POST requests.

**Default:** `/otlp/v1/traces` (local application relay)

**When to use:**
- Send to a remote Jaeger/Grafana Loki instance
- Use a custom collector URL in your infrastructure
- Test with a local OTel collector (e.g., `http://localhost:4318/v1/traces`)

**Request format:** OTLP JSON (`application/json`) with `traceparent` context propagation.

## Usage Patterns

### Development — Enable Debug Mode

During development, enable `debug` mode to see all DOM operations:

```javascript
window.__otel.setVerbosity('debug');
```

View traces in the browser DevTools Console or send to a local collector. See [Tracing Guide - Debug Mode](../tutorials/08-tracing.md#debug-mode).

### Production — Monitor User Interactions

Keep `user` mode (default) to trace only user-initiated interactions and network calls:

```javascript
window.__otel.setVerbosity('user');
const level = window.__otel.getVerbosity();
console.log(`Tracing: ${level} mode`);
```

Spans are exported to `/otlp/v1/traces` (or your configured endpoint).

### Feature Flags — Conditional Tracing

Enable tracing based on user preferences or feature flags:

```javascript
// Load feature flag (e.g., from localStorage or API)
const isDebugUser = localStorage.getItem('debug-mode') === 'true';
window.__otel.setVerbosity(isDebugUser ? 'debug' : 'user');
```

### Page Unload — Ensure Spans are Sent

Force a flush before page navigation:

```javascript
document.addEventListener('click', async (e) => {
  if (e.target.matches('a[href^="http"]')) {
    // External link - flush telemetry before navigation
    await window.__otel.forceFlush(2000);
  }
});
```

### SPA Navigation — Flush Between Pages

In single-page apps, flush after each route change:

```javascript
// Example: React Router or similar
router.beforeEach(async (to, from) => {
  if (to.path !== from.path) {
    await window.__otel.forceFlush(1000);
  }
  return true;
});
```

## Verbosity Levels

### `off` Mode

**Use when:** Performance is critical, or tracing overhead is unacceptable.

**Behavior:**
- No spans created
- No exports
- Minimal runtime overhead (shim only)

**Example:**

```javascript
window.__otel.setVerbosity('off');
```

### `user` Mode (Default)

**Use when:** Monitoring production applications or user-reported issues.

**Behavior:**
- Traces user interactions: `click`, `change`, `submit`, `focus`, `blur`
- Traces HTTP calls via `fetch` and `XMLHttpRequest`
- Creates spans with `traceparent` headers for server correlation
- Spans exported to configured OTLP endpoint

**Recommended for:**
- Production deployments
- Performance monitoring
- User issue investigation

**Example:**

```javascript
window.__otel.setVerbosity('user');
```

### `debug` Mode

**Use when:** Diagnosing framework behavior or troubleshooting rendering issues.

**Behavior:**
- Traces all user interactions (same as `user`)
- Traces all HTTP calls (same as `user`)
- **Additionally traces:**
  - DOM mount events
  - Attribute mutations
  - Text node updates
  - Child list mutations
  - Virtual DOM diffing operations

**High verbosity —** produces many spans. Recommended only for development.

**Example:**

```javascript
window.__otel.setVerbosity('debug');
// ... reproduce issue ...
window.__otel.forceFlush().then(() => {
  // Check dashboard for detailed traces
});
```

## Error Handling

### Natural Failures

If the OTel provider fails to initialize (e.g., CDN unavailable, permission denied), the shim provides no-op implementations:

```javascript
// Safe to call even if OTel initialization failed
window.__otel.setVerbosity('user');  // No-op if shim is active
const level = window.__otel.getVerbosity();  // Returns 'off' or current level
await window.__otel.forceFlush();  // Resolves immediately
```

### Export Failures

If the OTLP endpoint is unreachable, spans are accumulated in memory and retried on next batch. No exception is thrown — tracing fails silently.

**To diagnose:** Check the browser Network tab for failed POST requests to the OTLP endpoint.

### Timeout Handling

`forceFlush()` respects the timeout parameter:

```javascript
// 2-second timeout; resolves with false if spans can't be flushed in time
const flushed = await window.__otel.forceFlush(2000);
if (!flushed) {
  console.warn('Flush timeout — some spans may be lost');
}
```

## Browser Compatibility

| Feature | Chrome | Firefox | Safari | Edge |
|---------|--------|---------|--------|------|
| `setVerbosity()` | ✅ | ✅ | ✅ | ✅ |
| `getVerbosity()` | ✅ | ✅ | ✅ | ✅ |
| `provider` | ✅ | ✅ | ✅ | ✅ |
| `forceFlush()` | ✅ | ✅ | ✅ | ✅ |
| Meta tag parsing | ✅ | ✅ | ✅ | ✅ |
| `traceparent` headers | ✅ | ✅ | ✅ (iOS 14+) | ✅ |

## See Also

- [Tracing Tutorial](../tutorials/08-tracing.md) — Getting started with OpenTelemetry in Abies
- [Debugging Guide](../guides/debugging.md) — Using traces for troubleshooting
- [OpenTelemetry Specification](https://opentelemetry.io/docs/specs/otel/overview/) — Official OTel docs
- [OTLP Protocol](https://opentelemetry.io/docs/specs/otel/protocol/) — Export format specification

## Implementation Notes

**Source:** The `window.__otel` API is initialized by `abies.js` and `abies-otel.js` in `Picea.Abies.Browser/wwwroot/`.

**Tracking Issues:**
- [#214: Implement window.__otel runtime API](https://github.com/Picea/Abies/issues/214) — Full implementation including `setVerbosity`, `getVerbosity`, and `forceFlush`
- [#212: Complete OTel meta tag support](https://github.com/Picea/Abies/issues/212) — Parse and honor all meta tag configuration options
