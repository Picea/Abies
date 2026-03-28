// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

/**
 * Abies Time Travel Debugger — Debug-only JavaScript module (v2)
 *
 * This module is EXCLUDED from Release builds and only loads during Debug.
 * The actual replay logic and state machine live in C# (Mealy machine).
 * This module owns: mount injection, UI rendering, keyboard nav,
 * bridge invocation, and local timeline cache management.
 *
 * JSON Bridge Protocol:
 *   Request:  runtimeBridge(messageType: string, entryId: number) → JSON string
 *   Response: { status, cursorPosition, timelineSize, atStart, atEnd,
 *               currentEntry, modelSnapshotPreview, previousModelSnapshotPreview,
 *               timelineEntries? }
 */

// ═══════════════════════════════════════════════════════════════════
// Constants
// ═══════════════════════════════════════════════════════════════════

const MOUNT_POINT_ID = 'abies-debugger-timeline';
const INIT_FLAG = 'abiesDebuggerAdapterInitialized';

// Abies brand palette (dark theme tokens)
const T = {
    bg:         '#0B1220',
    bgElevated: '#101828',
    bgSoft:     '#1D2939',
    text:       '#F2F4F7',
    textMuted:  'rgba(242,244,247,0.78)',
    textSubtle: 'rgba(242,244,247,0.54)',
    border:     'rgba(242,244,247,0.10)',
    borderStrong: 'rgba(242,244,247,0.24)',
    accent:     '#89B42B',
    accentHover:'#B7D56A',
    accentSoft: 'rgba(137,180,43,0.16)',
    danger:     '#F04438',
    info:       '#2E90FA',
    font:       '12px/1.3 ui-monospace, SFMono-Regular, Menlo, Consolas, monospace',
    fontSmall:  '11px/1.2 ui-monospace, SFMono-Regular, Menlo, Consolas, monospace',
    radius:     '8px',
    radiusSmall:'6px',
    focusRing:  'rgba(137,180,43,0.42)',
};

// ═══════════════════════════════════════════════════════════════════
// Module State
// ═══════════════════════════════════════════════════════════════════

let runtimeBridge = null;
let localTimeline = [];
let lastResponse = null;
let filterText = '';
let panelExpanded = false;
let playbackTimer = null;

const PLAYBACK_INTERVAL_MS = 250;

// DOM element references (set once during panel creation)
let els = {};

// ═══════════════════════════════════════════════════════════════════
// Configuration Resolution
// ═══════════════════════════════════════════════════════════════════

function parseToggle(value) {
    if (typeof value === 'boolean') return value;
    if (value == null) return null;
    const s = String(value).trim().toLowerCase();
    if ('1,true,on,yes,enabled'.includes(s)) return true;
    if ('0,false,off,no,disabled'.includes(s)) return false;
    return null;
}

function resolveEnabled() {
    try {
        const url = new URL(window.location.href);
        const q = url.searchParams.get('abies-debugger') ?? url.searchParams.get('debugger');
        const p = parseToggle(q);
        if (p !== null) return p;
    } catch { /* ignore */ }

    if (typeof globalThis !== 'undefined' && '__abiesDebugger' in globalThis) {
        const cfg = globalThis.__abiesDebugger;
        if (typeof cfg === 'boolean') return cfg;
        if (cfg && typeof cfg === 'object' && 'enabled' in cfg) {
            const p = parseToggle(cfg.enabled);
            if (p !== null) return p;
        }
    }

    const meta = document.querySelector('meta[name="abies-debugger"]')
              || document.querySelector('meta[name="debugger"]');
    if (meta?.content) {
        const p = parseToggle(meta.content);
        if (p !== null) return p;
    }

    return true;
}

// ═══════════════════════════════════════════════════════════════════
// Exported API (contract with C# JSImport)
// ═══════════════════════════════════════════════════════════════════

/**
 * Mounts the debugger host container. Idempotent.
 * @returns {HTMLElement|null}
 */
export function mountDebugger() {
    if (!resolveEnabled()) {
        console.debug('[Abies Debugger] Disabled via config.');
        return null;
    }

    let mp = document.getElementById(MOUNT_POINT_ID);
    if (!mp) {
        mp = document.createElement('div');
        mp.id = MOUNT_POINT_ID;
        document.body.appendChild(mp);
    }

    if (mp.dataset[INIT_FLAG] !== '1') {
        mp.dataset[INIT_FLAG] = '1';
        createDebuggerUI(mp);
        attachEventHandlers(mp);
    }

    return mp;
}

