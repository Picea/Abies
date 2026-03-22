/**
 * abies-ui.js — Abies UI accessibility utilities
 *
 * Provides:
 *  - Modal focus trap (WCAG 2.1 AA — 2.1.2 No Keyboard Trap)
 *  - Table row keyboard grid navigation (WCAG 2.1 AA — 2.1.1/4.1.3)
 *
 * Load this file alongside abies-ui.tokens.css.
 * No build step. No dependencies. ES2024+.
 */

// ─── Focus Trap ───────────────────────────────────────────────────────────────

const FOCUSABLE_SELECTORS = [
  'button:not([disabled])',
  '[href]',
  'input:not([disabled])',
  'select:not([disabled])',
  'textarea:not([disabled])',
  '[tabindex]:not([tabindex="-1"])',
  'details > summary',
].join(', ');

/** @type {AbortController | null} */
let activeTrapController = null;
/** @type {HTMLElement | null} */
let trapReturnTarget = null;
/** @type {Element | null} */
let activeTrapDialog = null;

/**
 * Returns whether an element is effectively visible for focus purposes.
 * @param {Element} el
 * @returns {boolean}
 */
function isElementVisible(el) {
  const rects = el.getClientRects();
  if (rects.length === 0) {
    return false;
  }

  const win = el.ownerDocument.defaultView;
  if (!win) {
    return false;
  }

  const style = win.getComputedStyle(el);
  if (style.display === 'none' || style.visibility === 'hidden') {
    return false;
  }

  return true;
}

/**
 * Returns all focusable, visible elements within a container.
 * Excludes elements hidden via `[hidden]` or `[inert]` ancestors.
 * @param {Element} container
 * @returns {Element[]}
 */
function getFocusableElements(container) {
  return [...container.querySelectorAll(FOCUSABLE_SELECTORS)].filter(
    (el) =>
      isElementVisible(el) &&
      !el.closest('[hidden]') &&
      !el.closest('[inert]'),
  );
}

/**
 * Activates a focus trap on a `[role="dialog"][aria-modal="true"]` element.
 * Traps Tab / Shift+Tab within the modal and moves initial focus inside it.
 * Only one trap may be active at a time — calling this while a trap is active
 * deactivates the previous one first.
 * @param {Element} dialog
 */
function activateFocusTrap(dialog) {
  activeTrapController?.abort();

  const controller = new AbortController();
  activeTrapController = controller;
  activeTrapDialog = dialog;

  // Capture the element to restore focus to when the trap is deactivated.
  // Prefer data-focus-return on the dialog; fall back to the currently focused element.
  const returnId = dialog.getAttribute('data-focus-return');
  trapReturnTarget =
    (returnId ? document.getElementById(returnId) : null) ??
    (document.activeElement instanceof HTMLElement ? document.activeElement : null);

  const { signal } = controller;

  document.addEventListener(
    'keydown',
    (event) => {
      if (event.key !== 'Tab') return;

      const focusable = getFocusableElements(dialog);

      if (focusable.length === 0) {
        event.preventDefault();
        dialog.focus();
        return;
      }

      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      const active = document.activeElement;

      if (!dialog.contains(active)) {
        event.preventDefault();
        const anchor = event.shiftKey ? last : first;
        if (typeof anchor.focus === 'function') {
          anchor.focus();
        } else {
          dialog.focus();
        }
        return;
      }

      if (event.shiftKey) {
        if (active === first || active === dialog) {
          event.preventDefault();
          last.focus();
        }
      } else {
        if (active === last) {
          event.preventDefault();
          first.focus();
        }
      }
    },
    { signal },
  );

  document.addEventListener(
    'focusin',
    (event) => {
      const target = event.target;
      if (!(target instanceof Element)) return;
      if (dialog.contains(target)) return;

      const focusable = getFocusableElements(dialog);
      const anchor = focusable[0];
      if (anchor && typeof anchor.focus === 'function') {
        anchor.focus();
      } else {
        dialog.focus();
      }
    },
    { signal },
  );

  // Move initial focus: prefer [autofocus], then first focusable, then dialog root
  const autofocusEl = dialog.querySelector('[autofocus]');
  const focusable = getFocusableElements(dialog);

  if (autofocusEl) {
    autofocusEl.focus();
  } else if (focusable.length > 0) {
    focusable[0].focus();
  } else {
    dialog.focus();
  }
}

/**
 * Deactivates the active focus trap and restores focus to the element
 * identified by the removed dialog's `data-focus-return` attribute (if any),
 * falling back to the target captured during activation.
 * If no return target is found, focus falls back to `document.body`.
 * @param {Element} dialog
 */
