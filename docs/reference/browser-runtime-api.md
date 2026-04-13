---
description: 'Browser Runtime API Reference — window.__otel public surface for OpenTelemetry control'
---

# Browser Runtime API Reference

The Abies framework exposes a browser-level OpenTelemetry API via `window.__otel` for runtime observability control. This guide documents the public surface and common usage patterns.

## Overview

`window.__otel` is initialized when `abies.js` loads (or when the OTel shim is active). It allows you to:
- **Set tracing verbosity** — set `window.__otel.verbosity` to `off`, `user` (default), or `debug` before page load
- **Configure via URL** — use `?otel-verbosity=debug` query parameter
- **Configure via meta tag** — use `<meta name="otel-verbosity" content="debug">`

**Availability:** Always present after `abies.js` loads. In browsers without OTel support, a no-op shim is used.

## Configuration

### `window.__otel.verbosity` (Property)

Set the OpenTelemetry tracing verbosity level **before page load**.

**Type:** string
**Valid values:** `"off"`, `"user"`, `"debug"`

**Behavior:**

| Level | Behavior |
|-------|----------|
| `"off"` | Tracing disabled. No spans created, no exports. Lowest overhead. |
| `"user"` (default) | Production mode. Traces UI events (click, change, submit) and HTTP calls. Recommended for reporting. |
| `"debug"` | Developer mode. Traces all DOM operations (mount, attribute change, text update). High verbosity — use only during development. |

**Configuration priority** (highest first):
1. URL query parameter: `?otel-verbosity=debug`
2. `window.__otel.verbosity` (if set before abies.js loads)
3. `<meta name="otel-verbosity" content="debug">` tag
4. Default: `"user"`

**Example (set before abies.js loads):**

```html
<script>
  window.__otel = { verbosity: 'debug' };
</script>
<script src="abies.js"></script>
```

**Example (URL parameter):**

```
https://localhost:5209/?otel-verbosity=debug
```

### `window.__otel.provider` (Property)

Access the active OpenTelemetry tracer provider (if available and CDN load succeeded).

**Type:** `TracerProvider | null`

**Use cases:**
- Advanced instrumentation — create custom spans for non-standard events
- Integration with server-side tracing — ensure client spans are correlated

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

## Programmatic Tracing

The verbosity level is set before `abies.js` loads via one of the configuration methods above. There is no runtime API to change verbosity after initialization. To use different tracing levels:

**Option 1 — Set before page load via JavaScript:**

```html
<script>
  window.__otel = { verbosity: 'debug' };
</script>
<script src="abies.js"></script>
```

**Option 2 — Use URL query parameter:**

```uri
https://localhost:5209/?otel-verbosity=debug
```

**Option 3 — Use HTML meta tag:**

```html
<meta name="otel-verbosity" content="debug">
```

## Configuration Meta Tags

Tracing behavior can be initialized via `<meta>` tags in the HTML `<head>` in addition to other configuration methods above.

**Legacy alternative:** `<meta name="abies-otel-verbosity" content="...">` (deprecated, but supported for backward compatibility)

## Configuration Meta Tags

### `otel-verbosity`

```html
<meta name="otel-verbosity" content="user">
```

Sets the initial verbosity level. Valid values: `off`, `user`, `debug`. Default: `user`.

**When to use:**
- `off` — for performance-critical pages where tracing overhead is unacceptable
- `user` — standard production setting (recommended)
- `debug` — development/staging environments for detailed tracing

**Legacy alternative:** `<meta name="abies-otel-verbosity" content="...">` (deprecated, but supported for backward compatibility)

## Verbosity Levels

### `off` Mode

**Use when:** Performance is critical, or tracing overhead is unacceptable.

**Behavior:**
- No spans created
- No exports
- Minimal runtime overhead (shim only)

**Configuration:**

```html
<meta name="otel-verbosity" content="off">
<!-- or via URL: ?otel-verbosity=off -->
```

### `user` Mode (Default)

```html
<meta name="otel-verbosity" content="off">
<!-- or via URL: ?otel-verbosity=off -->
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
