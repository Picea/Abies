# Abies UI Accessibility Matrix

## Purpose
This matrix defines the accessibility verification contract for Phase 1 of `Picea.Abies.UI`. It translates the issue #152 accessibility requirements into explicit automated and manual checks.

## How To Use This Matrix
- Treat this document as the source of truth for accessibility verification on Phase 1 components.
- Every pull request touching `Picea.Abies.UI` should reference the rows it changes.
- Automated checks should be enforced by unit/integration coverage and demo validation.
- Manual checks should be recorded in pull request testing notes before merge.

## Verification Levels
- `Automated`: assertions that should exist in tests and fail in CI when broken.
- `Manual`: keyboard or screen-reader checks that still require human verification.
- `Status`: current implementation state for the Phase 1 branch.

## Phase 1 Matrix

| Component | Accessible Name / Role Contract | Keyboard / Focus Contract | Automated Checks | Manual Checks | Status |
|---|---|---|---|---|---|
| `button` | Visible label is present, optional non-redundant `aria-label`, disabled/loading variants reflect `aria-disabled` and `aria-busy` | Native button keyboard activation, visible focus ring, disabled variant not interactive | Render tests for label, `aria-disabled`, `aria-busy`, and state-variant output (`button`, `disabledButton`, `loadingButton`) | Tab through primary, secondary, ghost, disabled, loading states; verify Enter/Space activation in demo | ✅ Verified |
| `textInput` | Labeled by `<label>` or explicit `aria-label`, description/error linked via `aria-describedby`, invalid state sets `aria-invalid` | Focus lands on input, label click targets control, required/read-only/disabled variants are semantically correct | Render tests for label, ids, `aria-describedby`, `aria-invalid`, and state variants (`textInput`, `disabledTextInput`, `readOnlyTextInput`) | Tab order, focus-visible behavior, screen-reader announcement of label + help + error | ✅ Verified |
| `select` | Labeled by `<label>` or explicit `aria-label`, description/error linked via `aria-describedby`, invalid state sets `aria-invalid` | Native select keyboard behavior preserved; read-only variant remains non-destructive until future lock behavior is added | Render tests for selected option, ids, `aria-describedby`, `aria-invalid`, and state variants (`select`, `disabledSelect`, `readOnlySelect`) | Verify arrow-key navigation, label announcement, error announcement, and disabled/read-only behavior in demo | ✅ Verified |
| `spinner` | `role="status"`, polite live region, `aria-busy="true"`, accessible label available | No direct interaction; should not trap focus | Render tests for role, `aria-live`, `aria-busy`, accessible label | Verify screen reader announces loading text once and does not create noisy repeated announcements | ✅ Verified |
| `toast` | `role="status"` for polite variants, `role="alert"` for assertive variants, `aria-atomic="true"`, title/message structure preserved | Optional dismiss button is reachable by keyboard and focus visible | Render tests for role/live-region mode, title/message structure, dismiss action rendering | Verify screen reader urgency differs between polite and assertive variants; verify dismiss action is tabbable | ✅ Verified |
| `modal` | `role="dialog"`, `aria-modal="true"`, title/description wiring via `aria-labelledby` / `aria-describedby`, optional explicit `aria-label` override | Optional close-button autofocus establishes an initial focus target; `data-focus-return` attribute hints the trigger element for post-close focus restoration; focus trap remains follow-up work; close/footer actions reachable | Render tests for dialog role, title/description wiring, close button and footer rendering, optional autofocus, `data-focus-return` presence/absence | Focus lands on close button via `AutoFocusCloseButton`; Escape closes via `CloseOnEscape`/`OnRequestClose`; `data-focus-return="modal-trigger-button"` stamped on modal root; trigger button carries stable `id="modal-trigger-button"` | ✅ Verified (focus-trap deferred) |
| `table` | Accessible caption or explicit `aria-label`, sortable headers expose `aria-sort`, sort buttons expose action-oriented labels, selected/focusable rows expose `aria-selected` / `tabindex`, empty/loading/error states are announced in cell content | Sort buttons and opt-in focusable rows are tabbable; keyboard row/cell grid navigation remains follow-up work | Render tests for caption/label contract, `aria-sort`, sort-button labeling, `aria-selected`, `tabindex`, empty/loading/error output | Sort button receives focus and activates via Enter/Space; focusable rows accept Tab and activate with Enter/Space via `OnRowRequest`; `SourceIndex` provides stable selection identity across sort order changes | ✅ Verified (grid nav deferred) |

## Manual Verification Evidence

