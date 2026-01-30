# ADR-009: Sum Types for State Representation

**Status:** Accepted  
**Date:** 2024-01-15  
**Decision Makers:** Abies Core Team  
**Supersedes:** None  
**Superseded by:** None

## Context

Applications often have states that are mutually exclusive:

- A page is Home OR Article OR NotFound
- A request is Loading OR Success OR Error
- A user is Anonymous OR Authenticated

Traditional OOP approaches use:

- Nullable fields (checking `if (data != null)`)
- Boolean flags (`isLoading`, `hasError`)
- Inheritance hierarchies

These approaches have problems:

- Invalid states can be represented (e.g., both `isLoading` and `hasError` true)
- Pattern matching is incomplete or requires default cases
- Compiler doesn't help catch missing cases

## Decision

We use **sum types** (discriminated unions) to model mutually exclusive states, making illegal states unrepresentable at compile time.

Since C# doesn't have native discriminated unions, we emulate them using abstract records with sealed derived types:

```csharp
// Page is ONE of these, never multiple
public interface Page
{
    public sealed record Home(Home.Model Model) : Page;
    public sealed record Article(Article.Model Model) : Page;
    public sealed record NotFound : Page;
    public sealed record Login(Login.Model Model) : Page;
}

// Route is ONE of these
public abstract record Route
{
    public sealed record Home : Route;
    public sealed record Profile(UserName UserName) : Route;
    public sealed record Article(Slug Slug) : Route;
    public sealed record NotFound : Route;
}

// Message is ONE of these
public interface Message : Abies.Message
{
    public sealed record Increment : Message;
    public sealed record Decrement : Message;
    public sealed record Reset(int Value) : Message;
}
```

Pattern matching for exhaustive handling:

```csharp
public static Document View(Model model) => model.Page switch
{
    Page.Home home => ViewHome(home.Model),
    Page.Article article => ViewArticle(article.Model),
    Page.NotFound => ViewNotFound(),
    Page.Login login => ViewLogin(login.Model),
    // Compiler warns if a case is missing
};
```

Guidelines from `ddd.instructions.md`:

- Use `abstract record` base with `sealed record` cases
- Cases must be closed (no random inheritance allowed)
- Use exhaustive `switch` expressions
- Add `_ => throw new UnreachableException()` as safety net

## Consequences

### Positive

- **Illegal states unrepresentable**: Can't have a page that's both Home and Article
- **Exhaustive matching**: Compiler warns about unhandled cases
- **Self-documenting**: Type signature shows all possible states
- **Refactoring safety**: Adding a new case causes compile errors at all switch sites
- **No null checks**: State is always one of the defined cases
- **Pattern matching friendly**: Works naturally with C# switch expressions

### Negative

- **Verbosity**: Requires defining multiple record types
- **No native support**: C# doesn't have true DUs; must use workarounds
- **Inheritance restriction**: Must seal all derived types manually
- **IDE support**: Some IDEs handle abstract record hierarchies awkwardly

### Neutral

- F# has native discriminated unions; C# may add them in future versions
- Can use interfaces instead of abstract records (slightly different semantics)
- The pattern is well-established in the functional C# community

## Alternatives Considered

### Alternative 1: Nullable Fields with Boolean Flags

```csharp
public record LoadState(
    bool IsLoading,
    Data? Data,
    string? Error
);
```

- Familiar to most developers
- Invalid states possible (IsLoading true with Data not null)
- Checking requires null checks
- No compiler help for completeness

Rejected because it allows invalid states.

### Alternative 2: State Enum with Data Bag

```csharp
public enum PageState { Home, Article, NotFound }
public record Model(PageState State, object? Data);
```

- Simple initial implementation
- Type safety lost (casting required)
- No compile-time exhaustiveness
- Easy to mismatch state and data

Rejected because it loses type safety.

### Alternative 3: OneOf Library

Use a library like OneOf for discriminated unions:

```csharp
public OneOf<Home, Article, NotFound> Page { get; }
```

- True DU semantics
- External dependency
- Limited cases (up to ~32)
- Different syntax from records

Considered acceptable for specific cases but not as primary pattern.

### Alternative 4: Wait for C# Native DUs

C# language team is considering discriminated unions:

- Best ergonomics when available
- Unknown timeline
- Not available today

Can migrate when available; current approach is forward-compatible.

## Related Decisions

- [ADR-002: Pure Functional Programming Style](./ADR-002-pure-functional-programming.md)
- [ADR-008: Immutable State Management](./ADR-008-immutable-state.md)
- [ADR-010: Result/Option Types for Error Handling](./ADR-010-result-option-types.md)

## References

- [Discriminated Unions in C#](https://blog.ploeh.dk/2022/07/25/an-encapsulated-c-sharp-of-discriminated-union-example/)
- [Making Illegal States Unrepresentable](https://fsharpforfunandprofit.com/posts/designing-with-types-making-illegal-states-unrepresentable/)
- [Domain Modeling Made Functional](https://pragprog.com/titles/swdddf/domain-modeling-made-functional/)
- [OneOf Library](https://github.com/mcintyre321/OneOf)
