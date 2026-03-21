# Senior C# Developer — History

## About This File
Project-specific learnings from C#/.NET functional domain modeling work. Read this before every session.

## Platform
- **.NET 10** (LTS), C# 14
- **TUnit** for all testing
- **Picea.Abies** namespace root

## Functional Patterns Established
*None yet — Result/Option usage, workflow signatures, capability patterns tracked here.*

## Constrained Types Created
| Type | Invariants | Module |
|---|---|---|
| *None yet* | | |

## Bounded Contexts
*None yet — context boundaries and their relationships tracked here.*

## NuGet Packages Added
| Package | Why | Version | Date |
|---|---|---|---|
| *None yet* | | | |

## Performance Observations
*None yet — benchmark results and allocation profiles tracked here.*

## EF Core Patterns & Gotchas
*None yet.*

## Domain Modeling Decisions
*None yet — aggregate boundaries, event designs, ACL patterns tracked here.*

## Conventions
*None yet — propose team-wide conventions via `.squad/decisions/inbox/`.*

## Learnings

### Phase 2 UI Components (issue #166, 2026-03-21)

**Pattern: BEM-lite helper methods for CSS class mapping**
- Every new enum gets a private `ToXxxCss()` helper that maps enum → CSS modifier string.
- `BuildClassName(params string?[] tokens)` already skips nulls — pass `null` for absent modifiers rather than ternary empty strings.
- For optional gap/modifier classes, return `null` from the CSS helper (e.g., `StackGap.None → null`), then use the null-coalescing pattern `gapCss is not null ? $"...-{gapCss}" : null`.

**Pattern: ARIA-first progressBar**
- `aria-valuenow` uses the raw value (same scale as min/max), NOT the percentage.
- Indeterminate state: always emit `aria-label`; omit `aria-valuenow`.
- Determinate state with visible label: only add `aria-label` if `ShowLabel = false`.
- Percentage for the fill `style` comes from `(value - min) / (max - min) * 100`, clamped to [0, 100].
- Always use `CultureInfo.InvariantCulture` for double → HTML attribute string formatting.

**Pattern: labeled vs. plain divider**
- A labeled divider is a `<div>` wrapper (not an `<hr>`) containing two `role="presentation"` `<hr>` elements flanking the label `<span>`.
- A plain divider is a single `<hr role="separator">` — no `aria-label` because a label-less rule has no textual meaning.

**Pattern: skeleton multi-line**
- `Lines > 1` with `SkeletonShape.Text` renders N inner `<div class="abies-ui-skeleton abies-ui-skeleton--text-line">` children inside the outer wrapper. Lines = 1 renders the outer div directly with no children.

### UI Demo Phase 2 update (2026-03-21)

**Task:** Added Phase 2 component showcase to `Picea.Abies.UI.Demo/Program.cs`.

**Changes made:**
- `DemoModel` record: added `IsProgressBarDeterminate`, `ProgressBarValue`, `IsAlertVisible` optional fields with defaults.
- `DemoMessage`: added `ToggleProgressBarMode`, `IncrementProgress`, `ToggleAlert` sealed record message types.
- `Initialize`: set explicit values for all three new fields.
- `Transition`: added three new switch arms for the new messages.
- `View`: updated heading to `"Phase 2 component kit"` and intro text; added 10 new `ShowcaseSection` calls covering `stack` (vertical + horizontal), `card`, `divider` (plain + labeled), `progressBar` (determinate + indeterminate), `alert` (info + danger-live), `skeleton` (text-lines + avatar).

**Gotcha:** After replacing the end of the grid array, the closing `]))` of the modal `ShowcaseSection` was missing its trailing comma before the new sections. Watch for missing commas when appending inside array literals.

**Empty node:** `new Empty()` (from `Picea.Abies.DOM`) is the correct way to render nothing conditionally.
- When building `Node[]` via LINQ on a `div()` call, cast explicitly to `(Node)` in the lambda to force `IEnumerable<Node>` inference: `Select(_ => (Node)div(...))`.

**Pattern: alert live regions**
- `IsLive = true` → `role="alert"` + `aria-live="assertive"` (disruptive, e.g., errors)
- `IsLive = false` → `role="status"` + `aria-live="polite"` (non-disruptive announcements)

## Cross-Agent Context — 2026-03-21 (issue #166 Phase 2)

### From jsdev
`abies-ui.js` is now a static web asset in `Picea.Abies.UI/wwwroot/abies-ui.js`. It provides two runtime behaviours that complement the C# components:
1. **Focus trap** — activated automatically when `[role="dialog"][aria-modal="true"]` is inserted. `modal()` already emits this; no C# changes needed for the trap to work.
2. **Table grid navigation** — activated for all `tbody tr[tabindex="0"]` rows via event delegation. For `table()` components to be keyboard-navigable, rows must have `tabindex="0"`.
The file uses `AbortController` for teardown (ES2024+, no stored listener references). Load via `<script src="abies-ui.js" defer>` — not `type="module"`.

### From tester
`UiDemoPhase2Tests.cs` (10 E2E tests) covers accessibility contracts for all 7 Phase 2 components. Tests are authored against the expected DOM from `Program.cs`. Key DOM contracts expected:
- `div.abies-ui-card` — card
- `hr[role="separator"]` — plain divider
- `span.abies-ui-divider__label` — labeled divider
- `[role="progressbar"]` with `aria-valuemin` and no `aria-valuenow` for indeterminate state
- `[role="alert"]` or `[role="status"]` — alert
- `[aria-busy="true"]` — skeleton
If the component markup changes, update the E2E contracts in tandem.
- Always emit `aria-atomic="true"` so AT reads the whole message, not just changed text.
