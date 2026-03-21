### 2026-03-20T13:20:00Z: Issue #152 architecture direction for Picea.Abies.UI
**By:** Maurice Cornelius Gerardus Petrus Peters (via Gandalf)
**What:** Adopt a two-package Phase 1 structure with `Picea.Abies.UI` (pure Node components) and `Picea.Abies.UI.Demo` (showcase app + accessibility samples), using token-driven CSS custom properties and explicit accessibility contracts per component. Start with seven baseline components (`button`, `textInput`, `select`, `modal`, `table`, `spinner`, `toast`) and enforce zero hidden mutable state.
**Why:** Delivers ecosystem-critical starter kit quickly while preserving Abies MVU purity, ensuring WCAG 2.1 AA readiness, and creating reusable conventions for future community component libraries.

**Plan Summary:**
1. Publish conventions first: packaging, API signatures, style token contract, a11y checklist.
2. Implement seven core components in `Picea.Abies.UI` with typed props + clear slot/content patterns.
3. Add `Picea.Abies.UI.Demo` pages for usage variants, states, keyboard behavior, and ARIA examples.
4. Gate completion with accessibility checks, docs completeness, and demo parity against acceptance criteria.

**Risk Controls:**
- Avoid stateful drift by requiring function-only component APIs and no internal state stores.
- Avoid styling fragmentation by standardizing CSS variable names and semantic token mapping.
- Avoid inaccessible defaults by requiring keyboard and SR behavior definitions before component merge.
