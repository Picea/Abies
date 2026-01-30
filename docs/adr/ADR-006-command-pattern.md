# ADR-006: Command Pattern for Side Effects

**Status:** Accepted  
**Date:** 2024-01-15  
**Decision Makers:** Abies Core Team  
**Supersedes:** None  
**Superseded by:** None

## Context

In the MVU architecture, the `Update` function must be pure—given the same message and model, it must always return the same result. However, real applications need side effects:

- HTTP requests to APIs
- Local storage access
- Navigation
- Logging/telemetry
- Timer setup

We needed a pattern to:

1. Keep `Update` pure and testable
2. Express intent for side effects declaratively
3. Execute effects outside the update loop
4. Dispatch result messages back into the loop

## Decision

We adopt the **Command pattern** where `Update` returns both a new model and a `Command` value describing side effects to perform.

The runtime executes commands asynchronously via `HandleCommand`, which dispatches result messages back into the update loop.

Core types:

```csharp
public interface Command
{
    public record struct None : Command;
    public record struct Batch(IEnumerable<Command> Commands) : Command;
}

public static class Commands
{
    public static Command.None None = new();
    public static Command.Batch Batch(IEnumerable<Command> commands) => new(commands);
}
```

Update signature:

```csharp
public static (Model model, Command command) Update(Message message, Model model)
```

Example command flow:

```csharp
// 1. User action triggers message
public record FetchArticles : Message;

// 2. Update returns command describing intent
public static (Model, Command) Update(Message msg, Model model) => msg switch
{
    FetchArticles => (model with { IsLoading = true }, new LoadArticlesCommand()),
    ArticlesLoaded loaded => (model with { Articles = loaded.Articles, IsLoading = false }, Commands.None),
    _ => (model, Commands.None)
};

// 3. Command type expresses side effect
public sealed record LoadArticlesCommand(int Limit = 10) : Command;

// 4. HandleCommand executes effect and dispatches result
public static async Task HandleCommand(Command cmd, Func<Message, ValueTuple> dispatch)
{
    switch (cmd)
    {
        case LoadArticlesCommand load:
            var articles = await ArticleService.GetArticlesAsync(load.Limit);
            dispatch(new ArticlesLoaded(articles));
            break;
    }
}
```

Built-in navigation commands:

```csharp
public static class Navigation
{
    public interface Command : Abies.Command
    {
        public record struct PushState(Url Url) : Command;
        public record struct ReplaceState(Url Url) : Command;
        public record struct Load(Url Url) : Command;
        public record struct Back(int times) : Command;
        public record struct Forward(int times) : Command;
    }
}
```

## Consequences

### Positive

- **Pure updates**: `Update` remains a pure function; all effects are external
- **Testable**: Commands can be asserted without executing effects
- **Declarative intent**: Commands describe *what* to do, not *how*
- **Composable**: `Command.Batch` combines multiple effects
- **Async support**: Effects naturally handle async operations
- **Centralized effect handling**: All side effects go through one handler

### Negative

- **Indirection**: Effect logic is separate from update logic
- **Boilerplate**: Each effect requires a command type and handler case
- **Testing effects**: Effect execution still needs integration tests
- **Error handling**: Must dispatch error messages for failed effects

### Neutral

- Navigation commands are handled specially by the runtime
- Commands are not serializable by default (could be added for logging/replay)

## Alternatives Considered

### Alternative 1: Perform Effects Directly in Update

Allow `Update` to call async methods directly:

```csharp
public static async Task<Model> Update(Message msg, Model model)
{
    var data = await Http.GetAsync(...);  // Side effect in Update
    return model with { Data = data };
}
```

- Simpler mental model initially
- Update becomes impure and untestable
- Hard to reason about concurrent updates
- Breaks MVU guarantees

Rejected because purity is fundamental to MVU.

### Alternative 2: Effect Middleware (Redux-style)

Use middleware functions that intercept commands:

- More flexible composition
- More complex to understand
- Order-dependent behavior
- Overkill for most cases

Rejected for simplicity; can be reconsidered if needed.

### Alternative 3: Capability-Based Effects

Pass effect capabilities as function parameters:

```csharp
public static (Model, Command) Update(
    Message msg, Model model,
    Func<string, Task<Data>> fetchData)
```

- Explicit dependencies
- Increases Update signature complexity
- Harder to extend
- Still requires async handling

Rejected because Command pattern is simpler for the common case.

## Related Decisions

- [ADR-001: Model-View-Update Architecture](./ADR-001-mvu-architecture.md)
- [ADR-002: Pure Functional Programming Style](./ADR-002-pure-functional-programming.md)
- [ADR-007: Subscription Model for External Events](./ADR-007-subscriptions.md)

## References

- [Elm Commands and Subscriptions](https://guide.elm-lang.org/effects/)
- [The Elm Architecture Effects](https://guide.elm-lang.org/architecture/effects/)
- [`Abies/Types.cs`](../../Abies/Types.cs) - Command interface
- [`Abies/Navigation.cs`](../../Abies/Navigation.cs) - Navigation commands
- [`Abies.Conduit/Commands.cs`](../../Abies.Conduit/Commands.cs) - Example commands
