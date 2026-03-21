### 2026-03-20T14:05:00Z: Issue #152 review gate — execution readiness constraints
**By:** Elrond (Reviewer)
**What:** Mark issue #152 as `changes-requested` until the package defines (1) exact v1 scope boundaries for `table` behavior and non-goals, (2) measurable accessibility verification gates (automated + manual) per interactive component, and (3) explicit release criteria for `Picea.Abies.UI` and `Picea.Abies.UI.Demo` with CI job mapping.
**Why:** Current design direction is strong but still leaves ambiguity that can cause scope drift, inconsistent implementation, and unverifiable completion claims.

**Required completion contract before implementation start:**
1. `table` v1 contract: sorting/filtering behavior, keyboard model, and what is deferred to Phase 2+.
2. Accessibility gate definition: required checks, tools, and pass/fail threshold for each interactive component.
3. CI/release mapping: which pipeline jobs gate merge, package publish, and demo verification.
