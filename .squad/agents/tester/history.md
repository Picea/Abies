# Tester Agent — History

## Session: Phase 2 UI Component E2E Tests (2026-03-21)

### What was done
Created `Picea.Abies.UI.Demo.Testing.E2E/UiDemoPhase2Tests.cs` with 10 E2E tests covering accessibility contracts for the Phase 2 Abies.UI components (stack, card, divider, progressBar, alert, skeleton) plus focus-trap and keyboard navigation tests. Build verified: 0 errors, 0 warnings.

### Tests written
| Test | What it checks |
|------|----------------|
| `Stack_ShouldRenderChildren` | Vertical stack renders "Item 1" text |
| `Card_ShouldRenderContent` | `div.abies-ui-card` is visible |
| `Divider_ShouldRenderAsSeparator` | `hr[role="separator"]` is visible |
| `DividerLabeled_ShouldRenderLabelText` | `span.abies-ui-divider__label` is visible |
| `ProgressBar_ShouldHaveAccessibleRole` | `[role="progressbar"]` visible + `aria-valuemin` present |
| `ProgressBar_Indeterminate_ShouldNotHaveAriaValueNow` | Second progressbar has no `aria-valuenow` |
| `Alert_ShouldHaveAssistiveTechnologyRoleAndText` | `[role="alert"]` or `[role="status"]` is visible |
| `Skeleton_ShouldHaveAriaBusy` | `[aria-busy="true"]` is visible |
| `Modal_FocusShouldBeTrappedInsideWhenOpen` | Tab key keeps focus inside `[role="dialog"]` |
| `Table_ArrowKeysShouldNavigateRows` | ArrowDown moves focus to next `tbody tr[tabindex="0"]` |

## Learnings

- **TUnit assertion API**: Use `await Assert.That(value).IsTrue()` — NOT NUnit's `Assert.That(x, Is.True)`. `Is` does not exist in TUnit context.
- **Regex import for Playwright attribute assertions**: `ToHaveAttributeAsync` with a Regex matcher requires `System.Text.RegularExpressions.Regex`. No extra using is needed since implicit usings are enabled, but fully-qualify or add the using if targeting a narrower scope.
- **Project build quirk**: The `Picea.Abies.UI.Demo.Testing.E2E` project runs a `dotnet publish` on the WASM project as a `BeforeTargets="Build"` step. This makes the first build slower than expected, but `--no-restore` still works to skip restore.
- **No `.squad/agents/tester/` folder exists by default**: Must be created when the Tester role is first activated.
- **Tests are written against future DOM** — the demo app rendering Phase 2 components is being added by a parallel agent. Tests are authored against the expected final DOM structure and will be flaky until the demo app is updated.

## Cross-Agent Context — 2026-03-21 (issue #166 Phase 2)

### From csharpdev
7 Phase 2 components shipped in `Picea.Abies.UI`. DOM contracts tested in `UiDemoPhase2Tests.cs` are exactly what these components emit:
- `stack()` → `div.abies-ui-stack` with children
- `card()` → `div.abies-ui-card`
- `divider()` (plain) → `<hr role="separator" class="abies-ui-divider">`
- `divider()` (labeled) → `<div class="abies-ui-divider abies-ui-divider--labeled">` containing `<span class="abies-ui-divider__label">`
- `progressBar()` (determinate) → `[role="progressbar"]` with `aria-valuemin`, `aria-valuemax`, `aria-valuenow`
- `progressBar()` (indeterminate) → `[role="progressbar"]` WITHOUT `aria-valuenow`
- `alert()` → `[role="alert"]` (IsLive=true) or `[role="status"]` (IsLive=false)
- `skeleton()` → `[aria-busy="true"]`
Demo `Program.cs` heading is now `"Phase 2 component kit"` — update any test that navigates by heading text.

### From jsdev
`abies-ui.js` now loaded in demo via `<script src="abies-ui.js" defer>`. Two tests depend on it:
- `Modal_FocusShouldBeTrappedInsideWhenOpen` — requires focus trap to be active (needs `abies-ui.js` loaded and `[role="dialog"][aria-modal="true"]` present in DOM)
- `Table_ArrowKeysShouldNavigateRows` — requires grid nav listener (needs `tbody tr[tabindex="0"]` rows)
If either test fails: (1) check `abies-ui.js` is served correctly, (2) verify the demo emits the expected ARIA attributes on modal/table rows.
