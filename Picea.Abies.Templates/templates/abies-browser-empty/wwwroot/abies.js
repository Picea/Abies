// =============================================================================
// abies.js — Browser-Side Runtime for Abies
// =============================================================================
//
// WASM Bootstrap:
//   This module is both the browser-side runtime AND the WASM entry point.
//   Consumer applications only need a single script tag in their index.html:
//
//     <script type="module" src="abies.js"></script>
//
//   The last lines of this file import dotnet.js and call dotnet.run(),
//   which downloads WASM assets, starts the .NET runtime, and invokes
//   Program.Main(). No separate main.js or dotnet.js script tag is needed.
//
//   Boot chain:
//     1. Browser loads abies.js as an ES module (from <script> tag)
//     2. abies.js defines all exports (DOM mutations, events, navigation)
//     3. abies.js imports ./_framework/dotnet.js and calls dotnet.run()
//     4. .NET runtime starts → Program.Main() runs
//     5. Program.Main() calls Runtime.Run<TProgram, TModel, TArgument>()
//     6. Runtime.Run() calls JSHost.ImportAsync("Abies", "../abies.js")
//        → browser returns the CACHED module (ES modules evaluate once)
//        → no re-execution of bootstrap code
//     7. Runtime.Run() wires callbacks, sets up events/navigation
//     8. MVU loop starts, initial render paints the UI
//
//   ES Module Idempotency (ECMAScript §15.2.1.16.5):
//     Modules are evaluated exactly once per URL. When Runtime.Run() re-imports
//     abies.js via JSHost.ImportAsync, the browser returns the cached module
//     namespace without re-evaluating any top-level code. The bootstrap
//     (import dotnet + dotnet.run()) runs once and only once.
//
//   InteractiveWasm/Auto guard:
//     In server-rendered mode, the server inlines its own <script> that calls
//     dotnet.run() and sets globalThis.__ABIES_DOTNET_STARTED = true. When
//     Runtime.Run() later imports abies.js for the first time, the guard at
//     the bottom of this file skips dotnet.run() to avoid double-bootstrap.
//
// Architecture:
//   .NET produces binary patch batches (RenderBatchWriter.cs)
//   → transferred via MemoryView (zero-copy) to JS
//   → this module reads the binary format and applies DOM mutations
//
// Binary Protocol:
//   Header (8 bytes):
//     int32 patchCount
//     int32 stringTableOffset
//
//   Patches (16 bytes each):
//     int32 type (BinaryPatchType enum)
//     int32 field1 (string table index, -1 = null)
//     int32 field2 (string table index, -1 = null)
//     int32 field3 (string table index, -1 = null)
//
//   String Table:
//     Sequence of LEB128-prefixed UTF-8 strings
//
// Event Delegation:
//   Single listener per event type at document level.
//   Target elements carry data-event-{type}="{commandId}" attributes.
//   When an event fires, walk up from target looking for the attribute,
//   extract commandId, serialize relevant event data, call DispatchDomEvent.
//
// Callback Wiring:
//   The .NET side wires all callbacks via JSImport during Runtime.Run():
//     setDispatchCallback(fn)    — passes DispatchDomEvent [JSExport]
//     setOnUrlChangedCallback(fn) — passes OnUrlChanged [JSExport]
//
// OpenTelemetry Integration:
//   When <meta name="otel-verbosity"> (or legacy <meta name="abies-otel-verbosity">)
//   is present, this module dynamically imports abies-otel.js which loads the OTel
//   Web SDK from CDN and instruments:
//     - DOM events → spans with command IDs and event attributes
//     - fetch() → traceparent header injection for browser→server correlation
//     - DOM mutation batches → spans (debug verbosity only)
//   Configuration priority (highest first):
//     1. URL query parameter: ?otel-verbosity=off|user|debug
//     2. window.__otel.verbosity (if present and string)
//     3. <meta name="otel-verbosity" content="...">
//     4. <meta name="abies-otel-verbosity" content="..."> (legacy/internal)
//   If CDN loading fails, a no-op shim is used — the app still works.
//
// See also:
//   - Picea.Abies.Browser/Runtime.cs — C# bootstrap (callback wiring, MVU start)
//   - Picea.Abies.Browser/Interop.cs — JSImport/JSExport declarations
//   - Picea.Abies/RenderBatchWriter.cs — binary serialization (.NET side)
//   - Picea.Abies/DOM/Patch.cs — patch type definitions
//   - abies-otel.js — OpenTelemetry instrumentation module
// =============================================================================

