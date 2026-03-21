# Abies UI Component Library Conventions

## Purpose
This guide defines the conventions for shipping reusable Node-producing UI libraries in Abies, with Phase 1 expectations for `Picea.Abies.UI`.

## 1. Packaging Reusable Node-Producing Libraries

### Required package shape
- Ship the component library as a class library package that exposes only pure component APIs.
- Keep demo and verification assets in a separate package/app (`Picea.Abies.UI.Demo`), not in the production package.
- Keep runtime dependencies minimal and aligned with Abies core packages.

### Required repository structure
- Place component source in a dedicated package folder (`Picea.Abies.UI`).
- Place interactive examples, accessibility demos, and verification pages in `Picea.Abies.UI.Demo`.
- Keep docs near code and link from package README files.

### Public surface rule
- Export component constructors and immutable option records, including explicit state constructors when state changes semantics.
- Do not expose mutable services, global state registries, or component-internal stores.

## 2. Required Component API Shape

### Contract
- Every component is a pure function that returns a Node.
- Inputs are immutable option records (or equivalent immutable value types).
- Rendering output is fully determined by input options.
- No hidden mutable state in component APIs.

### Baseline API pattern
```csharp
namespace Picea.Abies.UI;

public sealed record ButtonOptions(
    string Label,
  string Type = "button",
    string Variant = "primary",
    string? AriaLabel = null,
  UiCommonOptions? Common = null);

public sealed record LoadingButtonOptions(
  string Label,
  string LoadingText = "Loading",
  string Type = "button",
  string Variant = "primary",
  string? AriaLabel = null,
  UiCommonOptions? Common = null);

public static class Components
{
  public static Node button(ButtonOptions options);
  public static Node disabledButton(ButtonOptions options);
  public static Node loadingButton(LoadingButtonOptions options);
}
```

State should be represented by API shape, not boolean flags in a single options type.
- Use `button(...)`, `disabledButton(...)`, and `loadingButton(...)` for button states.
- Use `textInput(...)`, `disabledTextInput(...)`, and `readOnlyTextInput(...)` for text input states.
- Use `select(...)`, `disabledSelect(...)`, and `readOnlySelect(...)` for select states.

### API review checks
- Option record is immutable.
- No component mutates shared state.
- Function returns only Node output.
- Interactive callbacks are explicit and optional.

## 3. Theming Via CSS Custom Properties (Token-First)

### Rules
- Style through CSS custom properties first; avoid hardcoded component colors.
- Use semantic tokens mapped from brand/foundation tokens.
- Keep tokens stable across components and map state tokens (`hover`, `active`, `disabled`, `focus`) explicitly.

### Minimum token contract
```css
:root {
  --abies-ui-color-bg: var(--abies-bg);
  --abies-ui-color-text: var(--abies-text);
  --abies-ui-color-border: var(--abies-border);
  --abies-ui-color-accent: var(--abies-brand-500);
  --abies-ui-color-accent-hover: var(--abies-brand-600);
  --abies-ui-focus-ring: var(--abies-focus-ring);
  --abies-ui-space-2: 0.5rem;
  --abies-ui-space-3: 0.75rem;
  --abies-ui-radius-2: 0.5rem;
}
```

### Theme policy
- `Picea.Abies.UI` consumes shared Abies token names and does not redefine brand ramps.
- Component CSS must document token dependencies per component.
- New tokens require documentation and demo verification before merge.

## 4. Accessibility Requirements And Verification

### Accessibility contract (required)
- Target WCAG 2.1 AA for all interactive components.
- Every interactive component must define:
  - Keyboard behavior (Tab sequence, Enter/Space behavior, Escape where relevant)
  - Focus behavior (initial focus, focus trap if modal, focus return on close)
  - ARIA behavior (role, name, state, and live-region usage)

### Verification expectations
- Automated checks:
  - Unit/integration assertions for ARIA roles, labels, states, and disabled/read-only behavior
  - Accessibility scan coverage in CI for demo surfaces
- Manual checks:
  - Keyboard-only walkthrough for each component state
  - Screen reader smoke validation for key flows

### Merge expectation
- No component merges without recorded automated and manual accessibility evidence.

## 5. Versioning And Release Expectations For Picea.Abies.UI

### Versioning
- Follow SemVer:
  - Patch: fixes with no API contract changes
  - Minor: additive, backward-compatible API changes
  - Major: breaking API or behavior contract changes

### Release scope control
- Release notes must explicitly map to v1 include/defer boundaries.
- Deferred items are not shipped under patch or minor releases unless planned and documented.
- Breaking accessibility contract changes require major version consideration.

### Release gates
- Pull request merge gates must pass:
  - PR Validation
  - CD build
  - E2E
  - CodeQL Security Analysis
  - Benchmark (js-framework-benchmark) benchmark-check
- Main branch release must confirm package, demo, and docs alignment before publish.

## 6. Contributor Checklist

Use this checklist before requesting review:

- [ ] Component API uses immutable option records and a pure Node-returning function.
- [ ] No hidden mutable state introduced.
- [ ] CSS uses token-first custom properties; no unapproved hardcoded theme values.
- [ ] Keyboard/focus/ARIA behavior is documented and implemented.
- [ ] Automated accessibility checks are added/updated.
- [ ] Manual accessibility verification is completed and recorded.
- [ ] Docs and demo examples are updated.
- [ ] Release notes impact (include/defer scope) is identified.
