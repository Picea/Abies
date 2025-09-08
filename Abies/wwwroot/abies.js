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
            "MONO_LOG_LEVEL": "debug", //enable Mono VM detailed logging by
            "MONO_LOG_MASK": "all", // categories, could be also gc,aot,type,...
        }
    })
    .create();

const registeredEvents = new Set();
// Expose a small debug bridge so tests/tools can read runtime events
if (typeof window !== 'undefined' && !window.__abiesDebug) {
    try {
        window.__abiesDebug = { logs: [], registeredEvents: [], consoleEnabled: false };
    } catch (e) { /* ignore */ }
}

function ensureEventListener(eventName) {
    if (registeredEvents.has(eventName)) return;
    if (window.__abiesDebug && window.__abiesDebug.consoleEnabled) {
        console.log('[Abies Debug] ensureEventListener for', eventName);
    }
    try { window.__abiesDebug && window.__abiesDebug.registeredEvents.push(eventName); } catch {}
    const opts = (eventName === 'click') ? { capture: true } : undefined;
    document.addEventListener(eventName, genericEventHandler, opts);
    registeredEvents.add(eventName);
}

function genericEventHandler(event) {
    const name = event.type;
    let origin = event.target;
    if (origin && origin.nodeType === 3 /* TEXT_NODE */) {
        origin = origin.parentElement;
    }
    const target = origin && origin.closest ? origin.closest(`[data-event-${name}]`) : null;
    if (!target) return;
    // Ignore events coming from nodes that have been detached/replaced
    if (!target.isConnected) return;
    if (name === 'click') {
        // For Abies-managed clicks, prevent native handlers and navigation early
        try {
            event.preventDefault();
            // Stop further propagation to avoid duplicate handlers interfering
            if (typeof event.stopPropagation === 'function') event.stopPropagation();
            if (typeof event.stopImmediatePropagation === 'function') event.stopImmediatePropagation();
        } catch { /* ignore */ }
    }
    const message = target.getAttribute(`data-event-${name}`);
    try {
        if (window.__abiesDebug && window.__abiesDebug.consoleEnabled) {
            console.log('[Abies Debug] genericEventHandler', { name, targetId: target.id, message, isConnected: target.isConnected });
        }
        try { window.__abiesDebug && window.__abiesDebug.logs.push({ type: 'genericEventHandler', name, targetId: target.id, message, isConnected: target.isConnected, time: Date.now() }); } catch {}
    } catch (err) {
        if (window.__abiesDebug && window.__abiesDebug.consoleEnabled) {
            console.log('[Abies Debug] genericEventHandler failed to stringify target', err);
        }
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
            addEventListeners(childElement); // Reattach event listeners to new subtree
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
            oldNode.parentNode.replaceChild(newElement, oldNode);
            addEventListeners(newElement); // Reattach event listeners to new subtree
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
            node.textContent = newText;
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
    // Generic debug hook (no app-specific attribute branches)
    try { window.__abiesDebug && window.__abiesDebug.logs.push({ type: 'updateAttribute', propertyName, propertyValue, nodeId, time: Date.now() }); } catch {}
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
    // Generic debug hook (no app-specific attribute branches)
    try { window.__abiesDebug && window.__abiesDebug.logs.push({ type: 'addAttribute', propertyName, propertyValue, nodeId, time: Date.now() }); } catch {}
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
    }


});
    
const config = getConfig();
const exports = await getAssemblyExports("Abies");

await runMain(); // Ensure the .NET runtime is initialized

// Make sure any existing data-event-* attributes in the initial DOM are discovered
try {
    addEventListeners();
    console.log('[Abies Debug] addEventListeners invoked after runMain');
} catch (err) {
    console.error('[Abies Debug] failed to invoke addEventListeners after runMain', err);
}