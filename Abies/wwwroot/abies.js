// wwwroot/js/pine.js

import { dotnet } from './_framework/dotnet.js';

// ═══════════════════════════════════════════════════════════════════════════════
// TRACING VERBOSITY SYSTEM
// ═══════════════════════════════════════════════════════════════════════════════
// Controls how much detail is traced. End users typically want 'user' level,
// while framework developers debugging Abies itself may want 'debug' level.
//
// Levels:
//   'off'   - No tracing at all
//   'user'  - UI Events and HTTP calls only (default for production)
//   'debug' - Everything including DOM mutations, attribute updates, etc.
//
// Configuration priority (highest to lowest):
//   1. window.__OTEL_VERBOSITY = 'debug'
//   2. <meta name="otel-verbosity" content="user">
//   3. URL parameter: ?otel_verbosity=debug
//   4. Default: 'user'
// ═══════════════════════════════════════════════════════════════════════════════

const VERBOSITY_LEVELS = { off: 0, user: 1, debug: 2 };

// Verbosity cache with invalidation support for runtime changes
let _verbosityCache = null;

function resetVerbosityCache() {
  _verbosityCache = null;
}

function getVerbosity() {
  if (_verbosityCache !== null) return _verbosityCache;
  try {
    // Priority 1: Global variable
    if (window.__OTEL_VERBOSITY) {
      const v = String(window.__OTEL_VERBOSITY).toLowerCase();
      if (v in VERBOSITY_LEVELS) { _verbosityCache = v; return _verbosityCache; }
    }
    // Priority 2: Meta tag
    const meta = document.querySelector('meta[name="otel-verbosity"]');
    if (meta) {
      const v = (meta.getAttribute('content') || '').toLowerCase();
      if (v in VERBOSITY_LEVELS) { _verbosityCache = v; return _verbosityCache; }
    }
    // Priority 3: URL parameter
    const params = new URLSearchParams(window.location.search);
    const urlParam = params.get('otel_verbosity');
    if (urlParam) {
      const v = urlParam.toLowerCase();
      if (v in VERBOSITY_LEVELS) { _verbosityCache = v; return _verbosityCache; }
    }
  } catch {}
  // Default
  _verbosityCache = 'user';
  return _verbosityCache;
}

// Check if a span should be recorded based on verbosity level
// 'user' spans: UI Event, HTTP (fetch/XHR)
// 'debug' spans: DOM mutations, attribute updates, etc.
function shouldTrace(spanName) {
  const verbosity = getVerbosity();
  if (verbosity === 'off') return false;
  if (verbosity === 'debug') return true;
  // 'user' level: only trace user interactions and HTTP calls
  const userLevelSpans = ['UI Event', 'HTTP GET', 'HTTP POST', 'HTTP PUT', 'HTTP DELETE', 'HTTP PATCH', 'HTTP OPTIONS', 'HTTP HEAD'];
  return userLevelSpans.some(prefix => spanName.startsWith(prefix)) || spanName === 'UI Event';
}

// Initialize a minimal no-op tracing API; upgrade to real OTel asynchronously if available
let trace = {
    getTracer: () => ({
        startSpan: () => ({
            setStatus: () => {},
            recordException: () => {},
            end: () => {}
        })
    })
};
let SpanStatusCode = { OK: 1, ERROR: 2 };

const isOtelDisabled = (() => {
  try {
    if (window.__OTEL_DISABLED === true) return true;
    if (getVerbosity() === 'off') return true;
    const enabledMeta = document.querySelector('meta[name="otel-enabled"]');
    const enabledValue = enabledMeta && enabledMeta.getAttribute('content');
    if (enabledValue && enabledValue.toLowerCase() === 'off') return true;
    const cdnMeta = document.querySelector('meta[name="otel-cdn"]');
    const cdnValue = cdnMeta && cdnMeta.getAttribute('content');
    if (cdnValue && cdnValue.toLowerCase() === 'off') return true;
  } catch {}
  return false;
})();

// =============================================================================
// DEFERRED OTEL INITIALIZATION - First Paint Optimization
// =============================================================================
// OTel CDN loading is deferred until AFTER first paint to avoid blocking startup.
// The lightweight shim is installed immediately so tracing works during startup.
// After first paint, we upgrade to the full CDN-based OTel if available.
//
// Performance optimization: Reduces First Paint from ~4.8s to ~100ms by:
// 1. Installing lightweight shim synchronously (no CDN dependency)
// 2. Deferring CDN imports to requestIdleCallback/setTimeout
// 3. Never blocking the critical path (dotnet.create() -> runMain())
// =============================================================================

// Install lightweight shim immediately for tracing during startup
function initLocalOtelShim() {
  if (isOtelDisabled) return; // Respect global disable switches
  if (window.__otel) return; // Already initialized

  const hex = (n) => Array.from(crypto.getRandomValues(new Uint8Array(n))).map(b => b.toString(16).padStart(2, '0')).join('');
  const nowNs = () => {
    const t = performance.timeOrigin + performance.now();
    return Math.round(t * 1e6).toString();
  };
  const endpoint = (function() {
    const meta = document.querySelector('meta[name="otlp-endpoint"]');
    if (meta && meta.content) return meta.content;
    if (window.__OTLP_ENDPOINT) return window.__OTLP_ENDPOINT;
    try { return new URL('/otlp/v1/traces', window.location.origin).href; } catch {}
    return 'http://localhost:4318/v1/traces';
  })();

  // Track the active span stack for proper parent-child relationships
  const state = { currentSpan: null, activeTraceContext: null, pendingSpans: [] };

  function makeSpan(name, kind = 1, explicitParent = undefined) {
    const parent = explicitParent !== undefined ? explicitParent : (state.currentSpan || state.activeTraceContext);
    const traceId = parent?.traceId || hex(16);
    const spanId = hex(8);
    return { traceId, spanId, parentSpanId: parent?.spanId, name, kind, start: nowNs(), end: null, attributes: {} };
  }

  let exportTimer = null;
  async function flushSpans() {
    if (state.pendingSpans.length === 0) return;
    const spans = state.pendingSpans.splice(0, state.pendingSpans.length);
    const payload = {
      resourceSpans: [{
        resource: { attributes: [{ key: 'service.name', value: { stringValue: 'Abies.Web' } }] },
        scopeSpans: [{
          scope: { name: 'Abies.JS.Shim', version: '1.0.0' },
          spans: spans.map(s => ({
            traceId: s.traceId,
            spanId: s.spanId,
            parentSpanId: s.parentSpanId || '',
            name: s.name,
            kind: s.kind,
            startTimeUnixNano: s.start,
            endTimeUnixNano: s.end,
            attributes: Object.entries(s.attributes).map(([k, v]) => ({
              key: k,
              value: typeof v === 'number' ? { intValue: v } : { stringValue: String(v) }
            })),
            status: { code: 1 }
          }))
        }]
      }]
    };
    try {
      await fetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });
    } catch { /* Silently ignore export errors */ }
  }

  function scheduleFlush() {
    if (exportTimer) return;
    exportTimer = setTimeout(() => {
      exportTimer = null;
      flushSpans();
    }, 500);
  }

  async function exportSpan(span) {
    state.pendingSpans.push(span);
    scheduleFlush();
  }

  // Minimal shim tracer
  trace = {
    getTracer: () => ({
      startSpan: (name, options) => {
        const s = makeSpan(name);
        if (options && options.attributes) Object.assign(s.attributes, options.attributes);
        const prev = state.currentSpan;
        state.currentSpan = s;
        state.activeTraceContext = s;
        return {
          spanContext: () => ({ traceId: s.traceId, spanId: s.spanId }),
          setAttribute: (key, value) => { s.attributes[key] = value; },
          setStatus: () => {},
          recordException: () => {},
          end: async () => {
            s.end = nowNs();
            state.currentSpan = prev;
            setTimeout(() => {
              if (state.activeTraceContext === s) state.activeTraceContext = prev;
            }, 100);
            await exportSpan(s);
          }
        };
      }
    })
  };
  SpanStatusCode = { OK: 1, ERROR: 2 };
  // Don't set tracer here - it will be set after the shim function completes

  // Helper to determine if a URL should be ignored for tracing
  function shouldIgnoreFetchForTracing(url) {
    try {
      if (!url) return false;
      // Ignore OTLP proxy endpoint
      if (/\/otlp\/v1\/traces$/.test(url)) return true;
      // Ignore common collector endpoints like http://localhost:4318/v1/traces
      if (/\/v1\/traces$/.test(url)) return true;
      // Ignore explicitly configured exporter URL if provided
      if (typeof window !== 'undefined' && window.__OTEL_EXPORTER_URL) {
        const configured = String(window.__OTEL_EXPORTER_URL);
        if (configured && url.startsWith(configured)) return true;
      }
      // Ignore Blazor framework/runtime/resource downloads
      if (url.includes('/_framework/')) return true;
    } catch {
      // On any error, fall back to tracing (do not silently skip)
    }
    return false;
  }

  // Store original fetch for potential restoration when upgrading to full OTel
  const origFetch = window.fetch.bind(window);
  window.__shimOrigFetch = origFetch;

  // Patch fetch for trace propagation
  try {
    window.fetch = async function(input, init) {
      const url = (typeof input === 'string') ? input : input.url;
      if (shouldIgnoreFetchForTracing(url)) return origFetch(input, init);
      const method = (init && init.method) || (typeof input !== 'string' && input.method) || 'GET';
      const parent = state.currentSpan || state.activeTraceContext;
      const sp = makeSpan(`HTTP ${method}`, 3, parent);
      sp.attributes['http.method'] = method;
      sp.attributes['http.url'] = url;
      const traceparent = `00-${sp.traceId}-${sp.spanId}-01`;
      const i = init ? { ...init } : {};
      const h = new Headers((i.headers) || (typeof input !== 'string' && input.headers) || {});
      h.set('traceparent', traceparent);
      i.headers = h;
      try {
        const res = await origFetch(input, i);
        sp.attributes['http.status_code'] = res.status;
        sp.end = nowNs();
        await exportSpan(sp);
        return res;
      } catch (e) {
        sp.attributes['error'] = true;
        sp.end = nowNs();
        await exportSpan(sp);
        throw e;
      }
    };
  } catch {}

  window.__otel = {
    provider: { forceFlush: async () => { await flushSpans(); } },
    exporter: { url: endpoint },
    endpoint,
    getVerbosity,
    setVerbosity: (level) => {
      if (level in VERBOSITY_LEVELS) {
        window.__OTEL_VERBOSITY = level;
        resetVerbosityCache();
      }
    }
  };
}

