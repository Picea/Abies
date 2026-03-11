# Tutorial 3: API Integration

Learn how to make HTTP requests and handle side effects using commands and the interpreter pattern.

**Prerequisites:** [Tutorial 2: Todo List](02-todo-list.md)

**Time:** 30 minutes

**What you'll learn:**

- Commands as descriptions of side effects
- The interpreter pattern for executing commands
- Handling loading states and errors
- The `Result<Message[], PipelineError>` return type
- Separating pure logic from effectful execution

## The Problem: Side Effects

In Tutorials 1 and 2, everything was synchronous and pure. But real applications need to:

- Fetch data from APIs
- Read/write to localStorage
- Send analytics events
- Interact with browser APIs

These are **side effects** — they interact with the outside world. The MVU architecture handles them through a two-step process:

1. **Transition** returns a `Command` describing *what* should happen (pure)
2. **Interpreter** executes the command and returns feedback messages (effectful)

This separation is the key insight: your business logic stays pure and testable, while side effects are isolated in a single, replaceable interpreter.

## Building a Quote Viewer

Let's build an app that fetches random quotes from an API.

### Model

```csharp
using Abies.DOM;
using Abies.Subscriptions;
using Automaton;
using static Abies.Html.Attributes;
using static Abies.Html.Elements;
using static Abies.Html.Events;

namespace QuoteViewer;

public record Quote(string Text, string Author);

public record Model(
    Quote? CurrentQuote,
    bool IsLoading,
    string? Error);
```

Notice the three-state pattern: we either have a quote, are loading, or have an error. This is a common API integration model.

### Messages

```csharp
public interface QuoteMessage : Message;

/// <summary>User clicked the "New Quote" button.</summary>
public record FetchNewQuote : QuoteMessage;

/// <summary>A quote was successfully loaded from the API.</summary>
public record QuoteLoaded(Quote Quote) : QuoteMessage;

/// <summary>The API request failed.</summary>
public record QuoteFailed(string Error) : QuoteMessage;
```

Note the separation: `FetchNewQuote` is a user action ("I want a new quote"). `QuoteLoaded` and `QuoteFailed` are responses from the outside world. The user triggers the intent; the interpreter reports the outcome.

### Commands

Commands are **descriptions of side effects**. They carry the data needed to perform the effect but don't execute anything themselves:

```csharp
/// <summary>Describes a request to fetch a random quote.</summary>
public record FetchQuote : Command;
```

A command is just a record — a plain data object. It says *what* to do, not *how* to do it.

