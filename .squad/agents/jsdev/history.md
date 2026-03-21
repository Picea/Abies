# Senior JavaScript Developer — History

## About This File
Project-specific learnings from JS/TS work. Read this before every session.

## Patterns Established

- **Focus trap via `AbortController` signal** — attaching the `keydown` listener with `{ signal }` from an `AbortController` allows single-call teardown (`controller.abort()`) with no manual `removeEventListener`. Clean and idiomatic ES2024+ pattern. Use it for any scoped event listener pair.
- **MutationObserver modal detection** — watch `document.body` with `{ childList: true, subtree: true }` to detect `[role="dialog"][aria-modal="true"]` additions/removals. Check both the mutated node itself (`node.matches(...)`) and its children (`node.querySelector(...)`) to handle the case where Abies inserts a wrapper element.
- **Event delegation for table grid nav** — a single `document.addEventListener('keydown', ..., { capture: true })` handles all tables on the page. Guard: check `instanceof HTMLTableRowElement` + `tabindex="0"` + `closest('tbody')` before acting.
- **`Set` for keyset guards** — `const GRID_NAV_KEYS = new Set([...])` + `GRID_NAV_KEYS.has(event.key)` is faster and more readable than `||` chains for multi-key checks.

## Dependencies Added
| Package | Why | Date |
|---|---|---|
| *None yet* | | |

## Performance Observations

- `getFocusableElements()` is called on every Tab keydown while a modal is open. For typical modal sizes (<50 elements) this is negligible. If performance becomes a concern, cache the collection and invalidate via a nested `MutationObserver` on the dialog.

## Gotchas & Quirks

- **`document.body.focus()` without `tabindex="-1"`** — programmatic `focus()` on `<body>` succeeds in modern browsers even without an explicit tabindex, but the element won't receive keyboard events unless tabindex is set. For focus-return fallback this is sufficient (we just want to move focus away from a removed element).
- **Abies modal removal timing** — when Abies replaces a modal with `new Empty()`, the DOM node is removed synchronously during the patch. The `MutationObserver` fires asynchronously (microtask checkpoint after the mutation), so `data-focus-return` must be read off the *removed* node (still available in `mutation.removedNodes`), not from the live DOM.

## Conventions

- `abies-ui.js` is a **side-effect module**: no exports, auto-initialises on `DOMContentLoaded` (or immediately if DOM is ready). Load with `<script src="abies-ui.js" defer></script>` — not `type="module"` — so it runs after the DOM is parsed without needing import graphs.
- CSS for UI components lives in `abies-ui.tokens.css`. JS in `abies-ui.js` must **never** apply inline styles — only toggle classes or attributes, letting CSS do the visual work.

## Learnings

### 2026-03-21 — abies-ui.js: focus trap + table grid nav

- Implementing WCAG 2.1.2 (No Keyboard Trap) correctly requires handling the *empty modal* edge case: if there are no focusable children, fall back to focusing the dialog root (which already has `tabindex="-1"`). Without this, Tab becomes completely dead inside an empty modal.
- The `deactivateFocusTrap` function must read `data-focus-return` from the **removed** node — by the time the MutationObserver callback fires, the dialog is no longer in the DOM, so `document.getElementById` will only work for the return target, not the dialog itself.
- `AbortController` + `{ signal }` is the idiomatic ES2024+ way to remove event listeners tied to a lifecycle. Storing `AbortController` in a module-level variable (`activeTrapController`) gives clean "only one active trap" semantics with a single `?.abort()` call.

## Cross-Agent Context — 2026-03-21 (issue #166 Phase 2)

### From csharpdev
7 Phase 2 components shipped in `Picea.Abies.UI`: `stack`, `card`, `divider`, `grid`, `progressBar`, `alert`, `skeleton`. CSS activation relies entirely on class names — `abies-ui.js` must never apply inline styles, only toggle classes/attributes. The `modal()` component (Phase 1) already emits `role="dialog" aria-modal="true"` — `abies-ui.js` focus trap targets this reliably. The `alert()` component emits `role="alert"` (live) or `role="status"` (polite) — no JS needed for announcement; native ARIA handles it.

### From tester
`UiDemoPhase2Tests.cs` includes `Modal_FocusShouldBeTrappedInsideWhenOpen` and `Table_ArrowKeysShouldNavigateRows` — both tests exercise `abies-ui.js` directly. These tests assert that (a) Tab keeps focus inside `[role="dialog"]` and (b) ArrowDown moves focus to the next `tbody tr[tabindex="0"]`. If `abies-ui.js` is not loaded or initialisation fails, these two tests will fail. The E2E project builds `Picea.Abies.UI.Demo` (WASM) as a `BeforeTargets="Build"` step — first build is slow.