// CDN-based OTel upgrade (deferred to after first paint)
async function upgradeToFullOtel() {
  if (isOtelDisabled) return;

  // Check if CDN is enabled
  const useCdn = (() => {
    try {
      if (window.__OTEL_USE_CDN === false) return false;
      const m = document.querySelector('meta[name="otel-cdn"]');
      const v = (m && m.getAttribute('content')) || '';
      if (v && v.toLowerCase() === 'off') return false;
    } catch {}
    return true;
  })();
  if (!useCdn) return;

  try {
    let api;
    try {
      api = await import('https://unpkg.com/@opentelemetry/api@1.8.0/build/esm/index.js');
      trace = api.trace;
      SpanStatusCode = api.SpanStatusCode;
    } catch { return; } // CDN failed, keep using shim

    const [
      { WebTracerProvider },
      traceBase,
      exporterMod,
      resourcesMod,
      semconvMod
    ] = await Promise.all([
      import('https://unpkg.com/@opentelemetry/sdk-trace-web@1.18.1/build/esm/index.js'),
      import('https://unpkg.com/@opentelemetry/sdk-trace-base@1.18.1/build/esm/index.js'),
      import('https://unpkg.com/@opentelemetry/exporter-trace-otlp-http@0.50.0/build/esm/index.js'),
      import('https://unpkg.com/@opentelemetry/resources@1.18.1/build/esm/index.js'),
      import('https://unpkg.com/@opentelemetry/semantic-conventions@1.18.1/build/esm/index.js')
    ]);
    const { BatchSpanProcessor } = traceBase;
    const { OTLPTraceExporter } = exporterMod;
    const { Resource } = resourcesMod;
    const { SemanticResourceAttributes } = semconvMod;

    const guessOtlp = () => {
      if (window.__OTLP_ENDPOINT) return window.__OTLP_ENDPOINT;
      try {
        const meta = document.querySelector('meta[name="otlp-endpoint"]');
        const v = meta && meta.getAttribute('content');
        if (v) return v;
      } catch {}
      try { return new URL('/otlp/v1/traces', window.location.origin).href; } catch {}
      return 'http://localhost:4318/v1/traces';
    };

    const endpoint = guessOtlp();
    const exporter = new OTLPTraceExporter({ url: endpoint });
    const provider = new WebTracerProvider({
      resource: new Resource({ [SemanticResourceAttributes.SERVICE_NAME]: 'Abies.Web' })
    });
    const bsp = new BatchSpanProcessor(exporter, {
      scheduledDelayMillis: 500,
      exportTimeoutMillis: 3000,
      maxQueueSize: 2048,
      maxExportBatchSize: 64
    });
    provider.addSpanProcessor(bsp);

    try {
      const { ZoneContextManager } = await import('https://unpkg.com/@opentelemetry/context-zone@1.18.1/build/esm/index.js');
      provider.register({ contextManager: new ZoneContextManager() });
    } catch {
      provider.register();
    }
    try {
      const { setGlobalTracerProvider } = api ?? await import('https://unpkg.com/@opentelemetry/api@1.8.0/build/esm/index.js');
      setGlobalTracerProvider(provider);
    } catch {}

    // Auto-instrument fetch/XHR
    // First, restore original fetch to avoid double-patching (shim + OTel instrumentations)
    if (window.__shimOrigFetch) {
      window.fetch = window.__shimOrigFetch;
      delete window.__shimOrigFetch;
    }

    try {
      const [core, fetchI, xhrI, docI, uiI] = await Promise.all([
        import('https://unpkg.com/@opentelemetry/instrumentation@0.50.0/build/esm/index.js'),
        import('https://unpkg.com/@opentelemetry/instrumentation-fetch@0.50.0/build/esm/index.js'),
        import('https://unpkg.com/@opentelemetry/instrumentation-xml-http-request@0.50.0/build/esm/index.js'),
        import('https://unpkg.com/@opentelemetry/instrumentation-document-load@0.50.0/build/esm/index.js'),
        import('https://unpkg.com/@opentelemetry/instrumentation-user-interaction@0.50.0/build/esm/index.js')
      ]);
      const { registerInstrumentations } = core;
      const { FetchInstrumentation } = fetchI;
      const { XMLHttpRequestInstrumentation } = xhrI;
      const { DocumentLoadInstrumentation } = docI;
      const { UserInteractionInstrumentation } = uiI;
      // Include common collector endpoint patterns and framework downloads
      const ignore = [/\/otlp\/v1\/traces$/, /\/v1\/traces$/, /\/_framework\//];
      const propagate = [/.*/];
      registerInstrumentations({
        instrumentations: [
          new FetchInstrumentation({ ignoreUrls: ignore, propagateTraceHeaderCorsUrls: propagate }),
          new XMLHttpRequestInstrumentation({ ignoreUrls: ignore, propagateTraceHeaderCorsUrls: propagate }),
          new DocumentLoadInstrumentation(),
          new UserInteractionInstrumentation()
        ]
      });
    } catch {}

    tracer = trace.getTracer('Abies.JS');
    window.__otel = {
      provider,
      exporter,
      endpoint: guessOtlp(),
      getVerbosity,
      setVerbosity: (level) => {
        if (level in VERBOSITY_LEVELS) {
          window.__OTEL_VERBOSITY = level;
          resetVerbosityCache();
        }
      }
    };
  } catch { /* Upgrade failed, continue with shim */ }
}

// Schedule deferred OTel upgrade using requestIdleCallback or fallback
function scheduleDeferredOtelUpgrade() {
  if (isOtelDisabled) return;

  const doUpgrade = () => {
    // Cap OTel init time to avoid blocking for too long
    const timeout = new Promise((_, reject) => setTimeout(() => reject(new Error('OTel timeout')), 5000));
    Promise.race([upgradeToFullOtel(), timeout]).catch(() => {});
  };

  // Use requestIdleCallback if available, otherwise setTimeout with 0ms delay
  // This ensures OTel loading happens during browser idle time, after first paint
  if (typeof requestIdleCallback === 'function') {
    requestIdleCallback(doUpgrade, { timeout: 2000 });
  } else {
    setTimeout(doUpgrade, 0);
  }
}

// Install shim immediately (no async, no blocking)
try { initLocalOtelShim(); } catch { /* ignore */ }

// tracer is created from trace (which was set by initLocalOtelShim or is the default no-op)
let tracer = trace.getTracer('Abies.JS');

// Wrap a function with tracing, respecting verbosity settings
function withSpan(name, fn) {
    return async (...args) => {
        // Skip tracing if verbosity level doesn't include this span
        if (!shouldTrace(name)) {
            return await fn(...args);
        }
        const span = tracer.startSpan(name);
        try {
            const result = await fn(...args);
            span.setStatus({ code: SpanStatusCode.OK });
            return result;
        } catch (err) {
            span.recordException(err);
            span.setStatus({ code: SpanStatusCode.ERROR });
            throw err;
        } finally {
            span.end();
        }
    };
}

const { setModuleImports, getAssemblyExports, getConfig, runMain } = await dotnet
    .withDiagnosticTracing(false)
    .create();

const registeredEvents = new Set();

// Pre-register all common event types at startup to avoid O(n) DOM scanning
// on every incremental update. These match the event types defined in Operations.cs.
const COMMON_EVENT_TYPES = [
    // Mouse events
    'click', 'dblclick', 'mousedown', 'mouseup', 'mouseover', 'mouseout',
    'mouseenter', 'mouseleave', 'mousemove', 'contextmenu', 'wheel',
    // Keyboard events
    'keydown', 'keyup', 'keypress',
    // Form events
    'input', 'change', 'submit', 'reset', 'focus', 'blur', 'invalid', 'search',
    // Touch events
    'touchstart', 'touchend', 'touchmove', 'touchcancel',
    // Pointer events
    'pointerdown', 'pointerup', 'pointermove', 'pointercancel',
    'pointerover', 'pointerout', 'pointerenter', 'pointerleave',
    'gotpointercapture', 'lostpointercapture',
    // Drag events
    'drag', 'dragstart', 'dragend', 'dragenter', 'dragleave', 'dragover', 'drop',
    // Clipboard events
    'copy', 'cut', 'paste',
    // Media events
    'play', 'pause', 'ended', 'volumechange', 'timeupdate', 'seeking', 'seeked',
    'loadeddata', 'loadedmetadata', 'canplay', 'canplaythrough', 'playing',
    'waiting', 'stalled', 'suspend', 'emptied', 'ratechange', 'durationchange',
    // Other events
    'scroll', 'resize', 'load', 'error', 'abort', 'select', 'toggle',
    'animationstart', 'animationend', 'animationiteration', 'animationcancel',
    'transitionstart', 'transitionend', 'transitionrun', 'transitioncancel'
];

function ensureEventListener(eventName) {
    if (registeredEvents.has(eventName)) return;
    // Attach to document to survive body innerHTML changes and use capture for early handling
    const opts = (eventName === 'click') ? { capture: true } : undefined;
    document.addEventListener(eventName, genericEventHandler, opts);
    registeredEvents.add(eventName);
}

// Helper to find an element with a specific attribute, traversing through shadow DOM boundaries
function findEventTarget(event, attributeName) {
    // First try the composed path to handle shadow DOM (for Web Components like fluent-button)
    const path = event.composedPath ? event.composedPath() : [];
    for (const el of path) {
        if (el.nodeType === 1 /* ELEMENT_NODE */ && el.hasAttribute && el.hasAttribute(attributeName)) {
            return el;
        }
    }
    // Fallback to closest() for non-shadow DOM cases
    let origin = event.target;
    if (origin && origin.nodeType === 3 /* TEXT_NODE */) {
        origin = origin.parentElement;
    }
    return origin && origin.closest ? origin.closest(`[${attributeName}]`) : null;
}

function genericEventHandler(event) {
    const name = event.type;
    const attributeName = `data-event-${name}`;
    const target = findEventTarget(event, attributeName);
    if (!target) return;
    // Ignore events coming from nodes that have been detached/replaced
    if (!target.isConnected) return;
    // For Abies-managed clicks, prevent native navigation immediately
    if (name === 'click') {
        try {
            event.preventDefault();
            if (typeof event.stopPropagation === 'function') event.stopPropagation();
            if (typeof event.stopImmediatePropagation === 'function') event.stopImmediatePropagation();
        } catch { /* ignore */ }
    }
    const message = target.getAttribute(attributeName);
    if (!message) {
        console.error(`No message id found in data-event-${name} attribute.`);
        return;
    }
    // Prevent default only for Abies-managed Enter keydown events (scoped)
    if (name === 'keydown' && event && event.key === 'Enter') {
        try { event.preventDefault(); } catch { /* ignore */ }
    }

    // Build rich UI context for tracing
    const tag = (target.tagName || '').toLowerCase();
    const text = (target.textContent || '').trim().substring(0, 50); // Truncate long text
    const classes = target.className || '';
    const ariaLabel = target.getAttribute('aria-label') || '';
    const elId = target.id || '';

    // Build human-readable action description
    let action = '';
    if (name === 'click') {
        if (tag === 'button' || tag === 'a' || tag === 'fluent-button') {
            action = `Click ${tag === 'a' ? 'Link' : 'Button'}: ${text || ariaLabel || elId || '(unnamed)'}`;
        } else if (tag === 'input') {
            const inputType = target.getAttribute('type') || 'text';
            action = `Click Input (${inputType})`;
        } else {
            action = `Click ${tag}: ${text || elId || '(element)'}`;
        }
    } else if (name === 'input' || name === 'change') {
        action = `Input: ${tag}${elId ? '#' + elId : ''}`;
    } else if (name === 'submit') {
        action = `Submit Form: ${elId || '(form)'}`;
    } else if (name === 'keydown' || name === 'keyup') {
        action = `Key ${name === 'keydown' ? 'Down' : 'Up'}: ${event.key || ''}`;
    } else {
        action = `${name}: ${tag}${elId ? '#' + elId : ''}`;
    }

    const spanOptions = {
        attributes: {
            'ui.event.type': name,
            'ui.element.tag': tag,
            'ui.element.id': elId,
            'ui.element.text': text,
            'ui.element.classes': classes,
            'ui.element.aria_label': ariaLabel,
            'ui.action': action,
            'abies.message_id': message
        }
    };

    // Use startActiveSpan if available (CDN mode) to properly set context for nested spans
    // This ensures FetchInstrumentation creates child spans under this UI Event
    if (typeof tracer.startActiveSpan === 'function') {
        tracer.startActiveSpan('UI Event', spanOptions, (span) => {
            try {
                const data = buildEventData(event, target);
                exports.Abies.Runtime.DispatchData(message, JSON.stringify(data));
                span.setStatus({ code: SpanStatusCode.OK });
            } catch (err) {
                span.recordException(err);
                span.setStatus({ code: SpanStatusCode.ERROR });
                console.error(err);
            } finally {
                // Delay ending the span slightly to allow async fetch calls to start within this context
                // The FetchInstrumentation will capture the parent span at fetch() call time
                setTimeout(() => span.end(), 50);
            }
        });
    } else {
        // Shim mode - use startSpan (shim handles context tracking internally)
        const span = tracer.startSpan('UI Event', spanOptions);
        try {
            const data = buildEventData(event, target);
            exports.Abies.Runtime.DispatchData(message, JSON.stringify(data));
            span.setStatus({ code: SpanStatusCode.OK });
        } catch (err) {
            span.recordException(err);
            span.setStatus({ code: SpanStatusCode.ERROR });
            console.error(err);
        } finally {
            // Shim uses activeTraceContext which persists briefly after span.end()
            span.end();
        }
    }
}

function buildEventData(event, target) {
    const data = {};
    if (target && 'value' in target) data.value = target.value;
    if (target && 'checked' in target) data.checked = target.checked;
    if ('key' in event) {
        data.key = event.key;
        data.repeat = event.repeat === true;
        data.altKey = event.altKey;
        data.ctrlKey = event.ctrlKey;
        data.shiftKey = event.shiftKey;
    }
    if ('clientX' in event) {
        data.clientX = event.clientX;
        data.clientY = event.clientY;
        data.button = event.button;
    }
    // Scroll position data — populated for scroll events from the target element
    if (event.type === 'scroll' && target) {
        data.scrollTop = target.scrollTop || 0;
        data.scrollLeft = target.scrollLeft || 0;
        data.scrollHeight = target.scrollHeight || 0;
        data.scrollWidth = target.scrollWidth || 0;
        data.clientHeight = target.clientHeight || 0;
        data.clientWidth = target.clientWidth || 0;
    }
    return data;
}

const subscriptionRegistry = new Map();

function dispatchSubscription(key, data) {
    try {
        exports.Abies.Runtime.DispatchSubscriptionData(key, JSON.stringify(data));
    } catch (err) {
        console.error(err);
    }
}

function buildVisibilityState() {
    return document.visibilityState === 'visible' ? 'Visible' : 'Hidden';
}

function encodeBase64(bytes) {
    let binary = '';
    const chunkSize = 0x8000;
    for (let i = 0; i < bytes.length; i += chunkSize) {
        const chunk = bytes.subarray(i, i + chunkSize);
        binary += String.fromCharCode(...chunk);
    }
    return btoa(binary);
}

async function toMessagePayload(data) {
    if (typeof data === 'string') {
        return { messageKind: 'text', data };
    }

    try {
        const buffer = data instanceof Blob ? await data.arrayBuffer() : data;
        const bytes = new Uint8Array(buffer);
        return { messageKind: 'binary', data: encodeBase64(bytes) };
    } catch {
        return { messageKind: 'text', data: '' };
    }
}

function subscribe(key, kind, data) {
    if (subscriptionRegistry.has(key)) return;

    let dispose;
    switch (kind) {
        case 'animationFrame': {
            let active = true;
            const loop = (timestamp) => {
                if (!active) return;
                dispatchSubscription(key, { timestamp });
                requestAnimationFrame(loop);
            };
            requestAnimationFrame(loop);
            dispose = () => { active = false; };
            break;
        }
        case 'animationFrameDelta': {
            let active = true;
            let last = null;
            const loop = (timestamp) => {
                if (!active) return;
                const delta = last === null ? 0 : timestamp - last;
                last = timestamp;
                dispatchSubscription(key, { timestamp, delta });
                requestAnimationFrame(loop);
            };
            requestAnimationFrame(loop);
            dispose = () => { active = false; };
            break;
        }
        case 'resize': {
            const handler = () => dispatchSubscription(key, {
                width: window.innerWidth,
                height: window.innerHeight
            });
            window.addEventListener('resize', handler);
            dispose = () => window.removeEventListener('resize', handler);
            break;
        }
        case 'visibilityChange': {
            const handler = () => dispatchSubscription(key, { state: buildVisibilityState() });
            document.addEventListener('visibilitychange', handler);
            dispose = () => document.removeEventListener('visibilitychange', handler);
            break;
        }
        case 'keyDown': {
            const pressed = new Set();
            const down = (event) => {
                if (event && event.repeat) return;
                const k = event?.key ?? '';
                if (pressed.has(k)) return;
                pressed.add(k);
                dispatchSubscription(key, buildEventData(event, event.target));
            };
            const up = (event) => {
                const k = event?.key ?? '';
                pressed.delete(k);
            };
            window.addEventListener('keydown', down);
            window.addEventListener('keyup', up);
            dispose = () => {
                window.removeEventListener('keydown', down);
                window.removeEventListener('keyup', up);
            };
            break;
        }
        case 'keyUp': {
            const handler = (event) => dispatchSubscription(key, buildEventData(event, event.target));
            window.addEventListener('keyup', handler);
            dispose = () => window.removeEventListener('keyup', handler);
            break;
        }
        case 'mouseDown': {
            const handler = (event) => dispatchSubscription(key, buildEventData(event, event.target));
            window.addEventListener('mousedown', handler);
            dispose = () => window.removeEventListener('mousedown', handler);
            break;
        }
        case 'mouseUp': {
            const handler = (event) => dispatchSubscription(key, buildEventData(event, event.target));
            window.addEventListener('mouseup', handler);
            dispose = () => window.removeEventListener('mouseup', handler);
            break;
        }
        case 'mouseMove': {
            // Throttle mouse move to once per animation frame (~60fps max) to prevent flooding
            let pending = null;
            let rafId = null;
            const handler = (event) => {
                pending = buildEventData(event, event.target);
                if (rafId === null) {
                    rafId = requestAnimationFrame(() => {
                        if (pending) {
                            dispatchSubscription(key, pending);
                            pending = null;
                        }
                        rafId = null;
                    });
                }
            };
            window.addEventListener('mousemove', handler);
            dispose = () => {
                window.removeEventListener('mousemove', handler);
                if (rafId !== null) {
                    cancelAnimationFrame(rafId);
                }
            };
            break;
        }
        case 'click': {
            const handler = (event) => dispatchSubscription(key, buildEventData(event, event.target));
            window.addEventListener('click', handler);
            dispose = () => window.removeEventListener('click', handler);
            break;
        }
        case 'websocket': {
            const options = data ? JSON.parse(data) : null;
            if (!options || !options.url) {
                throw new Error('WebSocket subscription requires a url.');
            }

            const ws = Array.isArray(options.protocols) && options.protocols.length > 0
                ? new WebSocket(options.url, options.protocols)
                : new WebSocket(options.url);

            const openHandler = () => dispatchSubscription(key, { type: 'open' });
            const closeHandler = (event) => dispatchSubscription(key, {
                type: 'close',
                code: event.code,
                reason: event.reason,
                wasClean: event.wasClean
            });
            const errorHandler = () => dispatchSubscription(key, { type: 'error' });
            const messageHandler = async (event) => {
                const payload = await toMessagePayload(event.data);
                dispatchSubscription(key, {
                    type: 'message',
                    messageKind: payload.messageKind,
                    data: payload.data
                });
            };

            ws.addEventListener('open', openHandler);
            ws.addEventListener('close', closeHandler);
            ws.addEventListener('error', errorHandler);
            ws.addEventListener('message', messageHandler);

            dispose = () => {
                try {
                    ws.removeEventListener('open', openHandler);
                    ws.removeEventListener('close', closeHandler);
                    ws.removeEventListener('error', errorHandler);
                    ws.removeEventListener('message', messageHandler);
                    ws.close(1000, 'subscription disposed');
                } catch { }
            };
            break;
        }
        case 'scroll': {
            // Element-level scroll subscription — targets a specific DOM element by ID.
            // Uses requestAnimationFrame throttling to prevent flooding the MVU loop.
            const options = data ? JSON.parse(data) : null;
            const elementId = options?.elementId;
            let pending = null;
            let rafId = null;
            const handler = (event) => {
                const el = event.target;
                pending = {
                    scrollTop: el.scrollTop || 0,
                    scrollLeft: el.scrollLeft || 0,
                    scrollHeight: el.scrollHeight || 0,
                    scrollWidth: el.scrollWidth || 0,
                    clientHeight: el.clientHeight || 0,
                    clientWidth: el.clientWidth || 0
                };
                if (rafId === null) {
                    rafId = requestAnimationFrame(() => {
                        if (pending) {
                            dispatchSubscription(key, pending);
                            pending = null;
                        }
                        rafId = null;
                    });
                }
            };
            if (elementId) {
                // Target a specific element
                const el = document.getElementById(elementId);
                if (el) {
                    el.addEventListener('scroll', handler, { passive: true });
                    dispose = () => {
                        el.removeEventListener('scroll', handler);
                        if (rafId !== null) cancelAnimationFrame(rafId);
                    };
                } else {
                    // Element not yet in DOM — use MutationObserver to wait for it
                    let resolved = false;
                    const observer = new MutationObserver(() => {
                        const el = document.getElementById(elementId);
                        if (el && !resolved) {
                            resolved = true;
                            observer.disconnect();
                            el.addEventListener('scroll', handler, { passive: true });
                        }
                    });
                    observer.observe(document.body, { childList: true, subtree: true });
                    dispose = () => {
                        observer.disconnect();
                        const el = document.getElementById(elementId);
                        if (el) el.removeEventListener('scroll', handler);
                        if (rafId !== null) cancelAnimationFrame(rafId);
                    };
                }
            } else {
                // Window-level scroll
                window.addEventListener('scroll', handler, { passive: true });
                dispose = () => {
                    window.removeEventListener('scroll', handler);
                    if (rafId !== null) cancelAnimationFrame(rafId);
                };
            }
            break;
        }
        default:
            throw new Error(`Unknown subscription kind: ${kind}`);
    }

    subscriptionRegistry.set(key, dispose);
}

function unsubscribe(key) {
    const dispose = subscriptionRegistry.get(key);
    if (!dispose) return;
    try {
        dispose();
    } finally {
        subscriptionRegistry.delete(key);
    }
}

/**
 * Discovers and registers event listeners for any custom (non-common) event types
 * in the given DOM subtree. Common event types are pre-registered at startup,
 * so this function primarily handles rare/custom event handlers.
 *
 * Uses TreeWalker instead of querySelectorAll for better memory efficiency.
 */
function addEventListeners(root) {
    // Scan the given scope for any data-event-* attributes.
    // Since common events are pre-registered, this is mostly a no-op for typical apps,
    // but we still need to scan for custom event types in newly added HTML.
    const scope = root || document;

    // Use TreeWalker for memory-efficient iteration
    const walker = document.createTreeWalker(scope, NodeFilter.SHOW_ELEMENT);

    // Include the root element itself if it's an element
    if (scope.nodeType === 1 /* ELEMENT_NODE */ && scope.attributes) {
        for (const attr of scope.attributes) {
            if (attr.name.startsWith('data-event-')) {
                const name = attr.name.substring('data-event-'.length);
                ensureEventListener(name);
            }
        }
    }

    // Walk descendants
    let el = walker.nextNode();
    while (el) {
        if (el.attributes) {
            for (const attr of el.attributes) {
                if (attr.name.startsWith('data-event-')) {
                    const name = attr.name.substring('data-event-'.length);
                    ensureEventListener(name);
                }
            }
        }
        el = walker.nextNode();
    }
}

/**
 * Lightweight scan for non-common event listeners in a DOM subtree.
 * COMMON_EVENT_TYPES covers all standard DOM events and are pre-registered at
 * document level, so this only does real work when custom/non-standard events
 * (e.g., CustomEvent types) are used. For typical apps this is a no-op scan.
 * @param {Element} root - The root element to scan.
 */
function ensureSubtreeEventListeners(root) {
    if (!root || root.nodeType !== 1) return;
    // Check the root element itself
    const attrs = root.attributes;
    if (attrs) {
        for (let i = 0; i < attrs.length; i++) {
            const name = attrs[i].name;
            if (name.length > 11 && name.startsWith('data-event-')) {
                ensureEventListener(name.substring(11));
            }
        }
    }
    // Walk descendants using TreeWalker (memory-efficient for large subtrees)
    const walker = document.createTreeWalker(root, NodeFilter.SHOW_ELEMENT);
    let el = walker.nextNode();
    while (el) {
        const elAttrs = el.attributes;
        if (elAttrs) {
            for (let i = 0; i < elAttrs.length; i++) {
                const name = elAttrs[i].name;
                if (name.length > 11 && name.startsWith('data-event-')) {
                    ensureEventListener(name.substring(11));
                }
            }
        }
        el = walker.nextNode();
    }
}

/**
 * Event handler for click events on elements with data-event-* attributes.
 * @param {Event} event - The DOM event.
 */

// =============================================================================
// BINARY RENDER BATCH - Zero-Copy Protocol
// =============================================================================
// Reads DOM patches directly from WASM memory without JSON serialization.
// This is inspired by Blazor's SharedMemoryRenderBatch but adapted for Abies.
//
// Binary Format:
//   Header (8 bytes):
//     - PatchCount: int32 (4 bytes)
//     - StringTableOffset: int32 (4 bytes)
//
//   Patch Entries (16 bytes each):
//     - Type: int32 (4 bytes) - BinaryPatchType enum value
//     - Field1: int32 (4 bytes) - string table index (-1 = null)
//     - Field2: int32 (4 bytes) - string table index (-1 = null)
//     - Field3: int32 (4 bytes) - string table index (-1 = null)
//
//   String Table:
//     - Strings stored as LEB128 length prefix + UTF8 bytes
// =============================================================================

const BinaryPatchType = {
    SetAppContent: 1,
    ReplaceChild: 2,
    AddChild: 3,
    RemoveChild: 4,
    ClearChildren: 5,
    UpdateAttribute: 6,
    AddAttribute: 7,
    RemoveAttribute: 8,
    UpdateText: 9,
    UpdateTextWithId: 10,
    MoveChild: 11,
    SetChildrenHtml: 12,
};

/**
 * Reads an int32 from a Uint8Array at the given offset (little-endian).
 * @param {Uint8Array} data - The binary data buffer.
 * @param {number} offset - The byte offset to read from.
 * @returns {number} The int32 value.
 */
function readInt32LE(data, offset) {
    return data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24);
}

