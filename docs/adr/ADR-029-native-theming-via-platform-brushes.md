# ADR-029: Native Theming via Platform Brushes

**Status:** Accepted
**Date:** 2026-08-08
**Decision Makers:** Maurice Peters

## Context

The native vocabulary took colours as literal strings — `Foreground("#1F1F1F")`,
`Background("gray")`. That is unusable in practice for one reason: **a literal colour is
wrong in one of the two themes.** An app styled for light mode is unreadable in dark mode
and vice versa, and there was no way to express "the normal text colour".

[ADR-027](./ADR-027-native-winui-renderer.md) anticipated this, listing as roadmap work a
"styling/theming story (map to XAML theme resources rather than reinventing)". This records
the decision it deferred.

The constraint that shapes it: `Picea.Abies.Native` is now published (2.3.x), so
`INativeBackend<T>` cannot gain members without a breaking change.

## Decision

**Map semantic roles onto the platform's own theme brushes. Do not build a token system.**

1. A `ThemeColor` enum names roles, not colours: `TextPrimary`, `TextSecondary`,
   `TextDisabled`, `Accent`, `SurfaceBase`, `SurfaceCard`, `Stroke`, `Success`, `Caution`,
   `Critical`.
2. `Theme` maps each role to a WinUI theme resource key
   (`TextFillColorPrimaryBrush`, `CardBackgroundFillColorDefaultBrush`, …).
3. `Properties.Foreground`/`Background`/`BorderBrush` gain `ThemeColor` overloads. The
   existing `string` overloads stay, for cases where a specific colour is genuinely wanted.
4. The value is carried as `"theme:<ResourceKey>"`, so the backend can tell a lookup from a
   literal without a new attribute or interface member.
5. The backend resolves it against `Application.Current.Resources`. **A key that does not
   resolve leaves the property at its default and reports through the renderer's fault
   callback** rather than throwing.

## Consequences

### Positive

- Light and dark work with no per-app effort, and match the rest of the OS — including
  high-contrast themes, which a hand-rolled palette would miss.
- Purely additive. No change to `INativeBackend<T>`, no change to existing views, and
  literal colours keep working.
- The encoding needs no new plumbing: it is an ordinary string attribute, so the diff,
  the interpreter and the headless tests treat it like any other.

### Negative

- The role list is small and opinionated. Apps wanting a brush outside it can pass an
  explicit key via `Theme.Encode(string)`, which is less discoverable than the enum.
- The resource keys are WinUI's. A future Avalonia or AppKit backend would need its own
  mapping for the same roles — which is the correct shape, but it is work.
- Roles are resolved per property write, so a runtime theme switch only takes effect for
  properties the diff rewrites. Full live re-theming would need the renderer to re-resolve
  brushes on a theme-change event; not done, and not currently needed.

### Neutral

- The mapped keys are not verified at build time. That is why a missing key degrades to a
  reported fault instead of an exception: the failure mode is a control with default
  styling and a logged message, not a lost window.

## Alternatives Considered

### Alternative 1: A framework-level token system

Define Abies's own tokens and resolve them to colours per theme. Rejected: it duplicates
what the platform already ships, drifts from the OS as WinUI evolves, and would not follow
high-contrast or user accent-colour settings without reimplementing all of it.

### Alternative 2: Typed brush objects in the vocabulary

Model brushes as first-class vocabulary values rather than encoded strings. Rejected: the
vocabulary is deliberately string-valued so the diff can compare attributes cheaply and the
interpreter stays platform-neutral. Introducing a typed brush would push platform types
into `Picea.Abies.Native`, which has no UI dependency by design.

### Alternative 3: Wait for a full design

Leave theming out until a complete story exists. Rejected: literal colours are actively
harmful — they ship apps that are unreadable in half the world's configurations — and this
mapping is small, additive and reversible.

## Related Decisions

- [ADR-027: Native WinUI Renderer](./ADR-027-native-winui-renderer.md)
- [ADR-028: Program Core/View Split](./ADR-028-program-core-view-split.md)