> **Principle:** This is the [Command Pattern](https://en.wikipedia.org/wiki/Command_pattern) from the Gang of Four. In Abies, it's combined with the [Interpreter Pattern](https://en.wikipedia.org/wiki/Interpreter_pattern) to create a clean separation between *describing* effects and *executing* them. The formal model is a *free monad* — commands form the "syntax" of an embedded DSL, and the interpreter provides the "semantics".

### Transition

```csharp
public sealed class QuoteApp : Program<Model, Unit>
{
    public static (Model, Command) Initialize(Unit _) =>
        (new Model(CurrentQuote: null, IsLoading: true, Error: null),
         new FetchQuote());  // ← command returned, not executed

    public static (Model, Command) Transition(Model model, Message message) =>
        message switch
        {
            FetchNewQuote =>
                (model with { IsLoading = true, Error = null },
                 new FetchQuote()),  // ← request a fetch

            QuoteLoaded msg =>
                (model with { CurrentQuote = msg.Quote, IsLoading = false },
                 Commands.None),     // ← no further effects

            QuoteFailed msg =>
                (model with { Error = msg.Error, IsLoading = false },
                 Commands.None),

            _ => (model, Commands.None)
        };
```

**Key insight:** `Initialize` returns `new FetchQuote()` as a command. This triggers an API call *without* the Transition function ever making a network request. The Transition function is still pure.

### View

```csharp
    public static Document View(Model model) =>
        new("Quote Viewer",
            div([class_("quote-app")],
            [
                h1([], [text("Random Quotes")]),
                model switch
                {
                    { IsLoading: true } => LoadingView(),
                    { Error: string err } => ErrorView(err),
                    { CurrentQuote: Quote q } => QuoteView(q),
                    _ => text("")
                },
                button([
                    class_("fetch-btn"),
                    onclick(new FetchNewQuote())
                ], [text("New Quote")])
            ]));

    static Node LoadingView() =>
        div([class_("loading")], [text("Loading...")]);

    static Node ErrorView(string error) =>
        div([class_("error")], [text($"⚠ {error}")]);

    static Node QuoteView(Quote quote) =>
        blockquote([class_("quote")],
        [
            p([], [text($"\"{quote.Text}\"")]),
            footer([], [text($"— {quote.Author}")])
        ]);

    public static Subscription Subscriptions(Model model) =>
        SubscriptionModule.None;
}
```

### The Interpreter

Now we write the code that actually executes side effects. The interpreter is a function with the signature:

```csharp
ValueTask<Result<Message[], PipelineError>> Interpret(Command command)
```

It receives a command, executes the effect, and returns an array of messages that flow back into `Transition`:

```csharp
using System.Net.Http.Json;

namespace QuoteViewer;

public static class QuoteInterpreter
{
    private static readonly HttpClient _http = new();

    public static async ValueTask<Result<Message[], PipelineError>> Interpret(
        Command command)
    {
        try
        {
            Message[] messages = command switch
            {
                FetchQuote => await HandleFetchQuote(),
                _ => []  // unknown commands produce no messages
            };

            return Result<Message[], PipelineError>.Ok(messages);
        }
        catch (Exception ex)
        {
            return Result<Message[], PipelineError>.Ok(
                [new QuoteFailed(ex.Message)]);
        }
    }

    private static async Task<Message[]> HandleFetchQuote()
    {
        var response = await _http.GetFromJsonAsync<QuoteResponse>(
            "https://api.example.com/quotes/random");

        return response is not null
            ? [new QuoteLoaded(new Quote(response.Content, response.Author))]
            : [new QuoteFailed("No quote returned")];
    }

    private record QuoteResponse(string Content, string Author);
}
```

**Key patterns:**

- The interpreter pattern-matches on command types, just like `Transition` matches on messages
- It wraps everything in a `try/catch` — the interpreter is the boundary where errors from the outside world are caught
- It returns `Result<Message[], PipelineError>.Ok(messages)` — the runtime feeds these messages back into `Transition`
- Unknown commands return an empty array (no messages)

### Wiring It Up

Pass the interpreter to the runtime when starting the application:

```csharp
using QuoteViewer;

await Abies.Browser.Runtime.Run<QuoteApp, Model, Unit>(
    interpreter: QuoteInterpreter.Interpret);
```

The `interpreter` parameter is optional. If omitted, a no-op interpreter is used (returns `Ok([])` for all commands). This is fine for apps like the counter that have no side effects.

## The Command Pipeline

Here's how commands flow through the system:

```
┌──────────────┐     ┌─────────────┐     ┌───────────────┐
│  Transition  │───▶│  Command    │───▶│  Interpreter   │
│              │     │  (data)     │     │               │
│ returns      │     │ FetchQuote  │     │ HTTP GET      │
│ (model, cmd) │     │             │     │ → Message[]   │
└──────────────┘     └─────────────┘     └───────┬───────┘
      ▲                                       │
      │              Messages                 │
      └──────────────[QuoteLoaded]──────────┘
```

1. `Transition` returns `(newModel, new FetchQuote())`
2. The runtime passes `FetchQuote` to the interpreter
3. The interpreter makes the HTTP call and returns `[new QuoteLoaded(quote)]`
4. The runtime feeds `QuoteLoaded` back into `Transition`
5. `Transition` returns `(model with quote, Commands.None)` — cycle complete

## Batching Commands

Sometimes a single transition needs to trigger multiple side effects:

```csharp
// In Transition:
InitialLoad =>
    (model with { IsLoading = true },
     Commands.Batch(
         new FetchQuote(),
         new FetchCategories(),
         new LogAnalytics("page_loaded")
     ))
```

`Commands.Batch(...)` combines multiple commands. The runtime sends each one to the interpreter. `Commands.None` is the identity element — batching with `None` has no effect.

> **Principle:** Commands form a [Monoid](https://en.wikipedia.org/wiki/Monoid) with `Commands.None` as the identity and `Commands.Batch` as the binary operation. This algebraic structure means commands compose naturally: you can combine any number of commands without special cases for zero, one, or many.

## Handling Multiple Command Types

As your app grows, the interpreter handles more command types:

```csharp
public static async ValueTask<Result<Message[], PipelineError>> Interpret(
    Command command)
{
    try
    {
        Message[] messages = command switch
        {
            FetchQuote => await HandleFetchQuote(),
            FetchCategories => await HandleFetchCategories(),
            SaveFavorite cmd => await HandleSaveFavorite(cmd),
            LogAnalytics cmd => await HandleLogAnalytics(cmd),
            _ => []
        };

        return Result<Message[], PipelineError>.Ok(messages);
    }
    catch (Exception ex)
    {
        return Result<Message[], PipelineError>.Ok(
            [new ApiError(ex.Message)]);
    }
}
```

## Testing

### Testing Transition (Pure)

The transition function requires no mocks:

```csharp
[Fact]
public void FetchNewQuote_SetsLoadingState_AndReturnsCommand()
{
    var model = new Model(
        CurrentQuote: new Quote("old", "author"),
        IsLoading: false,
        Error: null);

    var (newModel, command) = QuoteApp.Transition(model, new FetchNewQuote());

    Assert.True(newModel.IsLoading);
    Assert.Null(newModel.Error);
    Assert.IsType<FetchQuote>(command);
}

[Fact]
public void QuoteLoaded_StoresQuote_AndClearsLoading()
{
    var model = new Model(null, true, null);
    var quote = new Quote("To be or not to be", "Shakespeare");

    var (newModel, command) = QuoteApp.Transition(
        model, new QuoteLoaded(quote));

    Assert.Equal(quote, newModel.CurrentQuote);
    Assert.False(newModel.IsLoading);
    Assert.Equal(Commands.None, command);
}

[Fact]
public void QuoteFailed_StoresError_AndClearsLoading()
{
    var model = new Model(null, true, null);

    var (newModel, _) = QuoteApp.Transition(
        model, new QuoteFailed("Network timeout"));

    Assert.False(newModel.IsLoading);
    Assert.Equal("Network timeout", newModel.Error);
}
```

### Testing the Interpreter

Interpreter tests verify HTTP behavior using a fake handler:

```csharp
[Fact]
public async Task Interpret_FetchQuote_ReturnsQuoteLoaded()
{
    // Arrange: mock HTTP response
    var handler = new FakeHttpHandler(new QuoteResponse(
        "To be or not to be", "Shakespeare"));

    var interpreter = CreateInterpreter(handler);

    // Act
    var result = await interpreter(new FetchQuote());

    // Assert
    var messages = result.Match(ok => ok, _ => []);
    var loaded = Assert.IsType<QuoteLoaded>(Assert.Single(messages));
    Assert.Equal("Shakespeare", loaded.Quote.Author);
}
```

## Why This Architecture?

The command/interpreter split provides several benefits:

| Benefit | How |
| --- | --- |
| **Testability** | Transition tests need no mocks; interpreter tests mock only HTTP |
| **Replaceability** | Swap interpreters for testing, different platforms, or staging vs. production |
| **Visibility** | Every side effect is visible as a command in the Transition return value |
| **Time-travel debugging** | Record commands alongside messages for full replay |
| **Composition** | `Commands.Batch` combines effects; `Commands.None` is the identity |

## Real-World Example: The Conduit Interpreter

The Conduit demo application (a Medium.com clone) uses this pattern at scale. Its interpreter handles 15+ command types:

```csharp
// Simplified from Abies.Conduit.App/Interpreter.cs
public static async ValueTask<Result<Message[], PipelineError>> Interpret(
    Command command)
{
    try
    {
        Message[] messages = command switch
        {
            FetchArticles cmd => await HandleFetchArticles(cmd),
            FetchFeed cmd => await HandleFetchFeed(cmd),
            LoginUser cmd => await HandleLogin(cmd),
            RegisterUser cmd => await HandleRegister(cmd),
            FavoriteArticle cmd => await HandleFavorite(cmd),
            // ... 10+ more command types
            _ => []
        };

        return Result<Message[], PipelineError>.Ok(messages);
    }
    catch (Exception ex)
    {
        return Result<Message[], PipelineError>.Ok(
            [new ApiError([$"Network error: {ex.Message}"])]);
    }
}
```

See [Tutorial 7: Real-World App](07-real-world-app.md) for the complete walkthrough.

## Exercises

1. **Add retry logic** — When a fetch fails, show a "Retry" button that dispatches `FetchNewQuote` again. Add a retry count to the model.

2. **Add a favorites list** — Let users save quotes to a favorites list. Use a `SaveFavorite(Quote)` command and have the interpreter store it in `localStorage`.

3. **Add categories** — Fetch a list of categories from the API and let the user filter quotes by category. Use `Commands.Batch` to fetch both categories and a quote on startup.

4. **Add an offline fallback** — If the API call fails, return a hardcoded quote from the interpreter instead of an error.

## Key Concepts

| Concept | In This Tutorial |
| --- | --- |
| Command | `record FetchQuote : Command` — describes a side effect |
| Interpreter | `Interpret(Command) → Result<Message[], PipelineError>` |
| Commands.None | No side effect needed |
| Commands.Batch | Combine multiple commands |
| Result type | Success returns messages; errors are caught in interpreter |
| Loading state | `IsLoading` flag for UI feedback |

## Next Steps

→ [Tutorial 4: Routing](04-routing.md) — Learn client-side navigation and URL handling