/**
 * Reads a LEB128-encoded unsigned integer and returns both value and bytes consumed.
 * @param {Uint8Array} data - The binary data buffer.
 * @param {number} offset - The byte offset to start reading from.
 * @returns {{value: number, bytesRead: number}} The decoded value and number of bytes consumed.
 */
function readLEB128(data, offset) {
    let result = 0;
    let shift = 0;
    let bytesRead = 0;
    let byte;
    do {
        byte = data[offset + bytesRead];
        result |= (byte & 0x7F) << shift;
        shift += 7;
        bytesRead++;
    } while (byte & 0x80);
    return { value: result, bytesRead };
}

/**
 * Creates a string reader for the binary batch's string table.
 * @param {Uint8Array} data - The binary data buffer.
 * @param {number} stringTableOffset - The byte offset where the string table starts.
 * @returns {function(number): string|null} A function that reads a string by index.
 */
function createStringReader(data, stringTableOffset) {
    const decoder = new TextDecoder('utf-8');

    return function readString(index) {
        if (index === -1) return null;

        // Index points to the byte offset within the string table
        const absoluteOffset = stringTableOffset + index;

        // Read LEB128 length prefix
        const { value: length, bytesRead } = readLEB128(data, absoluteOffset);

        // Read UTF-8 bytes
        const stringStart = absoluteOffset + bytesRead;
        const stringBytes = data.subarray(stringStart, stringStart + length);

        return decoder.decode(stringBytes);
    };
}