/**
 * Wires the .NET runtime bridge callback.
 * Signature: (messageType: string, entryId: number) => string (JSON)
 */
export function setRuntimeBridge(callback) {
    runtimeBridge = typeof callback === 'function' ? callback : null;
    if (runtimeBridge) {
        // Fetch initial timeline state
        void invokeRuntimeBridge('get-timeline', -1);
    }
    updateDisabledStates();
}

/**
 * Called from C# (via JSImport) after every message capture so the debugger
 * panel refreshes automatically when new messages arrive at runtime.
 */
export function notifyTimelineChanged() {
    void invokeRuntimeBridge('get-timeline', -1);
}

// ═══════════════════════════════════════════════════════════════════
// Bridge Invocation
// ═══════════════════════════════════════════════════════════════════

async function invokeRuntimeBridge(messageType, entryId) {
    if (!runtimeBridge) return;

    try {
        const raw = await Promise.resolve(
            runtimeBridge(messageType, Number.isFinite(entryId) ? entryId : -1)
        );
        const response = JSON.parse(raw);
        lastResponse = response;

        // Sync local timeline when size changes or full list provided
        if (response.timelineEntries) {
            localTimeline = response.timelineEntries;
        } else if (response.timelineSize !== localTimeline.length) {
            // Timeline size changed but entries weren't included — fetch full list
            const syncRaw = await Promise.resolve(runtimeBridge('get-timeline', -1));
            const syncResponse = JSON.parse(syncRaw);
            if (syncResponse.timelineEntries) {
                localTimeline = syncResponse.timelineEntries;
            }
            // Merge cursor/status from original response (get-timeline doesn't change state)
            lastResponse = { ...syncResponse, ...response, timelineEntries: syncResponse.timelineEntries };
        }

        updateUI();

        // Emit event for external consumers / tests
        document.dispatchEvent(new CustomEvent('abies:debugger:message-dispatched', {
            detail: { messageType, entryId, response },
            bubbles: true
        }));
    } catch (err) {
        console.warn('[Abies Debugger] Bridge error:', err);
    }
}

// ═══════════════════════════════════════════════════════════════════
// UI Creation
// ═══════════════════════════════════════════════════════════════════

