# MVU Architecture

Model-View-Update (MVU) is the core architectural pattern in Abies. This document explains the pattern deeply and why it leads to reliable, testable applications.

## What is MVU?

MVU is a unidirectional data flow architecture where:

1. **Model** — The complete application state
2. **View** — A pure function that renders state to UI
3. **Update (Transition)** — A pure function that handles state transitions

```text
┌──────────────────────────────────────────────────────────────┐
│                         MVU Loop                             │
│                                                              │
│   ┌─────────┐    dispatch    ┌────────────┐   ┌─────────┐   │
│   │  View   │ ──────────────▶│ Transition │──▶│  Model  │   │
│   └─────────┘   (Message)    └────────────┘   └─────────┘   │
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
- **Abies** — .NET (browser + server)

## The Picea Kernel Foundation

Abies is built on the [Picea kernel](https://github.com/picea/picea), a generic state machine runtime. The kernel provides the `Automaton` abstraction — a pure transition function with side effects modeled as return values:

```csharp
// Automaton kernel type mapping to MVU:
//   Automaton<TState,  TEvent,  TEffect,  TParameters>
//       ≡     <TModel, Message, Command,  TArgument>
```

Abies extends the kernel with two MVU-specific capabilities:

| Kernel Concept | MVU Equivalent | Purpose |
| -------------- | -------------- | ------- |
| `TState` | `TModel` | Application state |
| `TEvent` | `Message` | User/system events |
| `TEffect` | `Command` | Side effects (described, not performed) |
| `TParameters` | `TArgument` | Initialization parameters |
| Observer | Render pipeline | View → Diff → Apply → Subscriptions |
| Interpreter | Command handler | Executes side effects, returns feedback |

This separation means the MVU loop is **platform-agnostic** — the same `Program` implementation runs identically in the browser (WASM) or on the server.

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

### 3. Transition (Update)

Transition is a pure function that takes a message and the current model, returning a new model and a command.

```csharp
public static (Model, Command) Transition(Model model, Message message)
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

> **Note:** In the Picea kernel, this function is called `Transition` (the Automaton term). MVU literature often calls it "Update" — they are the same concept.

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
- **Record structs for hot-path messages** — Avoids heap allocation

```csharp
// Prefer record struct for frequently dispatched messages
public interface CounterMessage : Message
{
    record struct Increment : CounterMessage;
    record struct Decrement : CounterMessage;
}
```

## Commands

Commands describe side effects to be performed by the runtime's interpreter.

```csharp
public record FetchArticles : Command;
public record SaveDraft(Article Article) : Command;
public record NavigateTo(Url Url) : Command;
```

The interpreter executes commands and returns feedback messages:

```csharp
Interpreter<Command, Message> interpreter = async command =>
{
    switch (command)
    {
        case FetchArticles:
            try
            {
                var articles = await api.GetArticles();
                return Result<Message[], PipelineError>.Ok([new ArticlesLoaded(articles)]);
            }
            catch (Exception ex)
            {
                return Result<Message[], PipelineError>.Ok([new LoadFailed(ex.Message)]);
            }
        default:
            return Result<Message[], PipelineError>.Ok([]);
    }
};
```

## The Program Interface

A complete Abies application implements the `Program<TModel, TArgument>` interface:

```csharp
public interface Program<TModel, TArgument> : Automaton<TModel, Message, Command, TArgument>
{
    // From Automaton:
    // static abstract (TModel, Command) Initialize(TArgument argument);
    // static abstract (TModel, Command) Transition(TModel model, Message message);

    // MVU extensions:
    static abstract Document View(TModel model);
    static abstract Subscription Subscriptions(TModel model);
}
```

### Example: Counter

```csharp
using Abies;
using Abies.DOM;
using Abies.Subscriptions;
using Automaton;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

public record CounterModel(int Count);

public interface CounterMessage : Message
{
    record struct Increment : CounterMessage;
    record struct Decrement : CounterMessage;
}

public class Counter : Program<CounterModel, Unit>
{
    public static (CounterModel, Command) Initialize(Unit _) =>
        (new CounterModel(0), Commands.None);

    public static (CounterModel, Command) Transition(CounterModel model, Message message) =>
        message switch
        {
            CounterMessage.Increment => (model with { Count = model.Count + 1 }, Commands.None),
            CounterMessage.Decrement => (model with { Count = model.Count - 1 }, Commands.None),
            _ => (model, Commands.None)
        };

    public static Document View(CounterModel model) =>
        new("Counter",
            div([], [
                button([onclick(() => new CounterMessage.Decrement())], [text("-")]),
                text(model.Count.ToString()),
                button([onclick(() => new CounterMessage.Increment())], [text("+")])
            ]));

    public static Subscription Subscriptions(CounterModel model) =>
        SubscriptionModule.None;
}
```

## The Runtime Loop

The Abies runtime orchestrates the MVU loop by composing the Picea kernel's `AutomatonRuntime` with rendering:

