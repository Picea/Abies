# ADR-001: Model-View-Update (MVU) Architecture

**Status:** Accepted  
**Date:** 2024-01-15  
**Decision Makers:** Abies Core Team  
**Supersedes:** None  
**Superseded by:** None

## Context

When designing Abies as a WebAssembly-based web application framework for .NET, we needed to choose an architectural pattern that would:

1. Be well-suited for reactive user interfaces
2. Enable predictable state management
3. Make application logic testable without complex mocking
4. Align with functional programming principles
5. Be familiar to developers who may have used Elm, F#, or similar functional languages
6. Work well with the constraints of WebAssembly (single-threaded, event-driven)

Traditional patterns like MVC, MVVM, and MVP were considered but found to have limitations around state management predictability and testability of UI logic.

## Decision

We adopt the **Model-View-Update (MVU)** architecture, also known as "The Elm Architecture" (TEA), as the fundamental pattern for Abies applications.

The MVU pattern consists of three core components:

1. **Model** - An immutable record representing the entire application state
2. **View** - A pure function `Model → Document` that produces a virtual DOM tree
3. **Update** - A pure function `(Message, Model) → (Model, Command)` that handles state transitions

Messages flow unidirectionally:
```
User Action → Message → Update → New Model → View → Virtual DOM → Real DOM
                           ↑
                     Commands/Effects
```

The pattern is implemented through the `Program<TModel, TArgument>` interface:

```csharp
public interface Program<TModel, in TArgument> 
{
    public static abstract (TModel, Command) Initialize(Url url, TArgument argument);
    public static abstract (TModel model, Command command) Update(Message message, TModel model);
    public static abstract Document View(TModel model);
    public static abstract Message OnUrlChanged(Url url);
    public static abstract Message OnLinkClicked(UrlRequest urlRequest);
    public static abstract Subscription Subscriptions(TModel model);
    public static abstract Task HandleCommand(Command command, Func<Message, ValueTuple> dispatch);
}
```

## Consequences

### Positive

- **Predictable state management**: The entire application state lives in a single immutable model, making it trivial to understand what state the application is in at any point in time
- **Testable update logic**: Since `Update` is a pure function, unit tests can verify state transitions without mocking or side effects
- **Time-travel debugging**: State history can be recorded and replayed since each state is an immutable snapshot
- **Clear separation of concerns**: Side effects are isolated in `HandleCommand`, keeping core logic pure
- **Simple mental model**: Developers can reason about the application as `(state, event) → newState`
- **Natural fit for .NET records**: C# records with `with` expressions work well for immutable state updates

### Negative

- **Learning curve**: Developers accustomed to imperative frameworks may need time to adapt to the unidirectional flow
- **Boilerplate for simple changes**: Each state change requires defining a message type and handling it in `Update`
- **Large model considerations**: Very large applications may need to think carefully about model structure to avoid performance issues
- **Callback indirection**: Side effects must be expressed as commands and handled asynchronously, adding indirection

### Neutral

- The pattern naturally leads to a message-heavy architecture with many small message types
- Component composition requires explicit message forwarding between parent and child

## Alternatives Considered

### Alternative 1: MVVM (Model-View-ViewModel)

MVVM is widely used in .NET UI development (WPF, MAUI). However:
- Two-way binding can lead to unpredictable state updates
- Testing ViewModels often requires mocking services
- Mutation-based updates are harder to track

Rejected because it conflicts with our pure functional programming goals.

### Alternative 2: Redux/Flux Pattern

Redux shares many characteristics with MVU but:
- Typically uses middleware for side effects (complexity)
- Action creators add indirection
- Less native to functional programming languages

MVU is essentially a purer, simpler version of the same ideas.

### Alternative 3: Component-Based with Local State (React-style)

This would allow components to manage their own state:
- More familiar to React developers
- Can lead to distributed state that's hard to track
- Testing requires rendering components

Rejected because centralized state provides better predictability.

## Related Decisions

- [ADR-002: Pure Functional Programming Style](./ADR-002-pure-functional-programming.md)
- [ADR-003: Virtual DOM Implementation](./ADR-003-virtual-dom.md)
- [ADR-006: Command Pattern for Side Effects](./ADR-006-command-pattern.md)
- [ADR-008: Immutable State Management](./ADR-008-immutable-state.md)

## References

- [The Elm Architecture](https://guide.elm-lang.org/architecture/)
- [Elmish (F#)](https://elmish.github.io/elmish/)
- [Bolero (Blazor + Elmish)](https://fsbolero.io/)
- [Model-View-Update Pattern](https://elmprogramming.com/model-view-update-part-1.html)