function createDebuggerUI(mp) {
    // Shell toggle button (always visible)
    const shell = mkEl('button', {
        type: 'button',
        'aria-label': 'Toggle Abies Debugger',
        'aria-expanded': 'false',
        style: css({
            position: 'fixed', right: '12px', bottom: '12px', zIndex: '2147483647',
            background: T.bgElevated, color: T.text,
            border: `1px solid ${T.borderStrong}`, borderRadius: T.radius,
            padding: '8px 10px', font: T.font, cursor: 'pointer',
            display: 'flex', alignItems: 'center', gap: '6px',
        }),
    }, [
        mkEl('span', { style: css({ width: '8px', height: '8px', borderRadius: '50%', background: T.accent, display: 'inline-block' }) }),
        document.createTextNode('Abies Debugger'),
    ]);
    els.shell = shell;
    mp.appendChild(shell);

    // Main panel (hidden by default)
    const panel = mkEl('div', {
        role: 'region',
        'aria-label': 'Abies Time Travel Debugger',
        style: css({
            position: 'fixed', right: '12px', bottom: '52px', zIndex: '2147483647',
            background: T.bgElevated, color: T.text,
            border: `1px solid ${T.borderStrong}`, borderRadius: T.radius,
            padding: '0', width: '380px', maxWidth: 'calc(100vw - 24px)',
            font: T.font, display: 'none',
            boxShadow: '0 8px 32px rgba(0,0,0,0.5)',
            overflow: 'hidden',
        }),
    });
    els.panel = panel;

    // ── Header ──
    const header = mkEl('div', {
        style: css({
            display: 'flex', justifyContent: 'space-between', alignItems: 'center',
            padding: '10px 12px', borderBottom: `1px solid ${T.border}`,
        }),
    });

    els.statusBadge = mkEl('span', {
        'aria-live': 'polite',
        style: css({
            fontSize: '11px', padding: '2px 8px', borderRadius: '999px',
            border: `1px solid ${T.borderStrong}`, background: T.accentSoft,
            textTransform: 'uppercase', letterSpacing: '0.5px',
        }),
    }, ['recording']);

    els.stepCounter = mkEl('span', {
        'aria-live': 'polite',
        style: css({ font: T.fontSmall, color: T.textMuted }),
    }, ['Step \u2014/\u2014']);

    header.append(els.statusBadge, els.stepCounter);
    panel.appendChild(header);

    // ── Scrubber ──
    const scrubberWrap = mkEl('div', {
        style: css({ padding: '8px 12px', borderBottom: `1px solid ${T.border}` }),
    });
    els.scrubber = mkEl('input', {
        type: 'range', min: '0', max: '0', value: '0',
        'aria-label': 'Timeline position',
        style: css({ width: '100%', accentColor: T.accent }),
    });
    scrubberWrap.appendChild(els.scrubber);
    panel.appendChild(scrubberWrap);

    // ── Filter + Event List ──
    const listSection = mkEl('div', {
        style: css({ padding: '8px 12px', borderBottom: `1px solid ${T.border}` }),
    });

    els.filterInput = mkEl('input', {
        type: 'text', placeholder: 'Filter messages\u2026 ( / )',
        'aria-label': 'Filter timeline messages',
        style: css({
            width: '100%', boxSizing: 'border-box',
            background: T.bgSoft, border: `1px solid ${T.borderStrong}`, color: T.text,
            borderRadius: T.radiusSmall, padding: '6px 8px', font: T.fontSmall,
            marginBottom: '6px',
        }),
    });
    listSection.appendChild(els.filterInput);

    els.eventList = mkEl('div', {
        role: 'listbox',
        'aria-label': 'Timeline entries',
        tabindex: '0',
        style: css({
            maxHeight: '160px', overflowY: 'auto',
        }),
    });
    listSection.appendChild(els.eventList);
    panel.appendChild(listSection);

    // ── Details Panel ──
    const detailsSection = mkEl('div', {
        style: css({
            padding: '8px 12px', borderBottom: `1px solid ${T.border}`,
            maxHeight: '200px', overflowY: 'auto',
        }),
    });
    els.details = mkEl('div', {
        style: css({ font: T.fontSmall, color: T.textMuted }),
    }, ['Select an entry to view details.']);
    detailsSection.appendChild(els.details);
    panel.appendChild(detailsSection);

    // ── Transport Controls ──
    const controls = mkEl('div', {
        style: css({
            display: 'flex', gap: '6px', padding: '10px 12px', flexWrap: 'wrap',
        }),
    });

    const btnCss = css({
        border: `1px solid ${T.borderStrong}`, background: T.bgSoft, color: T.text,
        borderRadius: T.radiusSmall, padding: '6px 10px', cursor: 'pointer',
        font: T.fontSmall, flex: '1', textAlign: 'center', minWidth: '48px',
    });

    els.btnBack = mkEl('button', { type: 'button', style: btnCss, 'data-intent': 'step-back' }, ['\u25C0 Back']);
    els.btnPlayPause = mkEl('button', { type: 'button', style: btnCss, 'data-intent': 'play' }, ['\u25B6 Play']);
    els.btnStep = mkEl('button', { type: 'button', style: btnCss, 'data-intent': 'step-forward' }, ['Step \u25B6']);
    els.btnClear = mkEl('button', { type: 'button', style: btnCss, 'data-intent': 'clear-timeline' }, ['Clear']);

    controls.append(els.btnBack, els.btnPlayPause, els.btnStep, els.btnClear);
    panel.appendChild(controls);

    mp.appendChild(panel);
}

// ═══════════════════════════════════════════════════════════════════
// Event Handling
// ═══════════════════════════════════════════════════════════════════