// =============================================================================
// Patch Type Opcodes — must match BinaryPatchType enum in RenderBatchWriter.cs
// =============================================================================
const OP_ADD_ROOT         = 0;
const OP_REPLACE_CHILD    = 1;
const OP_ADD_CHILD        = 2;
const OP_REMOVE_CHILD     = 3;
const OP_CLEAR_CHILDREN   = 4;
const OP_SET_CHILDREN_HTML = 5;
const OP_MOVE_CHILD       = 6;
const OP_UPDATE_ATTRIBUTE = 7;
const OP_ADD_ATTRIBUTE    = 8;
const OP_REMOVE_ATTRIBUTE = 9;
const OP_ADD_HANDLER      = 10;
const OP_REMOVE_HANDLER   = 11;
const OP_UPDATE_HANDLER   = 12;
const OP_UPDATE_TEXT      = 13;
const OP_ADD_TEXT         = 14;
const OP_REMOVE_TEXT      = 15;
const OP_ADD_RAW          = 16;
const OP_REMOVE_RAW       = 17;
const OP_REPLACE_RAW      = 18;
const OP_UPDATE_RAW       = 19;
const OP_ADD_HEAD_ELEMENT    = 20;
const OP_UPDATE_HEAD_ELEMENT = 21;
const OP_REMOVE_HEAD_ELEMENT = 22;
const OP_APPEND_CHILDREN_HTML = 23;

// =============================================================================
// Event types to register for delegation
// =============================================================================
const COMMON_EVENT_TYPES = [
    "click", "dblclick", "input", "change", "submit",
    "keydown", "keyup", "keypress",
    "focus", "blur", "focusin", "focusout",
    "mousedown", "mouseup", "mousemove",
    "mouseenter", "mouseleave", "mouseover", "mouseout",
    "wheel", "scroll",
    "touchstart", "touchmove", "touchend",
    "pointerdown", "pointerup", "pointermove",
    "contextmenu",
    "drag", "dragstart", "dragend", "dragover",
    "drop", "dragenter", "dragleave",
    "copy", "cut", "paste",
    "animationstart", "animationend", "transitionend",
    "load", "error", "resize",
    "select", "reset", "toggle"
];

// =============================================================================
// UTF-8 text decoder (reused)
// =============================================================================
const utf8Decoder = new TextDecoder("utf-8");

// =============================================================================
// DispatchDomEvent callback — set by .NET during initialization
// =============================================================================
let dispatchDomEvent = null;

// =============================================================================
// OpenTelemetry module — loaded dynamically when configured
// =============================================================================
let otelModule = null;

/**
 * Resolves OTel verbosity from multiple configuration sources.
 * Priority (highest first):
 *  1. URL query parameter: ?otel-verbosity=off|user|debug
 *  2. window.__otel.verbosity (if present and string)
 *  3. <meta name="otel-verbosity" content="...">
 *  4. <meta name="abies-otel-verbosity" content="..."> (legacy/internal)
 *
 * @returns {string|null} The verbosity level, or null if not configured.
 */
function resolveOtelVerbosity() {
    // 1. URL parameter (?otel-verbosity=...)
    try {
        const url = new URL(window.location.href);
        const urlVerbosity = url.searchParams.get("otel-verbosity");
        if (typeof urlVerbosity === "string" && urlVerbosity.length > 0) {
            return urlVerbosity;
        }
    } catch {
        // Ignore URL parsing issues
    }

    // 2. window.__otel.verbosity
    if (typeof window !== "undefined" && window.__otel && typeof window.__otel.verbosity === "string") {
        if (window.__otel.verbosity.length > 0) {
            return window.__otel.verbosity;
        }
    }

    // 3. Meta tags (prefer documented name, support legacy name for compatibility)
    const meta =
        document.querySelector('meta[name="otel-verbosity"]') ||
        document.querySelector('meta[name="abies-otel-verbosity"]');

    if (meta && typeof meta.content === "string" && meta.content.length > 0) {
        return meta.content;
    }

    return null;
}

