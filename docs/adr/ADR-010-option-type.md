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

The `Option<T>` type is a `readonly struct` with a boolean discriminator, defined in the Picea kernel (not in Abies itself):

```csharp
public readonly struct Option<T>
{
    private readonly T _value;
    private readonly bool _hasValue;

    public Option(T value)
    {
        _value = value;
        _hasValue = true;
    }

    public static Option<T> Some(T value) => new(value);
    public static Option<T> None => default;

    public bool IsSome => _hasValue;
    public bool IsNone => !_hasValue;
}
```

Usage patterns:

```csharp
// Expressing optional values
public record Model(
    Option<User> CurrentUser,  // Explicit: user may not exist
    string Title               // Required: always present
);

// Creating options
Option<User> loggedIn = Option<User>.Some(user);
Option<User> anonymous = Option<User>.None;

// Pattern matching
string greeting = currentUser switch
{
    { IsSome: true } some => $"Hello, {some.Value.Name}",
    _ => "Hello, Guest"
};
```

Guidelines from `ddd.instructions.md`:

- Use `Option<T>` for "might be missing" within domain/application
- Don't use null to represent "not found" in domain code
- At API boundaries, map Option to 404/empty response as appropriate

## Consequences

### Positive

- **Explicit optionality**: Type signature documents that value may be absent
- **No null checks**: Property-based or pattern matching handles both cases
- **Value type**: `Option<T>` is a `readonly struct`; no heap allocation for the wrapper itself
- **Composition**: Can chain operations on Options
- **Zero-allocation None**: `default` struct represents absence without allocation

### Negative

- **Verbosity**: More characters than `?` syntax
- **Learning curve**: Unfamiliar to developers used to nullable references
- **Interop friction**: Must convert at boundaries with null-using APIs
- **Boxing risk**: Using `Option<T>` in generic contexts may cause boxing if not carefully handled

### Neutral

- Nullable reference types are still used at API boundaries and interop
- Option doesn't replace `Nullable<T>` for value types in all cases
- Can be extended with map/bind/match extension methods
- Defined in the Picea kernel, shared across all Picea framework libraries

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

## Changelog

- **2026-03 (v2 migration)**: Updated to reflect current implementation after Picea migration.
  - Changed Option from `interface Option<T>` with `Some<T>`/`None<T>` record implementations → `readonly struct Option<T>` with `bool _hasValue` discriminator
  - Updated code examples to show struct-based API (`Option<T>.Some()`, `Option<T>.None`, `.IsSome`/`.IsNone`)
  - Clarified that `Option<T>` lives in the Picea kernel, not in Abies
  - Removed reference to `Abies/Option.cs` (file no longer exists in Abies)
  - Updated consequences to reflect value-type semantics (zero-allocation None, boxing risk)
