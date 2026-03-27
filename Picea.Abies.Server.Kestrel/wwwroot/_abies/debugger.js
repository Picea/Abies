// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

/**
 * Abies Time Travel Debugger — Debug-only JavaScript module
 *
 * This module is EXCLUDED from Release builds and only loads during Debug.
 * It owns debugger mount injection and adapter wiring at the mount point.
 *
 * The actual replay logic and state machine live in C# (Mealy machine).
 * This module only coordinates mount creation and UI intent -> adapter message dispatch.
 */

const MountPointId = 'abies-debugger-timeline';
const InitializationFlag = 'abiesDebuggerAdapterInitialized';
const IntentEventName = 'abies:debugger:intent';
const IntentAttributeName = 'data-abies-debugger-intent';
const PayloadAttributeName = 'data-abies-debugger-payload';
const ExpandedAttributeName = 'data-abies-debugger-expanded';
const ShellPanelAttributeName = 'data-abies-debugger-panel';
const StatusAttributeName = 'data-abies-debugger-status';
const JumpInputAttributeName = 'data-abies-debugger-jump-input';
const LogListAttributeName = 'data-abies-debugger-log';
const RuntimeSummaryAttributeName = 'data-abies-debugger-runtime-summary';
const TogglePanelIntentValue = 'toggle-panel';
const DebuggerEvents = {
    MessageDispatched: 'abies:debugger:message-dispatched'
};

let runtimeBridge = null;

function parseToggleValue(value) {
    if (typeof value === 'boolean') return value;
    if (value == null) return null;

    const normalized = String(value).trim().toLowerCase();
    if (normalized === '1' || normalized === 'true' || normalized === 'on' || normalized === 'yes' || normalized === 'enabled') {
        return true;
    }

    if (normalized === '0' || normalized === 'false' || normalized === 'off' || normalized === 'no' || normalized === 'disabled') {
        return false;
    }

    return null;
}

function resolveDebuggerEnabled() {
    try {
        const url = new URL(window.location.href);
        const queryValue =
            url.searchParams.get('abies-debugger') ??
            url.searchParams.get('debugger');
        const parsed = parseToggleValue(queryValue);
        if (parsed !== null) {
            return parsed;
        }
    } catch {
        // Ignore URL parsing issues
    }

    if (typeof globalThis !== 'undefined' && '__abiesDebugger' in globalThis) {
        const config = globalThis.__abiesDebugger;
        if (typeof config === 'boolean') {
            return config;
        }

        if (config && typeof config === 'object' && 'enabled' in config) {
            const parsed = parseToggleValue(config.enabled);
            if (parsed !== null) {
                return parsed;
            }
        }
    }

    const meta =
        document.querySelector('meta[name="abies-debugger"]') ||
        document.querySelector('meta[name="debugger"]');
    if (meta && typeof meta.content === 'string') {
        const parsed = parseToggleValue(meta.content);
        if (parsed !== null) {
            return parsed;
        }
    }

    return true;
}

/**
 * Mounts the debugger host container into the document body.
 * Idempotent: reuses the existing mount point when present.
 *
 * @returns {HTMLElement|null} The debugger mount point, or null when disabled.
 */
export function mountDebugger() {
    if (!resolveDebuggerEnabled()) {
        console.debug('[Abies Debugger] Startup disabled via config.');
        return null;
    }

    let mountPoint = document.getElementById(MountPointId);

    if (!mountPoint) {
        mountPoint = document.createElement('div');
        mountPoint.id = MountPointId;
        document.body.appendChild(mountPoint);
    }

    initializeDebuggerAdapter();
    ensureDebuggerShellVisible(mountPoint);
    return mountPoint;
}

export function setRuntimeBridge(callback) {
    runtimeBridge = typeof callback === 'function' ? callback : null;
    const mountPoint = document.getElementById(MountPointId);
    if (mountPoint) {
        updateBridgeAvailability(mountPoint);
        appendPanelLogEntry(
            mountPoint,
            runtimeBridge ? 'Runtime bridge connected.' : 'Runtime bridge unavailable.'
        );
    }
}

