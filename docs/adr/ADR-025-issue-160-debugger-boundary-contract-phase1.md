# ADR-025: Issue 160 Debugger Boundary Contract (Phase 1)

**Status:** Accepted  
**Date:** 2026-03-23  
**Decision Makers:** Maurice Peters  
**Related:** ADR-001 (MVU Architecture), ADR-011 (JavaScript Interop), ADR-024 (Four Render Modes)

## Context

Issue 160 introduces debugger capabilities (timeline capture and replay) that cut across runtime, browser integration, and UI concerns. Without a hard boundary, implementation can drift into mixed responsibilities (debug UI logic in JavaScript, runtime-only behavior hidden in adapter code, or replay side effects leaking into production paths).

`Picea.Abies/Runtime.cs` already exposes stable seams where debugger behavior must attach:

- Rendering seam: `_apply(allPatches)` in `Render(TModel state)`
- Command execution seam: `InterpretCommand(...)` switch (including `NavigationCommand` and interpreter default path)
- Subscription seam: `SubscriptionManager.Update(...)`, `SubscriptionManager.Start(...)`, and `DispatchFromSubscription(...)`

This ADR defines a strict Phase 1 contract for those seams.

## Decision

### 1. Debugger UI Host (Non-Negotiable)

The debugger UI is an Abies MVU application. It is not a JavaScript framework UI and not a plain imperative DOM panel.

### 2. JavaScript Boundary

JavaScript is limited to mount and transport adapter responsibilities:

- Mounting the debugger MVU surface into the browser page
- Transporting debugger payloads between runtime and debugger UI host
- Browser-only wiring needed to bootstrap and detach the debugger surface

JavaScript must not own debugger domain logic (timeline state machine, replay rules, or side-effect policy).

### 3. Domain Ownership

The timeline and replay domain lives in `Picea.Abies`.

This includes:

- Timeline event model and ordering
- Replay cursor/state model
- Replay transition and gate policy
- Contracts consumed by both runtime integration and debugger UI

No timeline/replay domain types are defined in browser adapter layers.

### 4. Replay Side-Effect Gates

When runtime is in replay mode, the following effects are gated off:

- Command interpreter execution (`interpreter(command)` path)
- Subscription lifecycle effects (`SubscriptionManager.Start/Update` execution side effects)
- Navigation side effects (`navigationExecutor?.Invoke(...)`)

Replay mode may render and update debugger-visible state, but it must not execute live external effects.

### 5. Release-Strip Strategy

Debugger implementation must be removable from production artifacts for both C# and browser assets.

- C# strip: compile-time conditional inclusion (or equivalent linker-safe mechanism) so debugger-only runtime branches and types are omitted in release-strip builds.
- Browser strip: debugger adapter assets are isolated and excluded from release-strip output bundles.
- Contract requirement: stripping debugger features does not alter normal runtime behavior or public runtime contracts.

### 6. Phase 1 Scope

In scope:

- Boundary establishment across runtime, domain, and adapter layers
- Replay gating at runtime seams listed in Context
- Minimal debugger mount + transport adapter path
- Release-strip mechanism definition and integration points

Out of scope:

- Rich debugger UX workflows beyond baseline timeline/replay controls
- Time-travel mutation tools that alter historical events
- Remote multi-session collaborative debugging
- Public plugin API for third-party debugger extensions

## Consequences

### Positive

- Enforces MVU-first architecture for debugger UX
- Keeps JavaScript thin and replaceable
- Prevents replay from triggering unsafe side effects
- Enables shipping debug-capable and stripped artifacts from one codebase

### Negative

- Adds upfront design cost in `Picea.Abies` before UI polish
- Requires explicit seams in runtime paths that were previously implicit
- Build/release configuration becomes more complex due to strip variants

### Neutral

- Existing `Runtime<TProgram, TModel, TArgument>` flow remains the execution backbone; debugger integration composes at known seams instead of replacing the runtime model.

## Alternatives Considered

### Alternative 1: JavaScript-First Debugger UI

Rejected because it breaks MVU consistency and moves domain decisions outside `Picea.Abies`.

### Alternative 2: Runtime Hooks Without Replay Gating

Rejected because replay would risk executing navigation/interpreter/subscription effects and violate deterministic debugging expectations.

### Alternative 3: Debugger Always Included in Release

Rejected because production artifacts need a strip path for size, surface area, and operational risk control.

## Related Decisions

- [ADR-001: MVU Architecture](./ADR-001-mvu-architecture.md)
- [ADR-011: JavaScript Interop](./ADR-011-javascript-interop.md)
- [ADR-024: Four Render Modes](./ADR-024-four-render-modes.md)

## References

- Issue 160: Debugger architecture boundary contract (team issue)
- Runtime seams: `Picea.Abies/Runtime.cs`
