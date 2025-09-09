// wwwroot/js/pine.js

import { dotnet } from './_framework/dotnet.js';
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

// Optional: wire browser spans to Aspire via OTLP/HTTP if available, but never block app startup
void (async () => {
  try {
    const otelInit = (async () => {
      // Try to load the OTel API first; if it fails, keep using no-op
      let api;
      try {
        api = await import('https://unpkg.com/@opentelemetry/api@1.8.0/build/esm/index.js');
        trace = api.trace;
        SpanStatusCode = api.SpanStatusCode;
      } catch {}
      const [{ WebTracerProvider }] = await Promise.all([
        import('https://unpkg.com/@opentelemetry/sdk-trace-web@1.18.1/build/esm/index.js')
      ]);
      const { BatchSpanProcessor } = await import('https://unpkg.com/@opentelemetry/sdk-trace-base@1.18.1/build/esm/index.js');
      const { OTLPTraceExporter } = await import('https://unpkg.com/@opentelemetry/exporter-trace-otlp-http@0.50.0/build/esm/index.js');
      const { Resource } = await import('https://unpkg.com/@opentelemetry/resources@1.18.1/build/esm/index.js');
      const { SemanticResourceAttributes } = await import('https://unpkg.com/@opentelemetry/semantic-conventions@1.18.1/build/esm/index.js');

      const guessOtlp = () => {
        // Allow explicit global override
        if (window.__OTLP_ENDPOINT) return window.__OTLP_ENDPOINT;
        // Prefer a same-origin proxy to avoid CORS issues with collectors
        try { return new URL('/otlp/v1/traces', window.location.origin).href; } catch {}
        // Fallback to common local collector endpoints
        const candidates = [
          'http://localhost:4318/v1/traces', // default OTLP/HTTP collector
          'http://localhost:19062/v1/traces', // Aspire (http)
          'https://localhost:21202/v1/traces' // Aspire (https)
        ];
        return candidates[0];
      };

      const exporter = new OTLPTraceExporter({ url: guessOtlp() });
      const provider = new WebTracerProvider({
        resource: new Resource({ [SemanticResourceAttributes.SERVICE_NAME]: 'Abies.Web' })
      });
      provider.addSpanProcessor(new BatchSpanProcessor(exporter));
      provider.register();
      try {
        const { setGlobalTracerProvider } = api ?? await import('https://unpkg.com/@opentelemetry/api@1.8.0/build/esm/index.js');
        setGlobalTracerProvider(provider);
      } catch {}
    })();

    // Cap OTel init time so poor connectivity doesn't delay the app
    const timeout = new Promise((_, reject) => setTimeout(() => reject(new Error('OTel init timeout')), 1500));
    await Promise.race([otelInit, timeout]).catch(() => {});
  } catch {}
})();

const tracer = trace.getTracer('Abies.JS');

