# Message API

Messages are the events that drive state changes in an Abies application. Every user interaction, subscription event, and navigation change is represented as a `Message`.

## Interface Definition

```csharp
public interface Message;
```

The `Message` interface is a marker interface with no members. All message types implement this interface.

## Creating Messages

Messages are typically defined as records or record structs:

```csharp
// Simple message with no data
public record Increment : Message;

// Message with data
public record TextChanged(string Value) : Message;

// Value-type message (zero allocation)
public record struct Tick : Message;

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
public record ErrorOccurred(string Error, string Code) : Message;
```

### Nested Message Hierarchies

Organize messages by feature using interface nesting:

```csharp
public interface HomeMessage : Message
{
    record ArticlesLoaded(List<Article> Articles) : HomeMessage;
    record TagSelected(string Tag) : HomeMessage;
    record PageChanged(int Page) : HomeMessage;
}

public interface ProfileMessage : Message
{
    record ProfileLoaded(Profile Profile) : ProfileMessage;
    record FollowClicked : ProfileMessage;
    record UnfollowClicked : ProfileMessage;
}
```

This pattern groups related messages under a common interface, enabling focused pattern matching:

```csharp
public static (Model, Command) Transition(Model model, Message message) =>
    message switch
    {
        HomeMessage homeMsg => HandleHome(model, homeMsg),
        ProfileMessage profileMsg => HandleProfile(model, profileMsg),
        UrlChanged(var url) => Route(model, url),
        _ => (model, Commands.None)
    };
```

### Sum Types for Related Outcomes

Group related outcomes:

```csharp
public interface LoadResult : Message
{
    record Success(Data Data) : LoadResult;
    record Failure(string Error) : LoadResult;
    record Loading : LoadResult;
}
```

## Using Messages

### In Event Handlers

Simple message dispatch:

```csharp
button([onclick(new Increment())], [text("+")])
```

Message from event data:

```csharp
input([
    type("text"),
    oninput(e => new TextChanged(e?.Value ?? ""))
])
```

### In Transition

```csharp
public static (Model, Command) Transition(Model model, Message message) =>
    message switch
    {
        Increment => (model with { Count = model.Count + 1 }, Commands.None),
        Decrement => (model with { Count = model.Count - 1 }, Commands.None),
        TextChanged(var value) => (model with { Text = value }, Commands.None),
        _ => (model, Commands.None)
    };
```

## Built-in Message Types

Abies defines two built-in message types for navigation:

### UrlChanged

```csharp
public record UrlChanged(Url Url) : Message;
```

Dispatched when the URL changes — from browser back/forward, programmatic navigation (`Navigation.PushUrl`), or the initial page load.

### UrlRequest

```csharp
public interface UrlRequest : Message
{
    record Internal(Url Url) : UrlRequest;
    record External(string Href) : UrlRequest;
}
```

Dispatched when a link is clicked:

| Variant | Description |
|---------|-------------|
| `Internal` | A link within the application. The `Url` is already parsed. |
| `External` | A link to an external site. Contains the raw URL string. |

## Best Practices

### 1. Use Past Tense or Descriptive Naming

Messages describe what happened or what was received:

```csharp
// ✅ Good — describes what happened
public record ButtonClicked : Message;
public record DataLoaded(Data Data) : Message;
public record UserLoggedOut : Message;

// ❌ Avoid — imperative (sounds like a command)
public record ClickButton : Message;
public record LoadData : Message;
public record LogoutUser : Message;
```

### 2. Include Only Necessary Data

```csharp
// ✅ Good — minimal data
public record ItemSelected(int Id) : Message;

// ❌ Avoid — too much data
public record ItemSelected(Item Item, int Index, bool WasDoubleClick, DateTime When) : Message;
```

### 3. Make Messages Immutable

Using records ensures immutability:

```csharp
// ✅ Records are immutable by default
public record CountChanged(int NewCount) : Message;
```

### 4. Handle All Cases

Use a catch-all pattern in `Transition` for safety:

```csharp
public static (Model, Command) Transition(Model model, Message message) =>
    message switch
    {
        Increment => (model with { Count = model.Count + 1 }, Commands.None),
        Decrement => (model with { Count = model.Count - 1 }, Commands.None),
        Reset => (model with { Count = 0 }, Commands.None),
        _ => (model, Commands.None)  // Unknown messages are no-ops
    };
```

### 5. Use Record Structs for High-Frequency Messages

For messages dispatched very frequently (e.g., every animation frame), use `record struct` to avoid heap allocation:

```csharp
public record struct Tick : Message;
public record struct MouseMoved(double X, double Y) : Message;
```

## Message Flow

```text
1. User clicks button with onclick(new Increment())
2. Browser runtime captures DOM event and dispatches message
3. Transition(model, Increment) is called
4. New (model, command) returned
5. View(newModel) renders updated virtual DOM
6. Runtime diffs and applies patches to the real DOM
7. Interpreter executes any commands, feeding feedback messages back to step 3
```

## Testing Messages

```csharp
[Fact]
public void Increment_IncreasesCount()
{
    var model = new Model(Count: 5);

    var (result, cmd) = Counter.Transition(model, new Increment());

    Assert.Equal(6, result.Count);
    Assert.IsType<Command.None>(cmd);
}

[Fact]
public void TextChanged_UpdatesText()
{
    var model = new Model(Text: "");

    var (result, _) = MyApp.Transition(model, new TextChanged("hello"));

    Assert.Equal("hello", result.Text);
}
```

## See Also

- [Program](program.md) — Where `Transition` processes messages
- [Command](command.md) — Side effects returned alongside model updates
- [HTML Events](html-events.md) — How DOM events produce messages
- [Navigation](navigation.md) — Built-in `UrlChanged` and `UrlRequest` messages
