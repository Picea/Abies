# Command API Reference

Commands represent side effects to be executed by the runtime.

## Interface Definition

```csharp
public interface Command
{
    public record struct None : Command;
    public record struct Batch(IEnumerable<Command> Commands) : Command;
}
```

## Factory Methods

The `Commands` static class provides factory methods:

```csharp
public static class Commands
{
    public static Command.None None = new();
    public static Command.Batch Batch(IEnumerable<Command> commands) => new(commands);
}
```

## Built-in Commands

### Commands.None

A command that does nothing. Use when Update has no side effects.

```csharp
public static (Model, Command) Update(Message msg, Model model)
    => msg switch
    {
        Increment => (model with { Count = model.Count + 1 }, Commands.None),
        _ => (model, Commands.None)
    };
```

### Commands.Batch

Combines multiple commands into one.

```csharp
var command = Commands.Batch([
    new LoadArticlesCommand(),
    new LoadTagsCommand(),
    new LoadUserCommand()
]);
```

## Creating Custom Commands

Commands are records implementing the `Command` interface:

```csharp
// Simple command
public record LoadArticles : Command;

// Command with data
public record SaveArticle(string Title, string Body) : Command;

// Command with complex data
public record SubmitForm(FormData Data, string Endpoint) : Command;
```

## Handling Commands

Commands are executed in `HandleCommand`:

```csharp
public static async Task HandleCommand(
    Command command, 
    Func<Message, ValueTuple> dispatch)
{
    switch (command)
    {
        case LoadArticles:
            try
            {
                var articles = await api.GetArticlesAsync();
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
                await api.SaveAsync(save.Title, save.Body);
                dispatch(new ArticleSaved());
            }
            catch (Exception ex)
            {
                dispatch(new SaveFailed(ex.Message));
            }
            break;

        case Commands.None:
            // Nothing to do
            break;
            
        case Command.Batch batch:
            foreach (var cmd in batch.Commands)
            {
                await HandleCommand(cmd, dispatch);
            }
            break;
    }
}
```

## Navigation Commands

Abies provides built-in navigation commands:

```csharp
namespace Abies.Navigation
{
    public interface Command : Abies.Command
    {
        public record Load(Url Url) : Command;
        public record PushState(Url Url) : Command;
        public record ReplaceState(Url Url) : Command;
        public record Back : Command;
        public record Forward : Command;
        public record Go(int Delta) : Command;
        public record Reload : Command;
    }
}
```

### Usage

```csharp
// Navigate to a new URL (adds to history)
return (model, new Navigation.Command.PushState(Url.Create("/profile")));

// Replace current URL (no history entry)
return (model, new Navigation.Command.ReplaceState(Url.Create("/login")));

// Go back
return (model, new Navigation.Command.Back());

// Go forward
return (model, new Navigation.Command.Forward());

// Navigate by history offset
return (model, new Navigation.Command.Go(-2));  // Go back 2 pages

// Full page reload
return (model, new Navigation.Command.Reload());
```

## Command Patterns

### HTTP Requests

```csharp
public record FetchData(string Endpoint) : Command;
public record PostData(string Endpoint, object Payload) : Command;
public record DeleteData(string Endpoint) : Command;

// Handling
case FetchData fetch:
    var response = await http.GetAsync(fetch.Endpoint);
    var data = await response.Content.ReadFromJsonAsync<Data>();
    dispatch(new DataLoaded(data));
    break;
```

### Local Storage

```csharp
public record SaveToStorage(string Key, string Value) : Command;
public record LoadFromStorage(string Key) : Command;
public record RemoveFromStorage(string Key) : Command;

// Handling
case SaveToStorage save:
    await js.InvokeVoidAsync("localStorage.setItem", save.Key, save.Value);
    break;

case LoadFromStorage load:
    var value = await js.InvokeAsync<string>("localStorage.getItem", load.Key);
    dispatch(new StorageLoaded(load.Key, value));
    break;
```

### Delayed Actions

```csharp
public record DelayedMessage(TimeSpan Delay, Message Message) : Command;

// Handling
case DelayedMessage delayed:
    await Task.Delay(delayed.Delay);
    dispatch(delayed.Message);
    break;
```

### Focus Management

```csharp
public record FocusElement(string ElementId) : Command;

// Handling
case FocusElement focus:
    await js.InvokeVoidAsync("document.getElementById", focus.ElementId, "focus");
    break;
```

## Error Handling

Always handle errors in command execution:

```csharp
case LoadData:
    try
    {
        var data = await api.GetDataAsync();
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
        dispatch(new UnexpectedError(ex.Message));
    }
    break;
```

## Best Practices

### 1. One Command = One Effect

```csharp
// ✅ Good - focused commands
public record LoadArticles : Command;
public record LoadTags : Command;

// ❌ Avoid - multi-purpose commands
public record LoadEverything : Command;
```

### 2. Include Necessary Data

```csharp
// ✅ Good - self-contained
public record SaveArticle(string Title, string Body, List<string> Tags) : Command;

// ❌ Avoid - requires external lookup
public record SaveCurrentArticle : Command;
```

### 3. Return Typed Results

```csharp
// ✅ Good - specific result messages
dispatch(new ArticleLoaded(article));
dispatch(new ArticleNotFound(slug));
dispatch(new NetworkError(message));

// ❌ Avoid - generic results
dispatch(new Success());
dispatch(new Failure());
```

### 4. Track Loading States

```csharp
// In Update
case LoadArticles:
    return (model with { IsLoading = true, Error = null }, new LoadArticlesCommand());

case ArticlesLoaded loaded:
    return (model with { Articles = loaded.Articles, IsLoading = false }, Commands.None);

case LoadFailed failed:
    return (model with { Error = failed.Message, IsLoading = false }, Commands.None);
```

## Testing Commands

### Test Command Creation

```csharp
[Fact]
public void SubmitForm_ReturnsCorrectCommand()
{
    var model = new Model(Title: "Test", Body: "Content");
    
    var (_, command) = Update(new SubmitClicked(), model);
    
    var saveCommand = Assert.IsType<SaveArticle>(command);
    Assert.Equal("Test", saveCommand.Title);
    Assert.Equal("Content", saveCommand.Body);
}
```

### Test Command Handling

```csharp
[Fact]
public async Task LoadArticles_DispatchesLoadedMessage()
{
    var messages = new List<Message>();
    var fakeApi = new FakeApi(returns: new List<Article> { new("test") });
    
    await HandleCommand(new LoadArticles(), msg => { messages.Add(msg); return default; });
    
    Assert.Single(messages);
    var loaded = Assert.IsType<ArticlesLoaded>(messages[0]);
    Assert.Single(loaded.Articles);
}
```

## See Also

- [Message API](./message.md) — Result messages
- [Subscription API](./subscription.md) — Continuous effects
- [Concepts: Commands and Effects](../concepts/commands-effects.md) — Deep dive
