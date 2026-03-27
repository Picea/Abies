// =============================================================================
// abies-otel.js — Browser-Side OpenTelemetry Instrumentation for Abies
// =============================================================================
// This module adds OpenTelemetry distributed tracing to Abies browser apps.
// It is loaded dynamically by abies.js when OTel is configured via:
//
//   <meta name="otel-verbosity" content="user">
//   (or legacy: <meta name="abies-otel-verbosity" content="user">)
//
// Verbosity levels:
//   "off"   — No tracing (module not loaded)
//   "user"  — UI events + HTTP calls (default for production)
//   "debug" — Everything including DOM mutations and attribute changes
//
// Architecture:
//   This module hooks into abies.js's event dispatch path to create spans
//   for each UI event. It wraps window.fetch to inject W3C Trace Context
//   headers (traceparent) for browser→server correlation. Traces are
//   exported to the server's OTLP proxy endpoint at /otlp/v1/traces.
//
// CDN Loading:
//   The OTel SDK is loaded from jsDelivr CDN. If the CDN is unreachable,
//   a no-op shim is used — tracing degrades gracefully, the app still works.
//
// See also:
//   - Picea.Abies.Server.Kestrel/OtlpProxyEndpoint.cs — server proxy
//   - abies.js — core runtime (loads this module conditionally)
// =============================================================================

// =============================================================================
// CDN URLs for OpenTelemetry Web SDK
// =============================================================================
const CDN_BASE = "https://cdn.jsdelivr.net/npm";
// Use jsDelivr's +esm transform so nested imports are browser-resolvable
// (no bare specifiers or extensionless subpath imports).
const OTEL_API_URL = `${CDN_BASE}/@opentelemetry/api@1.9.0/+esm`;
const OTEL_SDK_URL = `${CDN_BASE}/@opentelemetry/sdk-trace-web@1.30.1/+esm`;
const OTEL_EXPORTER_URL = `${CDN_BASE}/@opentelemetry/exporter-trace-otlp-proto@0.57.2/+esm`;
const OTEL_RESOURCES_URL = `${CDN_BASE}/@opentelemetry/resources@1.30.1/+esm`;

// =============================================================================
// State
// =============================================================================
let tracer = null;
let traceExporter = null;
let verbosity = "user";
let initialized = false;

// =============================================================================
// No-Op Shim — used when CDN loading fails
// =============================================================================
const noopSpan = {
    setAttribute() { return this; },
    setStatus() { return this; },
    end() {},
    spanContext() { return { traceId: "0", spanId: "0", traceFlags: 0 }; },
};

const noopTracer = {
    startSpan() { return noopSpan; },
    startActiveSpan(name, fn) { return fn(noopSpan); },
};

// =============================================================================
// W3C Trace Context — traceparent header generation
// =============================================================================

/**
 * Generates a W3C traceparent header value from a span context.
 * Format: {version}-{traceId}-{spanId}-{traceFlags}
 * @param {{ traceId: string, spanId: string, traceFlags: number }} ctx
 * @returns {string} The traceparent header value.
 */
function generateTraceparent(ctx) {
    const flags = (ctx.traceFlags || 0).toString(16).padStart(2, "0");
    return `00-${ctx.traceId}-${ctx.spanId}-${flags}`;
}

function resolveServiceName(defaultName) {
    try {
        const meta =
            document.querySelector('meta[name="otel-service-name"]') ||
            document.querySelector('meta[name="abies-otel-service-name"]');
        const configuredName = meta?.content?.trim();
        return configuredName || defaultName;
    } catch {
        return defaultName;
    }
}

// =============================================================================
// Fetch Wrapper — injects traceparent for browser→server correlation
// =============================================================================

let originalFetch = null;

function exportSpan(span) {
    if (!traceExporter || !span) return;

    try {
        traceExporter.export([span], () => {
            // Ignore exporter callback results; transport failures surface via console.
        });
    } catch {
        // Swallow exporter errors to avoid breaking the app.
    }
}

function installFetchWrapper() {
    if (originalFetch) return; // already installed
    originalFetch = window.fetch;

    window.fetch = function (input, init) {
        if (!tracer) return originalFetch.call(this, input, init);

        const url = typeof input === "string" ? input : input?.url || "";
        const method = init?.method || "GET";

        try {
            const resolved = new URL(url, location.href);
            if (resolved.origin === location.origin && resolved.pathname === "/otlp/v1/traces") {
                return originalFetch.call(this, input, init);
            }
        } catch {
            // Invalid URL — let the underlying fetch handle it.
        }

        return tracer.startActiveSpan(`HTTP ${method}`, (span) => {
            span.setAttribute("http.method", method);
            span.setAttribute("http.url", url);

            // Inject traceparent header only for same-origin requests.
            // Cross-origin injection can trigger CORS preflights (traceparent
            // is not a CORS-safelisted header) and leaks trace context to
            // third parties.
            let patchedInit = init;
            try {
                const resolved = new URL(url, location.href);
                if (resolved.origin === location.origin) {
                    const ctx = span.spanContext();
                    const headers = new Headers(init?.headers || {});
                    headers.set("traceparent", generateTraceparent(ctx));
                    patchedInit = { ...init, headers };
                }
            } catch {
                // Invalid URL — skip header injection, let fetch handle the error
            }

            return originalFetch
                .call(this, input, patchedInit)
                .then((response) => {
                    span.setAttribute("http.status_code", response.status);
                    span.setStatus({
                        code: response.ok ? 1 : 2, // OK=1, ERROR=2
                        message: response.ok ? "" : `HTTP ${response.status}`,
                    });
                    span.end();
                    exportSpan(span);
                    return response;
                })
                .catch((err) => {
                    span.setStatus({ code: 2, message: err.message });
                    span.end();
                    exportSpan(span);
                    throw err;
                });
        });
    };
}