function withSpan(name, fn) {
    return async (...args) => {
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
    .withDiagnosticTracing(true)
    .withConfig({
        environmentVariables: {
            "MONO_LOG_LEVEL": "debug", //enable Mono VM detailed logging by
            "MONO_LOG_MASK": "all", // categories, could be also gc,aot,type,...
        }
    })
    .create();

const registeredEvents = new Set();

// Lightweight debug bridge always available; can enable console via consoleEnabled
if (typeof window !== 'undefined' && !window.__abiesDebug) {
    try {
        window.__abiesDebug = { logs: [], registeredEvents: [], events: [], replacements: [], attributes: [], consoleEnabled: false };
    } catch (e) { /* ignore */ }
}

function dbgLog() {
    try {
        if (window.__abiesDebug && window.__abiesDebug.consoleEnabled) {
            // eslint-disable-next-line no-console
            console.log.apply(console, arguments);
        }
    } catch { /* ignore */ }
}

function ensureEventListener(eventName) {
    if (registeredEvents.has(eventName)) return;
    dbgLog('[Abies Debug] ensureEventListener for', eventName);
    try { window.__abiesDebug && window.__abiesDebug.registeredEvents.push(eventName); } catch {}
    // Attach to document to survive body innerHTML changes and use capture for early handling
    const opts = (eventName === 'click') ? { capture: true } : undefined;
    document.addEventListener(eventName, genericEventHandler, opts);
    registeredEvents.add(eventName);
}

function genericEventHandler(event) {
    const name = event.type;
    // Normalize to an Element for closest(); handle rare Text node targets
    let origin = event.target;
    if (origin && origin.nodeType === 3 /* TEXT_NODE */) {
        origin = origin.parentElement;
    }
    const target = origin && origin.closest ? origin.closest(`[data-event-${name}]`) : null;
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
    const message = target.getAttribute(`data-event-${name}`);
    try {
        dbgLog('[Abies Debug] genericEventHandler', { name, targetId: target.id, message, isConnected: target.isConnected });
        try { window.__abiesDebug && window.__abiesDebug.logs.push({ type: 'genericEventHandler', name, targetId: target.id, message, isConnected: target.isConnected, time: Date.now() }); } catch {}
        // Structured event entry when debugging is enabled
        if (name === 'click' && window.__abiesDebug) {
            try {
                const text = (target.textContent || '').trim();
                window.__abiesDebug.events.push({ type: 'click', targetId: target.id || null, text, at: Date.now() });
            } catch { /* ignore */ }
        }
        // Prevent default only for Abies-managed Enter keydown events (scoped)
        if (name === 'keydown' && event && event.key === 'Enter') {
                try { event.preventDefault(); } catch { /* ignore */ }
            }
    } catch (err) {
        dbgLog('[Abies Debug] genericEventHandler failed to stringify target', err);
    }
    if (!message) {
        console.error(`No message id found in data-event-${name} attribute.`);
        return;
    }
    const span = tracer.startSpan('dispatchEvent', {
        attributes: {
            event: name,
            messageId: message
        }
    });
    try {
        const data = buildEventData(event, target);
        exports.Abies.Runtime.DispatchData(message, JSON.stringify(data));
    // Keydown Enter prevention is handled above; click default already prevented
        span.setStatus({ code: SpanStatusCode.OK });
    } catch (err) {
        span.recordException(err);
        span.setStatus({ code: SpanStatusCode.ERROR });
        // Do not rethrow to avoid crashing the page when handler IDs are stale
        console.error(err);
    } finally {
        span.end();
    }
}

function buildEventData(event, target) {
    const data = {};
    if (target && 'value' in target) data.value = target.value;
    if (target && 'checked' in target) data.checked = target.checked;
    if ('key' in event) {
        data.key = event.key;
        data.altKey = event.altKey;
        data.ctrlKey = event.ctrlKey;
        data.shiftKey = event.shiftKey;
    }
    if ('clientX' in event) {
        data.clientX = event.clientX;
        data.clientY = event.clientY;
        data.button = event.button;
    }
    return data;
}

/**
 * Adds event listeners to the document body for interactive elements.
 */
function addEventListeners(root) {
    const scope = root || document;
    // Build a list including the scope element (if Element) plus all descendants
    const nodes = [];
    if (scope && scope.nodeType === 1 /* ELEMENT_NODE */) nodes.push(scope);
    scope.querySelectorAll('*').forEach(el => {
        nodes.push(el);
    });
    nodes.forEach(el => {
        for (const attr of el.attributes) {
            if (attr.name.startsWith('data-event-')) {
                const name = attr.name.substring('data-event-'.length);
                try {
                    if (window.__abiesDebug && window.__abiesDebug.consoleEnabled) {
                        console.log('[Abies Debug] addEventListeners found', attr.name, 'on', el.id || el.tagName);
                    }
                    try { window.__abiesDebug && window.__abiesDebug.logs.push({ type: 'addEventListeners', attr: attr.name, el: el.id || el.tagName, time: Date.now() }); } catch {}
                } catch (err) {
                    /* ignore logging errors */
                }
                ensureEventListener(name);
            }
        }
    });
}

/**
 * Event handler for click events on elements with data-event-* attributes.
 * @param {Event} event - The DOM event.
 */


setModuleImports('abies.js', {

    /**
     * Adds a child element to a parent element in the DOM using HTML content.
     * @param {number} parentId - The ID of the parent element.
     * @param {string} childHtml - The HTML string of the child element to add.
     */
    addChildHtml: withSpan('addChildHtml', async (parentId, childHtml) => {
        const parent = document.getElementById(parentId);
        if (parent) {
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = childHtml;
            const childElement = tempDiv.firstElementChild;
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
     * Replaces an existing node with new HTML content.
     * @param {number} oldNodeId - The ID of the node to replace.
     * @param {string} newHtml - The HTML string to replace with.
     */
    replaceChildHtml: withSpan('replaceChildHtml', async (oldNodeId, newHtml) => {
        const oldNode = document.getElementById(oldNodeId);
        if (oldNode && oldNode.parentNode) {
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = newHtml;
            const newElement = tempDiv.firstElementChild;
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
        // Optional attribute debug logging
        try {
            if (window.__abiesDebug) {
                window.__abiesDebug.attributes.push({ op: 'update', name: propertyName, value: propertyValue, nodeId, el: node.id || node.tagName, at: Date.now() });
            }
        } catch { /* ignore */ }
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
        // Optional attribute debug logging
        try {
            if (window.__abiesDebug) {
                window.__abiesDebug.attributes.push({ op: 'add', name: propertyName, value: propertyValue, nodeId, el: node.id || node.tagName, at: Date.now() });
            }
        } catch { /* ignore */ }
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
            try { window.__abiesDebug && window.__abiesDebug.attributes.push({ op: 'remove', name: propertyName, nodeId, el: node.id || node.tagName, at: Date.now() }); } catch { /* ignore */ }
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
    }


});
    
const config = getConfig();
const exports = await getAssemblyExports("Abies");

await runMain(); // Ensure the .NET runtime is initialized

// Make sure any existing data-event-* attributes in the initial DOM are discovered
try { addEventListeners(); } catch (err) { /* ignore */ }
