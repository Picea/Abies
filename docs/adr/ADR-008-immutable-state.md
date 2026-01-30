# ADR-008: Immutable State Management

**Status:** Accepted  
**Date:** 2024-01-15  
**Decision Makers:** Abies Core Team  
**Supersedes:** None  
**Superseded by:** None

## Context

MVU requires state transitions to be explicit and trackable. The question of how to represent and update state is fundamental:

1. **Mutable state**: Objects are modified in place
2. **Immutable state**: New objects are created for each state change

The choice affects:

- Predictability of state changes
- Ability to compare states (equality, change detection)
- Memory and performance characteristics
- Developer experience with updates

## Decision

We mandate **immutable state** using C# records with `with` expressions for all model and domain types.

Key principles:

1. All model types are `record` or `record struct`
2. State updates create new instances: `model with { Property = newValue }`
3. Collections use immutable types or are replaced wholesale
4. Identity is determined by value equality, not reference

Example model definition:

```csharp
// Model is immutable record
public record Model(
    Page Page,
    Route CurrentRoute,
    User? CurrentUser = null
);

// Nested types are also records
public record User(
    UserName Username,
    Email Email,
    Token Token,
    string Image,
    string Bio
);

// Value types wrap primitives
public readonly record struct UserName(string Value);
public readonly record struct Email(string Value);
```

Update pattern:

```csharp
public static (Model, Command) Update(Message msg, Model model) => msg switch
{
    // Creates new Model with updated Page
    PageLoaded loaded => (model with { Page = loaded.Page }, Commands.None),
    
    // Nested update creates new Model with new nested record
    UserUpdated updated => (
        model with { CurrentUser = model.CurrentUser with { Bio = updated.Bio } },
        Commands.None
    ),
    
    _ => (model, Commands.None)
};
```

## Consequences

### Positive

- **Predictable state**: Each state is a discrete snapshot; no hidden mutations
- **Easy comparison**: Record equality works out of the box
- **Safe sharing**: Immutable data can be freely shared without defensive copying
- **Time-travel debugging**: State history is naturally available
- **Thread safety**: Immutable data is inherently thread-safe
- **Undo/redo**: Trivial to implement by storing state snapshots

### Negative

- **Memory allocation**: Each update allocates new objects
- **Nested updates verbose**: Deep nesting requires chained `with` expressions
- **Learning curve**: Developers from mutable backgrounds may find it unfamiliar
- **Collection updates**: Updating a list element requires replacing the whole list

### Neutral

- `record struct` can reduce allocations for small value types
- Large collections may need optimization (e.g., using array indices)

## Alternatives Considered

### Alternative 1: Mutable Classes with Change Notification

Use `INotifyPropertyChanged` or similar:

```csharp
public class Model : INotifyPropertyChanged
{
    private int _count;
    public int Count
    {
        get => _count;
        set { _count = value; OnPropertyChanged(); }
    }
}
```

- Familiar to MVVM developers
- Change tracking built-in
- State mutations are hidden
- Race conditions in concurrent code
- Harder to reason about state transitions

Rejected because it conflicts with pure FP and MVU principles.

### Alternative 2: Structural Sharing (Persistent Data Structures)

Use libraries like LanguageExt with persistent collections:

- Efficient updates for large collections
- More memory efficient for partial updates
- Additional dependency
- Learning curve for persistent data structures
- Overkill for most UI state

Not rejected but considered an optimization to add if needed.

### Alternative 3: Copy-on-Write with Manual Cloning

Implement `Clone()` methods:

```csharp
public Model Clone() => new Model(this.Page.Clone(), ...);
```

- Explicit control
- Tedious boilerplate
- Easy to forget deep cloning
- Error-prone

Rejected because C# records provide this automatically.

## Related Decisions

- [ADR-001: Model-View-Update Architecture](./ADR-001-mvu-architecture.md)
- [ADR-002: Pure Functional Programming Style](./ADR-002-pure-functional-programming.md)
- [ADR-009: Sum Types for State Representation](./ADR-009-sum-types.md)

## References

- [C# Records](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-9#record-types)
- [Immutability in C#](https://docs.microsoft.com/en-us/dotnet/csharp/write-safe-efficient-code)
- [LanguageExt Immutable Collections](https://github.com/louthy/language-ext)
- [The Value of Values (Rich Hickey)](https://www.infoq.com/presentations/Value-Values/)