function attachEventHandlers(mp) {
    // Shell toggle
    els.shell.addEventListener('click', () => {
        panelExpanded = !panelExpanded;
        els.panel.style.display = panelExpanded ? 'block' : 'none';
        els.shell.setAttribute('aria-expanded', String(panelExpanded));
    });

    // Transport control buttons
    els.panel.addEventListener('click', (e) => {
        const btn = e.target.closest?.('[data-intent]');
        if (!btn || btn.disabled) return;

        const intent = btn.getAttribute('data-intent');
        if (!intent) return;

        // Play/Pause has its own handler with playback loop logic
        if (intent === 'play' || intent === 'pause') {
            handlePlayPause();
            return;
        }

        // Any other transport action stops an active playback
        stopPlayback();
        void invokeRuntimeBridge(intent, -1);
    });

    // Scrubber input (jump on change — stops active playback)
    els.scrubber.addEventListener('input', () => {
        stopPlayback();
        const val = parseInt(els.scrubber.value, 10);
        if (Number.isFinite(val) && val >= 0) {
            void invokeRuntimeBridge('jump-to-entry', val);
        }
    });

    // Event list click (jump to entry — stops active playback)
    els.eventList.addEventListener('click', (e) => {
        stopPlayback();
        const item = e.target.closest?.('[data-sequence]');
        if (!item) return;
        const seq = parseInt(item.getAttribute('data-sequence'), 10);
        if (Number.isFinite(seq) && seq >= 0) {
            void invokeRuntimeBridge('jump-to-entry', seq);
        }
    });

    // Filter input
    els.filterInput.addEventListener('input', () => {
        filterText = els.filterInput.value.trim().toLowerCase();
        renderEventList();
    });

    // Keyboard navigation (only when focus is inside the debugger panel)
    document.addEventListener('keydown', (e) => {
        if (!panelExpanded) return;

        // Only intercept keys when focus is inside the debugger mount point.
        // This prevents stealing Space/Arrow/Home/End from the host application.
        const mp = document.getElementById(MOUNT_POINT_ID);
        const active = document.activeElement;
        if (!mp || !mp.contains(active)) return;

        const isOurFilter = active === els.filterInput;

        switch (e.key) {
            case ' ':
                if (isOurFilter) return;
                e.preventDefault();
                handlePlayPause();
                break;
            case 'ArrowLeft':
                if (isOurFilter) return;
                e.preventDefault();
                stopPlayback();
                void invokeRuntimeBridge('step-back', -1);
                break;
            case 'ArrowRight':
                if (isOurFilter) return;
                e.preventDefault();
                stopPlayback();
                void invokeRuntimeBridge('step-forward', -1);
                break;
            case 'Home':
                if (isOurFilter) return;
                e.preventDefault();
                stopPlayback();
                if (localTimeline.length > 0) {
                    void invokeRuntimeBridge('jump-to-entry', 0);
                }
                break;
            case 'End':
                if (isOurFilter) return;
                e.preventDefault();
                stopPlayback();
                if (localTimeline.length > 0) {
                    void invokeRuntimeBridge('jump-to-entry', localTimeline.length - 1);
                }
                break;
            case '/':
                if (!isOurFilter) {
                    e.preventDefault();
                    els.filterInput.focus();
                }
                break;
            case 'Escape':
                if (isOurFilter) {
                    els.filterInput.blur();
                    filterText = '';
                    els.filterInput.value = '';
                    renderEventList();
                }
                break;
        }
    });
}

function handlePlayPause() {
    if (!lastResponse) return;
    const isPlaying = lastResponse.status === 'playing';
    if (isPlaying) {
        stopPlayback();
        void invokeRuntimeBridge('pause', -1);
    } else {
        // If at end of timeline, rewind to start first (like a media player)
        const prepare = lastResponse.atEnd && localTimeline.length > 0
            ? invokeRuntimeBridge('jump-to-entry', 0)
            : Promise.resolve();
        void prepare
            .then(() => invokeRuntimeBridge('play', -1))
            .then(() => startPlayback());
    }
}

/**
 * Starts a timed auto-step loop that calls step-forward at PLAYBACK_INTERVAL_MS intervals.
 * Each step updates the UI; the loop stops when the end is reached, the state is no longer
 * "playing", or stopPlayback() is called explicitly (e.g. by Pause, Jump, Clear).
 */
function startPlayback() {
    stopPlayback();
    scheduleNextStep();
}

function scheduleNextStep() {
    playbackTimer = setTimeout(async () => {
        playbackTimer = null;

        // Guard: stop if state drifted away from playing
        if (!lastResponse || lastResponse.status !== 'playing') return;

        await invokeRuntimeBridge('step-forward', -1);

        // After the step, check if we should continue
        if (lastResponse && lastResponse.status === 'playing' && !lastResponse.atEnd) {
            scheduleNextStep();
        }
    }, PLAYBACK_INTERVAL_MS);
}

function stopPlayback() {
    if (playbackTimer !== null) {
        clearTimeout(playbackTimer);
        playbackTimer = null;
    }
}

