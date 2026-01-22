# Commands and Side Effects

Abies keeps side effects out of `Update`. You return a `Command` that the
runtime executes, then dispatch a message with the result.

## Defining commands

Commands are any type that implements `Abies.Command`.

```csharp
public record LoadArticles(int Offset) : Command;
public record SaveArticle(Article Draft) : Command;
```

You can also compose commands:

```csharp
var command = Commands.Batch([
    new LoadArticles(0),
    new LoadArticles(20)
]);
```

## Handling commands

`HandleCommand` runs effects and dispatches messages back into the MVU loop.

```csharp
public static async Task HandleCommand(Command command, Func<Message, System.ValueTuple> dispatch)
{
    switch (command)
    {
        case LoadArticles load:
            var articles = await api.GetArticles(load.Offset);
            dispatch(new ArticlesLoaded(articles));
            break;
        case SaveArticle save:
            await api.Save(save.Draft);
            dispatch(new ArticleSaved());
            break;
    }
}
```

## Navigation commands

The runtime handles navigation commands specially.

```csharp
return (model, new Navigation.Command.PushState(Url.Create("/profile/maurice")));
```

Supported navigation commands:
- `Navigation.Command.Load`
- `Navigation.Command.PushState`
- `Navigation.Command.ReplaceState`
- `Navigation.Command.Back` / `Forward` / `Go` / `Reload`

## Design notes

- Keep `Update` pure. All IO belongs in `HandleCommand`.
- Return `Commands.None` when there are no effects to run.
- Commands can be simple records; no special base class is required.

## Subscriptions

For long-lived external event sources (timers, browser events, sockets), use
`Subscriptions` instead of one-off commands. See
[Program and Runtime](./runtime-program.md) for details.
