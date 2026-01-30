# Commands and Side Effects

Abies keeps side effects out of pure functions by using Commands. This document explains the command pattern and how to handle side effects properly.

## The Problem

Side effects break purity and testability:

```csharp
// ❌ Impure Update - can't test without mocking HTTP
public static (Model, Command) Update(Message msg, Model model)
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
// ✅ Pure Update - returns a command describing what to do
public static (Model, Command) Update(Message msg, Model model)
    => msg switch
    {
        FetchData => (model with { IsLoading = true }, new LoadDataCommand()),
        DataLoaded loaded => (model with { Data = loaded.Data, IsLoading = false }, Commands.None),
        _ => (model, Commands.None)
    };
```

The runtime handles the impure execution:

```csharp
public static async Task HandleCommand(Command cmd, Dispatch dispatch)
{
    if (cmd is LoadDataCommand)
    {
        var data = await httpClient.GetAsync("/api/data");
        dispatch(new DataLoaded(data));
    }
}
```

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
┌────────────────────────────────────────────────────────────────┐
│                      Command Flow                              │
│                                                                │
│  1. User Action          2. Update                             │
│     ↓                       ↓                                  │
│  ┌─────────┐           ┌─────────────────────┐                │
│  │ Message │ ────────▶ │ Update(msg, model)  │                │
│  └─────────┘           │ → (model, Command)  │                │
│                        └─────────────────────┘                │
│                                  │                             │
│                                  ▼                             │
│  4. Result Message        3. Runtime                           │
│     ↓                       ↓                                  │
│  ┌─────────┐           ┌─────────────────────┐                │
│  │ Message │ ◀──────── │ HandleCommand(cmd)  │                │
│  └─────────┘           │ dispatch(message)   │                │
│                        └─────────────────────┘                │
│                                                                │
│  5. Back to step 2 with result message                        │
└────────────────────────────────────────────────────────────────┘
```

## Handling Commands

The `HandleCommand` function executes side effects and dispatches results:

```csharp
public static async Task HandleCommand(
    Command command, 
    Func<Message, ValueTuple> dispatch)
{
    switch (command)
    {
        case LoadArticles load:
            try
            {
                var articles = await api.GetArticles(load.Page);
                dispatch(new ArticlesLoaded(articles));
            }
            catch (Exception ex)
            {
                dispatch(new LoadFailed(ex.Message));
            }
            break;

        case SaveArticle save:
            try
            {
                var saved = await api.SaveArticle(save.Draft);
                dispatch(new ArticleSaved(saved));
            }
            catch (Exception ex)
            {
                dispatch(new SaveFailed(ex.Message));
            }
            break;

        case Commands.None:
            // No effect to perform
            break;
    }
}
```

## Common Command Types

### HTTP Requests

```csharp
public record FetchUser(string Username) : Command;
public record CreatePost(PostDraft Draft) : Command;
public record DeleteComment(string CommentId) : Command;

// Handling
case FetchUser fetch:
    var user = await api.GetUser(fetch.Username);
    dispatch(new UserLoaded(user));
    break;
```

### Local Storage

```csharp
public record SaveToStorage(string Key, string Value) : Command;
public record LoadFromStorage(string Key) : Command;
public record ClearStorage : Command;

// Handling
case SaveToStorage save:
    await jsRuntime.InvokeVoidAsync("localStorage.setItem", save.Key, save.Value);
    dispatch(new StorageSaved());
    break;
```

### Navigation

Navigation commands are handled specially by the runtime:

```csharp
// Built-in navigation commands
new Navigation.Command.PushState(Url.Create("/profile"))
new Navigation.Command.ReplaceState(Url.Create("/login"))
new Navigation.Command.Back()
new Navigation.Command.Forward()
new Navigation.Command.Reload()
```

### Timers and Delays

```csharp
public record DelayedAction(TimeSpan Delay, Message After) : Command;

// Handling
case DelayedAction delay:
    await Task.Delay(delay.Delay);
    dispatch(delay.After);
    break;
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

### No Command

When Update has no side effects:

