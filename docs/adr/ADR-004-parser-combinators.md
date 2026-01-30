# ADR-004: Parser Combinators for Routing

**Status:** Accepted  
**Date:** 2024-01-15  
**Decision Makers:** Abies Core Team  
**Supersedes:** None  
**Superseded by:** None

## Context

Web applications need URL routing to map paths to application states. Routing libraries typically fall into several categories:

1. **Declarative configuration** (ASP.NET MVC attribute routing)
2. **Pattern matching strings** (Express.js style `/user/:id`)
3. **Parser combinators** (Elm URL.Parser, Scala Cats Parse)

Abies needed a routing solution that:

- Fits the functional programming style
- Is type-safe (captures route parameters with correct types)
- Is composable (routes can be combined)
- Is testable (pure functions)
- Supports both simple and complex routing scenarios

## Decision

We implement routing using **parser combinators** as the primary mechanism, with an optional template-based syntax for convenience.

The parser combinator approach in `Abies.Route.Parse`:

```csharp
public static Parser<Route> Match =>
    RouteParse.Path(
            RouteParse.Segment.Literal("profile"),
            RouteParse.Segment.Parameter("userName"))
        .Map(match => new Profile(match.GetRequired<string>("userName")))
    | RouteParse.Path(RouteParse.Segment.Literal("article"),
            RouteParse.Segment.Parameter<int>("id", RouteParse.Int))
        .Map(match => new Article(match.GetRequired<int>("id")))
    | RouteParse.Root.Map(_ => new Home());
```

The template-based alternative in `Abies.Route.Templates`:

```csharp
var router = RouteTemplates.Build<Route>(routes =>
{
    routes.Map("/profile/{userName}", m => new Profile(m.GetRequired<string>("userName")));
    routes.Map("/article/{id:int}", m => new Article(m.GetRequired<int>("id")));
    routes.Map("/", _ => new Home());
});
```

Key components:

- `Parser<T>` interface with `Parse(ReadOnlySpan<char> input)` method
- `ParseResult<T>` ref struct for allocation-free results
- Combinators: `Or` (`|`), `Slash` (`/`), `Map`, `Many`, `Many1`, `Optional`
- LINQ query syntax support (`from`, `select`, `where`)
- Segment types: `Literal`, `Parameter` (string, int, double, custom)

## Consequences

### Positive

- **Type safety**: Route parameters are strongly typed; invalid routes fail at parse time
- **Composability**: Parsers combine naturally using operators and LINQ
- **Testability**: Parsers are pure functions; no HTTP context needed
- **Performance**: Uses `ReadOnlySpan<char>` for zero-allocation parsing
- **Extensibility**: Custom parsers can be written for specialized needs
- **Dual API**: Both functional and template styles are supported

### Negative

- **Learning curve**: Parser combinators are unfamiliar to many developers
- **Verbose for simple cases**: Simple routes require more code than string patterns
- **Debugging complexity**: Composed parsers can be hard to debug when they fail
- **Order sensitivity**: Alternative (`|`) parsers try in order; most specific first

### Neutral

- Templates provide a simpler on-ramp for developers familiar with ASP.NET routing
- Performance characteristics favor hot paths due to span-based implementation

## Alternatives Considered

### Alternative 1: String Pattern Matching

Use regex or glob patterns like Express.js:

```csharp
router.Map("/profile/:userName", (userName) => new Profile(userName));
```

- Familiar to web developers
- Stringly typed (parameters are strings)
- Regex parsing can be slow
- Less composable

Rejected because type safety is a core goal.

### Alternative 2: Attribute-Based Routing

Use attributes like ASP.NET MVC:

```csharp
[Route("/profile/{userName}")]
public class ProfileRoute { }
```

- Familiar to .NET developers
- Reflection-based (slower, harder to test)
- OOP-centric (requires classes)
- Doesn't fit functional style

Rejected because it conflicts with pure FP approach.

### Alternative 3: Discriminated Union Pattern Matching Only

Match URLs with switch expressions directly:

```csharp
Route FromUrl(Url url) => url.Path.Value switch
{
    var p when p.StartsWith("/profile/") => new Profile(p[9..]),
    "/" => new Home(),
    _ => new NotFound()
};
```

- No learning curve
- Manual parsing is error-prone
- No type extraction help
- Hard to compose

Rejected because it doesn't scale to complex routes.

## Related Decisions

- [ADR-001: Model-View-Update Architecture](./ADR-001-mvu-architecture.md)
- [ADR-002: Pure Functional Programming Style](./ADR-002-pure-functional-programming.md)

## References

- [Elm URL.Parser](https://package.elm-lang.org/packages/elm/url/latest/Url-Parser)
- [Parser Combinators in Haskell](https://wiki.haskell.org/Parsing_a_simple_imperative_language)
- [Monadic Parser Combinators (Hutton & Meijer)](http://www.cs.nott.ac.uk/~pszgmh/monparsing.pdf)
- [`Abies/Parser.cs`](../../Abies/Parser.cs) - Core parser implementation
- [`Abies/Route.cs`](../../Abies/Route.cs) - Route-specific parsers
