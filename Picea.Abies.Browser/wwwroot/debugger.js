// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

/**
 * Abies Time Travel Debugger — Debug-only JavaScript module
 * 
 * This module is EXCLUDED from Release builds and only loads during Debug.
 * It initializes the debugger UI mount point and wires event handlers.
 * 
 * The actual replay logic and state machine live in C# (Mealy machine).
 * This module only coordinates user input → adapter message dispatch.
 */

(function() {
    'use strict';

    const MountPointId = 'abies-debugger-timeline';
    const DebuggerEvents = {
        MessageDispatched: 'abies:debugger:message-dispatched'
    };

    /**
     * Initialize the debugger UI when the module loads.
     */
    function initializeDebugger() {
        const mountPoint = document.getElementById(MountPointId);
        if (!mountPoint) {
            console.debug('[Abies Debugger] Mount point not found. Skipping initialization.');
            return;
        }

        if (mountPoint.dataset.abiesDebuggerInitialized === '1') {
            return;
        }

        mountPoint.dataset.abiesDebuggerInitialized = '1';

        console.debug('[Abies Debugger] Initializing at mount point:', MountPointId);

        // Build minimal timeline UI structure
        const ui = buildDebuggerUI(mountPoint);
        
        // Wire event handlers
        wireEventHandlers(ui, mountPoint);
        
        console.debug('[Abies Debugger] Initialization complete.');
    }

    /**
     * Build the debugger UI structure in the mount point.
     */
    function buildDebuggerUI(mountPoint) {
        const ui = {
            controlBar: document.createElement('div'),
            messageLog: document.createElement('div'),
            timelineInspector: document.createElement('div'),
            playButton: document.createElement('button'),
            pauseButton: document.createElement('button'),
            stepForwardButton: document.createElement('button'),
            stepBackButton: document.createElement('button'),
            jumpInput: document.createElement('input'),
            clearButton: document.createElement('button')
        };

        // Set IDs for test verification
        ui.controlBar.id = 'control-bar';
        ui.messageLog.id = 'message-log';
        ui.timelineInspector.id = 'timeline-inspector';
        ui.playButton.id = 'play-button';
        ui.pauseButton.id = 'pause-button';
        ui.stepForwardButton.id = 'step-forward-button';
        ui.stepBackButton.id = 'step-back-button';
        ui.jumpInput.id = 'jump-input';
        ui.clearButton.id = 'clear-button';

        // Set button labels
        ui.playButton.textContent = '▶ Play';
        ui.pauseButton.textContent = '⏸ Pause';
        ui.stepForwardButton.textContent = '→ Step';
        ui.stepBackButton.textContent = '← Back';
        ui.clearButton.textContent = '✕ Clear';

        ui.jumpInput.type = 'number';
        ui.jumpInput.placeholder = 'Jump to entry...';
        ui.jumpInput.min = '0';

        // Build structure
        ui.controlBar.className = 'debugger-control-bar';
        ui.controlBar.appendChild(ui.playButton);
        ui.controlBar.appendChild(ui.pauseButton);
        ui.controlBar.appendChild(ui.stepForwardButton);
        ui.controlBar.appendChild(ui.stepBackButton);
        ui.controlBar.appendChild(ui.jumpInput);
        ui.controlBar.appendChild(ui.clearButton);

        ui.messageLog.className = 'debugger-message-log';
        ui.messageLog.innerHTML = '<div class="log-entry">Debugger timeline initialized</div>';

        ui.timelineInspector.className = 'debugger-timeline-inspector';
        ui.timelineInspector.textContent = 'Timeline: 0 entries';

        mountPoint.appendChild(ui.controlBar);
        mountPoint.appendChild(ui.messageLog);
        mountPoint.appendChild(ui.timelineInspector);

        return ui;
    }

    /**
     * Wire event handlers for button clicks and keyboard shortcuts.
     */
    function wireEventHandlers(ui, mountPoint) {
        // Button click handlers
        ui.playButton.addEventListener('click', () => {
            dispatchDebuggerMessage({ type: 'play' });
        });

        ui.pauseButton.addEventListener('click', () => {
            dispatchDebuggerMessage({ type: 'pause' });
        });

        ui.stepForwardButton.addEventListener('click', () => {
            dispatchDebuggerMessage({ type: 'step-forward' });
        });

        ui.stepBackButton.addEventListener('click', () => {
            dispatchDebuggerMessage({ type: 'step-back' });
        });

        ui.clearButton.addEventListener('click', () => {
            dispatchDebuggerMessage({ type: 'clear-timeline' });
        });

        ui.jumpInput.addEventListener('keydown', (evt) => {
            if (evt.key === 'Enter' && ui.jumpInput.value) {
                const entryNumber = parseInt(ui.jumpInput.value, 10);
                if (!isNaN(entryNumber)) {
                    dispatchDebuggerMessage({
                        type: 'jump-to-entry',
                        entryId: entryNumber
                    });
                }
            }
        });

        // Keyboard shortcuts global handlers
        document.addEventListener('keydown', (evt) => {
            // Only dispatch if not typing in input fields
            if (evt.target === ui.jumpInput) {
                return;
            }

            switch (evt.key) {
                case ' ': // Space → play/pause toggle
                    evt.preventDefault();
                    dispatchDebuggerMessage({ type: 'play' });
                    break;
                case 'ArrowRight': // Right arrow → step forward
                    evt.preventDefault();
                    dispatchDebuggerMessage({ type: 'step-forward' });
                    break;
                case 'ArrowLeft': // Left arrow → step backward
                    evt.preventDefault();
                    dispatchDebuggerMessage({ type: 'step-back' });
                    break;
                case 'j':
                case 'J': // J → focus jump input
                    evt.preventDefault();
                    ui.jumpInput.focus();
                    break;
                case 'Escape': // Escape → blur/close
                    evt.preventDefault();
                    document.activeElement?.blur();
                    break;
            }
        });

        // Focus indicators for keyboard navigation
        mountPoint.addEventListener('focusin', () => {
            mountPoint.classList.add('focused');
        });

        mountPoint.addEventListener('focusout', () => {
            mountPoint.classList.remove('focused');
        });
    }

    /**
     * Dispatch a debugger message event.
     * The C# bridge will handle the actual replay logic.
     */
    function dispatchDebuggerMessage(message) {
        const event = new CustomEvent(DebuggerEvents.MessageDispatched, {
            detail: message,
            bubbles: true,
            cancelable: true
        });

        document.dispatchEvent(event);
        console.debug('[Abies Debugger] Message dispatched:', message);
    }

    // Auto-initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeDebugger);
    } else {
        initializeDebugger();
    }

    // Export for testing
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = {
            initializeDebugger,
            buildDebuggerUI,
            wireEventHandlers
        };
    }
})();