function ensureDebuggerShellVisible(mountPoint) {
    if (!mountPoint) {
        return;
    }

    const existingShell = mountPoint.querySelector('[data-abies-debugger-shell="1"]');
    if (existingShell) {
        ensureShellInteractive(existingShell);
        ensureDebuggerPanel(mountPoint);
        return;
    }

    if (mountPoint.children.length > 0) {
        return;
    }

    const shell = document.createElement('button');
    shell.type = 'button';
    shell.setAttribute('data-abies-debugger-shell', '1');
    shell.setAttribute(IntentAttributeName, TogglePanelIntentValue);
    shell.style.position = 'fixed';
    shell.style.right = '12px';
    shell.style.bottom = '12px';
    shell.style.zIndex = '2147483647';
    shell.style.background = '#101828';
    shell.style.color = '#F2F4F7';
    shell.style.border = '1px solid rgba(242,244,247,0.24)';
    shell.style.borderRadius = '8px';
    shell.style.padding = '8px 10px';
    shell.style.font = '12px/1.2 ui-monospace, SFMono-Regular, Menlo, Consolas, monospace';
    shell.style.cursor = 'pointer';
    shell.textContent = 'Abies Debugger';
    mountPoint.appendChild(shell);

    ensureDebuggerPanel(mountPoint);
}

function ensureShellInteractive(shell) {
    shell.setAttribute(IntentAttributeName, TogglePanelIntentValue);
    shell.style.cursor = 'pointer';

    if (shell.tagName !== 'BUTTON') {
        shell.setAttribute('role', 'button');
        shell.setAttribute('tabindex', '0');
    }
}

function ensureDebuggerPanel(mountPoint) {
    if (mountPoint.querySelector(`[${ShellPanelAttributeName}="1"]`)) {
        return;
    }

    const panel = document.createElement('div');
    panel.setAttribute(ShellPanelAttributeName, '1');
    panel.style.position = 'fixed';
    panel.style.right = '12px';
    panel.style.bottom = '52px';
    panel.style.zIndex = '2147483647';
    panel.style.background = '#101828';
    panel.style.color = '#F2F4F7';
    panel.style.border = '1px solid rgba(242,244,247,0.24)';
    panel.style.borderRadius = '8px';
    panel.style.padding = '10px 12px';
    panel.style.width = '320px';
    panel.style.maxWidth = 'calc(100vw - 24px)';
    panel.style.font = '12px/1.3 ui-monospace, SFMono-Regular, Menlo, Consolas, monospace';
    panel.style.display = 'none';

    const header = document.createElement('div');
    header.style.display = 'flex';
    header.style.justifyContent = 'space-between';
    header.style.alignItems = 'center';
    header.style.marginBottom = '8px';

    const title = document.createElement('strong');
    title.textContent = 'Abies Debugger';

    const status = document.createElement('span');
    status.setAttribute(StatusAttributeName, '1');
    status.textContent = 'recording';
    status.style.fontSize = '11px';
    status.style.padding = '2px 6px';
    status.style.borderRadius = '999px';
    status.style.border = '1px solid rgba(242,244,247,0.24)';
    status.style.background = 'rgba(137,180,43,0.16)';

    header.appendChild(title);
    header.appendChild(status);

    const controls = document.createElement('div');
    controls.style.display = 'grid';
    controls.style.gridTemplateColumns = 'repeat(3, minmax(0, 1fr))';
    controls.style.gap = '6px';
    controls.style.marginBottom = '8px';

    const controlSpecs = [
        ['Play', 'play'],
        ['Pause', 'pause'],
        ['Step', 'step-forward'],
        ['Back', 'step-back'],
        ['Clear', 'clear-timeline']
    ];

    for (const [label, intent] of controlSpecs) {
        const button = document.createElement('button');
        button.type = 'button';
        button.textContent = label;
        button.setAttribute(IntentAttributeName, intent);
        button.setAttribute(PayloadAttributeName, '{}');
        button.style.border = '1px solid rgba(242,244,247,0.24)';
        button.style.background = '#1D2939';
        button.style.color = '#F2F4F7';
        button.style.borderRadius = '6px';
        button.style.padding = '6px 8px';
        button.style.cursor = 'pointer';
        controls.appendChild(button);
    }

    const jumpRow = document.createElement('div');
    jumpRow.style.display = 'flex';
    jumpRow.style.gap = '6px';
    jumpRow.style.marginBottom = '8px';

    const jumpInput = document.createElement('input');
    jumpInput.type = 'number';
    jumpInput.min = '0';
    jumpInput.placeholder = 'Entry #';
    jumpInput.setAttribute(JumpInputAttributeName, '1');
    jumpInput.style.flex = '1';
    jumpInput.style.background = '#1D2939';
    jumpInput.style.border = '1px solid rgba(242,244,247,0.24)';
    jumpInput.style.color = '#F2F4F7';
    jumpInput.style.borderRadius = '6px';
    jumpInput.style.padding = '6px 8px';

    const jumpButton = document.createElement('button');
    jumpButton.type = 'button';
    jumpButton.textContent = 'Jump';
    jumpButton.setAttribute(IntentAttributeName, 'jump-to-entry');
    jumpButton.style.border = '1px solid rgba(242,244,247,0.24)';
    jumpButton.style.background = '#1D2939';
    jumpButton.style.color = '#F2F4F7';
    jumpButton.style.borderRadius = '6px';
    jumpButton.style.padding = '6px 10px';
    jumpButton.style.cursor = 'pointer';

    jumpRow.appendChild(jumpInput);
    jumpRow.appendChild(jumpButton);

    const logLabel = document.createElement('div');
    logLabel.textContent = 'Actions';
    logLabel.style.color = 'rgba(242,244,247,0.78)';
    logLabel.style.marginBottom = '4px';

    const runtimeSummary = document.createElement('div');
    runtimeSummary.setAttribute(RuntimeSummaryAttributeName, '1');
    runtimeSummary.style.fontSize = '11px';
    runtimeSummary.style.color = 'rgba(242,244,247,0.78)';
    runtimeSummary.style.marginBottom = '6px';

    const log = document.createElement('ul');
    log.setAttribute(LogListAttributeName, '1');
    log.style.listStyle = 'none';
    log.style.margin = '0';
    log.style.padding = '0';
    log.style.maxHeight = '120px';
    log.style.overflowY = 'auto';

    panel.appendChild(header);
    panel.appendChild(controls);
    panel.appendChild(jumpRow);
    panel.appendChild(runtimeSummary);
    panel.appendChild(logLabel);
    panel.appendChild(log);
    mountPoint.appendChild(panel);

    updateBridgeAvailability(mountPoint);
    appendPanelLogEntry(mountPoint, 'Debugger ready.');
}

