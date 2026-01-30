# ADR-007: Subscription Model for External Events

**Status:** Accepted  
**Date:** 2024-01-15  
**Decision Makers:** Abies Core Team  
**Supersedes:** None  
**Superseded by:** None

## Context

Beyond one-off side effects (Commands), applications need to handle continuous external event sources:

- Timers and intervals
- Browser events (resize, visibility change)
- Keyboard/mouse events at the document level
- WebSocket connections
- Animation frames

These differ from commands because:

1. They're ongoing, not one-shot
2. Their lifecycle depends on application state
3. They need to be started and stopped as state changes

## Decision

We implement a **Subscription model** inspired by Elm's subscriptions, where the `Subscriptions` function returns a declarative description of active subscriptions based on the current model.

The runtime manages subscription lifecycle automatically—starting new subscriptions, stopping removed ones, and keeping unchanged subscriptions running.

Core types:

```csharp
public abstract record Subscription
{
    public sealed record None : Subscription;
    public sealed record Batch(IReadOnlyList<Subscription> Subscriptions) : Subscription;
    public sealed record Source(SubscriptionKey Key, StartSubscription Start) : Subscription;
}

public readonly record struct SubscriptionKey(string Value);
public delegate Task StartSubscription(Dispatch dispatch, CancellationToken cancellationToken);
```

Program interface:

```csharp
public interface Program<TModel, TArgument>
{
    public static abstract Subscription Subscriptions(TModel model);
    // ... other members
}
```

Example usage:

```csharp
public static Subscription Subscriptions(Model model) => model.GameState switch
{
    GameState.Playing => SubscriptionModule.Batch([
        SubscriptionModule.Every(TimeSpan.FromSeconds(1), _ => new Tick()),
        SubscriptionModule.OnKeyDown(key => new KeyPressed(key))
    ]),
    _ => SubscriptionModule.None
};
```

Built-in subscriptions:

| Subscription | Description |
|-------------|-------------|
| `Every(interval, toMessage)` | Periodic timer events |
| `OnAnimationFrame(toMessage)` | Browser animation frames |
| `OnResize(toMessage)` | Viewport size changes |
| `OnVisibilityChange(toMessage)` | Document visibility |
| `OnKeyDown(toMessage)` | Keyboard key down |
| `OnKeyUp(toMessage)` | Keyboard key up |
| `OnMouseDown/Up/Move(toMessage)` | Mouse events |
| `OnClick(toMessage)` | Click events |
| `WebSocket(options, toMessage)` | WebSocket connection |

## Consequences

### Positive

- **Declarative**: Subscriptions are described, not imperatively managed
- **State-driven**: Active subscriptions change automatically with state
- **Leak-free**: Runtime handles cleanup when subscriptions are removed
- **Testable**: Subscription functions are pure; return value can be inspected
- **Composable**: `Batch` combines multiple subscriptions
- **Keyed**: Each subscription has a stable key for identity tracking

### Negative

- **Indirection**: Subscription setup is separate from where events are handled
- **Key management**: Developers must ensure stable, unique keys
- **Browser coupling**: Built-in subscriptions depend on JavaScript interop
- **State reconstruction**: If model changes frequently, subscription comparison runs often

### Neutral

- Subscriptions use `CancellationToken` for cleanup coordination
- Custom subscriptions can be created with `SubscriptionModule.Create`
- Subscription data is deserialized from JSON for type safety

## Alternatives Considered

### Alternative 1: Imperative Subscription Management

Manage subscriptions manually in `Update`:

```csharp
case StartTimer => 
    _timerId = SetInterval(...);  // Imperative side effect
    return model;
```

- Direct control
- Easy to forget cleanup
- Impure Update function
- Manual lifecycle management

Rejected because it breaks purity and is error-prone.

### Alternative 2: Observable/Reactive Streams

Use Rx.NET or similar reactive library:

- Powerful composition operators
- Large dependency
- Different mental model from MVU
- Overkill for typical subscription needs

Rejected for simplicity; Rx can still be used in custom subscriptions.

### Alternative 3: Event Aggregator/Message Bus

Use a global event bus that modules subscribe to:

- Decoupled communication
- Hard to track subscription lifecycle
- Global mutable state
- Conflicts with pure FP style

Rejected because it introduces hidden state.

## Related Decisions

- [ADR-001: Model-View-Update Architecture](./ADR-001-mvu-architecture.md)
- [ADR-006: Command Pattern for Side Effects](./ADR-006-command-pattern.md)
- [ADR-011: JavaScript Interop Strategy](./ADR-011-javascript-interop.md)

## References

- [Elm Subscriptions](https://guide.elm-lang.org/effects/time.html)
- [Elmish Subscriptions](https://elmish.github.io/elmish/#subscriptions)
- [`Abies/Subscriptions/Subscription.cs`](../../Abies/Subscriptions/Subscription.cs) - Subscription types
- [`Abies/Subscriptions/SubscriptionManager.cs`](../../Abies/Subscriptions/SubscriptionManager.cs) - Lifecycle management
