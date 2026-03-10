# Commands and Side Effects

Abies keeps side effects out of pure functions by using Commands. This document explains the command pattern and how to handle side effects properly.

## The Problem

Side effects break purity and testability:

```csharp
// ❌ Impure Transition - can't test without mocking HTTP
public static (Model, Command) Transition(Model model, Message msg)
{
    if (msg is FetchData)
    {
        var data = await httpClient.GetAsync("/api/data");  // Side effect!
        return (model with { Data = data }, Commands.None);
    }
    return (model, Commands.None);
}
```

## The Solution: Commands

Commands describe side effects without performing them:

```csharp
// ✅ Pure Transition - returns a command describing what to do
public static (Model, Command) Transition(Model model, Message msg)
    => msg switch
    {
        FetchData => (model with { IsLoading = true }, new LoadDataCommand()),
        DataLoaded loaded => (model with { Data = loaded.Data, IsLoading = false }, Commands.None),
        _ => (model, Commands.None)
    };
```

The runtime's **interpreter** handles the impure execution:

```csharp
Interpreter<Command, Message> interpreter = async command =>
{
    switch (command)
    {
        case LoadDataCommand:
            var data = await httpClient.GetAsync("/api/data");
            return Result<Message[], PipelineError>.Ok([new DataLoaded(data)]);
        default:
            return Result<Message[], PipelineError>.Ok([]);
    }
};
```

## The Picea Kernel: Commands as Effects

In the Picea kernel's type system, Commands map to the `TEffect` parameter of `Automaton`:

```csharp
// Automaton<TState, TEvent, TEffect, TParameters>
//    ≡     <Model,  Message, Command, Argument>
```

The kernel provides built-in handling for structural commands:

| Command | Handled By | Behavior |
| ------- | ---------- | -------- |
| `Commands.None` | Runtime | No-op (identity element) |
| `Commands.Batch([...])` | Runtime | Flatten and interpret each sub-command |
| `NavigationCommand.*` | Runtime | Built-in URL navigation |
| All other commands | Your interpreter | Your business logic |

You never need to handle `None`, `Batch`, or navigation commands in your interpreter — the runtime takes care of them.

## Defining Commands

Commands are any type implementing `Abies.Command`:

```csharp
// Simple command with no data
public record LoadArticles : Command;

// Command with parameters
public record SaveArticle(Article Draft) : Command;

// Command with multiple parameters
public record SearchArticles(string Query, int Page, int Limit) : Command;
```

## The Command Flow

```text
┌──────────────────────────────────────────────────────────────┐
│                      Command Flow                            │
│                                                              │
│  1. User Action          2. Transition                       │
│     ↓                       ↓                                │
│  ┌─────────┐           ┌─────────────────────────┐        │
│  │ Message │ ────────▶ │ Transition(model, msg) │        │
│  └─────────┘           │ → (model, Command)     │        │
│                        └─────────────────────────┘        │
│                                  │                           │
│                                  ▼                           │
│  4. Feedback Messages      3. Interpreter                    │
│     ↓                       ↓                                │
│  ┌───────────┐         ┌─────────────────────────┐        │
│  │ Message[] │ ◀────── │ interpreter(command)   │        │
│  └───────────┘         │ → Result<Message[]>    │        │
│                        └─────────────────────────┘        │
│                                                              │
│  5. Feedback messages re-enter the MVU loop at step 2         │
└──────────────────────────────────────────────────────────────┘
```

## Writing an Interpreter

The `Interpreter<Command, Message>` is a delegate that converts commands into feedback messages:

```csharp
Interpreter<Command, Message> interpreter = async command =>
{
    switch (command)
    {
        case LoadArticles load:
            try
            {
                var articles = await api.GetArticles(load.Page);
                return Result<Message[], PipelineError>.Ok(
                    [new ArticlesLoaded(articles)]);
            }
            catch (Exception ex)
            {
                return Result<Message[], PipelineError>.Ok(
                    [new LoadFailed(ex.Message)]);
            }

        case SaveArticle save:
            try
            {
                var saved = await api.SaveArticle(save.Draft);
                return Result<Message[], PipelineError>.Ok(
                    [new ArticleSaved(saved)]);
            }
            catch (Exception ex)
            {
                return Result<Message[], PipelineError>.Ok(
                    [new SaveFailed(ex.Message)]);
            }

        default:
            return Result<Message[], PipelineError>.Ok([]);
    }
};
```

### Browser Usage

```csharp
await Abies.Browser.Runtime.Run<MyApp, Model, Unit>(
    interpreter: interpreter);
```

### Server Usage

```csharp
app.MapAbies<MyApp, Model, Unit>("/",
    renderMode: RenderMode.InteractiveServer("/ws"),
    interpreter: interpreter);
```

## Common Command Types

### HTTP Requests

```csharp
public record FetchUser(string Username) : Command;
public record CreatePost(PostDraft Draft) : Command;
public record DeleteComment(string CommentId) : Command;
```

### Navigation

Navigation commands are handled automatically by the runtime:

