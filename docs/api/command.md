# Command API

Commands represent side effects in the Abies MVU architecture. They are returned from `Initialize` and `Transition` as pure data — the runtime's interpreter executes them outside the pure transition function.

## Command Interface

```csharp
public interface Command
{
    sealed record None : Command;
    sealed record Batch(IReadOnlyList<Command> Commands) : Command;
}
```

The `Command` interface is a marker type with two built-in variants:

| Variant | Description |
|---------|-------------|
| `Command.None` | No side effect. The identity element of the command monoid. |
| `Command.Batch` | Multiple commands to execute. The binary operation of the command monoid. |

### Monoid Structure

Commands form a **monoid** — an algebraic structure with an identity element and an associative binary operation:

- **Identity:** `Command.None` — produces no effect.
- **Binary operation:** `Command.Batch` — combines multiple commands.

This means commands compose naturally: you can always combine any number of commands into one, and `None` acts as a neutral element that doesn't change the result.

## Commands Factory

The `Commands` static class provides ergonomic factory methods:

```csharp
public static class Commands
{
    public static readonly Command None;
    public static Command Batch(params Command[] commands);
    public static Command Batch(IEnumerable<Command> commands);
}
```

### Commands.None

```csharp
public static readonly Command None
```

A singleton instance of `Command.None`. Use this when a transition produces no side effects:

```csharp
public static (Model, Command) Transition(Model model, Message message) =>
    message switch
    {
        Increment => (model with { Count = model.Count + 1 }, Commands.None),
        _ => (model, Commands.None)
    };
```

### Commands.Batch

```csharp
public static Command Batch(params Command[] commands)
public static Command Batch(IEnumerable<Command> commands)
```

Combines multiple commands into a single command. Includes smart collapsing:

| Input | Output |
|-------|--------|
| Zero commands | `Commands.None` |
| One command | The single command (unwrapped) |
| N commands | `Command.Batch(commands)` |

```csharp
// Returns Commands.None
Commands.Batch();

// Returns the single command (not wrapped in Batch)
Commands.Batch(Navigation.PushUrl(someUrl));

// Returns Command.Batch with both commands
Commands.Batch(
    Navigation.PushUrl(someUrl),
    new FetchArticles(page: 1)
);
```

## Custom Commands

Define application-specific commands by implementing the `Command` interface:

```csharp
public interface ArticleCommand : Command
{
    record Fetch(string Slug) : ArticleCommand;
    record Favorite(string Slug) : ArticleCommand;
    record Unfavorite(string Slug) : ArticleCommand;
}

public interface AuthCommand : Command
{
    record Login(string Email, string Password) : AuthCommand;
    record Register(string Username, string Email, string Password) : AuthCommand;
}
```

Return them from `Transition`:

```csharp
public static (Model, Command) Transition(Model model, Message message) =>
    message switch
    {
        ClickedArticle(var slug) =>
            (model with { Loading = true }, new ArticleCommand.Fetch(slug)),

        ClickedFavorite(var slug) =>
            (model, new ArticleCommand.Favorite(slug)),

        _ => (model, Commands.None)
    };
```

## Interpreter

Commands are executed by an `Interpreter<Command, Message>` delegate:

```csharp
public delegate ValueTask<Result<Message[], PipelineError>> Interpreter<TEffect, TEvent>(
    TEffect effect);
```

The interpreter receives a command and returns zero or more feedback messages that are dispatched back into the MVU loop:

```csharp
Interpreter<Command, Message> interpreter = async command =>
{
    switch (command)
    {
        case ArticleCommand.Fetch fetch:
            var article = await httpClient.GetFromJsonAsync<Article>($"/api/articles/{fetch.Slug}");
            return Result<Message[], PipelineError>.Ok(
                [new ArticleFetched(article!)]);

        case ArticleCommand.Favorite fav:
            await httpClient.PostAsync($"/api/articles/{fav.Slug}/favorite", null);
            return Result<Message[], PipelineError>.Ok(
                [new ArticleFavorited(fav.Slug)]);

        default:
            return Result<Message[], PipelineError>.Ok([]);
    }
};
```

### Built-in Command Handling

The runtime wraps the caller-supplied interpreter with structural command handling:

| Command Type | Handling |
|-------------|----------|
| `Command.None` | No-op — returns empty message array |
| `Command.Batch` | Flattens and interprets each sub-command, collecting all feedback messages |
| `NavigationCommand` | Handled by the runtime's built-in navigation executor |
| Any other `Command` | Falls through to the caller-supplied interpreter |

This means your interpreter only needs to handle your application-specific commands. `None`, `Batch`, and navigation commands are handled automatically.

### Optional Interpreter

The interpreter is optional in both browser and server runtimes. When omitted, a no-op interpreter is used that returns empty message arrays for all commands. This is suitable for applications with no side effects beyond DOM updates and navigation:

```csharp
// No interpreter needed — Counter has no custom commands
await Abies.Browser.Runtime.Run<Counter, CounterModel, Unit>();

// With interpreter — Conduit needs HTTP calls
await Abies.Browser.Runtime.Run<ConduitProgram, ConduitModel, Unit>(
    interpreter: MyInterpreter.Interpret);
```

## See Also

- [Program](program.md) — Where commands are returned from `Initialize` and `Transition`
- [Navigation](navigation.md) — Built-in navigation commands
- [Runtime](runtime.md) — How the runtime executes commands via the interpreter
- [Message](message.md) — Feedback messages produced by the interpreter