function toggleDebuggerPanel(mountPoint) {
    const isExpanded = mountPoint.getAttribute(ExpandedAttributeName) === '1';
    const nextExpanded = !isExpanded;
    mountPoint.setAttribute(ExpandedAttributeName, nextExpanded ? '1' : '0');

    const panel = mountPoint.querySelector(`[${ShellPanelAttributeName}="1"]`);
    if (panel) {
        panel.style.display = nextExpanded ? 'block' : 'none';
    }
}

function appendPanelLogEntry(mountPoint, message) {
    const log = mountPoint.querySelector(`[${LogListAttributeName}="1"]`);
    if (!log) {
        return;
    }

    const item = document.createElement('li');
    item.textContent = message;
    item.style.padding = '4px 0';
    item.style.borderTop = '1px solid rgba(242,244,247,0.1)';
    log.prepend(item);

    while (log.children.length > 8) {
        log.removeChild(log.lastChild);
    }
}

function updateRuntimeSummary(mountPoint, response) {
    const summary = mountPoint.querySelector(`[${RuntimeSummaryAttributeName}="1"]`);
    if (!summary) {
        return;
    }

    if (!response || typeof response !== 'object') {
        summary.textContent = runtimeBridge
            ? 'Runtime connected. Awaiting debugger commands.'
            : 'Runtime bridge unavailable in this mode.';
        return;
    }

    const status = typeof response.status === 'string' ? response.status : 'unknown';
    const cursor = Number.isFinite(response.cursorPosition) ? response.cursorPosition : '?';
    const size = Number.isFinite(response.timelineSize) ? response.timelineSize : '?';
    summary.textContent = `Runtime ${status} | cursor ${cursor} | timeline ${size}`;
}

function updateBridgeAvailability(mountPoint) {
    const interactiveControls = mountPoint.querySelectorAll(
        `[${ShellPanelAttributeName}="1"] button[${IntentAttributeName}]:not([${IntentAttributeName}="${TogglePanelIntentValue}"])`
    );

    for (const control of interactiveControls) {
        control.disabled = !runtimeBridge;
        control.style.opacity = runtimeBridge ? '1' : '0.55';
    }

    const jumpInput = mountPoint.querySelector(`[${JumpInputAttributeName}="1"]`);
    if (jumpInput) {
        jumpInput.disabled = !runtimeBridge;
        jumpInput.style.opacity = runtimeBridge ? '1' : '0.55';
    }

    updateRuntimeSummary(mountPoint, null);
}