/**
 * Applies a binary render batch to the DOM.
 * This is the zero-copy alternative to applyPatches that avoids JSON serialization.
 * @param {Object|Uint8Array} batchData - The binary batch data (Span wrapper or Uint8Array).
 *        When using JSType.MemoryView, this is a Span wrapper object with slice() method.
 */
function applyBinaryBatchImpl(batchData) {
    // JSType.MemoryView passes a Span wrapper object with slice() method, NOT a raw Uint8Array
    // The Span wrapper provides slice() to create a copy of the data as a Uint8Array
    // We must call slice() to get the actual data since the original Span may be short-lived
    let data;
    if (batchData instanceof Uint8Array) {
        data = batchData;
    } else if (batchData && typeof batchData.slice === 'function') {
        // This is a Span wrapper - call slice() to get a Uint8Array copy
        data = batchData.slice();
    } else if (batchData && typeof batchData.copyTo === 'function') {
        // Alternative: use copyTo if slice isn't available
        const temp = new Uint8Array(batchData.length);
        batchData.copyTo(temp);
        data = temp;
    } else {
        console.error('[Binary Batch] Unknown batchData type:', typeof batchData, batchData);
        return;
    }

    // Read header
    const patchCount = readInt32LE(data, 0);
    const stringTableOffset = readInt32LE(data, 4);

    if (getVerbosity() >= 2) { // debug level
        console.debug(`[Binary Batch] patchCount=${patchCount}, stringTableOffset=${stringTableOffset}, dataLength=${data.length}`);
    }

    // Create string reader
    const readString = createStringReader(data, stringTableOffset);

    // Process each patch (16 bytes each, starting after 8-byte header)
    const HEADER_SIZE = 8;
    const PATCH_SIZE = 16;

    for (let i = 0; i < patchCount; i++) {
        const patchOffset = HEADER_SIZE + (i * PATCH_SIZE);

        const type = readInt32LE(data, patchOffset);
        const field1 = readInt32LE(data, patchOffset + 4);
        const field2 = readInt32LE(data, patchOffset + 8);
        const field3 = readInt32LE(data, patchOffset + 12);

        switch (type) {
            case BinaryPatchType.SetAppContent: {
                const html = readString(field1);
                document.body.innerHTML = html;
                addEventListeners();
                window.abiesReady = true;
                break;
            }
            case BinaryPatchType.AddChild: {
                const parentId = readString(field1);
                const html = readString(field2);
                const parent = document.getElementById(parentId);
                if (parent) {
                    const childElement = parseHtmlFragment(html);
                    if (childElement) {
                        parent.appendChild(childElement);
                        // Common events are pre-registered at document level via COMMON_EVENT_TYPES.
                        // Scan for non-common/custom event types that need dynamic registration.
                        ensureSubtreeEventListeners(childElement);
                    }
                }
                break;
            }
            case BinaryPatchType.RemoveChild: {
                const childId = readString(field2);
                const child = document.getElementById(childId);
                if (child && child.parentNode) {
                    child.remove();
                }
                break;
            }
            case BinaryPatchType.ClearChildren: {
                const parentId = readString(field1);
                const parent = document.getElementById(parentId);
                if (parent) {
                    parent.replaceChildren();
                }
                break;
            }
            case BinaryPatchType.ReplaceChild: {
                const targetId = readString(field1);
                const html = readString(field2);
                const oldNode = document.getElementById(targetId);
                if (oldNode && oldNode.parentNode) {
                    const newNode = parseHtmlFragment(html);
                    if (newNode) {
                        oldNode.parentNode.replaceChild(newNode, oldNode);
                        // Common events are pre-registered at document level via COMMON_EVENT_TYPES.
                        // Scan for non-common/custom event types that need dynamic registration.
                        ensureSubtreeEventListeners(newNode);
                    }
                }
                break;
            }
            case BinaryPatchType.UpdateAttribute:
            case BinaryPatchType.AddAttribute: {
                const targetId = readString(field1);
                const attrName = readString(field2);
                const attrValue = readString(field3);
                const node = document.getElementById(targetId);
                if (node) {
                    const lower = attrName.toLowerCase();
                    const isBooleanAttr = (
                        lower === 'disabled' || lower === 'checked' || lower === 'selected' || lower === 'readonly' ||
                        lower === 'multiple' || lower === 'required' || lower === 'autofocus' || lower === 'inert' ||
                        lower === 'hidden' || lower === 'open' || lower === 'loop' || lower === 'muted' || lower === 'controls'
                    );
                    if (lower === 'value' && 'value' in node) {
                        node.value = attrValue;
                        node.setAttribute(attrName, attrValue);
                    } else if (isBooleanAttr) {
                        node.setAttribute(attrName, '');
                        try { if (lower in node) node[lower] = true; } catch { /* ignore */ }
                    } else {
                        node.setAttribute(attrName, attrValue);
                    }
                    if (attrName.startsWith('data-event-')) {
                        ensureEventListener(attrName.substring('data-event-'.length));
                    }
                }
                break;
            }
            case BinaryPatchType.RemoveAttribute: {
                const targetId = readString(field1);
                const attrName = readString(field2);
                const node = document.getElementById(targetId);
                if (node) {
                    const lower = attrName.toLowerCase();
                    const isBooleanAttr = (
                        lower === 'disabled' || lower === 'checked' || lower === 'selected' || lower === 'readonly' ||
                        lower === 'multiple' || lower === 'required' || lower === 'autofocus' || lower === 'inert' ||
                        lower === 'hidden' || lower === 'open' || lower === 'loop' || lower === 'muted' || lower === 'controls'
                    );
                    node.removeAttribute(attrName);
                    if (isBooleanAttr) {
                        try { if (lower in node) node[lower] = false; } catch { /* ignore */ }
                    }
                }
                break;
            }
            case BinaryPatchType.UpdateText: {
                // targetId is now the PARENT element's ID (text nodes no longer have wrapper spans)
                const parentId = readString(field1);
                const text = readString(field2);
                const parent = document.getElementById(parentId);
                if (parent) {
                    // Find and update the first text node child
                    let foundText = false;
                    for (const child of parent.childNodes) {
                        if (child.nodeType === Node.TEXT_NODE) {
                            child.textContent = text;
                            foundText = true;
                            break;
                        }
                    }
                    // If no text node found, create one (shouldn't happen normally)
                    if (!foundText) {
                        parent.insertBefore(document.createTextNode(text), parent.firstChild);
                    }
                    // Handle TEXTAREA special case
                    const tag = (parent.tagName || '').toUpperCase();
                    if (tag === 'TEXTAREA') {
                        try { parent.value = text; } catch { /* ignore */ }
                    }
                }
                break;
            }
            case BinaryPatchType.UpdateTextWithId: {
                // This case is no longer used since text nodes don't have IDs
                // Keep for backwards compatibility but log a warning
                console.warn('UpdateTextWithId is deprecated - text nodes no longer have wrapper spans');
                const parentId = readString(field1);
                const text = readString(field2);
                const newId = readString(field3);
                const parent = document.getElementById(parentId);
                if (parent) {
                    for (const child of parent.childNodes) {
                        if (child.nodeType === Node.TEXT_NODE) {
                            child.textContent = text;
                            break;
                        }
                    }
                }
                break;
            }
            case BinaryPatchType.MoveChild: {
                const parentId = readString(field1);
                const childId = readString(field2);
                const beforeId = readString(field3); // null if -1
                const parent = document.getElementById(parentId);
                const child = document.getElementById(childId);
                if (parent && child) {
                    const before = beforeId ? document.getElementById(beforeId) : null;
                    parent.insertBefore(child, before);
                }
                break;
            }
            case BinaryPatchType.SetChildrenHtml: {
                // Bulk set all children via a single innerHTML assignment.
                // This replaces N individual parseHtmlFragment + appendChild + addEventListeners calls
                // with ONE DOM operation. Inspired by ivi's _hN template pattern and blockdom.
                const parentId = readString(field1);
                const html = readString(field2);
                const parent = document.getElementById(parentId);
                if (parent) {
                    parent.innerHTML = html;
                    // Common events are pre-registered at document level via COMMON_EVENT_TYPES.
                    // Scan for non-common/custom event types that need dynamic registration.
                    ensureSubtreeEventListeners(parent);
                }
                break;
            }
            default:
                console.error(`Unknown binary patch type: ${type}`);
        }
    }
}

