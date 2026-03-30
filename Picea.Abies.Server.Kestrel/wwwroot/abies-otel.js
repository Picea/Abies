// =============================================================================
// abies-otel.js — Browser-Side OpenTelemetry for Interactive Server Events
// =============================================================================
// This module is loaded dynamically by abies-server.js when OTel is enabled via:
//   <meta name="otel-verbosity" content="user">
// It creates UI-event spans in the browser and returns W3C trace context so
// InteractiveServer WebSocket events can parent server-side processing spans.
// =============================================================================

const CDN_BASE = "https://cdn.jsdelivr.net/npm";
// Use jsDelivr's +esm transform so nested imports are browser-resolvable
// (no bare specifiers or extensionless subpath imports).
const OTEL_API_URL = `${CDN_BASE}/@opentelemetry/api@1.9.0/+esm`;
const OTEL_SDK_URL = `${CDN_BASE}/@opentelemetry/sdk-trace-web@1.30.1/+esm`;
const OTEL_EXPORTER_URL = `${CDN_BASE}/@opentelemetry/exporter-trace-otlp-proto@0.57.2/+esm`;
const OTEL_RESOURCES_URL = `${CDN_BASE}/@opentelemetry/resources@1.30.1/+esm`;

let tracer = null;
let traceExporter = null;
let verbosity = "user";
let initialized = false;

const noopSpan = {
    setAttribute() { return this; },
    setStatus() { return this; },
    end() {},
    spanContext() { return { traceId: "0", spanId: "0", traceFlags: 0, traceState: null }; },
};

const noopTracer = {
    startSpan() { return noopSpan; },
};

function generateTraceparent(ctx) {
    const flags = (ctx.traceFlags || 0).toString(16).padStart(2, "0");
    return `00-${ctx.traceId}-${ctx.spanId}-${flags}`;
}

function generateTracestate(ctx) {
    const traceState = ctx.traceState;
    if (!traceState) {
        return null;
    }

    if (typeof traceState.serialize === "function") {
        return traceState.serialize();
    }

    return typeof traceState === "string"
        ? traceState
        : null;
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

function exportSpan(span) {
    if (!traceExporter || !span) {
        return;
    }

    try {
        traceExporter.export([span], () => {
            // Ignore exporter callback results; transport failures surface via console.
        });
    } catch {
    }
}

// =============================================================================
// Element Label Resolution — extracts a human-readable label from a DOM element
// =============================================================================

function resolveElementLabel(el) {
    if (!el) return "";

    const ariaLabel = el.getAttribute("aria-label");
    if (ariaLabel) return ariaLabel.trim().substring(0, 80);

    const testId = el.getAttribute("data-testid") || el.getAttribute("data-test-id");
    if (testId) return testId.trim().substring(0, 80);

    const tag = el.tagName?.toLowerCase() || "";

    if (tag === "button" || tag === "a") {
        const text = el.textContent?.trim().replace(/\s+/g, " ").substring(0, 60);
        if (text) return text;
    }

    if (tag === "input" || tag === "select" || tag === "textarea") {
        const placeholder = el.getAttribute("placeholder");
        if (placeholder) return placeholder.trim().substring(0, 60);
        const id = el.id;
        if (id) {
            try {
                const label = document.querySelector(`label[for="${id}"]`);
                const labelText = label?.textContent?.trim().replace(/\s+/g, " ").substring(0, 60);
                if (labelText) return labelText;
            } catch { }
        }
        const type = el.getAttribute("type");
        return type ? `${tag}[${type}]` : tag;
    }

    if (el.id) return `#${el.id}`;
    return tag || "unknown";
}

export function traceEvent(commandId, eventType, eventData, element) {
    if (!tracer) {
        return null;
    }

    if (verbosity === "user") {
        const interactiveEvents = [
            "click", "dblclick", "submit", "change", "input",
            "keydown", "keyup", "focus", "blur"
        ];
        if (!interactiveEvents.includes(eventType)) {
            return null;
        }
    }

    const elementLabel = resolveElementLabel(element);
    const spanName = elementLabel ? `UI: ${eventType} [${elementLabel}]` : `UI: ${eventType}`;
    const span = tracer.startSpan(spanName);
    span.setAttribute("dom.event.type", eventType);
    span.setAttribute("abies.command.id", commandId);

    if (element) {
        const tag = element.tagName?.toLowerCase();
        if (tag) span.setAttribute("ui.element.tag", tag);
        if (elementLabel) span.setAttribute("ui.element.label", elementLabel);
        const elType = element.getAttribute("type");
        if (elType) span.setAttribute("ui.element.type", elType);
        if (element.id) span.setAttribute("ui.element.id", element.id);
    }

    if (eventData) {
        try {
            const data = typeof eventData === "string" ? JSON.parse(eventData) : eventData;
            if (data.key) span.setAttribute("dom.event.key", data.key);
            if (data.value !== undefined) span.setAttribute("dom.event.value", String(data.value).substring(0, 100));
            if (data.checked !== undefined) span.setAttribute("dom.event.checked", data.checked);
        } catch {
        }
    }

    setTimeout(() => {
        span.end();
        exportSpan(span);
    }, 0);

    const context = span.spanContext();
    return {
        traceparent: generateTraceparent(context),
        tracestate: generateTracestate(context)
    };
}

export async function initialize(level = "user") {
    if (initialized) return tracer !== noopTracer;
    initialized = true;
    verbosity = level;

    try {
        const [api, sdk, exporter, resources] = await Promise.all([
            import(/* webpackIgnore: true */ OTEL_API_URL),
            import(/* webpackIgnore: true */ OTEL_SDK_URL),
            import(/* webpackIgnore: true */ OTEL_EXPORTER_URL),
            import(/* webpackIgnore: true */ OTEL_RESOURCES_URL),
        ]);

        traceExporter = new exporter.OTLPTraceExporter({
            url: `${window.location.origin}/otlp/v1/traces`
        });

        const resourceAttributes = {
            "service.name": resolveServiceName("abies.server.ui"),
            "service.namespace": "abies",
        };
        const resource = typeof resources.resourceFromAttributes === "function"
            ? resources.resourceFromAttributes(resourceAttributes)
            : new resources.Resource(resourceAttributes);
        const provider = new sdk.WebTracerProvider({ resource });
        provider.register();

        tracer = api.trace.getTracer("abies-server-browser", "1.0.0");
        return true;
    } catch (err) {
        console.warn("[abies-server-otel] Failed to load OTel SDK from CDN. Tracing disabled.", err);
        tracer = noopTracer;
        traceExporter = null;
        return false;
    }
}