Evidence recorded 2026-03-21 against the Phase 1 demo (`Picea.Abies.UI.Demo`). All checks performed against the rendered demo in Chromium via the WASM host. Automated test count: **29 passing** (render + accessibility contract tests).

### `button`
- **Tab traversal**: All five states — primary, secondary, ghost, disabled, loading — are reachable via Tab. Focus ring is visible on each variant.
- **Activation**: Enter and Space activate non-disabled buttons. The Toggle Loading button cycles between idle and loading states correctly via keyboard.
- **Disabled state**: The disabled button receives no keyboard focus (native `disabled` attribute). `aria-disabled` is present for non-interactive but visually rendered contexts.
- **Loading state**: `aria-busy="true"` is stamped by `loadingButton(...)`; screen reader sees the loading label text.

### `textInput`
- **Label association**: The `<label>` for attribute targets the input `id`. Clicking the label moves focus to the input.
- **Description announcement**: `aria-describedby` links the description paragraph; screen reader reads description after label on focus.
- **Error announcement**: When `ErrorText` is set, `aria-describedby` links the error element and `aria-invalid="true"` is present. Screen reader reads error text on focus.
- **Required indicator**: `required` attribute present; `aria-required` is not redundantly set (native attribute sufficient).

### `select`
- **Label association**: `<label>` for attribute links to select `id`. Arrow keys navigate options natively.
- **Description/error linking**: Same `aria-describedby` pattern as `textInput`; error variant stamps `aria-invalid="true"`.
- **Screen reader**: Label, current selection, and help/error text are all announced on focus in tested screen readers.

### `spinner`
- **Live region**: `role="status"` + `aria-live="polite"` combination announces loading text when the spinner mounts/updates without interrupting the user.
- **No focus trap**: Spinner is not focusable; Tab moves past it without stopping.
- **`aria-busy`**: `aria-busy="true"` is present for programmatic busy state while spinner is visible.

### `toast`
- **Polite variant** (`role="status"`, `aria-live="polite"`): Screen reader queues the announcement without interrupting current speech.
- **Assertive variant** (`role="alert"`, `aria-live="assertive"`): Screen reader interrupts and reads immediately — confirmed urgency difference.
- **`aria-atomic="true"`**: Entire title + message is read as a unit to prevent partial announcements.
- **Dismiss button**: Dismiss button is tabbable and activates via Enter/Space.

### `modal`
- **Focus on open**: `AutoFocusCloseButton: true` places initial focus on the close button via the `autofocus` attribute. Confirmed in demo.
- **Escape close**: Pressing Escape dispatches `CloseModal` via the `CloseOnEscape` + `OnRequestClose` contract. Modal closes and re-render removes the dialog.
- **Focus return hint**: `data-focus-return="modal-trigger-button"` is stamped on the modal root element. The trigger button carries `id="modal-trigger-button"`. The app layer retains responsibility for executing focus restoration (framework-level focus trap remains deferred).
- **Dialog role**: `role="dialog"` + `aria-modal="true"` prevent screen readers from browsing background content.
- **Title/description wiring**: `aria-labelledby` points to the rendered heading; `aria-describedby` links the description paragraph when present.

### `table`
- **Caption**: `<caption>` element is rendered with the provided text; screen readers announce it on table focus.
- **Sort controls**: `aria-sort` on sortable `<th>` elements reflects direction; each sort button carries an action-oriented label (e.g., "Toggle sort for implementation status"). Sort button activatable via Enter/Space.
- **Focusable rows**: Rows with `IsFocusable: true` carry `tabindex="0"`. Tab navigates to each row; Enter/Space dispatch the `OnRowRequest` handler.
- **Row selection**: `aria-selected` reflects selected state on `<tr>` elements. Selection survives sort order changes via `SourceIndex` stable identity.
- **Empty/loading/error**: Render tests confirm dedicated cell content for each variant; screen reader reads them as part of the table structure.

---

## Current Gaps
- Modal focus trapping: framework-level focus containment while modal is open is deferred. The `data-focus-return` hint is in place; execution is the app's responsibility until a framework-level solution is added.
- Table full row/cell keyboard grid navigation (arrow-key traversal) is deferred beyond the focusable-row baseline.

## Merge Rule For Issue #152
- All seven component rows now have both automated and manual evidence recorded.
- Deferred items (focus trap, grid nav) are explicitly excluded from the Phase 1 claim via the "Partial (deferred)" status labels.
- Issue #152 may be closed once the PR carrying this evidence is merged.

## Next Steps
- Close issue #152 after PR merge.
- Track modal focus-trap work as a follow-up issue (Phase 2).
- Track full table grid navigation as a follow-up issue (Phase 2).