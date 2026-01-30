# ADR-010: Option Type for Optional Values

**Status:** Accepted  
**Date:** 2024-01-15  
**Decision Makers:** Abies Core Team  
**Supersedes:** None  
**Superseded by:** None

## Context

Applications frequently deal with values that may or may not be present:

- A user who may not be logged in
- A search result that may not exist
- An optional configuration value

C# traditionally handles this with:

1. **Nullable reference types** (`string?`)
2. **Null checks** (`if (value != null)`)
3. **Null-coalescing operators** (`??`, `?.`)

While nullable reference types help, they have limitations:

- Null is still a runtime concept
- Easy to forget null checks
- Doesn't compose well in functional pipelines
- Can't express "no value" for value types without `Nullable<T>`

## Decision

We provide an **Option type** (`Option<T>`) as an explicit representation of optional values, inspired by functional languages.

Core types in `Abies/Option.cs`:

```csharp
public interface Option<T>;

public readonly record struct Some<T>(T Value) : Option<T>;
public readonly struct None<T> : Option<T>;
```

Usage patterns:

```csharp
// Expressing optional values
public record Model(
    Option<User> CurrentUser,  // Explicit: user may not exist
    string Title               // Required: always present
);

// Creating options
Option<User> loggedIn = new Some<User>(user);
Option<User> anonymous = new None<User>();

// Pattern matching
string greeting = currentUser switch
{
    Some<User> some => $"Hello, {some.Value.Name}",
    None<User> => "Hello, Guest"
};
```

Guidelines from `ddd.instructions.md`:

- Use `Option<T>` for "might be missing" within domain/application
- Don't use null to represent "not found" in domain code
- At API boundaries, map Option to 404/empty response as appropriate

## Consequences

### Positive

- **Explicit optionality**: Type signature documents that value may be absent
- **No null checks**: Pattern matching handles both cases
- **Value type None**: `None<T>` is a struct; no allocation for empty case
- **Composition**: Can chain operations on Options
- **Compiler enforcement**: Can't access `.Value` without matching

### Negative

- **Verbosity**: More characters than `?` syntax
- **Learning curve**: Unfamiliar to developers used to nullable references
- **Interop friction**: Must convert at boundaries with null-using APIs
- **Generic noise**: `Some<T>`, `None<T>` require type parameters

### Neutral

- Nullable reference types are still used at API boundaries and interop
- Option doesn't replace `Nullable<T>` for value types in all cases
- Can be extended with map/bind/match extension methods

## Alternatives Considered

### Alternative 1: Nullable Reference Types Only

Use C# 8+ nullable annotations exclusively:

```csharp
public record Model(User? CurrentUser);
```

- Built into language
- Familiar syntax
- Still allows null at runtime
- Less composable
- Can't distinguish "not provided" from "explicitly null"

Not rejected entirely—used at API boundaries—but Option preferred in domain.

### Alternative 2: LanguageExt Option

Use the Option type from LanguageExt library:

- Full functional toolkit (Map, Bind, Match)
- Large library dependency
- Many additional concepts (Either, Try, etc.)
- May be overkill for core framework

Considered for advanced scenarios; custom Option preferred for simplicity.

### Alternative 3: Maybe Monad with Methods

Implement a Maybe type with fluent methods:

```csharp
public readonly struct Maybe<T>
{
    public Maybe<TResult> Map<TResult>(Func<T, TResult> f) => ...;
    public Maybe<TResult> Bind<TResult>(Func<T, Maybe<TResult>> f) => ...;
}
```

- More functional API
- More complex implementation
- Requires understanding monads

Can be added as extension methods to current Option if needed.

## Related Decisions

- [ADR-002: Pure Functional Programming Style](./ADR-002-pure-functional-programming.md)
- [ADR-008: Immutable State Management](./ADR-008-immutable-state.md)
- [ADR-009: Sum Types for State Representation](./ADR-009-sum-types.md)

## References

- [Option Type (Wikipedia)](https://en.wikipedia.org/wiki/Option_type)
- [Null References: The Billion Dollar Mistake](https://www.infoq.com/presentations/Null-References-The-Billion-Dollar-Mistake-Tony-Hoare/)
- [F# Options](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/options)
- [LanguageExt](https://github.com/louthy/language-ext)
- [`Abies/Option.cs`](../../Abies/Option.cs) - Implementation