function deactivateFocusTrap(dialog) {
  activeTrapController?.abort();
  activeTrapController = null;
  activeTrapDialog = null;

  const returnId = dialog.getAttribute('data-focus-return');
  const returnTarget =
    (returnId ? document.getElementById(returnId) : null) ??
    trapReturnTarget;

  if (returnTarget instanceof HTMLElement) {
    returnTarget.focus();
  } else {
    // Last resort: body may not be focusable — temporarily grant tabindex.
    const hadTabindex = document.body.hasAttribute('tabindex');
    const previousTabindex = document.body.getAttribute('tabindex');
    document.body.setAttribute('tabindex', '-1');
    document.body.focus();
    if (hadTabindex && previousTabindex !== null) {
      document.body.setAttribute('tabindex', previousTabindex);
    } else {
      document.body.removeAttribute('tabindex');
    }
  }
  trapReturnTarget = null;
}

/**
 * Initialises a `MutationObserver` on `document.body` that activates and
 * deactivates focus traps as `[role="dialog"][aria-modal="true"]` elements
 * are added to or removed from the DOM.
 */
function initFocusTrap() {
  const DIALOG_SELECTOR = '[role="dialog"][aria-modal="true"]';

  const observer = new MutationObserver((mutations) => {
    for (const mutation of mutations) {
      for (const node of mutation.addedNodes) {
        if (node.nodeType !== Node.ELEMENT_NODE) continue;

        const dialog =
          node.matches(DIALOG_SELECTOR)
            ? node
            : node.querySelector(DIALOG_SELECTOR);

        if (dialog) {
          activateFocusTrap(dialog);
        }
      }

      for (const node of mutation.removedNodes) {
        if (node.nodeType !== Node.ELEMENT_NODE) continue;

        const dialog =
          node.matches(DIALOG_SELECTOR)
            ? node
            : node.querySelector(DIALOG_SELECTOR);

        if (
          dialog &&
          activeTrapController &&
          activeTrapDialog &&
          (dialog === activeTrapDialog || dialog.contains(activeTrapDialog))
        ) {
          deactivateFocusTrap(activeTrapDialog);
        }
      }
    }
  });

  observer.observe(document.body, { childList: true, subtree: true });
}

// ─── Table Row Grid Navigation ────────────────────────────────────────────────

const GRID_NAV_KEYS = new Set(['ArrowDown', 'ArrowUp', 'Home', 'End']);

/**
 * Returns all keyboard-navigable (`tabindex="0"`) rows within a `<tbody>`.
 * @param {HTMLTableSectionElement} tbody
 * @returns {HTMLTableRowElement[]}
 */
function getNavigableRows(tbody) {
  return [...tbody.querySelectorAll('tr[tabindex="0"]')];
}

/**
 * Handles keyboard navigation across table rows using Arrow, Home, and End keys.
 * Operates via event delegation from `document` so no per-table setup is needed.
 * Skips gracefully when the event target is not a focusable `<tr>` inside a `<tbody>`.
 * @param {KeyboardEvent} event
 */
function handleTableGridKeydown(event) {
  if (!GRID_NAV_KEYS.has(event.key)) return;

  const target = /** @type {Element} */ (event.target);
  if (!(target instanceof HTMLTableRowElement)) return;
  if (target.getAttribute('tabindex') !== '0') return;

  const tbody = target.closest('tbody');
  if (!tbody) return;

  const rows = getNavigableRows(tbody);
  if (rows.length === 0) return;

  event.preventDefault();

  const currentIndex = rows.indexOf(target);

  let nextRow;
  switch (event.key) {
    case 'ArrowDown':
      nextRow = rows[Math.min(currentIndex + 1, rows.length - 1)];
      break;
    case 'ArrowUp':
      nextRow = rows[Math.max(currentIndex - 1, 0)];
      break;
    case 'Home':
      nextRow = rows[0];
      break;
    case 'End':
      nextRow = rows[rows.length - 1];
      break;
  }

  nextRow?.focus();
}

/**
 * Initialises table row grid navigation via a single capturing `keydown`
 * listener on `document`. No per-table setup required.
 */
function initTableGridNav() {
  document.addEventListener('keydown', handleTableGridKeydown, {
    capture: true,
  });
}

// ─── Auto-initialization ──────────────────────────────────────────────────────

function init() {
  initFocusTrap();
  initTableGridNav();
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', init);
} else {
  init();
}
