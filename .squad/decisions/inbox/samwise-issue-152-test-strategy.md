### 2026-03-20T00:00:00Z: Issue #152 Phase 1 test strategy and merge gates
**By:** Samwise (Tester)
**What:**
- Adopt a component-kit test pyramid with unit as the base, integration + accessibility in the middle, and focused E2E smoke/critical flows at the top.
- Require WCAG 2.1 AA checks and keyboard-only interaction coverage for every interactive Phase 1 component (`button`, `textInput`, `modal`, `table`, `spinner`, `toast`, `select`).
- Enforce CI merge gates: all component unit/integration tests, accessibility matrix, and a focused E2E suite for cross-component workflows must pass before merge.
- Apply deterministic E2E setup (API/fixtures, not UI setup) to reduce flakiness and keep the suite stable in CI.
**Why:**
- Issue #152 requires zero mutable state components, WCAG 2.1 AA compliance, and confidence to publish `Picea.Abies.UI` as an ecosystem foundation.
- A strict but lean gate balances reliability and delivery speed for Phase 1.
