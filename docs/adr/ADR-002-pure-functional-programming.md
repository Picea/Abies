# ADR-002: Pure Functional Programming Style

**Status:** Accepted  
**Date:** 2024-01-15  
**Decision Makers:** Abies Core Team  
**Supersedes:** None  
**Superseded by:** None

## Context

Abies is written in C#, a multi-paradigm language that supports both object-oriented and functional programming styles. We needed to decide which paradigm would best serve the framework's goals:

1. Predictable application behavior
2. Testability without complex mocking infrastructure
3. Safe concurrent operations in the WebAssembly runtime
4. Clear separation between pure computation and side effects
5. Compatibility with modern C# features (records, pattern matching)

The choice affects how users write their applications and how the framework itself is structured.

## Decision

We adopt a **pure functional programming style** as the primary paradigm for Abies applications and the framework core.

Key principles:

1. **Immutability by default**: Use `record` and `record struct` types instead of mutable classes
2. **Pure functions**: Core logic (Update, View) must not have side effects
3. **Explicit data flow**: Pass data through function parameters, not shared mutable state
4. **No inheritance hierarchies**: Prefer composition and sum types over OOP inheritance
5. **Avoid "I" prefix**: Do not use the `IService` naming convention for interfaces

Guidelines from `csharp.instructions.md`:

```csharp
// DO: Use records for state
public record Model(int Count, bool IsLoading);

// DO: Use pure functions
public static (Model, Command) Update(Message msg, Model model) =>
    msg switch
    {
        Increment => (model with { Count = model.Count + 1 }, Commands.None),
        _ => (model, Commands.None)
    };

// DON'T: Use mutable state or side effects in Update
public static Model Update(Message msg, Model model)
{
    _counter++;  // Side effect - forbidden
    return model;
}
```

**Exception**: Performance-critical hot paths may use imperative techniques for optimization, but must be thoroughly documented.

## Consequences

### Positive

- **Testability**: Pure functions are trivial to test—pass in data, assert on output
- **Predictability**: No hidden state mutations or side effects in core logic
- **Thread safety**: Immutable data structures are inherently thread-safe
- **Debuggability**: Functions with explicit inputs/outputs are easier to trace
- **Refactoring safety**: Pure functions can be safely extracted, inlined, or reordered
- **Leverages modern C#**: Records, pattern matching, switch expressions all shine in FP style

### Negative

- **Learning curve**: Developers from OOP backgrounds may find the style unfamiliar
- **Verbosity**: Immutable updates (`with`) can be verbose for nested structures
- **Performance overhead**: Immutable updates allocate new objects; optimization may be needed in hot paths
- **Limited OOP ecosystem**: Some .NET libraries expect mutable objects or service patterns

### Neutral

- C# is not a pure FP language; some compromises are necessary
- Developers need to understand the exception for performance-critical code

## Alternatives Considered

### Alternative 1: Traditional OOP with Mutable Services

Standard .NET approach with dependency injection, mutable state, and service classes:

- Familiar to most .NET developers
- Rich ecosystem of OOP patterns and libraries
- Harder to test without mocking frameworks
- State mutations can be hard to track

Rejected because it conflicts with MVU's requirements for predictable state.

### Alternative 2: Hybrid Approach (Mix FP and OOP Freely)

Allow developers to choose per-module:

- Maximum flexibility
- Inconsistent codebases
- Confusion about which style to use when
- Integration points between styles are error-prone

Rejected because consistency is more valuable than flexibility here.

### Alternative 3: Full F# Implementation

F# provides better native FP support with discriminated unions, immutability by default, etc.:

- Better language-level FP support
- Smaller developer community than C#
- Requires learning a new language
- Tooling differences (IDE support)

Rejected to maximize reach to the broader .NET community.

## Related Decisions

- [ADR-001: Model-View-Update Architecture](./ADR-001-mvu-architecture.md)
- [ADR-008: Immutable State Management](./ADR-008-immutable-state.md)
- [ADR-009: Sum Types for State Representation](./ADR-009-sum-types.md)
- [ADR-010: Result/Option Types for Error Handling](./ADR-010-result-option-types.md)

## References

- [Functional Programming in C#](https://www.manning.com/books/functional-programming-in-c-sharp)
- [Domain Modeling Made Functional (Scott Wlaschin)](https://pragprog.com/titles/swdddf/domain-modeling-made-functional/)
- [C# Records Documentation](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-9#record-types)
- [Pattern Matching in C#](https://docs.microsoft.com/en-us/dotnet/csharp/pattern-matching)
