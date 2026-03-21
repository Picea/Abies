# Galadriel — Frontend Dev

## Project Context
- User: Maurice Cornelius Gerardus Petrus Peters
- Project: Abies (Tolkien legendarium theme)
- Created: 2026-03-19

## Learnings

- UI branding directive: apply Abies conference brand palette to UI work by default.
- Exception rule: Conduit-related projects keep their existing styling conventions and are excluded from palette enforcement.
- Issue #152 Phase 1 frontend contract: pure Node-returning component APIs with immutable option records, token-first styling, and explicit keyboard/focus/ARIA behavior per component.
- 2026-03-21: `Picea.Abies.UI` Phase 1 contract now uses shared `UiCommonOptions` extensibility (`Id`, `Class`, `Style`, `DataTestId`, extra attributes) plus component-level accessibility defaults for buttons, field wrappers, toast live regions, modal labeling, and table empty/loading/error states without introducing hidden state.
- 2026-03-21: Created `Picea.Abies.UI.Demo` as a minimal runnable WebAssembly showcase using the `Picea.Abies.SubscriptionsDemo` host pattern, local demo-only styling, and synced `abies-ui.tokens.css` from `Picea.Abies.UI` so issue #152 can validate the seven phase-1 components without touching unrelated apps.