/**
 * Initializes OpenTelemetry if configured via meta tag, URL param, or window.__otel.
 * Called once during setupEventDelegation.
 */
async function initializeOtel() {
    const verbosity = resolveOtelVerbosity();
    if (!verbosity || verbosity === "off") return;

    try {
        // Resolve the OTel module path relative to this script
        // In WASM mode: abies.js is at /abies.js, so otel is at /abies-otel.js
        // In server mode: abies.js is loaded from the server
        const scriptUrl = new URL("./abies-otel.js", import.meta.url);
        const mod = await import(/* webpackIgnore: true */ scriptUrl.href);
        const success = await mod.initialize(verbosity);

        // Only keep the module reference if initialization succeeded.
        // When CDN loading fails, initialize() returns false and uses a
        // no-op shim internally — no point calling traceEvent/traceBatch.
        otelModule = success ? mod : null;
    } catch (err) {
        console.warn("[abies] Failed to load OTel module. Tracing disabled.", err);
        otelModule = null;
    }
}

// =============================================================================
// HTML fragment parser helper — reusable <template> container
// =============================================================================
// Uses <template> instead of <div> to avoid HTML5 foster parenting.
// The HTML parser enforces context-sensitive rules: <tr> is invalid inside
// <div>, so the parser strips it. <template> has an inert DocumentFragment
// that accepts any HTML structure, preserving <tr>, <td>, <option>, etc.
// See: HTML Living Standard §13.2.6.1 (foster parenting algorithm)
// =============================================================================
const _fragmentTemplate = document.createElement("template");

/**
 * Parses an HTML string into a DOM element.
 * Uses <template> to preserve context-dependent elements (tr, td, option, etc.).
 * @param {string} html - The HTML to parse.
 * @returns {Element} The parsed DOM element.
 */
function parseHtmlFragment(html) {
    _fragmentTemplate.innerHTML = html;
    return _fragmentTemplate.content.firstElementChild;
}

// =============================================================================
// Binary Reader
// =============================================================================

/**
 * Reads a LEB128-encoded unsigned integer from the data view.
 * @param {Uint8Array} bytes - The binary data.
 * @param {{ offset: number }} state - Mutable offset tracker.
 * @returns {number} The decoded integer.
 */
function readLeb128(bytes, state) {
    let result = 0;
    let shift = 0;
    let b;
    do {
        b = bytes[state.offset++];
        result |= (b & 0x7F) << shift;
        shift += 7;
    } while (b & 0x80);
    return result;
}

/**
 * Reads the string table from binary data.
 * @param {Uint8Array} bytes - The full binary data.
 * @param {number} offset - Byte offset where string table starts.
 * @param {number} totalLength - Total byte length of the data.
 * @returns {string[]} Array of decoded strings.
 */
function readStringTable(bytes, offset, totalLength) {
    const strings = [];
    const state = { offset };

    while (state.offset < totalLength) {
        const byteLen = readLeb128(bytes, state);
        const str = utf8Decoder.decode(bytes.subarray(state.offset, state.offset + byteLen));
        strings.push(str);
        state.offset += byteLen;
    }

    return strings;
}

// =============================================================================
// Event Data Extraction
// =============================================================================

/**
 * Extracts relevant data from a DOM event for serialization to .NET.
 * @param {Event} event - The DOM event.
 * @returns {string} JSON-serialized event data.
 */
function extractEventData(event) {
    const data = {};

    // Input/textarea value
    if (event.target && "value" in event.target) {
        data.value = event.target.value;
    }

    // Checkbox checked state
    if (event.target && "checked" in event.target) {
        data.checked = event.target.checked;
    }

    // Keyboard events
    if (event instanceof KeyboardEvent) {
        data.key = event.key;
        data.code = event.code;
        data.altKey = event.altKey;
        data.ctrlKey = event.ctrlKey;
        data.shiftKey = event.shiftKey;
        data.metaKey = event.metaKey;
    }

    // Mouse events
    if (event instanceof MouseEvent) {
        data.clientX = event.clientX;
        data.clientY = event.clientY;
        data.button = event.button;
        data.altKey = event.altKey;
        data.ctrlKey = event.ctrlKey;
        data.shiftKey = event.shiftKey;
        data.metaKey = event.metaKey;
    }

    // Pointer events
    if (typeof PointerEvent !== "undefined" && event instanceof PointerEvent) {
        data.pointerId = event.pointerId;
        data.pointerType = event.pointerType;
        data.pressure = event.pressure;
    }

    // Touch events
    if (typeof TouchEvent !== "undefined" && event instanceof TouchEvent) {
        data.touches = Array.from(event.touches).map(t => ({
            clientX: t.clientX,
            clientY: t.clientY,
            identifier: t.identifier
        }));
    }

    // Drag events
    if (event instanceof DragEvent && event.dataTransfer) {
        data.types = event.dataTransfer.types;
    }

    // Wheel events
    if (event instanceof WheelEvent) {
        data.deltaX = event.deltaX;
        data.deltaY = event.deltaY;
        data.deltaZ = event.deltaZ;
        data.deltaMode = event.deltaMode;
    }

    return JSON.stringify(data);
}