// =============================================================================
// Event Instrumentation — creates spans for DOM events
// =============================================================================

/**
 * Creates a span for a dispatched DOM event.
 * Called by abies.js's event delegation when OTel is active.
 *
 * @param {string} commandId - The Abies command ID.
 * @param {string} eventType - The DOM event type (e.g., "click").
 * @param {object} eventData - Parsed event data.
 * @returns {{ span: object, traceparent: string }|null}
 */
export function traceEvent(commandId, eventType, eventData) {
    if (!tracer) return null;

    // In "user" mode, only trace interactive events
    if (verbosity === "user") {
        const interactiveEvents = [
            "click", "dblclick", "submit", "change", "input",
            "keydown", "keyup", "focus", "blur",
        ];
        if (!interactiveEvents.includes(eventType)) return null;
    }

    const span = tracer.startSpan(`UI: ${eventType}`);
    span.setAttribute("dom.event.type", eventType);
    span.setAttribute("abies.command.id", commandId);

    if (eventData) {
        try {
            const data = typeof eventData === "string" ? JSON.parse(eventData) : eventData;
            if (data.key) span.setAttribute("dom.event.key", data.key);
            if (data.value !== undefined) span.setAttribute("dom.event.value", String(data.value).substring(0, 100));
            if (data.checked !== undefined) span.setAttribute("dom.event.checked", data.checked);
        } catch {
            // Ignore parse errors
        }
    }

    // Auto-end after a short delay (the span represents the event dispatch,
    // not the full update cycle — that's measured server-side)
    setTimeout(() => {
        span.end();
        exportSpan(span);
    }, 0);

    return {
        span,
        traceparent: generateTraceparent(span.spanContext()),
    };
}

/**
 * Traces a DOM mutation batch (debug verbosity only).
 * @param {number} patchCount - Number of patches in the batch.
 */
export function traceBatch(patchCount) {
    if (!tracer || verbosity !== "debug") return;

    const span = tracer.startSpan("DOM: applyBatch");
    span.setAttribute("dom.patch_count", patchCount);
    span.end();
    exportSpan(span);
}

// =============================================================================
// Initialization
// =============================================================================

/**
 * Initializes the OTel SDK by loading from CDN.
 * Falls back to no-op shim on failure.
 *
 * @param {string} level - Verbosity level ("user" or "debug").
 * @returns {Promise<boolean>} True if SDK loaded successfully.
 */
export async function initialize(level = "user") {
    if (initialized) return tracer !== noopTracer;
    initialized = true;
    verbosity = level;

    try {
        // Dynamic import from CDN
        const [api, sdk, exporter, resources] = await Promise.all([
            import(/* webpackIgnore: true */ OTEL_API_URL),
            import(/* webpackIgnore: true */ OTEL_SDK_URL),
            import(/* webpackIgnore: true */ OTEL_EXPORTER_URL),
            import(/* webpackIgnore: true */ OTEL_RESOURCES_URL),
        ]);

        // Determine the OTLP endpoint (same origin, proxy path)
        const otlpEndpoint = `${window.location.origin}/otlp/v1/traces`;

        // Configure the OTLP HTTP exporter
        traceExporter = new exporter.OTLPTraceExporter({
            url: otlpEndpoint,
        });

        // The browser host currently accepts protobuf OTLP, and the CDN ESM
        // path does not flush spans reliably via SimpleSpanProcessor in live
        // Conduit validation. We export each ended span explicitly instead.
        const resourceAttributes = {
            "service.name": resolveServiceName("abies.browser.ui"),
            "service.namespace": "abies",
        };
        const resource = typeof resources.resourceFromAttributes === "function"
            ? resources.resourceFromAttributes(resourceAttributes)
            : new resources.Resource(resourceAttributes);
        const provider = new sdk.WebTracerProvider({ resource });
        provider.register();

        // Get a tracer
        tracer = api.trace.getTracer("abies-browser", "1.0.0");

        // Install fetch wrapper for trace context propagation
        installFetchWrapper();

        console.info(
            `[abies-otel] Initialized (verbosity=${level}, endpoint=${otlpEndpoint})`
        );
        return true;
    } catch (err) {
        // CDN failed — use no-op shim, app continues without tracing
        console.warn("[abies-otel] Failed to load OTel SDK from CDN. Tracing disabled.", err);
        tracer = noopTracer;
        traceExporter = null;
        return false;
    }
}

/**
 * Returns whether OTel is active (SDK loaded successfully).
 * @returns {boolean}
 */
export function isActive() {
    return tracer !== null && tracer !== noopTracer;
}
