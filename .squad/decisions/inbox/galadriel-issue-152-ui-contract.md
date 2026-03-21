### 2026-03-20T00:00:00Z: Phase 1 UI kit contract for issue #152
**By:** Galadriel (Frontend Dev)
**What:**
- Phase 1 components in `Picea.Abies.UI` use pure Node-returning functions with immutable `record` options; no hidden mutable UI state.
- Public API follows `Component(options)` with `options` records supporting `Class`, `Style`, `Attributes`, and `DataTestId` extensibility to keep components composable.
- Accessibility baseline is WCAG 2.1 AA with explicit keyboard, focus, and ARIA contracts per component before release.
- Theming is token-first via CSS custom properties; component CSS consumes semantic tokens (`--abies-bg`, `--abies-text`, `--abies-border`, `--abies-accent`, etc.) mapped from existing Abies brand and neutral ramps.
- Demo app IA includes Overview, Foundations (tokens/accessibility), and per-component pages with API, states, and keyboard docs.
- Consistency constraints: pure rendering only, no inline hex/px literals in component styles, deterministic focus behavior, standardized states (`default|hover|focus|disabled|error|loading`), and typed API naming conventions.
**Why:** Establishes a coherent frontend contract for ecosystem adoption and consistent implementation of issue #152.
