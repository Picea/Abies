# Abies UI Issue #152 Execution Plan

## Purpose
This document converts the accepted Issue #152 architecture decisions into an execution-ready delivery plan for `Picea.Abies.UI` and `Picea.Abies.UI.Demo`.

## Scope Baseline
- Package model: two-package Phase 1 delivery
  - `Picea.Abies.UI`: pure Node-returning component functions
  - `Picea.Abies.UI.Demo`: usage examples, keyboard/a11y behavior samples, and verification harness
- Phase 1 component set:
  - `button`
  - `textInput`
  - `select`
  - `modal`
  - `table`
  - `spinner`
  - `toast`
- Core constraints:
  - No hidden mutable state in component APIs
  - Token-first styling via CSS custom properties
  - Accessibility contracts required for all interactive components

## Delivery Phases

### Phase 1: Conventions And Contracts
Owner: Architect + Frontend Dev

Checklist:
- [ ] Finalize public API conventions for component signatures and immutable options records.
- [ ] Finalize shared design token contract and CSS variable naming map.
- [ ] Define required accessibility behavior contract per interactive component.
- [ ] Define demo content contract (states, variants, keyboard flows, SR notes).
- [ ] Publish this phase output in docs and link from package readmes.

Deliverables:
- API contract section in package docs
- Token contract table used by all components
- Accessibility matrix template (automated + manual checks)

### Phase 2: Core Component Implementation
Owner: Frontend Dev

Checklist:
- [ ] Implement `button` v1 include set.
- [ ] Implement `textInput` v1 include set.
- [ ] Implement `select` v1 include set.
- [ ] Implement `modal` v1 include set.
- [ ] Implement `table` v1 include set.
- [ ] Implement `spinner` v1 include set.
- [ ] Implement `toast` v1 include set.
- [ ] Verify each component conforms to pure function + no hidden state rule.

Deliverables:
- Component implementations in `Picea.Abies.UI`
- Unit/integration tests for component contracts

### Phase 3: Demo, Accessibility Verification, And Quality Gates
Owner: Tester + Frontend Dev

Checklist:
- [ ] Build demo pages for each component covering all v1 states and variants.
- [ ] Execute automated accessibility checks for all interactive components.
- [ ] Execute manual keyboard/screen reader checks per accessibility matrix.
- [ ] Add/verify test coverage gates for accessibility regressions.
- [ ] Capture pass/fail evidence in PR artifacts.

Deliverables:
- Complete `Picea.Abies.UI.Demo` coverage for v1 scope
- Accessibility verification evidence attached to PR
- Updated tests and CI evidence

### Phase 4: Merge And Release Readiness
Owner: Reviewer + DevOps Engineer

Checklist:
- [ ] Confirm all merge gates are green for pull request.
- [ ] Confirm release gates are green on `main` after merge.
- [ ] Confirm package/version/release notes are aligned with v1 boundaries.
- [ ] Confirm deferred items remain outside release scope.

Deliverables:
- Reviewer approval for merge
- Successful publish-ready build on `main`

## Ownership Map

| Workstream | Primary Owner | Supporting Owner(s) | Reviewer Gate |
|---|---|---|---|
| Architecture constraints and phase plan | Gandalf (Architect) | Galadriel (Frontend Dev) | Elrond (Reviewer) |
| Component API and implementation | Galadriel (Frontend Dev) | Faramir (Senior C# Developer), Legolas (Senior JavaScript Specialist) | Elrond (Reviewer) |
| Accessibility strategy and test implementation | Samwise (Tester) | Galadriel (Frontend Dev) | Elrond (Reviewer) |
| CI and release workflow readiness | Gimli (DevOps Engineer) | Samwise (Tester), Boromir (Technical Writer) | Elrond (Reviewer) |
| Documentation and usage guidance | Boromir (Technical Writer) | Gandalf (Architect), Galadriel (Frontend Dev) | Elrond (Reviewer) |

## V1 Include/Defer Boundaries

| Component | V1 Include | Deferred Beyond V1 |
|---|---|---|
| `button` | Primary/secondary/ghost variants, disabled/loading states, keyboard activation parity, focus-visible behavior | Split-button, icon-only permission model variants, advanced command palette behaviors |
| `textInput` | Label/help/error text, required/disabled/read-only, validation visuals, keyboard and SR labeling contract | Masked inputs, rich formatting, async suggestion/autocomplete |
| `select` | Single-select, labeled control, disabled/read-only, keyboard navigation and ARIA combobox/listbox baseline | Multi-select, virtualization, grouped async data sources |
| `modal` | Open/close lifecycle, focus trap, escape/close controls, labelled title/body/description slots | Nested modal orchestration, non-modal drawers, complex animation choreography |
| `table` | Static and data-bound rendering, sortable columns (single-column), row selection baseline, keyboard row/cell navigation baseline, empty/loading/error states | Server-side paging orchestration, column resizing/reorder/drag, grouped headers, advanced filter builder |
| `spinner` | Inline and block usage, size variants, accessible busy semantics (`aria-busy`/status text pairing) | Progress percentage hybrid controls, skeleton orchestration layer |
| `toast` | Info/success/warning/error variants, timeout + manual dismiss, polite/assertive live region mapping | Stacked queue prioritization, undo workflows, cross-page persisted toasts |

## Merge Gate Checklist (Pull Request)

Required status checks must be green before merge:
- [ ] `PR Validation` workflow passes (title, description, branch, draft, size checks)
- [ ] `CD` workflow `build` job passes
- [ ] `E2E` workflow `e2e` job passes
- [ ] `CodeQL Security Analysis` workflow `analyze` job passes
- [ ] `Benchmark (js-framework-benchmark)`/`benchmark-check` passes

Additional quality confirmations:
- [ ] Accessibility matrix is complete (automated + manual) for all interactive v1 components.
- [ ] Deferred items are not implemented in v1 PR scope.
- [ ] PR description follows template sections and includes testing evidence.

## Release Gate Checklist (Post-Merge On Main)

Required conditions before package release:
- [ ] Merge gate checklist completed at PR time.
- [ ] `CD` workflow succeeds on `main` including pack/publish steps.
- [ ] Release notes align exactly with v1 include/defer boundaries.
- [ ] Demo validation on release artifact confirms no regressions in keyboard/a11y behavior.

## Non-Goals (Frozen For Issue #152)
- No Phase 2+ feature expansion for deferred component behaviors.
- No additional component families outside the Phase 1 seven-component set.
- No relaxation of WCAG 2.1 AA-aligned accessibility verification.

## Kickoff Exit Criteria
Issue #152 implementation kickoff is complete when:
- This plan is accepted as the execution source of truth.
- Owners are assigned per phase/workstream.
- First implementation PR references this plan and states which phase checklist items it closes.
