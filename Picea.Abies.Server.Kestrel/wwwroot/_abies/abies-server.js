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
//   Patches (20 bytes each):
//     int32 type (BinaryPatchType enum)
//     int32 field1 (string table index, -1 = null)
//     int32 field2 (string table index, -1 = null)
//     int32 field3 (string table index, -1 = null)
//     int32 field4 (string table index, -1 = null)
//
//   String Table:
//     Sequence of LEB128-prefixed UTF-8 strings
//
// Event Protocol (Client → Server):
//   JSON text frames:
//     { "commandId": "...", "eventName": "...", "eventData": "...",
//       "traceparent": "..."?, "tracestate": "..."? }
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
    const DEBUGGER_COMMAND_ID = "__debugger_command__";

    // =========================================================================
    // Event types to register for delegation
    // Must match all helpers exposed in Picea.Abies/Html/Events.cs
    // =========================================================================
    const COMMON_EVENT_TYPES = [
        // Mouse events
        "click", "dblclick", "mousedown", "mouseup", "mousemove",
        "mouseenter", "mouseleave", "mouseover", "mouseout", "wheel", "contextmenu",
        // Keyboard events
        "keydown", "keyup", "keypress",
        // Input / Form events
        "input", "change", "submit", "reset", "invalid", "select",
        // Focus events
        "focus", "blur", "focusin", "focusout",
        // Pointer events
        "pointerdown", "pointerup", "pointermove", "pointercancel",
        "pointerover", "pointerout", "pointerenter", "pointerleave",
        "gotpointercapture", "lostpointercapture",
        // Touch events
        "touchstart", "touchend", "touchmove", "touchcancel",
        // Drag events
        "drag", "dragstart", "dragend", "dragenter", "dragleave", "dragover", "drop",
        // Scroll & Resize
        "scroll", "resize",
        // Clipboard events
        "copy", "cut", "paste",
        // Animation events
        "animationstart", "animationend", "animationiteration", "animationcancel",
        // Transition events
        "transitionstart", "transitionend", "transitionrun", "transitioncancel",
        // Media events
        "play", "pause", "ended", "timeupdate", "volumechange",
        "seeking", "seeked", "ratechange", "durationchange",
        "canplay", "canplaythrough", "waiting", "playing",
        "stalled", "suspend", "emptied", "loadeddata", "loadedmetadata",
        "loadstart", "progress", "abort",
        // Load & Error events
        "load", "error", "unload", "beforeunload",
        // Toggle & Misc events
        "toggle", "close", "cancel", "fullscreenchange", "fullscreenerror",
        // Composition events (for IME / CJK input)
        "compositionstart", "compositionupdate", "compositionend"
    ];

    // =========================================================================
    // UTF-8 text decoder (reused)
    // =========================================================================
    const utf8Decoder = new TextDecoder("utf-8");

    // =========================================================================
    // OpenTelemetry — optional event trace context propagation
    // =========================================================================
    let otelModule = null;
    let debuggerModuleUrl = null;

    function loadDebuggerModule() {
        if (!debuggerModuleUrl) {
            return Promise.resolve(null);
        }

        return import(/* webpackIgnore: true */ debuggerModuleUrl).catch(() => null);
    }

    function refreshDebuggerState() {
        if (!resolveDebuggerEnabled()) {
            return;
        }

        Promise.resolve()
            .then(() => loadDebuggerModule())
            .then(mod => {
                if (mod && typeof mod.notifyTimelineChanged === "function") {
                    mod.notifyTimelineChanged();
                }
            })
            .catch(() => {
                // Best-effort only for debug builds.
            });
    }

    // =========================================================================
    // Debugger UI startup configuration
    // =========================================================================
    // Default behavior: enabled in startup flow. Opt-out options:
    //   - URL query: ?abies-debugger=off|false|0
    //   - Global: window.__abiesDebugger = { enabled: false } or false
    //   - Meta: <meta name="abies-debugger" content="off">
    // Unknown values are ignored and fall back to enabled.

    function parseToggleValue(value) {
        if (typeof value === "boolean") return value;
        if (value == null) return null;

        const normalized = String(value).trim().toLowerCase();
        if (normalized === "1" || normalized === "true" || normalized === "on" || normalized === "yes" || normalized === "enabled") {
            return true;
        }

        if (normalized === "0" || normalized === "false" || normalized === "off" || normalized === "no" || normalized === "disabled") {
            return false;
        }

        return null;
    }

    function resolveDebuggerEnabled() {
        try {
            const url = new URL(window.location.href);
            const queryValue =
                url.searchParams.get("abies-debugger") ??
                url.searchParams.get("debugger");
            const parsed = parseToggleValue(queryValue);
            if (parsed !== null) {
                return parsed;
            }
        } catch {
            // Ignore URL parsing issues
        }

        if (typeof globalThis !== "undefined" && "__abiesDebugger" in globalThis) {
            const config = globalThis.__abiesDebugger;
            if (typeof config === "boolean") {
                return config;
            }

            if (config && typeof config === "object" && "enabled" in config) {
                const parsed = parseToggleValue(config.enabled);
                if (parsed !== null) {
                    return parsed;
                }
            }
        }

        const meta =
            document.querySelector('meta[name="abies-debugger"]') ||
            document.querySelector('meta[name="debugger"]');
        if (meta && typeof meta.content === "string") {
            const parsed = parseToggleValue(meta.content);
            if (parsed !== null) {
                return parsed;
            }
        }

        return true;
    }

    function initializeDebuggerDefaults() {
        const enabled = resolveDebuggerEnabled();

        const existing = globalThis.__abiesDebugger;
        if (existing && typeof existing === "object" && !Array.isArray(existing)) {
            existing.enabled = enabled;
        } else {
            globalThis.__abiesDebugger = { enabled };
        }

        return enabled;
    }

    function ensureDebuggerSurfaceVisible() {
        if (!resolveDebuggerEnabled()) {
            return;
        }

        const mountPoint = document.getElementById("abies-debugger-timeline");
        if (!mountPoint || mountPoint.children.length > 0) {
            return;
        }

        if (mountPoint.querySelector('[data-abies-debugger-shell="1"]')) {
            return;
        }

        const shell = document.createElement("button");
        shell.type = "button";
        shell.setAttribute("data-abies-debugger-shell", "1");
        shell.setAttribute("data-abies-debugger-intent", "toggle-panel");
        shell.style.position = "fixed";
        shell.style.right = "12px";
        shell.style.bottom = "12px";
        shell.style.zIndex = "2147483647";
        shell.style.background = "#101828";
        shell.style.color = "#F2F4F7";
        shell.style.border = "1px solid rgba(242,244,247,0.24)";
        shell.style.borderRadius = "8px";
        shell.style.padding = "8px 10px";
        shell.style.font = "12px/1.2 ui-monospace, SFMono-Regular, Menlo, Consolas, monospace";
        shell.style.cursor = "pointer";
        shell.textContent = "Abies Debugger";
        mountPoint.appendChild(shell);
    }

    async function initializeDebugger(scriptTag) {
        if (!initializeDebuggerDefaults()) {
            return;
        }

        try {
            // Keep this best-effort: debugger.js is debug-only and absent in Release.
            // Resolve it as a sibling to abies-server.js so the server package owns
            // both assets under the same guaranteed /_abies/ static path.
            const moduleUrl = new URL("./debugger.js", scriptTag.src || window.location.href);
            debuggerModuleUrl = moduleUrl.href;
            const mod = await import(/* webpackIgnore: true */ debuggerModuleUrl);
            if (mod && typeof mod.mountDebugger === "function") {
                mod.mountDebugger();
            }

            if (mod && typeof mod.setRuntimeBridge === "function") {
                mod.setRuntimeBridge((type, entryId, dataJson) => sendDebuggerCommand(type, entryId, dataJson));
            }

            if (mod && typeof mod.notifyTimelineChanged === "function") {
                mod.notifyTimelineChanged();
            }
        } catch {
            // No-op when debugger bundle is missing (Release or non-browser host).
        }
    }

    function remountDebuggerAfterRootPatch() {
        if (!resolveDebuggerEnabled()) {
            return;
        }

        const moduleUrl = debuggerModuleUrl ?? new URL("/_abies/debugger.js", window.location.href).href;

        Promise.resolve()
            .then(() => import(/* webpackIgnore: true */ moduleUrl))
            .then(mod => {
                if (mod && typeof mod.mountDebugger === "function") {
                    mod.mountDebugger();
                }
            })
            .catch(() => {
                // Keep this best-effort: debugger.js is absent in Release builds.
            })
            .finally(() => {
                ensureDebuggerSurfaceVisible();
            });
    }

    function resolveOtelVerbosity() {
        try {
            const url = new URL(window.location.href);
            const urlVerbosity = url.searchParams.get("otel-verbosity");
            if (typeof urlVerbosity === "string" && urlVerbosity.length > 0) {
                return urlVerbosity;
            }
        } catch {
            // Ignore URL parsing issues
        }

        if (typeof window !== "undefined" && window.__otel && typeof window.__otel.verbosity === "string") {
            if (window.__otel.verbosity.length > 0) {
                return window.__otel.verbosity;
            }
        }

        const meta =
            document.querySelector('meta[name="otel-verbosity"]') ||
            document.querySelector('meta[name="abies-otel-verbosity"]');

        if (meta && typeof meta.content === "string" && meta.content.length > 0) {
            return meta.content;
        }

        return null;
    }

    async function initializeOtel(scriptTag) {
        const verbosity = resolveOtelVerbosity();
        if (!verbosity || verbosity === "off") return;

        try {
            const moduleUrl = new URL("../abies-otel.js", scriptTag.src || window.location.href);
            const mod = await import(/* webpackIgnore: true */ moduleUrl.href);
            const success = await mod.initialize(verbosity);
            otelModule = success ? mod : null;
        } catch (err) {
            console.warn("[abies-server] Failed to load OTel module. Tracing disabled.", err);
            otelModule = null;
        }
    }

    function createEventTraceContext(commandId, eventName, eventData) {
        if (!otelModule) return null;

        try {
            return otelModule.traceEvent(commandId, eventName, eventData);
        } catch {
            return null;
        }
    }

    // =========================================================================
    // HTML fragment parser — uses <template> for context-independent parsing
    // =========================================================================
    const _fragmentTemplate = document.createElement("template");
    const TEXT_MARKER_PREFIX = "abies-text:";
    const TEXT_MARKER_START_SUFFIX = ":start";
    const TEXT_MARKER_END_SUFFIX = ":end";

    function parseHtmlFragment(html) {
        _fragmentTemplate.innerHTML = html;
        return _fragmentTemplate.content.firstElementChild;
    }

    function textMarkerValue(id, isStart) {
        return `${TEXT_MARKER_PREFIX}${id}${isStart ? TEXT_MARKER_START_SUFFIX : TEXT_MARKER_END_SUFFIX}`;
    }

    function createManagedTextFragment(id, value) {
        const fragment = document.createDocumentFragment();
        fragment.appendChild(document.createComment(textMarkerValue(id, true)));
        fragment.appendChild(document.createTextNode(value));
        fragment.appendChild(document.createComment(textMarkerValue(id, false)));
        return fragment;
    }

    function findManagedTextRange(parent, id) {
        const startValue = textMarkerValue(id, true);
        const endValue = textMarkerValue(id, false);

        for (const child of parent.childNodes) {
            if (child.nodeType !== Node.COMMENT_NODE || child.data !== startValue) {
                continue;
            }

            let current = child.nextSibling;
            let textNode = null;
            while (current) {
                if (current.nodeType === Node.COMMENT_NODE && current.data === endValue) {
                    return { start: child, text: textNode, end: current };
                }

                if (!textNode && current.nodeType === Node.TEXT_NODE) {
                    textNode = current;
                }

                current = current.nextSibling;
            }
        }

        return null;
    }

    function removeManagedTextRange(range) {
        let current = range.start;
        while (current) {
            const next = current.nextSibling;
            current.remove();
            if (current === range.end) {
                break;
            }

            current = next;
        }
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
            data.repeat = event.repeat;
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

    function applyPatch(type, f1, f2, f3, f4) {
        switch (type) {
            case OP_ADD_ROOT: {
                const existingDebuggerMount = document.getElementById("abies-debugger-timeline");
                document.body.innerHTML = f2;
                if (existingDebuggerMount && !document.getElementById("abies-debugger-timeline")) {
                    document.body.appendChild(existingDebuggerMount);
                }
                remountDebuggerAfterRootPatch();
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
                    const range = findManagedTextRange(parent, f2);
                    if (range) {
                        if (range.text) {
                            range.text.textContent = f3;
                        } else {
                            parent.insertBefore(document.createTextNode(f3), range.end);
                        }

                        range.start.data = textMarkerValue(f4, true);
                        range.end.data = textMarkerValue(f4, false);
                        break;
                    }

                    // Backward compatibility for pre-marker DOM.
                    for (const child of parent.childNodes) {
                        if (child.nodeType === Node.TEXT_NODE) {
                            child.textContent = f3;
                            break;
                        }
                    }
                }
                break;
            }

            case OP_ADD_TEXT: {
                const parent = document.getElementById(f1);
                if (parent) parent.appendChild(createManagedTextFragment(f3, f2));
                break;
            }

            case OP_REMOVE_TEXT: {
                const parent = document.getElementById(f1);
                if (parent) {
                    const range = findManagedTextRange(parent, f2);
                    if (range) {
                        removeManagedTextRange(range);
                        break;
                    }

                    // Backward compatibility for pre-marker DOM.
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
        const entrySize = 20;

        for (let i = 0; i < patchCount; i++) {
            const offset = headerSize + (i * entrySize);
            const type = view.getInt32(offset, true);
            const f1Idx = view.getInt32(offset + 4, true);
            const f2Idx = view.getInt32(offset + 8, true);
            const f3Idx = view.getInt32(offset + 12, true);
            const f4Idx = view.getInt32(offset + 16, true);

            const f1 = f1Idx >= 0 ? strings[f1Idx] : null;
            const f2 = f2Idx >= 0 ? strings[f2Idx] : null;
            const f3 = f3Idx >= 0 ? strings[f3Idx] : null;
            const f4 = f4Idx >= 0 ? strings[f4Idx] : null;

            applyPatch(type, f1, f2, f3, f4);
        }

        refreshDebuggerState();
    }

    // =========================================================================
    // Event Delegation
    // =========================================================================

    const registeredEventTypes = new Set();
    let ws = null;
    let debuggerRequestSeq = 0;
    const pendingDebuggerResponses = new Map();

    // =========================================================================
    // WASM Handoff (InteractiveAuto)
    // =========================================================================
    // In Auto mode, the server renders interactively via WebSocket while WASM
    // downloads. Once WASM boots and applies its first patch batch (signaled
    // by data-abies-mode="wasm" on <body>), the server session is no longer
    // needed. We close the WebSocket and stop all server-side event handling
    // so that only WASM controls the DOM from that point forward.
    // =========================================================================
    let wasmActive = false;

    function sendEvent(commandId, eventName, eventData, traceContext) {
        if (wasmActive) return;
        if (ws && ws.readyState === WebSocket.OPEN) {
            const payload = {
                commandId: commandId,
                eventName: eventName,
                eventData: eventData
            };

            if (traceContext && typeof traceContext.traceparent === "string" && traceContext.traceparent.length > 0) {
                payload.traceparent = traceContext.traceparent;
            }

            if (traceContext && typeof traceContext.tracestate === "string" && traceContext.tracestate.length > 0) {
                payload.tracestate = traceContext.tracestate;
            }

            ws.send(JSON.stringify(payload));
        }
    }

    function sendDebuggerCommand(type, entryId, dataJson) {
        if (!ws || ws.readyState !== WebSocket.OPEN) {
            return Promise.resolve(JSON.stringify({
                status: "unavailable",
                appName: "",
                appVersion: "",
                cursorPosition: -1,
                timelineSize: 0,
                atStart: true,
                atEnd: true,
                modelSnapshotPreview: ""
            }));
        }

        const requestId = `dbg-${++debuggerRequestSeq}`;
        const eventData = JSON.stringify({
            requestId,
            type,
            entryId,
            data: dataJson ? JSON.parse(dataJson) : undefined
        });

        return new Promise(resolve => {
            pendingDebuggerResponses.set(requestId, resolve);
            sendEvent(DEBUGGER_COMMAND_ID, "debugger-command", eventData);

            setTimeout(() => {
                const pending = pendingDebuggerResponses.get(requestId);
                if (pending) {
                    pendingDebuggerResponses.delete(requestId);
                    pending(JSON.stringify({
                        status: "error",
                        appName: "",
                        appVersion: "",
                        cursorPosition: -1,
                        timelineSize: 0,
                        atStart: true,
                        atEnd: true,
                        modelSnapshotPreview: ""
                    }));
                }
            }, 2000);
        });
    }

    function registerEventType(eventType) {
        if (registeredEventTypes.has(eventType)) return;
        registeredEventTypes.add(eventType);

        const useCapture = eventType === "focus" || eventType === "blur"
            || eventType === "focusin" || eventType === "focusout";

        document.addEventListener(eventType, function (event) {
            const attrName = "data-event-" + eventType;
            const debugHandlers = window.__ABIES_DEBUG_HANDLERS ?? false;

            let el = event.target;
            let walkDepth = 0;
            while (el && el !== document) {
                if (el.hasAttribute && el.hasAttribute(attrName)) {
                    const commandId = el.getAttribute(attrName);
                    if (debugHandlers) {
                        console.log("[ServerEventDelegation] HIT: " + eventType + " on " + el.tagName + " (depth=" + walkDepth + "), commandId=" + commandId);
                    }
                    const eventData = extractEventData(event);
                    const traceContext = createEventTraceContext(commandId, eventType, eventData);
                    sendEvent(commandId, eventType, eventData, traceContext);

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
                walkDepth++;
                el = el.parentElement;
            }
            if (debugHandlers) {
                console.log("[ServerEventDelegation] MISS: " + eventType + " on " + (event.target?.tagName ?? "?") + ". Walked " + walkDepth + " levels, never found " + attrName);
            }
        }, useCapture);
    }

    // =========================================================================
    // Navigation — Client-Side Routing
    // =========================================================================

    function setupNavigation() {
        // Listen for popstate events (back/forward)
        window.addEventListener("popstate", function () {
            if (wasmActive) return; // WASM's own listener handles this
            const path = window.location.pathname + window.location.search + window.location.hash;
            sendEvent(URL_CHANGED_COMMAND_ID, "urlchange", path);
        });

        // Intercept clicks on <a> elements for client-side routing
        document.addEventListener("click", function (event) {
            if (wasmActive) return; // WASM's own listener handles this
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

    function connect(wsPath, isAutoMode) {
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
            refreshDebuggerState();
        };

        ws.onmessage = function (event) {
            // In Auto mode, stop applying server patches once WASM has taken
            // over rendering. The MutationObserver (below) is the primary
            // mechanism; this guard is a safety net for patches that arrive
            // between the attribute change and the observer firing.
            if (wasmActive) {
                return;
            }

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
                    } else if (msg.type === "debugger-response" && typeof msg.requestId === "string") {
                        const resolve = pendingDebuggerResponses.get(msg.requestId);
                        if (resolve) {
                            pendingDebuggerResponses.delete(msg.requestId);
                            const { type: _type, requestId: _requestId, ...response } = msg;
                            resolve(JSON.stringify(response));
                        }
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

        // In Auto mode, watch for the WASM handoff signal. Once WASM has
        // applied its first render batch (setting data-abies-mode="wasm"
        // on <body>), the server session is no longer needed. We close
        // the WebSocket immediately so no further server patches arrive
        // and collide with the WASM-rendered DOM.
        if (isAutoMode) {
            const observer = new MutationObserver(function () {
                if (document.body.getAttribute("data-abies-mode") === "wasm") {
                    wasmActive = true;
                    if (ws) {
                        ws.close();
                    }
                    observer.disconnect();
                }
            });
            observer.observe(document.body, {
                attributes: true,
                attributeFilter: ["data-abies-mode"]
            });
        }
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

        // In Auto mode (data-auto="true"), the server provides interactivity
        // via WebSocket while WASM downloads. Once WASM is ready, we hand
        // off to it and tear down the server connection.
        const isAutoMode = scriptTag.getAttribute("data-auto") === "true";

        // Best-effort debug UI startup (non-blocking).
        initializeDebugger(scriptTag);
        Promise.resolve().then(() => ensureDebuggerSurfaceVisible());

        initializeOtel(scriptTag);

        // Register all common event types for delegation
        COMMON_EVENT_TYPES.forEach(registerEventType);

        // Set up client-side navigation interception
        setupNavigation();

        // Connect to the server
        connect(wsPath, isAutoMode);
    }

    // Run on script load (this is a regular <script>, not type="module",
    // so it executes synchronously during parsing).
    init();

})();