```text
1. Initialize model via Program.Initialize(argument)
2. Render initial view via Program.View(model)
3. Diff against empty → initial patches (AddRoot)
4. Apply patches to platform (browser DOM or server transport)
5. Start initial subscriptions via Program.Subscriptions(model)
6. Interpret initial command (may produce feedback messages)
───── loop ─────
7. User interacts → Message dispatched
8. Call Program.Transition(model, message) → (newModel, command)
9. Render new view → Diff → Patches
10. Apply patches to platform
11. Update subscriptions
12. Interpret command → feedback messages → loop
```

### The Apply Delegate

The `Apply` delegate is the seam between the pure Abies core and platform-specific rendering:

```csharp
public delegate void Apply(IReadOnlyList<Patch> patches);
```

| Platform | Apply Implementation |
| -------- | -------------------- |
| **Browser** (`Abies.Browser`) | Binary batch → JS interop → DOM mutations |
| **Server** (`Abies.Server`) | Binary batch → WebSocket → client-side replay |
| **Tests** | Captures patches for assertions |

This architecture means the same `Program` runs identically across all platforms — the only difference is how patches reach the user's screen.

### Running in the Browser

```csharp
// Entire Program.cs:
await Abies.Browser.Runtime.Run<Counter, CounterModel, Unit>();
```

### Running on the Server

```csharp
// In server startup (e.g., Kestrel):
app.MapAbies<Counter, CounterModel, Unit>("/counter",
    renderMode: RenderMode.InteractiveServer("/ws/counter"),
    interpreter: myInterpreter);
```

## Render Modes

Abies supports four render modes, all executing the same `Program` implementation:

| Mode | Description | Interactivity |
| ---- | ----------- | ------------- |
| `Static` | Server renders HTML once, no JS | None |
| `InteractiveServer` | Server holds MVU session, patches over WebSocket | Full (server-driven) |
| `InteractiveWasm` | Server renders initial HTML, then WASM takes over | Full (client-driven) |
| `InteractiveAuto` | Starts as server, transitions to WASM when ready | Full (hybrid) |

See [Render Modes](./render-modes.md) for detailed documentation.

## Why MVU?

### Predictability

State changes are explicit and traceable. Every state transition goes through Transition with a message.

### Testability

Pure functions are trivially testable:

```csharp
[Fact]
public void ArticlesLoaded_UpdatesModelAndClearsLoading()
{
    var model = new Model(IsLoading: true, Articles: [], Error: null);
    var articles = new List<Article> { new("test", "Test Article") };

    var (newModel, command) = Transition(model, new ArticlesLoaded(articles));

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
- **OpenTelemetry** — Built-in tracing with `Abies.Runtime` activity source

### Simplicity

One mental model for everything:

- User clicks button → Message → Transition → View
- API returns data → Message → Transition → View
- Timer fires → Message → Transition → View
- WebSocket message → Message → Transition → View

## Comparison with Other Patterns

| Aspect | MVU | MVVM | MVC | Blazor Components |
| ------ | --- | ---- | --- | ----------------- |
| Data flow | Unidirectional | Bidirectional | Bidirectional | Mixed |
| State location | Single model | ViewModels | Controllers + Models | Per-component |
| Side effects | Explicit (Commands) | Mixed in VMs | Mixed in Controllers | Mixed in lifecycle |
| Testability | Excellent | Good | Moderate | Moderate |
| Render modes | All four | N/A | N/A | All (Blazor) |
| Platform agnostic | Yes (Apply delegate) | No | No | Partial |

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

### Delegated Transitions

Route messages to sub-updaters:

```csharp
public static (AppModel, Command) Transition(AppModel model, Message msg)
    => msg switch
    {
        HomeMessage homeMsg when model.Page is HomeModel home =>
            TransitionHome(homeMsg, home, model),

        ProfileMessage profileMsg when model.Page is ProfileModel profile =>
            TransitionProfile(profileMsg, profile, model),

        _ => (model, Commands.None)
    };
```

## Best Practices

1. **Keep models flat when possible** — Deep nesting adds complexity
2. **Use records** — Immutability and equality for free
3. **Use record structs for hot-path messages** — Avoid heap allocation
4. **Handle all message cases** — Use exhaustive pattern matching
5. **Return Commands.None explicitly** — Makes no-effect cases clear
6. **Name messages in past tense** — They describe what happened
7. **Keep View functions small** — Extract helper functions liberally
8. **Use `lazy()` for list items** — Enables memo key comparison to skip diffing

## See Also

- [Pure Functions](./pure-functions.md) — Why purity matters
- [Commands and Effects](./commands-effects.md) — Side effect handling
- [Virtual DOM](./virtual-dom.md) — How rendering works
- [Render Modes](./render-modes.md) — Browser, server, and hybrid execution
- [Subscriptions](./subscriptions.md) — External event sources