```csharp
case Increment:
    return (model with { Count = model.Count + 1 }, Commands.None);
```

## Error Handling

Always handle errors in `HandleCommand`:

```csharp
case LoadData:
    try
    {
        var data = await api.GetData();
        dispatch(new DataLoaded(data));
    }
    catch (HttpRequestException ex)
    {
        dispatch(new NetworkError(ex.Message));
    }
    catch (JsonException ex)
    {
        dispatch(new ParseError(ex.Message));
    }
    catch (Exception ex)
    {
        dispatch(new UnknownError(ex.Message));
    }
    break;
```

And handle errors in Update:

```csharp
case NetworkError error:
    return (model with { 
        Error = $"Network error: {error.Message}",
        IsLoading = false 
    }, Commands.None);

case ParseError error:
    return (model with { 
        Error = $"Invalid response: {error.Message}",
        IsLoading = false 
    }, Commands.None);
```

## Testing Commands

### Test Update Returns Correct Command

```csharp
[Fact]
public void FetchData_ReturnsLoadCommand()
{
    var model = new Model(IsLoading: false);
    
    var (newModel, command) = Update(new FetchData(), model);
    
    Assert.True(newModel.IsLoading);
    Assert.IsType<LoadDataCommand>(command);
}
```

### Test Command Handling

```csharp
[Fact]
public async Task LoadDataCommand_DispatchesLoadedMessage()
{
    var messages = new List<Message>();
    void Dispatch(Message msg) => messages.Add(msg);
    
    var fakeApi = new FakeApi(returns: new Data(42));
    
    await HandleCommand(new LoadDataCommand(), Dispatch);
    
    Assert.Single(messages);
    Assert.IsType<DataLoaded>(messages[0]);
    Assert.Equal(42, ((DataLoaded)messages[0]).Data.Value);
}
```

## Commands vs Subscriptions

| Aspect | Commands | Subscriptions |
| ------ | -------- | ------------- |
| Trigger | Once, from Update | Continuous, based on model |
| Lifetime | Fire-and-forget | Active while subscription exists |
| Use case | API calls, one-time effects | Timers, browser events, sockets |
| Example | Fetch user data | Listen for window resize |

Use Commands for:

- HTTP requests
- One-time side effects
- Imperative actions

Use Subscriptions for:

- Timers
- Browser events (resize, visibility)
- WebSocket connections
- Keyboard shortcuts

## Best Practices

### 1. Keep Commands Simple

One command = one side effect:

```csharp
// ❌ Command doing too much
public record LoadEverything : Command;

// ✅ Separate commands
public record LoadArticles : Command;
public record LoadUser : Command;
public record LoadTags : Command;
```

### 2. Include Necessary Data

Pass all data needed for the effect:

```csharp
// ❌ Missing context
public record Save : Command;

// ✅ Self-contained
public record SaveArticle(string Title, string Body, List<string> Tags) : Command;
```

### 3. Return Typed Results

Dispatch specific result messages:

```csharp
// ❌ Generic result
dispatch(new Success());

// ✅ Specific result
dispatch(new ArticleSaved(article.Slug));
```

### 4. Handle Loading States

Track loading in the model:

```csharp
case FetchArticle fetch:
    return (model with { IsLoading = true, Error = null }, new LoadArticle(fetch.Slug));

case ArticleLoaded loaded:
    return (model with { Article = loaded.Article, IsLoading = false }, Commands.None);

case LoadFailed failed:
    return (model with { Error = failed.Message, IsLoading = false }, Commands.None);
```

## Summary

Commands are the bridge between pure Update functions and impure side effects:

- **Update** — Returns commands (pure)
- **HandleCommand** — Executes commands (impure)
- **Messages** — Communicate results back

This pattern provides:

- ✅ Testable business logic
- ✅ Explicit side effects
- ✅ Clear data flow
- ✅ Error handling in one place

## See Also

- [Subscriptions](./subscriptions.md) — Long-lived event sources
- [MVU Architecture](./mvu-architecture.md) — The overall pattern
- [Tutorial: API Integration](../tutorials/03-api-integration.md) — Practical examples
