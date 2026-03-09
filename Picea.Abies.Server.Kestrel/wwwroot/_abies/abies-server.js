// =============================================================================
// abies-server.js — WebSocket Client for Abies Interactive Server Mode
// =============================================================================
// This script is loaded by server-rendered Abies pages for InteractiveServer
// and InteractiveAuto render modes. It:
//
//   1. Reads the WebSocket path from its own <script> tag's data-ws-path attribute
//   2. Opens a WebSocket connection to the server
//   3. Receives binary patch batches and applies them to the DOM
//   4. Captures DOM events via delegation and sends them as JSON text frames
//   5. Intercepts client-side navigation and sends URL changes to the server
//
// Binary Protocol (Server → Client):
//   Same format as the WASM interop — see RenderBatchWriter.cs:
//
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
// Event Protocol (Client → Server):
//   JSON text frames:
//     { "commandId": "...", "eventName": "...", "eventData": "..." }
//
// Navigation Protocol:
//   URL changes are sent as events with a reserved commandId:
//     { "commandId": "__url_changed__", "eventName": "urlchange", "eventData": "/new/path" }
//
// This file is self-contained — it shares the same binary protocol as
// Picea.Abies.Browser/wwwroot/abies.js but does not import from it. The shared
// code (binary reader, DOM patching, event extraction) is inlined here to
// avoid ES module bundling concerns in non-module <script> tags.
//
// IMPORTANT: The patch opcodes and binary format MUST stay in sync with
// RenderBatchWriter.cs and BinaryPatchType enum. If you change the protocol,
// update BOTH abies.js (WASM) and this file (server).
//
// See also:
//   - Picea.Abies/RenderBatchWriter.cs — binary serialization (.NET side)
//   - Picea.Abies/DOM/Patch.cs — patch type definitions
//   - Picea.Abies.Browser/wwwroot/abies.js — WASM-side equivalent
//   - Picea.Abies.Server.Kestrel/WebSocketTransport.cs — server-side WebSocket adapter
// =============================================================================

