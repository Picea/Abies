// wwwroot/js/pine.js

import { dotnet } from './_framework/dotnet.js';
import { trace, SpanStatusCode } from 'https://unpkg.com/@opentelemetry/api@1.8.0/build/esm/index.js';

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
            "MONO_LOG_LEVEL": "debug",
            "MONO_LOG_MASK": "all",
        }
    })
    .create();

const registeredEvents = new Set();

function ensureEventListener(eventName) {
    if (registeredEvents.has(eventName)) return;
    document.body.addEventListener(eventName, genericEventHandler);
    registeredEvents.add(eventName);
}

function genericEventHandler(event) {
    const name = event.type;
    const target = event.target.closest(`[data-event-${name}]`);
    if (!target) return;
    // Ignore events coming from nodes that have been detached/replaced
    if (!target.isConnected) return;
    const message = target.getAttribute(`data-event-${name}`);
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
        if (name === 'click') event.preventDefault();
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

function addEventListeners() {
    document.querySelectorAll('*').forEach(el => {
        for (const attr of el.attributes) {
            if (attr.name.startsWith('data-event-')) {
                const name = attr.name.substring('data-event-'.length);
                ensureEventListener(name);
            }
        }
    });
}

setModuleImports('abies.js', {
    addChildHtml: withSpan('addChildHtml', async (parentId, childHtml) => {
        const parent = document.getElementById(parentId);
        if (parent) {
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = childHtml;
            const childElement = tempDiv.firstElementChild;
            parent.appendChild(childElement);
            addEventListeners();
        } else {
            console.error(`Parent element with ID ${parentId} not found.`);
        }
    }),
    setTitle: withSpan('setTitle', async (title) => {
        document.title = title;
    }),
    removeChild: withSpan('removeChild', async (parentId, childId) =>  {
        const parent = document.getElementById(parentId);
        const child = document.getElementById(childId);
        if (parent && child && parent.contains(child)) {
            parent.removeChild(child);
        } else {
            console.error(`Cannot remove child with ID ${childId} from parent with ID ${parentId}.`);
        }
    }),
    replaceChildHtml: withSpan('replaceChildHtml', async (oldNodeId, newHtml) => {
        const oldNode = document.getElementById(oldNodeId);
        if (oldNode && oldNode.parentNode) {
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = newHtml;
            const newElement = tempDiv.firstElementChild;
            oldNode.parentNode.replaceChild(newElement, oldNode);
            addEventListeners();
        } else {
            console.error(`Node with ID ${oldNodeId} not found or has no parent.`);
        }
    }),
    updateTextContent: withSpan('updateTextContent', async (nodeId, newText) => {
        const node = document.getElementById(nodeId);
        if (node) {
            node.textContent = newText;
        } else {
            console.error(`Node with ID ${nodeId} not found.`);
        }
    }),
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
    setAppContent: withSpan('setAppContent', async (html) => {
        document.body.innerHTML = html;
        addEventListeners();
        window.abiesReady = true;
    }),
    getCurrentUrl: () => window.location.href,
    pushState: withSpan('pushState', async (url) => { history.pushState(null, "", url); }),
    replaceState: withSpan('replaceState', async (url) => { history.replaceState(null, "", url); }),
    back: withSpan('back', async (x) => { history.go(-x); }),
    forward: withSpan('forward', async (x) => { history.go(x); }),
    go: withSpan('go', async (x) => { history.go(x); }),
    load: withSpan('load', async (url) => { window.location.reload(url); }),
    reload: withSpan('reload', async () => { window.location.reload(); }),
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
    }
});

const config = getConfig();
const exports = await getAssemblyExports("Abies");
await runMain();