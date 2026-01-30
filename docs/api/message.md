# Message API Reference

Messages are the events that drive state changes in an Abies application.

## Interface Definition

```csharp
public interface Message;
```

The `Message` interface is a marker interface with no members. All message types implement this interface.

## Creating Messages

Messages are typically defined as records:

```csharp
// Simple message with no data
public record Increment : Message;

// Message with data
public record TextChanged(string Value) : Message;

// Message with multiple fields
public record ArticleLoaded(Article Article, List<Comment> Comments) : Message;
```

## Message Design Patterns

### Simple Events

For events with no associated data:

```csharp
public record Increment : Message;
public record Decrement : Message;
public record Reset : Message;
public record ToggleMenu : Message;
```

### Events with Data

For events carrying information:

```csharp
public record TextEntered(string Value) : Message;
public record ItemSelected(int Index) : Message;
public record UserLoggedIn(User User) : Message;
public record ErrorOccurred(string Message, string Code) : Message;
```

### Nested Message Hierarchies

Organize messages by feature:

```csharp
public interface Message : Abies.Message { }

public interface HomeMessage : Message
{
    public record ArticlesLoaded(List<Article> Articles) : HomeMessage;
    public record TagSelected(string Tag) : HomeMessage;
    public record PageChanged(int Page) : HomeMessage;
}

public interface ProfileMessage : Message
{
    public record ProfileLoaded(Profile Profile) : ProfileMessage;
    public record FollowClicked : ProfileMessage;
    public record UnfollowClicked : ProfileMessage;
}
```

### Sum Types for Related Events

Group related outcomes:

```csharp
public interface LoadResult : Message
{
    public record Success(Data Data) : LoadResult;
    public record Failure(string Error) : LoadResult;
    public record Loading : LoadResult;
}
```

## Using Messages

### In Event Handlers

```csharp
button([onclick(new Increment())], [text("+")])
```

### With Event Data

```csharp
input([
    type("text"),
    oninput(e => new TextChanged(e?.Value ?? ""))
], [])
```

### In Update

```csharp
public static (Model, Command) Update(Message msg, Model model)
    => msg switch
    {
        Increment => (model with { Count = model.Count + 1 }, Commands.None),
        TextChanged tc => (model with { Text = tc.Value }, Commands.None),
        _ => (model, Commands.None)
    };
```

## Built-in Message Types

### UrlRequest

Represents navigation requests:

```csharp
public interface UrlRequest : Message
{
    public record Internal(Url Url) : UrlRequest;
    public record External(string Url) : UrlRequest;
}
```

**Internal** — Links within your application  
**External** — Links to other websites

## Best Practices

### 1. Use Past Tense Naming

Messages describe what happened:

```csharp
// ✅ Good - past tense
public record ButtonClicked : Message;
public record DataLoaded(Data Data) : Message;
public record UserLoggedOut : Message;

// ❌ Avoid - imperative
public record ClickButton : Message;
public record LoadData : Message;
public record LogoutUser : Message;
```

### 2. Include Only Necessary Data

```csharp
// ✅ Good - minimal data
public record ItemSelected(int Id) : Message;

// ❌ Avoid - too much data
public record ItemSelected(Item Item, int Index, bool WasDoubleClick, DateTime When) : Message;
```

### 3. Make Messages Immutable

Using records ensures immutability:

```csharp
// ✅ Records are immutable by default
public record CountChanged(int NewCount) : Message;
```

### 4. Keep Messages Flat When Possible

```csharp
// ✅ Good - flat structure
public record ArticleLoaded(string Title, string Body, string Author) : Message;

// Consider - nested for complex data
public record ArticleLoaded(Article Article) : Message;
```

### 5. Handle All Cases

Use exhaustive pattern matching:

```csharp
public static (Model, Command) Update(Message msg, Model model)
    => msg switch
    {
        Increment => ...,
        Decrement => ...,
        Reset => ...,
        _ => (model, Commands.None)  // Catch-all for safety
    };
```

## Message Flow

```text
1. User clicks button with onclick(new Increment())
2. Runtime captures event and dispatches message
3. Update(Increment, model) is called
4. New model returned
5. View(newModel) renders updated UI
```

## Testing Messages

```csharp
[Fact]
public void Increment_IncreasesCount()
{
    var model = new Model(Count: 5);
    
    var (result, _) = Update(new Increment(), model);
    
    Assert.Equal(6, result.Count);
}

[Fact]
public void TextChanged_UpdatesText()
{
    var model = new Model(Text: "");
    
    var (result, _) = Update(new TextChanged("hello"), model);
    
    Assert.Equal("hello", result.Text);
}
```

## See Also

- [Command API](./command.md) — Side effect requests
- [Program API](./program.md) — Where Update lives
- [Concepts: MVU Architecture](../concepts/mvu-architecture.md) — Message flow