// =============================================================================
// Event Delegation
// =============================================================================

/**
 * Registered event type set (avoid duplicate listeners).
 */
const registeredEventTypes = new Set();

/**
 * Registers a single event type for delegation at the document level.
 * @param {string} eventType - The DOM event name (e.g., "click").
 */
function registerEventType(eventType) {
    if (registeredEventTypes.has(eventType)) return;
    registeredEventTypes.add(eventType);

    // Use capture for focus/blur (they don't bubble)
    const useCapture = eventType === "focus" || eventType === "blur"
        || eventType === "focusin" || eventType === "focusout";

    document.addEventListener(eventType, (event) => {
        if (!dispatchDomEvent) return;

        const attrName = `data-event-${eventType}`;

        // Walk up from target to find the handler attribute
        let el = event.target;
        while (el && el !== document) {
            if (el.hasAttribute && el.hasAttribute(attrName)) {
                const commandId = el.getAttribute(attrName);
                const eventData = extractEventData(event);

                // OTel: trace the event dispatch if instrumentation is active
                if (otelModule) {
                    otelModule.traceEvent(commandId, eventType, eventData);
                }

                dispatchDomEvent(commandId, eventType, eventData);

                // Prevent default for form submissions
                if (eventType === "submit") {
                    event.preventDefault();
                }

                // Prevent Enter in a handled keydown from also submitting
                // the enclosing form (browser default for input elements).
                if (eventType === "keydown" && event.key === "Enter") {
                    event.preventDefault();
                }

                // Prevent default on click events for anchor elements with
                // Abies event handlers — stops the browser from following
                // href="" which would trigger a competing navigation.
                if (eventType === "click") {
                    let clickedAnchor = event.target;
                    while (clickedAnchor && clickedAnchor.tagName !== "A") {
                        clickedAnchor = clickedAnchor.parentElement;
                    }
                    if (clickedAnchor) {
                        event.preventDefault();
                    }
                }
                return;
            }
            el = el.parentElement;
        }
    }, useCapture);
}

// =============================================================================
// DOM Mutation Handlers
// =============================================================================

/**
 * Applies a single patch to the DOM.
 * @param {number} type - The BinaryPatchType opcode.
 * @param {string|null} f1 - Field 1 (string from string table, or null).
 * @param {string|null} f2 - Field 2.
 * @param {string|null} f3 - Field 3.
 */