// ═══════════════════════════════════════════════════════════════════
// UI Update
// ═══════════════════════════════════════════════════════════════════

function updateUI() {
    if (!lastResponse || !els.panel) return;

    const r = lastResponse;

    // Status badge
    els.statusBadge.textContent = r.status ?? 'unknown';
    els.statusBadge.style.background =
        r.status === 'recording' ? T.accentSoft :
        r.status === 'playing'   ? 'rgba(46,144,250,0.16)' :
        'rgba(242,244,247,0.08)';

    // Step counter
    const cursor = Number.isFinite(r.cursorPosition) ? r.cursorPosition : -1;
    const size = Number.isFinite(r.timelineSize) ? r.timelineSize : 0;
    els.stepCounter.textContent = size > 0
        ? `Step ${cursor + 1}/${size}`
        : 'Step \u2014/\u2014';

    // Scrubber
    els.scrubber.max = String(Math.max(0, size - 1));
    els.scrubber.value = String(Math.max(0, cursor));
    els.scrubber.disabled = size === 0;

    // Play/Pause button label
    if (r.status === 'playing') {
        els.btnPlayPause.textContent = '\u23F8 Pause';
        els.btnPlayPause.setAttribute('data-intent', 'pause');
    } else {
        els.btnPlayPause.textContent = '\u25B6 Play';
        els.btnPlayPause.setAttribute('data-intent', 'play');
    }

    // Event list + details
    renderEventList();
    renderDetails();
    updateDisabledStates();
}

function updateDisabledStates() {
    if (!els.panel) return;

    const r = lastResponse;
    const hasBridge = !!runtimeBridge;
    const hasTimeline = r && r.timelineSize > 0;

    setDisabled(els.btnBack, !hasBridge || !hasTimeline || (r && r.atStart));
    setDisabled(els.btnStep, !hasBridge || !hasTimeline || (r && r.atEnd));
    setDisabled(els.btnPlayPause, !hasBridge || !hasTimeline);
    setDisabled(els.btnClear, !hasBridge || !hasTimeline);
    setDisabled(els.scrubber, !hasBridge || !hasTimeline);
}

function setDisabled(element, disabled) {
    if (!element) return;
    element.disabled = !!disabled;
    element.style.opacity = disabled ? '0.4' : '1';
    element.style.cursor = disabled ? 'default' : 'pointer';
}

// ═══════════════════════════════════════════════════════════════════
// Event List Rendering
// ═══════════════════════════════════════════════════════════════════

function renderEventList() {
    if (!els.eventList) return;

    const list = els.eventList;
    list.innerHTML = '';

    const cursor = lastResponse?.cursorPosition ?? -1;
    const filtered = filterText
        ? localTimeline.filter(e => e.messageType.toLowerCase().includes(filterText))
        : localTimeline;

    if (filtered.length === 0) {
        const empty = mkEl('div', {
            style: css({ padding: '12px 0', color: T.textSubtle, textAlign: 'center', font: T.fontSmall }),
        }, [localTimeline.length === 0 ? 'No events recorded yet.' : 'No matching events.']);
        list.appendChild(empty);
        return;
    }

    for (const entry of filtered) {
        const isSelected = entry.sequence === cursor;
        const item = mkEl('div', {
            role: 'option',
            'aria-selected': String(isSelected),
            'data-sequence': String(entry.sequence),
            style: css({
                display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                padding: '4px 8px', cursor: 'pointer', borderRadius: '4px',
                background: isSelected ? T.accentSoft : 'transparent',
                borderLeft: isSelected ? `3px solid ${T.accent}` : '3px solid transparent',
            }),
        });

        const left = mkEl('span', {
            style: css({ display: 'flex', gap: '6px', alignItems: 'center' }),
        });
        const seqLabel = mkEl('span', {
            style: css({ color: T.textSubtle, minWidth: '28px', textAlign: 'right', font: T.fontSmall }),
        }, [String(entry.sequence)]);
        const typeLabel = mkEl('span', {
            style: css({ color: isSelected ? T.text : T.textMuted, font: T.fontSmall }),
        }, [entry.messageType]);
        left.append(seqLabel, typeLabel);

        const patchLabel = mkEl('span', {
            style: css({ color: T.textSubtle, font: T.fontSmall }),
        }, [`${entry.patchCount}p`]);

        item.append(left, patchLabel);
        list.appendChild(item);
    }

    // Scroll selected into view
    const selected = list.querySelector('[aria-selected="true"]');
    if (selected) {
        selected.scrollIntoView({ block: 'nearest' });
    }
}

