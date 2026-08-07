# ADR-028: Splitting the Program Contract into Core and View

**Status:** Accepted
**Date:** 2026-08-07
**Decision Makers:** Maurice Peters

## Context

[ADR-027](./ADR-027-native-winui-renderer.md) established native WinUI as a third renderer and claimed
its headline benefit as "one pure program, three renderers". The native Counter sample demonstrated it —
but only by paying for it:

```csharp
public sealed class NativeCounterProgram : Program<CounterModel, Unit>
{
    public static (CounterModel, Command) Initialize(Unit a) => CounterProgram.Initialize(a);
    public static (CounterModel, Command) Transition(CounterModel m, Message msg) => CounterProgram.Transition(m, msg);
    public static Result<Message[], Message> Decide(CounterModel m, Message c) => CounterProgram.Decide(m, c);
    public static bool IsTerminal(CounterModel m) => CounterProgram.IsTerminal(m);
    public static Subscription Subscriptions(CounterModel m) => CounterProgram.Subscriptions(m);

    public static Document View(CounterModel model) => /* the only new code */;
}
```

Five members hand-forwarded to say "everything except the view is the same". This is not a style
preference: `Program<TModel, TArgument>` declares its members as `static abstract`, and **C# cannot
inherit static abstract implementations**. A type implementing the interface must supply every member
itself, so sharing a core across platforms has no language-level mechanism.

ADR-027 recorded the cost as a negative consequence: *"`View` is per-platform: a native program must
delegate to a shared program class (boilerplate) until the `Program` contract is split into Core + View
interfaces."* This ADR does that split.

## Decision

Split the contract along the seam that already exists conceptually, and add a framework-side adapter that
performs the forwarding once:

1. **`ProgramCore<TModel, TArgument>`** — initialization, transitions, decisions, termination,
   subscriptions. Mentions no DOM and no controls: the platform-free half.
2. **`ProgramView<TModel>`** — `View` alone.
3. **`Program<TModel, TArgument> : ProgramCore<TModel, TArgument>, ProgramView<TModel>`** — a complete
   program, structurally identical to what it was before.
4. **`WithView<TCore, TView, TModel, TArgument>`** — a sealed class implementing `Program` by forwarding
   core members to `TCore` and `View` to `TView`. The delegation still exists, but it is written once in
   the framework instead of once per app.

The sample becomes a view and nothing else:

```csharp
public sealed class NativeCounterView : ProgramView<CounterModel>
{
    public static Document View(CounterModel model) => /* ... */;
}

// Bootstrap pairs the shared core with the native view:
await Runtime.RunWithView<CounterProgram, NativeCounterView, CounterModel, Unit>(rootHost, window, Unit.Value);
```

`CounterProgram` is now used *directly* by the native host — the same class the WASM and server hosts
use, not a copy or a wrapper.

## Consequences

### Positive

- The central claim of ADR-027 is now structurally true rather than true-with-boilerplate. Sharing a
  program across renderers costs one type argument.
- `ProgramCore` is a useful concept in its own right: it names the platform-free half of a program, which
  is exactly what is testable with `Picea.Abies.Testing` and replayable in the time-travel debugger.
- Multiple views over one core are expressible and tested, which is the general case the native renderer
  is one instance of.

### Neutral

- **Fully backward compatible.** `Program` requires the same members as before, so every existing program
  — browser, server, WASM, Conduit, Counter — compiles untouched. The runtime's constraint is still
  `where TProgram : Program<TModel, TArgument>`; `WithView<...>` simply satisfies it. Verified by a full
  solution build and 496 tests with no source changes outside the native sample.
- `WithView` is a type-level composition, so the pairing appears at the bootstrap call site rather than in
  a declaration. `Runtime.RunWithView` exists to keep that call readable.

### Negative

- The generic parameter list on the composed type is long
  (`WithView<CounterProgram, NativeCounterView, CounterModel, Unit>`). `RunWithView` hides it at the one
  place it normally appears, but it surfaces in type errors.
- Two ways to express a program now exist — implement `Program` directly, or compose with `WithView`.
  Single-platform apps should keep implementing `Program`; `WithView` is for cores shared across
  renderers.

## Alternatives Considered

### Alternative 1: Leave the delegation to app authors

Rejected. Five forwarding members per platform per program is the kind of cost that quietly discourages
the thing the framework is advertising. It also invites drift: a forwarded member can be edited to differ
from the shared core, silently forking behaviour that looks shared.

### Alternative 2: Make `View` a runtime parameter rather than a type member

Pass `Func<TModel, Document>` to `Runtime.Start` and drop `View` from the contract. Rejected: it moves a
compile-time guarantee to a runtime one, and the delegate closes over whatever it likes, weakening the
purity the architecture depends on. It would also be a breaking change for every existing program.

### Alternative 3: Instance-based interfaces instead of static abstract

The inheritance problem is a consequence of `static abstract`. Rejected as far out of scope: the static
contract is what keeps programs allocation-free and is load-bearing across the whole framework and the
Picea kernel.

## Related Decisions

- [ADR-001: MVU Architecture](./ADR-001-mvu-architecture.md)
- [ADR-024: Four Render Modes](./ADR-024-four-render-modes.md)
- [ADR-027: Native WinUI Renderer](./ADR-027-native-winui-renderer.md)