function applyPatch(type, f1, f2, f3) {
    switch (type) {
        // =====================================================================
        // Tree Mutations
        // =====================================================================

        case OP_ADD_ROOT: {
            // f1 = rootId (unused — we render directly into document.body)
            // f2 = html
            document.body.innerHTML = f2;
            break;
        }

        case OP_REPLACE_CHILD: {
            // f1 = oldElementId, f2 = newElementId (unused), f3 = html
            const oldEl = document.getElementById(f1);
            if (oldEl) {
                const newEl = parseHtmlFragment(f3);
                if (newEl) {
                    oldEl.replaceWith(newEl);
                }
            }
            break;
        }

        case OP_ADD_CHILD: {
            // f1 = parentId, f2 = childId (unused), f3 = html
            const parent = document.getElementById(f1);
            if (parent) {
                const child = parseHtmlFragment(f3);
                if (child) {
                    parent.appendChild(child);
                }
            }
            break;
        }

        case OP_REMOVE_CHILD: {
            // f1 = parentId (unused), f2 = childId
            const child = document.getElementById(f2);
            if (child) {
                child.remove();
            }
            break;
        }

        case OP_CLEAR_CHILDREN: {
            // f1 = parentId
            const parent = document.getElementById(f1);
            if (parent) {
                parent.innerHTML = "";
            }
            break;
        }

        case OP_SET_CHILDREN_HTML: {
            // f1 = parentId, f2 = concatenated children html
            const parent = document.getElementById(f1);
            if (parent) {
                parent.innerHTML = f2;
            }
            break;
        }

        case OP_APPEND_CHILDREN_HTML: {
            // f1 = parentId, f2 = concatenated children html
            // Uses insertAdjacentHTML to preserve existing children and append new ones.
            // Unlike innerHTML, this doesn't destroy existing DOM nodes or event state.
            // Unlike parseHtmlFragment, this respects the parent's parsing context
            // (e.g., <tr> is valid inside <tbody>).
            const parent = document.getElementById(f1);
            if (parent) {
                parent.insertAdjacentHTML("beforeend", f2);
            }
            break;
        }

        case OP_MOVE_CHILD: {
            // f1 = parentId, f2 = childId, f3 = beforeId (null = append)
            const parent = document.getElementById(f1);
            const child = document.getElementById(f2);
            if (parent && child) {
                if (f3) {
                    const before = document.getElementById(f3);
                    if (before) {
                        parent.insertBefore(child, before);
                    }
                } else {
                    parent.appendChild(child);
                }
            }
            break;
        }

        // =====================================================================
        // Attribute Mutations
        // =====================================================================

        case OP_UPDATE_ATTRIBUTE:
        case OP_ADD_ATTRIBUTE: {
            // f1 = elementId, f2 = attrName, f3 = attrValue
            const el = document.getElementById(f1);
            if (el) {
                el.setAttribute(f2, f3);
                // For properties that diverge from their HTML attributes after
                // user interaction (value, checked), also set the DOM property
                // directly. setAttribute("value", "") only sets the default value,
                // not the live value that the user sees.
                if (f2 === "value" && "value" in el) {
                    el.value = f3;
                } else if (f2 === "checked" && "checked" in el) {
                    el.checked = f3 === "" || f3 === "true" || f3 === "checked";
                }
            }
            break;
        }

        case OP_REMOVE_ATTRIBUTE: {
            // f1 = elementId, f2 = attrName
            const el = document.getElementById(f1);
            if (el) {
                el.removeAttribute(f2);
            }
            break;
        }

        // =====================================================================
        // Handler Mutations — render as data-event-{name}="{commandId}"
        // =====================================================================
        // NOTE: f2 is the full attribute name (e.g. "data-event-click"),
        // not just the event name. This is because Handler.Name in C#
        // returns the Attribute.Name (the base record's Name property),
        // which is already prefixed with "data-event-".
        // =====================================================================

        case OP_ADD_HANDLER: {
            // f1 = elementId, f2 = attribute name (e.g. "data-event-click"), f3 = commandId
            const el = document.getElementById(f1);
            if (el) {
                el.setAttribute(f2, f3);
            }
            break;
        }

        case OP_REMOVE_HANDLER: {
            // f1 = elementId, f2 = attribute name, f3 = commandId (unused for removal)
            const el = document.getElementById(f1);
            if (el) {
                el.removeAttribute(f2);
            }
            break;
        }

        case OP_UPDATE_HANDLER: {
            // f1 = elementId, f2 = attribute name, f3 = newCommandId
            const el = document.getElementById(f1);
            if (el) {
                el.setAttribute(f2, f3);
            }
            break;
        }

        // =====================================================================
        // Text Mutations
        // =====================================================================

        case OP_UPDATE_TEXT: {
            // f1 = parentId, f2 = newText, f3 = newId (unused in DOM)
            const parent = document.getElementById(f1);
            if (parent) {
                // Find the text node child and update it
                for (const child of parent.childNodes) {
                    if (child.nodeType === Node.TEXT_NODE) {
                        child.textContent = f2;
                        break;
                    }
                }
            }
            break;
        }

        case OP_ADD_TEXT: {
            // f1 = parentId, f2 = text content, f3 = textId (unused in DOM)
            const parent = document.getElementById(f1);
            if (parent) {
                parent.appendChild(document.createTextNode(f2));
            }
            break;
        }

        case OP_REMOVE_TEXT: {
            // f1 = parentId, f2 = textId (unused — remove first text node)
            const parent = document.getElementById(f1);
            if (parent) {
                for (const child of parent.childNodes) {
                    if (child.nodeType === Node.TEXT_NODE) {
                        child.remove();
                        break;
                    }
                }
            }
            break;
        }

        // =====================================================================
        // Raw HTML Mutations
        // =====================================================================

        case OP_ADD_RAW: {
            // f1 = parentId, f2 = html, f3 = rawId
            const parent = document.getElementById(f1);
            if (parent) {
                const wrapper = document.createElement("span");
                wrapper.id = f3;
                wrapper.innerHTML = f2;
                parent.appendChild(wrapper);
            }
            break;
        }

        case OP_REMOVE_RAW: {
            // f1 = parentId (unused), f2 = rawId
            const el = document.getElementById(f2);
            if (el) {
                el.remove();
            }
            break;
        }

        case OP_REPLACE_RAW: {
            // f1 = oldId, f2 = newId, f3 = newHtml
            const oldEl = document.getElementById(f1);
            if (oldEl) {
                const wrapper = document.createElement("span");
                wrapper.id = f2;
                wrapper.innerHTML = f3;
                oldEl.replaceWith(wrapper);
            }
            break;
        }

        case OP_UPDATE_RAW: {
            // f1 = nodeId, f2 = newHtml, f3 = newId (unused — same node)
            const el = document.getElementById(f1);
            if (el) {
                el.innerHTML = f2;
            }
            break;
        }

        // =====================================================================
        // Head Element Mutations
        // =====================================================================
        // Managed head elements are identified by data-abies-head="{key}".
        // These never conflict with user-defined head elements from index.html.
        // =====================================================================

        case OP_ADD_HEAD_ELEMENT: {
            // f1 = key, f2 = html
            const head = document.head;
            if (head) {
                const el = parseHtmlFragment(f2);
                if (el) {
                    head.appendChild(el);
                }
            }
            break;
        }

        case OP_UPDATE_HEAD_ELEMENT: {
            // f1 = key, f2 = html
            const head = document.head;
            if (head) {
                const existing = head.querySelector(`[data-abies-head="${f1}"]`);
                if (existing) {
                    const newEl = parseHtmlFragment(f2);
                    if (newEl) {
                        existing.replaceWith(newEl);
                    }
                } else {
                    // Fallback: element not found, add it
                    const el = parseHtmlFragment(f2);
                    if (el) {
                        head.appendChild(el);
                    }
                }
            }
            break;
        }

        case OP_REMOVE_HEAD_ELEMENT: {
            // f1 = key
            const head = document.head;
            if (head) {
                const existing = head.querySelector(`[data-abies-head="${f1}"]`);
                if (existing) {
                    existing.remove();
                }
            }
            break;
        }
    }
}

