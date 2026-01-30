# MVU Architecture

Model-View-Update (MVU) is the core architectural pattern in Abies. This document explains the pattern deeply and why it leads to reliable, testable applications.

## What is MVU?

MVU is a unidirectional data flow architecture where:

1. **Model** — The complete application state
2. **View** — A pure function that renders state to UI
3. **Update** — A pure function that handles state transitions

```text
┌──────────────────────────────────────────────────────────────┐
│                         MVU Loop                             │
│                                                              │
│   ┌─────────┐    dispatch    ┌─────────┐      ┌─────────┐   │
│   │  View   │ ──────────────▶│ Update  │─────▶│  Model  │   │
│   └─────────┘   (Message)    └─────────┘      └─────────┘   │
│        ▲                                           │         │
│        │                                           │         │
│        └───────────────────────────────────────────┘         │
│                        (new Model)                           │
└──────────────────────────────────────────────────────────────┘
```

## Origins

MVU originated in Elm, a functional programming language for web applications. The pattern has proven so effective that it's been adopted by many frameworks:

- **Elm** — Original implementation
- **Redux** — JavaScript (inspired by Elm)
- **SwiftUI** — Apple platforms
- **Jetpack Compose** — Android
- **Abies** — .NET WebAssembly

## The Three Components

### 1. Model

The model is a single, immutable data structure containing all application state.

```csharp
public record Model(
    User? CurrentUser,
    List<Article> Articles,
    bool IsLoading,
    string? Error,
    Route CurrentRoute
);
```

**Key properties:**

- **Immutable** — Never mutated, always replaced
- **Single source of truth** — All UI state lives here
- **Plain data** — No behavior, just data
- **Serializable** — Can be saved/restored for time-travel debugging

### 2. View

The View is a pure function that transforms the model into a virtual DOM tree.

```csharp
public static Document View(Model model)
    => new("My App",
        div([], [
            model.IsLoading
                ? LoadingSpinner()
                : ArticleList(model.Articles),
            model.Error is not null
                ? ErrorBanner(model.Error)
                : text("")
        ]));
```

**Key properties:**

- **Pure** — Same model always produces same output
- **Declarative** — Describes what to render, not how
- **Composable** — Build complex views from simple functions
- **No side effects** — No API calls, no DOM manipulation

### 3. Update

Update is a pure function that takes a message and the current model, returning a new model.

```csharp
public static (Model model, Command command) Update(Message message, Model model)
    => message switch
    {
        ArticlesLoaded loaded => 
            (model with { Articles = loaded.Articles, IsLoading = false }, Commands.None),
        
        LoadFailed failed => 
            (model with { Error = failed.Message, IsLoading = false }, Commands.None),
        
        Refresh => 
            (model with { IsLoading = true }, new LoadArticlesCommand()),
        
        _ => (model, Commands.None)
    };
```

**Key properties:**

- **Pure** — No side effects, completely testable
- **Exhaustive** — Pattern matching ensures all cases handled
- **Returns commands** — Side effects requested, not performed

## Messages

Messages are the only way to change state. They represent events that occurred.

```csharp
// User actions
public record ButtonClicked : Message;
public record TextEntered(string Value) : Message;
public record FormSubmitted : Message;

// System events
public record DataLoaded(List<Item> Items) : Message;
public record RequestFailed(string Error) : Message;
public record TimerTicked : Message;
```

**Design principles:**

- **Past tense naming** — Messages describe what happened
- **Minimal data** — Include only necessary information
- **Immutable** — Use records for automatic equality

## Commands

Commands describe side effects to be performed by the runtime.

```csharp
public record FetchArticles : Command;
public record SaveDraft(Article Article) : Command;
public record NavigateTo(Url Url) : Command;
```

The runtime executes commands and dispatches result messages:

