# Program Interface

The `Program<TModel, TArgument>` interface is the compile-time contract for an Abies MVU application. It is a strict decider contract: commands are validated through `Decide`, state evolution happens through `Transition`, and runtime termination is defined by `IsTerminal`.

## Type Signature

```csharp
public interface Program<TModel, TArgument> : Decider<TModel, Message, Message, Command, Message, TArgument>
{
    static abstract Result<Message[], Message> Decide(TModel state, Message command);
    static abstract bool IsTerminal(TModel state);
    static abstract Document View(TModel model);
    static abstract Subscription Subscriptions(TModel model);
}
```

## Decider Kernel Mapping

The `Program` interface inherits the decider core members and adds MVU-specific rendering/subscription members:

| Decider | MVU | Member | Signature |
|-----------|-----|--------|-----------|
| `TState` | `TModel` | — | The application state |
| `TCommand` | `Message` | — | User/system commands fed into `Decide` |
| `TEvent` | `Message` | — | Events produced by `Decide` and consumed by `Transition` |
| `TEffect` | `Command` | — | Side effects |
| `TError` | `Message` | — | Command rejection type |
| `TParameters` | `TArgument` | — | Initialization parameters |
| `Initialize` | `Initialize` | From kernel | `static (TModel, Command) Initialize(TArgument argument)` |
| `Transition` | `Transition` | From kernel | `static (TModel, Command) Transition(TModel model, Message message)` |
| `Decide` | `Decide` | From kernel | `static Result<Message[], Message> Decide(TModel state, Message command)` |
| `IsTerminal` | `IsTerminal` | From kernel | `static bool IsTerminal(TModel state)` |
| — | `View` | MVU-specific | `static Document View(TModel model)` |
| — | `Subscriptions` | MVU-specific | `static Subscription Subscriptions(TModel model)` |

The interface has **six static members in practice**: four from decider semantics (`Initialize`, `Decide`, `Transition`, `IsTerminal`) and two MVU-specific members (`View`, `Subscriptions`).

## Members

### Initialize

```csharp
static (TModel, Command) Initialize(TArgument argument)
```

Creates the initial model and optionally returns an initial command. Called once when the runtime starts.

- **`argument`** — Initialization parameters. Use `Unit` for parameterless applications.
- **Returns** — A tuple of the initial model and an initial command (use `Commands.None` for no initial side effects).

> **Navigation note:** The runtime dispatches `UrlChanged(initialUrl)` as the first message *after* initialization. Applications do not receive the initial URL in `Initialize` — they handle it in `Transition` like any other URL change.

### Transition

```csharp
static (TModel, Command) Transition(TModel model, Message message)
```

The pure state transition function. Given the current model and a message, produces a new model and optionally a command.

- **`model`** — The current application state.
- **`message`** — The event to process (user interaction, subscription event, URL change, etc.).
- **Returns** — A tuple of the new model and a command to execute.

This function must be **pure** — no side effects, no mutation. All side effects are expressed as `Command` values that the runtime's interpreter executes.

### Decide

```csharp
static Result<Message[], Message> Decide(TModel state, Message command)
```

Validates and maps an incoming command to zero or more events.

- **`state`** — Current application state.
- **`command`** — Incoming command message.
- **Returns** — `Ok(events)` when accepted, `Err(message)` when rejected.

Abies runtime processes commands through `Decide` before any transition runs.

### IsTerminal

```csharp
static bool IsTerminal(TModel state)
```

Declares whether the runtime should stop handling incoming commands for the current state.

- **`state`** — Current application state.
- **Returns** — `true` when command handling should stop; otherwise `false`.

### View

```csharp
static Document View(TModel model)
```

Renders the current model as a virtual DOM `Document`. Called after every state transition.

- **`model`** — The current application state.
- **Returns** — A `Document` describing the desired UI.

This function must be **pure**. The runtime diffs the returned document against the previous one and applies the minimal set of patches to the actual DOM.

### Subscriptions

```csharp
static Subscription Subscriptions(TModel model)
```

Declares the external event sources the application wants to listen to. Called after every state transition.

- **`model`** — The current application state.
- **Returns** — A `Subscription` describing the desired set of active subscriptions.

Subscriptions are **declarative** — the runtime's `SubscriptionManager` handles the lifecycle (starting new subscriptions, keeping unchanged ones, stopping removed ones). Return `SubscriptionModule.None` when the application has no subscriptions in its current state.

## Type Parameters

| Parameter | Description |
|-----------|-------------|
| `TModel` | The application model (state). Typically an immutable record. |
| `TArgument` | Initialization parameters. Use `Unit` for parameterless applications. |

## Complete Example

```csharp
using Picea.Abies;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using Picea;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Events;

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

    public static Result<Message[], Message> Decide(CounterModel _, Message command) =>
        Result<Message[], Message>.Ok([command]);

    public static bool IsTerminal(CounterModel _) => false;

    public static Document View(CounterModel model) =>
        new("Counter",
            div([], [
                button([onclick(new CounterMessage.Decrement())], [text("-")]),
                text(model.Count.ToString()),
                button([onclick(new CounterMessage.Increment())], [text("+")])
            ]));

    public static Subscription Subscriptions(CounterModel model) =>
        SubscriptionModule.None;
}
```

## Running a Program

### Browser (WASM)

```csharp
await Picea.Abies.Browser.Runtime.Run<Counter, CounterModel, Unit>();
```

### Server (ASP.NET Core)

```csharp
app.MapAbies<Counter, CounterModel, Unit>("/", new RenderMode.InteractiveServer());
```

See [Runtime](runtime.md) for the full API reference of both hosting models.

## Design Principles

### Pure Functions Only

All six members are pure functions. Side effects are expressed as `Command` values returned from `Initialize` and `Transition`. The runtime's interpreter executes commands and feeds any resulting messages back into the loop.

### Navigation is a Message

URL changes and link clicks are modeled as regular `Message` types (`UrlChanged`, `UrlRequest`) rather than dedicated interface members. Applications opt into navigation by handling these messages in `Transition`. This follows the Open/Closed Principle — the `Program` interface is closed for modification but open for extension via new message types.

### Static Abstract Members

All members are `static abstract`, meaning the program type itself is the capability — no instance is needed. This enables the runtime to call program functions directly via the type parameter constraint, with zero allocation overhead.

## See Also

- [Command](command.md) — Side effects returned from `Initialize` and `Transition`
- [Message](message.md) — Events processed by `Transition`
- [Subscription](subscription.md) — External event sources declared by `Subscriptions`
- [DOM Types](dom-types.md) — The `Document` and `Node` types returned by `View`
- [Runtime](runtime.md) — How the runtime executes the MVU loop