function updatePanelStatus(mountPoint, intent) {
    const status = mountPoint.querySelector(`[${StatusAttributeName}="1"]`);
    if (!status) {
        return;
    }

    const next =
        intent === 'play' ? 'playing' :
        intent === 'pause' ? 'paused' :
        intent === 'clear-timeline' ? 'recording' :
        intent === 'step-forward' || intent === 'step-back' || intent === 'jump-to-entry' ? 'paused' :
        status.textContent || 'recording';

    status.textContent = next;
}

function payloadForIntent(target, mountPoint) {
    const rawPayload = target.getAttribute(PayloadAttributeName);
    if (rawPayload) {
        return parsePayload(rawPayload);
    }

    const intent = target.getAttribute(IntentAttributeName);
    if (intent === 'jump-to-entry') {
        const jumpInput = mountPoint.querySelector(`[${JumpInputAttributeName}="1"]`);
        const entryId = Number.parseInt(jumpInput?.value ?? '', 10);
        if (Number.isFinite(entryId) && entryId >= 0) {
            return { entryId };
        }
    }

    return undefined;
}

function historyIntentFromTarget(target) {
    const historyItem = target.closest?.('[data-sequence]');
    if (!historyItem) {
        return null;
    }

    const entryId = Number.parseInt(historyItem.getAttribute('data-sequence') ?? '', 10);
    if (!Number.isFinite(entryId) || entryId < 0) {
        return null;
    }

    return {
        intent: 'jump-to-entry',
        payload: { entryId }
    };
}

async function invokeRuntimeBridge(message, mountPoint) {
    if (!runtimeBridge) {
        appendPanelLogEntry(mountPoint, 'Runtime bridge unavailable in this mode.');
        return;
    }

    try {
        const entryId = Number.isFinite(message?.entryId) ? message.entryId : -1;
        const raw = await Promise.resolve(runtimeBridge(message.type, entryId));
        const [status = 'unknown', cursorRaw = '?', timelineRaw = '?'] = String(raw ?? '').split('|');
        const response = {
            status,
            cursorPosition: Number.parseInt(cursorRaw, 10),
            timelineSize: Number.parseInt(timelineRaw, 10)
        };

        const statusTag = mountPoint.querySelector(`[${StatusAttributeName}="1"]`);
        if (statusTag) {
            statusTag.textContent = response.status;
        }

        updateRuntimeSummary(mountPoint, response);
        appendPanelLogEntry(
            mountPoint,
            `${message.type} -> ${response.status} (cursor ${Number.isFinite(response.cursorPosition) ? response.cursorPosition : '?'}, timeline ${Number.isFinite(response.timelineSize) ? response.timelineSize : '?'})`
        );
    } catch (err) {
        appendPanelLogEntry(mountPoint, `${message.type} -> runtime bridge error`);
        console.warn('[Abies Debugger] Runtime bridge error.', err);
    }
}

/**
 * Initialize debugger adapter wiring when the module loads.
 */
export function initializeDebuggerAdapter() {
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
export function bootstrapIntentTransport(mountPoint) {
    mountPoint.addEventListener(IntentEventName, (event) => {
        forwardIntentToRuntimeBridge(event?.detail, mountPoint);
    });

    mountPoint.addEventListener('click', (event) => {
        const target = event.target instanceof Element
            ? event.target.closest(`[${IntentAttributeName}]`)
            : null;

        const historyIntent = event.target instanceof Element
            ? historyIntentFromTarget(event.target)
            : null;

        if (!target && historyIntent) {
            updatePanelStatus(mountPoint, historyIntent.intent);
            emitIntent(mountPoint, historyIntent);
            return;
        }

        if (!target) {
            return;
        }

        const intent = target.getAttribute(IntentAttributeName);
        if (!intent) {
            return;
        }

        const payload = payloadForIntent(target, mountPoint);
        if (intent === TogglePanelIntentValue) {
            event.stopPropagation();
            toggleDebuggerPanel(mountPoint);
            return;
        }

        updatePanelStatus(mountPoint, intent);

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
export function emitIntent(mountPoint, detail) {
    mountPoint.dispatchEvent(new CustomEvent(IntentEventName, {
        detail,
        bubbles: false,
        cancelable: false
    }));
}

/**
 * Parse optional JSON payload for data-abies-debugger-payload.
 */
export function parsePayload(rawPayload) {
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
export function forwardIntentToRuntimeBridge(detail, mountPoint) {
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

    void invokeRuntimeBridge(message, mountPoint);
}

mountDebugger();