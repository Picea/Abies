# ADR-015: Tracing Verbosity Levels

**Status:** Accepted  
**Date:** 2025-01-22  
**Decision Makers:** Maurice Peters  
**Supersedes:** None  
**Superseded by:** None

## Context

Abies provides comprehensive distributed tracing via OpenTelemetry, capturing browser-side spans and propagating trace context to the .NET backend (see ADR-013). However, production tracing needs differ significantly from debugging needs:

1. **Production/End-User Tracing**: Focus on user-visible interactions (clicks, navigation, HTTP requests) for performance monitoring and user journey analysis.

2. **Debug Tracing**: Capture everything including internal framework operations (DOM mutations, virtual DOM diffing, state transitions) for troubleshooting.

3. **No Tracing**: Some deployments may want telemetry completely disabled for privacy, performance, or compliance reasons.

OpenTelemetry officially supports several mechanisms for controlling trace verbosity:

- **Sampling**: Probabilistic or rate-limited trace selection (coarse-grained)
- **Custom Samplers**: Decision-making based on span attributes
- **TracerConfigurator**: Disable/enable specific tracer providers
- **Custom SpanProcessor**: Filter spans before export

However, these mechanisms are typically configured at the SDK level. Abies needed a simpler, browser-friendly approach that:

- Works without SDK configuration
- Allows runtime toggling for debugging
- Supports multiple configuration methods (meta tags, URL params, JavaScript)
- Caches decisions for performance

## Decision

Implement a three-level verbosity system in `abies.js`:

### Verbosity Levels

| Level   | Value | What Gets Traced                         |
|---------|-------|------------------------------------------|
| `off`   | 0     | Nothing - all tracing disabled           |
| `user`  | 1     | UI Events + HTTP requests only (default) |
| `debug` | 2     | Everything including DOM mutations       |

### Configuration Priority

1. `window.__OTEL_VERBOSITY` (highest priority - programmatic)
2. `<meta name="otel-verbosity" content="...">` (HTML configuration)
3. `?otel_verbosity=...` URL parameter (quick toggle)
4. Default: `'user'` (sensible production default)

### Implementation

```javascript
const VERBOSITY_LEVELS = { off: 0, user: 1, debug: 2 };

function getVerbosity() {
    if (_cachedVerbosity !== null) return _cachedVerbosity;
    
    // Check all sources in priority order
    let level = window.__OTEL_VERBOSITY
        || document.querySelector('meta[name="otel-verbosity"]')?.content
        || new URLSearchParams(window.location.search).get('otel_verbosity')
        || 'user';
    
    _cachedVerbosity = VERBOSITY_LEVELS[level] !== undefined ? level : 'user';
    return _cachedVerbosity;
}

function shouldTrace(spanName) {
    const verbosity = getVerbosity();
    if (verbosity === 'off') return false;
    if (verbosity === 'debug') return true;
    
    // 'user' level - only trace UI events and HTTP requests
    const userLevelPrefixes = ['UI Event', 'HTTP GET', 'HTTP POST', ...];
    return userLevelPrefixes.some(p => spanName.startsWith(p));
}
```

### API Exposure

```javascript
window.__otel = {
    getVerbosity,
    setVerbosity: (v) => { _cachedVerbosity = v; window.__OTEL_VERBOSITY = v; }
};
```

## Consequences

### Positive

- **Clear separation of concerns**: Production monitoring vs. debugging
- **Zero-configuration default**: Works out of the box with sensible defaults
- **Runtime flexibility**: URL param allows quick debugging without code changes
- **Performance**: Caching avoids repeated DOM/URL parsing
- **Backward compatible**: Existing deployments continue working

### Negative

- **Not standard OpenTelemetry**: Custom implementation, not using official samplers
- **Span-name coupling**: Filtering relies on span name prefixes
- **Cache invalidation**: Requires explicit `setVerbosity()` to change at runtime

### Neutral

- Verbosity applies per-span, not per-trace (different from sampling)
- Debug level can produce significant telemetry volume

## Alternatives Considered

### Alternative 1: OpenTelemetry Custom Sampler

Use the official `Sampler` interface to make sampling decisions.

**Rejected because:**

- Requires SDK-level configuration
- Not easily toggled at runtime
- Overkill for browser-side simple filtering

### Alternative 2: TracerConfigurator

Use `TracerConfigurator` to enable/disable specific tracer providers.

**Rejected because:**

- All-or-nothing approach per tracer
- Doesn't support partial filtering (user vs debug spans)

### Alternative 3: SpanProcessor Filter

Implement a custom `SpanProcessor` that filters before export.

**Rejected because:**

- More complex implementation
- Spans are still created (performance overhead)
- Better suited for backend aggregation

### Alternative 4: Environment Variables Only

Use only `OTEL_SDK_DISABLED` or similar.

**Rejected because:**

- No granularity between user and debug levels
- Not suitable for browser environment
- Can't toggle at runtime

## Related Decisions

- [ADR-013: OpenTelemetry Integration](./ADR-013-opentelemetry.md) - Base tracing architecture
- [ADR-011: JavaScript Interop](./ADR-011-javascript-interop.md) - Runtime API exposure

## References

- [OpenTelemetry Sampling Concepts](https://opentelemetry.io/docs/concepts/sampling/)
- [OpenTelemetry Trace SDK Specification](https://opentelemetry.io/docs/specs/otel/trace/sdk/)
- [Abies Tracing Tutorial](../tutorials/08-tracing.md)
- [Abies Debugging Guide](../guides/debugging.md)