```csharp
public static async Task HandleCommand(Command command, Func<Message, ValueTuple> dispatch)
{
    switch (command)
    {
        case FetchArticles:
            try
            {
                var articles = await api.GetArticles();
                dispatch(new ArticlesLoaded(articles));
            }
            catch (Exception ex)
            {
                dispatch(new LoadFailed(ex.Message));
            }
            break;
    }
}
```

## The Runtime Loop

Abies runtime orchestrates the MVU loop:

```text
1. Initialize model via Program.Init()
2. Render initial view via Program.View(model)
3. User interacts → Message dispatched
4. Call Program.Update(message, model) → (newModel, command)
5. If command exists, execute HandleCommand()
6. Diff old and new virtual DOM
7. Apply patches to browser DOM
8. Goto step 3
```

```csharp
Runtime.Run<Program>(browser, onError);
```

## Why MVU?

### Predictability

State changes are explicit and traceable. Every state transition goes through Update with a message.

### Testability

Pure functions are trivially testable:

```csharp
[Fact]
public void ArticlesLoaded_UpdatesModelAndClearsLoading()
{
    var model = new Model(IsLoading: true, Articles: [], Error: null);
    var articles = new List<Article> { new("test", "Test Article") };
    
    var (newModel, command) = Update(new ArticlesLoaded(articles), model);
    
    Assert.False(newModel.IsLoading);
    Assert.Equal(articles, newModel.Articles);
    Assert.Equal(Commands.None, command);
}
```

### Debuggability

With immutable state and explicit messages:

- **Time-travel debugging** — Replay message history
- **State snapshots** — Serialize any point in time
- **Action logging** — Record all messages for analysis

### Simplicity

One mental model for everything:

- User clicks button → Message → Update → View
- API returns data → Message → Update → View
- Timer fires → Message → Update → View

## Comparison with Other Patterns

| Aspect | MVU | MVVM | MVC |
| ------ | --- | ---- | --- |
| Data flow | Unidirectional | Bidirectional | Bidirectional |
| State location | Single model | ViewModels | Controllers + Models |
| Side effects | Explicit (Commands) | Mixed in VMs | Mixed in Controllers |
| Testability | Excellent | Good | Moderate |
| Complexity | Low | Medium | Medium-High |

## Scaling MVU

### Nested Models

For larger apps, compose models:

```csharp
public record AppModel(
    Route Route,
    User? User,
    PageModel Page
);

public interface PageModel { }
public record HomeModel(List<Article> Articles) : PageModel;
public record ProfileModel(UserProfile Profile) : PageModel;
```

### Nested Messages

Organize messages by feature:

```csharp
public interface Message : Abies.Message { }

public interface HomeMessage : Message
{
    public record ArticlesLoaded(List<Article> Articles) : HomeMessage;
    public record TagSelected(string Tag) : HomeMessage;
}

public interface ProfileMessage : Message
{
    public record ProfileLoaded(UserProfile Profile) : ProfileMessage;
    public record FollowClicked : ProfileMessage;
}
```

### Delegated Updates

Route messages to sub-updaters:

```csharp
public static (AppModel, Command) Update(Message msg, AppModel model)
    => msg switch
    {
        HomeMessage homeMsg when model.Page is HomeModel home =>
            UpdateHome(homeMsg, home, model),
        
        ProfileMessage profileMsg when model.Page is ProfileModel profile =>
            UpdateProfile(profileMsg, profile, model),
        
        _ => (model, Commands.None)
    };
```

## Best Practices

1. **Keep models flat when possible** — Deep nesting adds complexity
2. **Use records** — Immutability and equality for free
3. **Handle all message cases** — Use exhaustive pattern matching
4. **Return Commands.None explicitly** — Makes no-effect cases clear
5. **Name messages in past tense** — They describe what happened
6. **Keep View functions small** — Extract helper functions liberally

## See Also

- [Pure Functions](./pure-functions.md) — Why purity matters
- [Commands and Effects](./commands-effects.md) — Side effect handling
- [Virtual DOM](./virtual-dom.md) — How rendering works
