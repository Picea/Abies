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
const OTEL_API_URL = `${CDN_BASE}/@opentelemetry/api@1/build/esm/index.js`;
const OTEL_SDK_URL = `${CDN_BASE}/@opentelemetry/sdk-trace-web@1/build/esm/index.js`;
const OTEL_EXPORTER_URL = `${CDN_BASE}/@opentelemetry/exporter-trace-otlp-http@0/build/esm/index.js`;

// =============================================================================
// State
// =============================================================================
let tracer = null;
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

// =============================================================================
// Fetch Wrapper — injects traceparent for browser→server correlation
// =============================================================================

let originalFetch = null;

function installFetchWrapper() {
    if (originalFetch) return; // already installed
    originalFetch = window.fetch;

    window.fetch = function (input, init) {
        if (!tracer) return originalFetch.call(this, input, init);

        const url = typeof input === "string" ? input : input?.url || "";
        const method = init?.method || "GET";

        return tracer.startActiveSpan(`HTTP ${method}`, (span) => {
            span.setAttribute("http.method", method);
            span.setAttribute("http.url", url);

            // Inject traceparent header
            const ctx = span.spanContext();
            const headers = new Headers(init?.headers || {});
            headers.set("traceparent", generateTraceparent(ctx));

            const patchedInit = { ...init, headers };

            return originalFetch
                .call(this, input, patchedInit)
                .then((response) => {
                    span.setAttribute("http.status_code", response.status);
                    span.setStatus({
                        code: response.ok ? 1 : 2, // OK=1, ERROR=2
                        message: response.ok ? "" : `HTTP ${response.status}`,
                    });
                    span.end();
                    return response;
                })
                .catch((err) => {
                    span.setStatus({ code: 2, message: err.message });
                    span.end();
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
    setTimeout(() => span.end(), 0);

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
}

// =============================================================================
// Immediate Span Processor
// =============================================================================
// A simple span processor that exports each span immediately when it ends.
// This avoids relying on sdk.BatchSpanProcessor or sdk.Resource, which may
// not be re-exported by @opentelemetry/sdk-trace-web from the CDN build.
// =============================================================================

class ImmediateSpanProcessor {
    /**
     * @param {import('@opentelemetry/sdk-trace-base').SpanExporter | any} exporterInstance
     */
    constructor(exporterInstance) {
        this._exporter = exporterInstance;
    }

    // Called when a span is started; no-op.
    onStart(_span, _parentContext) {
        // no-op
    }

    // Called when a span ends; export it immediately.
    onEnd(span) {
        try {
            this._exporter.export([span], () => {
                // ignore export result; failures are logged by the exporter
            });
        } catch {
            // Swallow exporter errors to avoid breaking the app.
        }
    }

    shutdown() {
        return Promise.resolve();
    }

    forceFlush() {
        return Promise.resolve();
    }
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
        const [api, sdk, exporter] = await Promise.all([
            import(/* webpackIgnore: true */ OTEL_API_URL),
            import(/* webpackIgnore: true */ OTEL_SDK_URL),
            import(/* webpackIgnore: true */ OTEL_EXPORTER_URL),
        ]);

        // Determine the OTLP endpoint (same origin, proxy path)
        const otlpEndpoint = `${window.location.origin}/otlp/v1/traces`;

        // Configure the OTLP HTTP exporter
        const traceExporter = new exporter.OTLPTraceExporter({
            url: otlpEndpoint,
        });

        // Create the tracer provider and attach the immediate span processor.
        // We use ImmediateSpanProcessor instead of sdk.BatchSpanProcessor
        // because BatchSpanProcessor and Resource may not be re-exported
        // by the CDN build of @opentelemetry/sdk-trace-web.
        const provider = new sdk.WebTracerProvider();
        provider.addSpanProcessor(new ImmediateSpanProcessor(traceExporter));
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