// =============================================================================
// Exports — called by .NET via JSImport
// =============================================================================

/**
 * Sets innerHTML on the app root for the initial render.
 * @param {string} rootId - The root element ID (e.g., "app").
 * @param {string} html - The full HTML to set.
 */
export function renderInitial(rootId, html) {
    const root = document.getElementById(rootId);
    if (root) {
        root.innerHTML = html;
    }
}

/**
 * Applies a binary-encoded batch of DOM patches.
 *
 * The data is received as a MemoryView (Span<byte> from .NET).
 * We must call .slice() to get a stable Uint8Array before
 * the interop call returns (the Span is stack-allocated).
 *
 * @param {MemoryView} batchData - The binary patch data from .NET.
 */
export function applyBinaryBatch(batchData) {
    // MemoryView → stable Uint8Array (must copy before interop returns)
    const bytes = batchData.slice();
    const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);

    // Read header
    const patchCount = view.getInt32(0, true);        // little-endian
    const stringTableOffset = view.getInt32(4, true);

    // Read string table
    const strings = readStringTable(bytes, stringTableOffset, bytes.byteLength);

    // OTel: trace the batch if debug verbosity is active
    if (otelModule) {
        otelModule.traceBatch(patchCount);
    }

    // Apply each patch
    const headerSize = 8;
    const entrySize = 16;

    for (let i = 0; i < patchCount; i++) {
        const offset = headerSize + (i * entrySize);
        const type = view.getInt32(offset, true);
        const f1Idx = view.getInt32(offset + 4, true);
        const f2Idx = view.getInt32(offset + 8, true);
        const f3Idx = view.getInt32(offset + 12, true);

        const f1 = f1Idx >= 0 ? strings[f1Idx] : null;
        const f2 = f2Idx >= 0 ? strings[f2Idx] : null;
        const f3 = f3Idx >= 0 ? strings[f3Idx] : null;

        applyPatch(type, f1, f2, f3);
    }

    // Signal that WASM has taken over rendering after the first batch.
    // In InteractiveAuto mode, the server renders the page initially,
    // then WASM boots and applies patches. This attribute tells tests
    // and tooling that WASM is ready for interaction.
    if (!applyBinaryBatch._signaled) {
        applyBinaryBatch._signaled = true;
        document.body.setAttribute("data-abies-mode", "wasm");
    }
}