/**
 * Parses an HTML string fragment into a DOM element, using the appropriate
 * container element to ensure browser parsing succeeds. Browsers strip
 * table-related elements (tr, td, etc.) when placed inside invalid containers.
 * @param {string} html - The HTML string to parse.
 * @returns {Element|null} The first element child from the parsed HTML, or null if none.
 */
function parseHtmlFragment(html) {
    const trimmedHtml = html.trimStart();
    let tempContainer;

    // Extract the first tag name to avoid prefix-matching issues
    // (e.g., <thead> matching <th>, <track> matching <tr>)
    const tagMatch = /^<\s*([a-zA-Z0-9]+)/.exec(trimmedHtml);
    const tagName = tagMatch ? tagMatch[1].toLowerCase() : null;

    if (tagName === 'tr') {
        tempContainer = document.createElement('tbody');
    } else if (tagName === 'td' || tagName === 'th') {
        tempContainer = document.createElement('tr');
    } else if (tagName === 'thead' || tagName === 'tbody' || tagName === 'tfoot' || tagName === 'colgroup' || tagName === 'caption') {
        tempContainer = document.createElement('table');
    } else if (tagName === 'col') {
        tempContainer = document.createElement('colgroup');
    } else if (tagName === 'option' || tagName === 'optgroup') {
        tempContainer = document.createElement('select');
    } else {
        tempContainer = document.createElement('div');
    }
    tempContainer.innerHTML = html;
    return tempContainer.firstElementChild;
}