```csharp
// Built-in navigation commands
new NavigationCommand.Push(Url.FromUri(new Uri("/profile", UriKind.Relative)))
new NavigationCommand.Replace(Url.FromUri(new Uri("/login", UriKind.Relative)))
new NavigationCommand.GoBack()
new NavigationCommand.GoForward()
new NavigationCommand.External("https://example.com")
```

### Timers and Delays

```csharp
public record DelayedAction(TimeSpan Delay, Message After) : Command;

// In interpreter:
case DelayedAction delay:
    await Task.Delay(delay.Delay);
    return Result<Message[], PipelineError>.Ok([delay.After]);
```

## Composing Commands

### Batch Commands

Execute multiple commands:

```csharp
var command = Commands.Batch([
    new LoadArticles(0),
    new LoadTags(),
    new LoadCurrentUser()
]);
```

The runtime flattens and interprets each sub-command, collecting all feedback messages.

### No Command

When Transition has no side effects:

```csharp
case CounterMessage.Increment:
    return (model with { Count = model.Count + 1 }, Commands.None);
```

## Error Handling

Always handle errors in your interpreter:

```csharp
case LoadData:
    try
    {
        var data = await api.GetData();
        return Result<Message[], PipelineError>.Ok([new DataLoaded(data)]);
    }
    catch (HttpRequestException ex)
    {
        return Result<Message[], PipelineError>.Ok([new NetworkError(ex.Message)]);
    }
    catch (JsonException ex)
    {
        return Result<Message[], PipelineError>.Ok([new ParseError(ex.Message)]);
    }
```

And handle error messages in Transition:

```csharp
case NetworkError error:
    return (model with {
        Error = $"Network error: {error.Message}",
        IsLoading = false
    }, Commands.None);
```

## Testing Commands

### Test Transition Returns Correct Command

```csharp
[Fact]
public void FetchData_ReturnsLoadCommand()
{
    var model = new Model(IsLoading: false);

    var (newModel, command) = Transition(model, new FetchData());

    Assert.True(newModel.IsLoading);
    Assert.IsType<LoadDataCommand>(command);
}
```

### Test Interpreter with Runtime

```csharp
[Fact]
public async Task Interpreter_DispatchesFeedbackMessage()
{
    var patches = new List<IReadOnlyList<Patch>>();
    var runtime = await Runtime<MyApp, Model, Unit>.Start(
        apply: p => patches.Add(p),
        interpreter: async cmd =>
        {
            if (cmd is LoadDataCommand)
                return Result<Message[], PipelineError>.Ok(
                    [new DataLoaded(testData)]);
            return Result<Message[], PipelineError>.Ok([]);
        });

    await runtime.Dispatch(new FetchData());

    // Model updated with loaded data
    Assert.False(runtime.Model.IsLoading);
    Assert.NotEmpty(runtime.Model.Data);
}
```

## Commands vs Subscriptions

| Aspect | Commands | Subscriptions |
| ------ | -------- | ------------- |
| Trigger | Once, from Transition | Continuous, based on model |
| Lifetime | Fire-and-forget | Active while subscription exists |
| Use case | API calls, one-time effects | Timers, browser events, sockets |
| Example | Fetch user data | Listen for window resize |

## Best Practices

### 1. Keep Commands Simple

One command = one side effect:

```csharp
// ❌ Command doing too much
public record LoadEverything : Command;

// ✅ Separate commands, batched
return (model, Commands.Batch([new LoadArticles(), new LoadUser(), new LoadTags()]));
```

### 2. Include Necessary Data

Pass all data needed for the effect:

```csharp
// ❌ Missing context
public record Save : Command;

// ✅ Self-contained
public record SaveArticle(string Title, string Body, List<string> Tags) : Command;
```

### 3. Return Typed Feedback

Return specific result messages:

```csharp
// ❌ Generic result
return Ok([new Success()]);

// ✅ Specific result
return Ok([new ArticleSaved(article.Slug)]);
```

### 4. Handle Loading States

```csharp
case FetchArticle fetch:
    return (model with { IsLoading = true, Error = null }, new LoadArticle(fetch.Slug));

case ArticleLoaded loaded:
    return (model with { Article = loaded.Article, IsLoading = false }, Commands.None);

case LoadFailed failed:
    return (model with { Error = failed.Message, IsLoading = false }, Commands.None);
```

## Summary

Commands are the bridge between pure Transition functions and impure side effects:

- **Transition** — Returns commands (pure)
- **Interpreter** — Executes commands and returns feedback messages (impure)
- **Runtime** — Handles structural commands (None, Batch, Navigation) automatically

This pattern provides:

- ✅ Testable business logic
- ✅ Explicit side effects
- ✅ Clear data flow
- ✅ Platform-agnostic (same interpreter works in browser and server)

## See Also

- [Subscriptions](./subscriptions.md) — Long-lived event sources
- [MVU Architecture](./mvu-architecture.md) — The overall pattern
- [Render Modes](./render-modes.md) — How commands execute across platforms