/**
 * Sets the document title.
 * @param {string} title - The new page title.
 */
export function setTitle(title) {
    document.title = title;
}

/**
 * Navigates via history.pushState and dispatches a popstate event.
 * @param {string} url - The target URL.
 */
export function navigateTo(url) {
    history.pushState(null, "", url);
    // Trigger popstate so the .NET side picks up the URL change
    window.dispatchEvent(new PopStateEvent("popstate"));
}

/**
 * Replaces the current URL via history.replaceState (no new history entry).
 * @param {string} url - The target URL.
 */
export function replaceUrl(url) {
    history.replaceState(null, "", url);
    // Trigger popstate so the .NET side picks up the URL change
    window.dispatchEvent(new PopStateEvent("popstate"));
}

/**
 * Navigates back one step in browser history.
 */
export function historyBack() {
    history.back();
}

/**
 * Navigates forward one step in browser history.
 */
export function historyForward() {
    history.forward();
}

/**
 * Navigates to an external URL (full page load).
 * @param {string} href - The external URL.
 */
export function externalNavigate(href) {
    window.location.href = href;
}

/**
 * Returns the browser window's origin (e.g., "http://localhost:5000").
 * Used by WASM apps to resolve API URLs relative to the hosting server.
 * @returns {string} The window origin, or empty string if unavailable.
 */
export function getOrigin() {
    return globalThis.location?.origin ?? "";
}

/**
 * Returns the current browser URL (window.location.href).
 * Called by .NET during initialization to determine the initial route.
 * @returns {string} The current URL.
 */
export function getCurrentUrl() {
    return window.location.href;
}

/**
 * Sets up event delegation for all common event types.
 * Called once during initialization.
 */
export function setupEventDelegation() {
    COMMON_EVENT_TYPES.forEach(registerEventType);

    // Initialize OpenTelemetry if configured (non-blocking)
    initializeOtel();
}

// =============================================================================
// Navigation — URL Change Detection & Link Click Interception
// =============================================================================
// Two event sources feed URL changes into the .NET subscription:
//
//   1. popstate — fired by browser back/forward or programmatic
//      history.pushState/replaceState (we fire synthetic popstate after those)
//
//   2. Click interception — internal <a> links are caught before the browser
//      navigates, pushed via pushState, and reported to .NET
//
// The onUrlChanged callback is wired during setupNavigation and calls the
// .NET [JSExport] OnUrlChanged method.
// =============================================================================

let onUrlChangedCallback = null;

/**
 * Sets up navigation: registers popstate listener and intercepts internal link clicks.
 * Called once during initialization. The .NET side has already set up the
 * OnUrlChanged [JSExport], which is called via the dispatch callback.
 */