// ═══════════════════════════════════════════════════════════════════
// Details Panel Rendering
// ═══════════════════════════════════════════════════════════════════

function renderDetails() {
    if (!els.details || !lastResponse) return;

    const r = lastResponse;
    const entry = r.currentEntry;

    if (!entry) {
        els.details.innerHTML = '';
        els.details.appendChild(
            mkEl('span', { style: css({ color: T.textSubtle }) }, ['Select an entry to view details.'])
        );
        return;
    }

    els.details.innerHTML = '';

    // Message type + args
    const typeRow = detailRow('Message', entry.messageType);
    els.details.appendChild(typeRow);

    if (entry.argsPreview && entry.argsPreview !== '{}') {
        const argsRow = detailRow('Payload', '');
        const argsCode = mkEl('pre', {
            style: css({
                margin: '2px 0 0 0', padding: '6px 8px', background: T.bg,
                borderRadius: '4px', font: T.fontSmall, color: T.textMuted,
                whiteSpace: 'pre-wrap', wordBreak: 'break-all', maxHeight: '60px', overflowY: 'auto',
            }),
        }, [formatJson(entry.argsPreview)]);
        argsRow.appendChild(argsCode);
        els.details.appendChild(argsRow);
    }

    // Separator
    els.details.appendChild(mkEl('hr', {
        style: css({ border: 'none', borderTop: `1px solid ${T.border}`, margin: '6px 0' }),
    }));

    // Before / After model preview
    if (r.previousModelSnapshotPreview) {
        const beforeRow = detailRow('Before', '');
        beforeRow.appendChild(modelPreviewBlock(r.previousModelSnapshotPreview));
        els.details.appendChild(beforeRow);
    }

    if (r.modelSnapshotPreview) {
        const afterRow = detailRow('After', '');
        afterRow.appendChild(modelPreviewBlock(r.modelSnapshotPreview));
        els.details.appendChild(afterRow);
    }

    // Separator + patch count
    els.details.appendChild(mkEl('hr', {
        style: css({ border: 'none', borderTop: `1px solid ${T.border}`, margin: '6px 0' }),
    }));
    els.details.appendChild(
        detailRow('Patches', String(entry.patchCount))
    );
}

function detailRow(label, value) {
    const row = mkEl('div', {
        style: css({ marginBottom: '4px' }),
    });
    const labelEl = mkEl('span', {
        style: css({ color: T.textSubtle, font: T.fontSmall }),
    }, [`${label}: `]);
    const valueEl = mkEl('span', {
        style: css({ color: T.text, font: T.fontSmall }),
    }, [value]);
    row.append(labelEl, valueEl);
    return row;
}

function modelPreviewBlock(json) {
    return mkEl('pre', {
        style: css({
            margin: '2px 0 0 0', padding: '6px 8px', background: T.bg,
            borderRadius: '4px', font: T.fontSmall, color: T.textMuted,
            whiteSpace: 'pre-wrap', wordBreak: 'break-all',
            maxHeight: '80px', overflowY: 'auto',
        }),
    }, [formatJson(json)]);
}

function formatJson(str) {
    try {
        const parsed = typeof str === 'string' ? JSON.parse(str) : str;
        return JSON.stringify(parsed, null, 2);
    } catch {
        return str || '';
    }
}

// ═══════════════════════════════════════════════════════════════════
// DOM Helpers
// ═══════════════════════════════════════════════════════════════════

function mkEl(tag, attrs, children) {
    const e = document.createElement(tag);
    if (attrs) {
        for (const [k, v] of Object.entries(attrs)) {
            if (k === 'style') {
                e.setAttribute('style', v);
            } else {
                e.setAttribute(k, v);
            }
        }
    }
    if (children) {
        for (const child of children) {
            e.append(typeof child === 'string' ? document.createTextNode(child) : child);
        }
    }
    return e;
}

function css(obj) {
    return Object.entries(obj).map(([k, v]) => `${camelToKebab(k)}:${v}`).join(';');
}

function camelToKebab(s) {
    return s.replace(/[A-Z]/g, m => `-${m.toLowerCase()}`);
}

// ═══════════════════════════════════════════════════════════════════
// Auto-mount
// ═══════════════════════════════════════════════════════════════════

mountDebugger();
