// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

/**
 * Abies Time Travel Debugger — Debug-only JavaScript module
 * 
 * This module is EXCLUDED from Release builds and only loads during Debug.
 * It initializes debugger adapter wiring at the mount point.
 * 
 * The actual replay logic and state machine live in C# (Mealy machine).
 * This module only coordinates UI intent -> adapter message dispatch.
 */

(function() {
    'use strict';

    const MountPointId = 'abies-debugger-timeline';
    const InitializationFlag = 'abiesDebuggerAdapterInitialized';
    const IntentEventName = 'abies:debugger:intent';
    const IntentAttributeName = 'data-abies-debugger-intent';
    const PayloadAttributeName = 'data-abies-debugger-payload';
    const DebuggerEvents = {
        MessageDispatched: 'abies:debugger:message-dispatched'
    };

    /**
     * Initialize debugger adapter wiring when the module loads.
     */
    function initializeDebuggerAdapter() {
        const mountPoint = document.getElementById(MountPointId);
        if (!mountPoint) {
            console.debug('[Abies Debugger] Mount point not found. Skipping adapter initialization.');
            return;
        }

        if (mountPoint.dataset[InitializationFlag] === '1') {
            return;
        }

        mountPoint.dataset[InitializationFlag] = '1';
        bootstrapIntentTransport(mountPoint);

        console.debug('[Abies Debugger] Adapter initialized at mount point:', MountPointId);
    }

    /**
     * Attach event listeners that forward UI intent to the runtime bridge event.
     */
    function bootstrapIntentTransport(mountPoint) {
        mountPoint.addEventListener(IntentEventName, (event) => {
            forwardIntentToRuntimeBridge(event?.detail, mountPoint);
        });

        mountPoint.addEventListener('click', (event) => {
            const target = event.target instanceof Element
                ? event.target.closest(`[${IntentAttributeName}]`)
                : null;

            if (!target) {
                return;
            }

            const intent = target.getAttribute(IntentAttributeName);
            if (!intent) {
                return;
            }

            const payload = parsePayload(target.getAttribute(PayloadAttributeName));
            emitIntent(mountPoint, {
                intent,
                payload
            });
        });
    }

    /**
     * Emit a normalized debugger intent event from UI controls.
     * This is the single source of truth for intent propagation from click controls.
     */
    function emitIntent(mountPoint, detail) {
        mountPoint.dispatchEvent(new CustomEvent(IntentEventName, {
            detail,
            bubbles: false,
            cancelable: false
        }));
    }

    /**
     * Parse optional JSON payload for data-abies-debugger-payload.
     */
    function parsePayload(rawPayload) {
        if (!rawPayload) {
            return undefined;
        }

        try {
            return JSON.parse(rawPayload);
        } catch {
            return rawPayload;
        }
    }

    /**
     * Normalize incoming intent detail and dispatch to runtime bridge event.
     */
    function forwardIntentToRuntimeBridge(detail, mountPoint) {
        let message = detail;

        if (!message || typeof message !== 'object') {
            return;
        }

        if (typeof message.type !== 'string' && typeof message.intent === 'string') {
            message = typeof message.payload === 'object' && message.payload !== null
                ? { ...message.payload, type: message.intent }
                : { type: message.intent };
        }

        if (typeof message.type !== 'string' || !message.type) {
            return;
        }

        const event = new CustomEvent(DebuggerEvents.MessageDispatched, {
            detail: message,
            bubbles: true,
            cancelable: true
        });

        document.dispatchEvent(event);
        mountPoint.dispatchEvent(new CustomEvent('abies:debugger:bridge-dispatched', {
            detail: message,
            bubbles: true,
            cancelable: false
        }));
    }

    // Auto-initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeDebuggerAdapter);
    } else {
        initializeDebuggerAdapter();
    }

    // Export for testing
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = {
            initializeDebuggerAdapter,
            bootstrapIntentTransport,
            forwardIntentToRuntimeBridge,
            parsePayload,
            emitIntent
        };
    }
})();