export function setupNavigation() {
    // Get the .NET callback for URL changes
    onUrlChangedCallback = onUrlChangedDotNet;

    // Listen for popstate events (back/forward, pushState, replaceState)
    window.addEventListener("popstate", () => {
        if (onUrlChangedCallback) {
            onUrlChangedCallback(window.location.pathname + window.location.search + window.location.hash);
        }
    });

    // Intercept clicks on <a> elements for client-side routing
    document.addEventListener("click", (event) => {
        // Only intercept left-click without modifiers
        if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
            return;
        }

        // Walk up from the click target to find an <a> element
        let anchor = event.target;
        while (anchor && anchor.tagName !== "A") {
            anchor = anchor.parentElement;
        }

        if (!anchor) return;

        // Skip links that have an Abies event handler — the event delegation
        // system already dispatched the click as a message to .NET. Re-routing
        // via pushState would reset the page state.
        if (anchor.hasAttribute("data-event-click")) {
            event.preventDefault();
            return;
        }

        // Skip links with target attributes (e.g., target="_blank")
        if (anchor.hasAttribute("target")) return;

        // Skip links with download attribute
        if (anchor.hasAttribute("download")) return;

        // Skip links with rel="external"
        if (anchor.getAttribute("rel") === "external") return;

        const href = anchor.getAttribute("href");
        if (!href) return;

        // Skip javascript: URLs
        if (href.startsWith("javascript:")) return;

        // Skip anchor-only links (just "#")
        if (href === "#") return;

        // Determine if internal: same origin or relative path
        try {
            const url = new URL(href, window.location.origin);

            if (url.origin !== window.location.origin) {
                // External link — let the browser handle it
                return;
            }

            // Internal link — prevent default, push state, notify .NET
            event.preventDefault();
            const path = url.pathname + url.search + url.hash;
            history.pushState(null, "", path);

            if (onUrlChangedCallback) {
                onUrlChangedCallback(path);
            }
        } catch {
            // Invalid URL — let the browser handle it
            return;
        }
    });
}

// .NET OnUrlChanged callback — set after assembly exports are loaded
let onUrlChangedDotNet = null;

/**
 * Sets the .NET callback for URL change notifications.
 * Called by Runtime.Run() via JSImport during initialization.
 * @param {Function} callback - The OnUrlChanged function from .NET.
 */
export function setOnUrlChangedCallback(callback) {
    onUrlChangedDotNet = callback;
}

/**
 * Sets the .NET callback for dispatching DOM events.
 * Called by Runtime.Run() via JSImport during initialization.
 * @param {Function} callback - The DispatchDomEvent function from .NET.
 */
export function setDispatchCallback(callback) {
    dispatchDomEvent = callback;
}

// =============================================================================
// WASM Bootstrap — Self-Initializing Module
// =============================================================================
// This section makes abies.js the single entry point for browser WASM apps.
// Consumer index.html only needs: <script type="module" src="abies.js"></script>
//
// IMPORTANT: Do NOT use top-level `await` here!
//
// If abies.js uses `await dotnet.run()`, the module evaluation blocks until
// dotnet.run() completes. But dotnet.run() starts the .NET runtime, which
// calls Runtime.Run() → JSHost.ImportAsync("../abies.js"). JSHost.ImportAsync
// waits for abies.js module evaluation to finish — creating a DEADLOCK:
//
//   abies.js awaits dotnet.run()
//     → dotnet.run() starts .NET → Runtime.Run() → JSHost.ImportAsync("abies.js")
//       → JSHost.ImportAsync waits for abies.js evaluation to complete
//         → abies.js is waiting for dotnet.run() → DEADLOCK
//
// By calling dotnet.run() WITHOUT await, the module evaluation completes
// immediately (all exports are available). When JSHost.ImportAsync later
// requests abies.js, the browser returns the fully-evaluated cached module.
//
// Guard (__ABIES_DOTNET_STARTED):
//   In InteractiveWasm/Auto render mode, the server inlines its own bootstrap
//   script that calls dotnet.run() before abies.js is loaded. When Runtime.Run()
//   later imports abies.js for the first time, the guard prevents a second
//   dotnet.run() call which would fail (runtime already running).
//
//   Pure browser mode:  flag unset → bootstrap runs normally.
//   Server-rendered:    flag set by inline script → bootstrap skipped.
//
// The import path './_framework/dotnet.js' is resolved relative to this module's
// URL (the wwwroot root), which is where the .NET WASM SDK places dotnet.js.
// =============================================================================

import { dotnet } from './_framework/dotnet.js';

if (!globalThis.__ABIES_DOTNET_STARTED) {
    globalThis.__ABIES_DOTNET_STARTED = true;
    dotnet.run().catch(err => console.error('[abies] .NET WASM startup failed:', err));
}