(function () {
    "use strict";

    // =========================================================================
    // Patch Type Opcodes — must match BinaryPatchType in RenderBatchWriter.cs
    // =========================================================================
    const OP_ADD_ROOT              = 0;
    const OP_REPLACE_CHILD         = 1;
    const OP_ADD_CHILD             = 2;
    const OP_REMOVE_CHILD          = 3;
    const OP_CLEAR_CHILDREN        = 4;
    const OP_SET_CHILDREN_HTML     = 5;
    const OP_MOVE_CHILD            = 6;
    const OP_UPDATE_ATTRIBUTE      = 7;
    const OP_ADD_ATTRIBUTE         = 8;
    const OP_REMOVE_ATTRIBUTE      = 9;
    const OP_ADD_HANDLER           = 10;
    const OP_REMOVE_HANDLER        = 11;
    const OP_UPDATE_HANDLER        = 12;
    const OP_UPDATE_TEXT            = 13;
    const OP_ADD_TEXT               = 14;
    const OP_REMOVE_TEXT            = 15;
    const OP_ADD_RAW                = 16;
    const OP_REMOVE_RAW             = 17;
    const OP_REPLACE_RAW            = 18;
    const OP_UPDATE_RAW             = 19;
    const OP_ADD_HEAD_ELEMENT       = 20;
    const OP_UPDATE_HEAD_ELEMENT    = 21;
    const OP_REMOVE_HEAD_ELEMENT    = 22;
    const OP_APPEND_CHILDREN_HTML   = 23;

    // =========================================================================
    // Reserved command ID for URL change events
    // =========================================================================
    const URL_CHANGED_COMMAND_ID = "__url_changed__";

    // =========================================================================
    // Event types to register for delegation
    // =========================================================================
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

    // =========================================================================
    // UTF-8 text decoder (reused)
    // =========================================================================
    const utf8Decoder = new TextDecoder("utf-8");

    // =========================================================================
    // HTML fragment parser — uses <template> for context-independent parsing
    // =========================================================================
    const _fragmentTemplate = document.createElement("template");

    function parseHtmlFragment(html) {
        _fragmentTemplate.innerHTML = html;
        return _fragmentTemplate.content.firstElementChild;
    }

    // =========================================================================
    // Binary Reader
    // =========================================================================

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

    // =========================================================================
    // Event Data Extraction
    // =========================================================================

    function extractEventData(event) {
        const data = {};

        if (event.target && "value" in event.target) {
            data.value = event.target.value;
        }

        if (event.target && "checked" in event.target) {
            data.checked = event.target.checked;
        }

        if (event instanceof KeyboardEvent) {
            data.key = event.key;
            data.code = event.code;
            data.altKey = event.altKey;
            data.ctrlKey = event.ctrlKey;
            data.shiftKey = event.shiftKey;
            data.metaKey = event.metaKey;
        }

        if (event instanceof MouseEvent) {
            data.clientX = event.clientX;
            data.clientY = event.clientY;
            data.button = event.button;
            data.altKey = event.altKey;
            data.ctrlKey = event.ctrlKey;
            data.shiftKey = event.shiftKey;
            data.metaKey = event.metaKey;
        }

        if (typeof PointerEvent !== "undefined" && event instanceof PointerEvent) {
            data.pointerId = event.pointerId;
            data.pointerType = event.pointerType;
            data.pressure = event.pressure;
        }

        if (typeof TouchEvent !== "undefined" && event instanceof TouchEvent) {
            data.touches = Array.from(event.touches).map(t => ({
                clientX: t.clientX,
                clientY: t.clientY,
                identifier: t.identifier
            }));
        }

        if (event instanceof DragEvent && event.dataTransfer) {
            data.types = event.dataTransfer.types;
        }

        if (event instanceof WheelEvent) {
            data.deltaX = event.deltaX;
            data.deltaY = event.deltaY;
            data.deltaZ = event.deltaZ;
            data.deltaMode = event.deltaMode;
        }

        return JSON.stringify(data);
    }

    // =========================================================================
    // DOM Mutation — applies a single patch
    // =========================================================================

    function applyPatch(type, f1, f2, f3) {
        switch (type) {
            case OP_ADD_ROOT: {
                document.body.innerHTML = f2;
                break;
            }

            case OP_REPLACE_CHILD: {
                const oldEl = document.getElementById(f1);
                if (oldEl) {
                    const newEl = parseHtmlFragment(f3);
                    if (newEl) oldEl.replaceWith(newEl);
                }
                break;
            }

            case OP_ADD_CHILD: {
                const parent = document.getElementById(f1);
                if (parent) {
                    const child = parseHtmlFragment(f3);
                    if (child) parent.appendChild(child);
                }
                break;
            }

            case OP_REMOVE_CHILD: {
                const child = document.getElementById(f2);
                if (child) child.remove();
                break;
            }

            case OP_CLEAR_CHILDREN: {
                const parent = document.getElementById(f1);
                if (parent) parent.innerHTML = "";
                break;
            }

            case OP_SET_CHILDREN_HTML: {
                const parent = document.getElementById(f1);
                if (parent) parent.innerHTML = f2;
                break;
            }

            case OP_APPEND_CHILDREN_HTML: {
                const parent = document.getElementById(f1);
                if (parent) parent.insertAdjacentHTML("beforeend", f2);
                break;
            }

            case OP_MOVE_CHILD: {
                const parent = document.getElementById(f1);
                const child = document.getElementById(f2);
                if (parent && child) {
                    if (f3) {
                        const before = document.getElementById(f3);
                        if (before) parent.insertBefore(child, before);
                    } else {
                        parent.appendChild(child);
                    }
                }
                break;
            }

            case OP_UPDATE_ATTRIBUTE:
            case OP_ADD_ATTRIBUTE: {
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
                const el = document.getElementById(f1);
                if (el) el.removeAttribute(f2);
                break;
            }

            case OP_ADD_HANDLER: {
                const el = document.getElementById(f1);
                if (el) el.setAttribute(f2, f3);
                break;
            }

            case OP_REMOVE_HANDLER: {
                const el = document.getElementById(f1);
                if (el) el.removeAttribute(f2);
                break;
            }

            case OP_UPDATE_HANDLER: {
                const el = document.getElementById(f1);
                if (el) el.setAttribute(f2, f3);
                break;
            }

            case OP_UPDATE_TEXT: {
                const parent = document.getElementById(f1);
                if (parent) {
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
                const parent = document.getElementById(f1);
                if (parent) parent.appendChild(document.createTextNode(f2));
                break;
            }

            case OP_REMOVE_TEXT: {
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

            case OP_ADD_RAW: {
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
                const el = document.getElementById(f2);
                if (el) el.remove();
                break;
            }

            case OP_REPLACE_RAW: {
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
                const el = document.getElementById(f1);
                if (el) el.innerHTML = f2;
                break;
            }

            case OP_ADD_HEAD_ELEMENT: {
                const head = document.head;
                if (head) {
                    const el = parseHtmlFragment(f2);
                    if (el) head.appendChild(el);
                }
                break;
            }

            case OP_UPDATE_HEAD_ELEMENT: {
                const head = document.head;
                if (head) {
                    const existing = head.querySelector(`[data-abies-head="${f1}"]`);
                    if (existing) {
                        const newEl = parseHtmlFragment(f2);
                        if (newEl) existing.replaceWith(newEl);
                    } else {
                        const el = parseHtmlFragment(f2);
                        if (el) head.appendChild(el);
                    }
                }
                break;
            }

            case OP_REMOVE_HEAD_ELEMENT: {
                const head = document.head;
                if (head) {
                    const existing = head.querySelector(`[data-abies-head="${f1}"]`);
                    if (existing) existing.remove();
                }
                break;
            }
        }
    }

    // =========================================================================
    // Binary Batch Processor
    // =========================================================================

    function applyBinaryBatch(bytes) {
        const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);

        const patchCount = view.getInt32(0, true);
        const stringTableOffset = view.getInt32(4, true);

        const strings = readStringTable(bytes, stringTableOffset, bytes.byteLength);

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
    }

    // =========================================================================
    // Event Delegation
    // =========================================================================

    const registeredEventTypes = new Set();
    let ws = null;

    function sendEvent(commandId, eventName, eventData) {
        if (ws && ws.readyState === WebSocket.OPEN) {
            ws.send(JSON.stringify({
                commandId: commandId,
                eventName: eventName,
                eventData: eventData
            }));
        }
    }

    function registerEventType(eventType) {
        if (registeredEventTypes.has(eventType)) return;
        registeredEventTypes.add(eventType);

        const useCapture = eventType === "focus" || eventType === "blur"
            || eventType === "focusin" || eventType === "focusout";

        document.addEventListener(eventType, function (event) {
            const attrName = "data-event-" + eventType;

            let el = event.target;
            while (el && el !== document) {
                if (el.hasAttribute && el.hasAttribute(attrName)) {
                    const commandId = el.getAttribute(attrName);
                    const eventData = extractEventData(event);
                    sendEvent(commandId, eventType, eventData);

                    if (eventType === "submit") {
                        event.preventDefault();
                    }

                    if (eventType === "keydown" && event.key === "Enter") {
                        event.preventDefault();
                    }

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

    // =========================================================================
    // Navigation — Client-Side Routing
    // =========================================================================

    function setupNavigation() {
        // Listen for popstate events (back/forward)
        window.addEventListener("popstate", function () {
            const path = window.location.pathname + window.location.search + window.location.hash;
            sendEvent(URL_CHANGED_COMMAND_ID, "urlchange", path);
        });

        // Intercept clicks on <a> elements for client-side routing
        document.addEventListener("click", function (event) {
            if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
                return;
            }

            let anchor = event.target;
            while (anchor && anchor.tagName !== "A") {
                anchor = anchor.parentElement;
            }

            if (!anchor) return;

            // Skip links with Abies event handlers (already handled by delegation)
            if (anchor.hasAttribute("data-event-click")) {
                event.preventDefault();
                return;
            }

            if (anchor.hasAttribute("target")) return;
            if (anchor.hasAttribute("download")) return;
            if (anchor.getAttribute("rel") === "external") return;

            const href = anchor.getAttribute("href");
            if (!href) return;
            if (href.startsWith("javascript:")) return;
            if (href === "#") return;

            try {
                const url = new URL(href, window.location.origin);

                if (url.origin !== window.location.origin) {
                    return;
                }

                event.preventDefault();
                const path = url.pathname + url.search + url.hash;
                history.pushState(null, "", path);

                sendEvent(URL_CHANGED_COMMAND_ID, "urlchange", path);
            } catch {
                return;
            }
        });
    }

    // =========================================================================
    // Server-Initiated Navigation
    // =========================================================================
    // When the MVU runtime on the server produces a NavigationCommand (e.g.,
    // Navigation.PushUrl after saving an article), the server sends a text
    // frame with a JSON navigation message. The client applies it to the
    // browser's history API without triggering a full page reload.
    // =========================================================================

    function handleNavigationMessage(msg) {
        switch (msg.action) {
            case "push":
                if (msg.url) {
                    history.pushState(null, "", msg.url);
                }
                break;
            case "replace":
                if (msg.url) {
                    history.replaceState(null, "", msg.url);
                }
                break;
            case "back":
                history.back();
                break;
            case "forward":
                history.forward();
                break;
            case "external":
                if (msg.url) {
                    window.location.href = msg.url;
                }
                break;
        }
    }

    // =========================================================================
    // WebSocket Connection
    // =========================================================================

    function connect(wsPath) {
        const protocol = window.location.protocol === "https:" ? "wss:" : "ws:";
        // Append the current page path as a query parameter so the server
        // can initialize the MVU session at the correct route. The Origin
        // header only contains scheme+host (no path), and Referer is not
        // always sent on WebSocket upgrade requests.
        const currentPath = window.location.pathname + window.location.search + window.location.hash;
        const wsUrl = protocol + "//" + window.location.host + wsPath
            + "?url=" + encodeURIComponent(currentPath);

        ws = new WebSocket(wsUrl);
        ws.binaryType = "arraybuffer";

        ws.onopen = function () {
            // Connection established — the server will send the initial
            // patch batch (the diff from empty → current view) automatically
            // when the session starts.
        };

        ws.onmessage = function (event) {
            if (event.data instanceof ArrayBuffer) {
                // Binary frame: patch batch from server
                const bytes = new Uint8Array(event.data);
                applyBinaryBatch(bytes);
            } else if (typeof event.data === "string") {
                // Text frame: server-to-client message (e.g., navigation command)
                try {
                    const msg = JSON.parse(event.data);
                    if (msg.type === "navigate") {
                        handleNavigationMessage(msg);
                    }
                } catch (e) {
                    // Ignore malformed text frames
                }
            }
        };

        ws.onclose = function () {
            ws = null;
            // TODO: Implement reconnection logic for production use.
            // For now, the connection is not re-established.
        };

        ws.onerror = function () {
            // Error will be followed by onclose, so cleanup happens there.
        };
    }

    // =========================================================================
    // Initialization
    // =========================================================================

    function init() {
        // Find our own <script> tag to read configuration from data attributes
        const scriptTag = document.currentScript;
        if (!scriptTag) {
            console.error("[abies-server] Could not find script tag.");
            return;
        }

        const wsPath = scriptTag.getAttribute("data-ws-path");
        if (!wsPath) {
            console.error("[abies-server] Missing data-ws-path attribute on script tag.");
            return;
        }

        // Register all common event types for delegation
        COMMON_EVENT_TYPES.forEach(registerEventType);

        // Set up client-side navigation interception
        setupNavigation();

        // Connect to the server
        connect(wsPath);
    }

    // Run on script load (this is a regular <script>, not type="module",
    // so it executes synchronously during parsing).
    init();

})();