setModuleImports('abies.js', {

    /**
     * Adds a child element to a parent element in the DOM using HTML content.
     * @param {number} parentId - The ID of the parent element.
     * @param {string} childHtml - The HTML string of the child element to add.
     */
    addChildHtml: withSpan('addChildHtml', async (parentId, childHtml) => {
        const parent = document.getElementById(parentId);
        if (parent) {
            const childElement = parseHtmlFragment(childHtml);
            parent.appendChild(childElement);
            // Reattach event listeners to new elements within this subtree
            addEventListeners(childElement);
        } else {
            console.error(`Parent element with ID ${parentId} not found.`);
        }
    }),


    /**
     * Sets the title of the document.
     * @param {string} title - The new title of the document.
     */
    setTitle: withSpan('setTitle', async (title) => {
        document.title = title;
    }),

    /**
     * Removes a child element from the DOM.
     * @param {number} parentId - The ID of the parent element.
     * @param {number} childId - The ID of the child element to remove.
     */
    removeChild: withSpan('removeChild', async (parentId, childId) =>  {
        const parent = document.getElementById(parentId);
        const child = document.getElementById(childId);
        if (parent && child && parent.contains(child)) {
            parent.removeChild(child);
        } else {
            console.error(`Cannot remove child with ID ${childId} from parent with ID ${parentId}.`);
        }
    }),

    /**
     * Clears all children from a parent element.
     * This is more efficient than multiple removeChild calls when clearing all children.
     * @param {string} parentId - The ID of the parent element to clear.
     */
    clearChildren: withSpan('clearChildren', async (parentId) => {
        const parent = document.getElementById(parentId);
        if (parent) {
            // replaceChildren() with no args removes all children efficiently
            parent.replaceChildren();
        } else {
            console.error(`Cannot clear children: parent with ID ${parentId} not found.`);
        }
    }),

    /**
     * Replaces an existing node with new HTML content.
     * @param {number} oldNodeId - The ID of the node to replace.
     * @param {string} newHtml - The HTML string to replace with.
     */
    replaceChildHtml: withSpan('replaceChildHtml', async (oldNodeId, newHtml) => {
        const oldNode = document.getElementById(oldNodeId);
        if (oldNode && oldNode.parentNode) {
            const newElement = parseHtmlFragment(newHtml);
            try {
                oldNode.parentNode.replaceChild(newElement, oldNode);
                // Reattach event listeners to new elements within this subtree
                addEventListeners(newElement);
            } catch (err) {
                console.error(`Node with ID ${oldNodeId} not found or has no parent.`, err);
            }
        } else {
            console.error(`Node with ID ${oldNodeId} not found or has no parent.`);
        }
    }),

    /**
     * Moves a child element to a new position within its parent.
     * Uses insertBefore semantics: moves child before beforeId, or appends if beforeId is null.
     * This is more efficient than remove+add as it preserves the element and its event listeners.
     * @param {string} parentId - The ID of the parent element.
     * @param {string} childId - The ID of the child element to move.
     * @param {string|null} beforeId - The ID of the element to insert before, or null to append.
     */
    moveChild: withSpan('moveChild', async (parentId, childId, beforeId) => {
        const parent = document.getElementById(parentId);
        const child = document.getElementById(childId);
        if (!parent) {
            console.error(`Parent with ID ${parentId} not found for moveChild.`);
            return;
        }
        if (!child) {
            console.error(`Child with ID ${childId} not found for moveChild.`);
            return;
        }
        const before = beforeId ? document.getElementById(beforeId) : null;
        if (beforeId && !before) {
            console.error(`Before element with ID ${beforeId} not found for moveChild.`);
            return;
        }
        // insertBefore with null as second argument appends to end
        parent.insertBefore(child, before);
    }),

    /**
     * Sets all children of a parent element via a single innerHTML assignment.
     * This is dramatically faster than N individual addChildHtml calls because
     * it eliminates per-child parseHtmlFragment + appendChild + addEventListeners overhead.
     * @param {string} parentId - The ID of the parent element.
     * @param {string} html - The concatenated HTML for all children.
     */
    setChildrenHtml: withSpan('setChildrenHtml', async (parentId, html) => {
        const parent = document.getElementById(parentId);
        if (parent) {
            parent.innerHTML = html;
            // Common events are pre-registered at document level via COMMON_EVENT_TYPES.
            // Scan for non-common/custom event types that need dynamic registration.
            ensureSubtreeEventListeners(parent);
        } else {
            console.error(`Parent element with ID ${parentId} not found for setChildrenHtml.`);
        }
    }),

    /**
     * Updates the text content of a DOM element.
     * @param {number} nodeId - The ID of the node to update.
     * @param {string} newText - The new text content.
     */
    updateTextContent: withSpan('updateTextContent', async (nodeId, newText) => {
        const node = document.getElementById(nodeId);
        if (node) {
            // Keep text nodes and form control values in sync
            node.textContent = newText;
            // If this is a textarea, also update its value property
            const tag = (node.tagName || '').toUpperCase();
            if (tag === 'TEXTAREA') {
                try { node.value = newText; } catch { /* ignore */ }
            }
        } else {
            console.error(`Node with ID ${nodeId} not found.`);
        }
    }),

    /**
     * Updates or adds an attribute of a DOM element.
     * @param {number} nodeId - The ID of the node to update.
     * @param {string} propertyName - The name of the attribute/property.
     * @param {string} propertyValue - The new value for the attribute/property.
     */
    updateAttribute: withSpan('updateAttribute', async (nodeId, propertyName, propertyValue) => {
        const node = document.getElementById(nodeId);
        if (!node) {
            console.error(`Node with ID ${nodeId} not found.`);
            return;
        }
        const lower = propertyName.toLowerCase();
        const isBooleanAttr = (
            lower === 'disabled' || lower === 'checked' || lower === 'selected' || lower === 'readonly' ||
            lower === 'multiple' || lower === 'required' || lower === 'autofocus' || lower === 'inert' ||
            lower === 'hidden' || lower === 'open' || lower === 'loop' || lower === 'muted' || lower === 'controls'
        );
        if (lower === 'value' && ('value' in node)) {
            // Keep the live value in sync for inputs/textareas
            node.value = propertyValue;
            node.setAttribute(propertyName, propertyValue);
        } else if (isBooleanAttr) {
            // Boolean attributes: presence => true
            node.setAttribute(propertyName, '');
            try { if (lower in node) node[lower] = true; } catch { /* ignore */ }
        } else {
            node.setAttribute(propertyName, propertyValue);
        }
        if (propertyName.startsWith('data-event-')) {
            const name = propertyName.substring('data-event-'.length);
            ensureEventListener(name);
        }
    }),

    addAttribute: withSpan('addAttribute', async (nodeId, propertyName, propertyValue) => {
        const node = document.getElementById(nodeId);
        if (!node) {
            console.error(`Node with ID ${nodeId} not found.`);
            return;
        }
        const lower = propertyName.toLowerCase();
        const isBooleanAttr = (
            lower === 'disabled' || lower === 'checked' || lower === 'selected' || lower === 'readonly' ||
            lower === 'multiple' || lower === 'required' || lower === 'autofocus' || lower === 'inert' ||
            lower === 'hidden' || lower === 'open' || lower === 'loop' || lower === 'muted' || lower === 'controls'
        );
        if (lower === 'value' && ('value' in node)) {
            node.value = propertyValue;
            node.setAttribute(propertyName, propertyValue);
        } else if (isBooleanAttr) {
            node.setAttribute(propertyName, '');
            try { if (lower in node) node[lower] = true; } catch { /* ignore */ }
        } else {
            node.setAttribute(propertyName, propertyValue);
        }
        if (propertyName.startsWith('data-event-')) {
            const name = propertyName.substring('data-event-'.length);
            ensureEventListener(name);
        }
    }),

    /**
     * Removes an attribute/property from a DOM element.
     * @param {number} nodeId - The ID of the node to update.
     * @param {string} propertyName - The name of the attribute/property to remove.
     */
    removeAttribute: withSpan('removeAttribute', async (nodeId, propertyName) =>{
        const node = document.getElementById(nodeId);
        if (node) {
            const lower = propertyName.toLowerCase();
            const isBooleanAttr = (
                lower === 'disabled' || lower === 'checked' || lower === 'selected' || lower === 'readonly' ||
                lower === 'multiple' || lower === 'required' || lower === 'autofocus' || lower === 'inert' ||
                lower === 'hidden' || lower === 'open' || lower === 'loop' || lower === 'muted' || lower === 'controls'
            );
            node.removeAttribute(propertyName);
            if (isBooleanAttr) {
                try { if (lower in node) node[lower] = false; } catch { /* ignore */ }
            }
        } else {
            console.error(`Node with ID ${nodeId} not found.`);
        }
    }),

    setLocalStorage: withSpan('setLocalStorage', async (key, value) => {
        localStorage.setItem(key, value);
    }),

    getLocalStorage: withSpan('getLocalStorage', (key) => {
        return localStorage.getItem(key);
    }),

    removeLocalStorage: withSpan('removeLocalStorage', async (key) => {
        localStorage.removeItem(key);
    }),

    getValue: withSpan('getValue', (id) => {
        const el = document.getElementById(id);
        return el ? el.value : null;
    }),

    /**
     * Sets the inner HTML of the 'app' div.
     * @param {string} html - The HTML content to set.
     */
    setAppContent: withSpan('setAppContent', async (html) => {
        document.body.innerHTML = html;
    // Keep runtime generic: no app-specific hooks here
        addEventListeners(); // Ensure event listeners are attached
        // Signal that the app is ready for interaction
        window.abiesReady = true;
    }),

    // Expose functions to .NET via JS interop (if needed)
    getCurrentUrl: () => {
        return window.location.href;
    },

    pushState: withSpan('pushState', async (url) => {
        history.pushState(null, "", url);
    }),

    replaceState: withSpan('replaceState', async (url) => {
        history.replaceState(null, "", url);
    }),

    back: withSpan('back', async (x) => {
        history.go(-x);
    }),

    forward: withSpan('forward', async (x) => {
        history.go(x);
    }),

    go: withSpan('go', async (x) => {
        history.go(x);
    }),

    load: withSpan('load', async (url) => {
        window.location.reload(url);
    }),

    reload: withSpan('reload', async () => {
        window.location.reload();
    }),

    onUrlChange: (callback) => {
    window.addEventListener("popstate", () => callback(window.location.href));
    },

    onFormSubmit: (callback) => {
        document.addEventListener("submit", (event) => {
            event.preventDefault();
            const form = event.target;
            callback(form.action);
        });
    },

    onLinkClick: (callback) => {
    document.addEventListener("click", (event) => {
            const link = event.target.closest("a");
            if (!link) return;
            // Skip if this anchor has Abies handlers; genericEventHandler will dispatch
            const hasAbiesHandler = Array.from(link.attributes).some(a => a.name.startsWith('data-event-'));
            if (hasAbiesHandler) return;
            // Skip anchors with empty or hash hrefs which are used as UI controls
            const rawHref = link.getAttribute('href') || '';
            if (rawHref === '' || rawHref === '#') return;
            event.preventDefault();
            callback(link.href);
        });
    // Do not globally prevent Enter here; Abies-managed keydown handles scoped prevention
    },

    subscribe: (key, kind, data) => {
        subscribe(key, kind, data);
    },

    unsubscribe: (key) => {
        unsubscribe(key);
    },

    /**
     * Apply a binary render batch to the DOM.
     * This is the zero-copy alternative to applyPatches that avoids JSON serialization.
     * The data parameter is a MemoryView into WASM memory, received as a Uint8Array.
     * @param {Uint8Array} batchData - The binary batch data.
     */
    applyBinaryBatch: withSpan('applyBinaryBatch', (batchData) => {
        applyBinaryBatchImpl(batchData);
    })
});

const config = getConfig();
const exports = await getAssemblyExports("Abies");

await runMain(); // Ensure the .NET runtime is initialized

// Pre-register all common event types now that the runtime is ready.
// This is done after runMain() to avoid TDZ issues with exports.
// Since ensureEventListener checks registeredEvents Set, this is idempotent.
COMMON_EVENT_TYPES.forEach(ensureEventListener);

// Make sure any existing data-event-* attributes in the initial DOM are discovered
try { addEventListeners(); } catch (err) { /* ignore */ }

// Defer OTel CDN upgrade to after first paint for faster startup
// The lightweight shim is already installed and working, this just upgrades it
scheduleDeferredOtelUpgrade();
