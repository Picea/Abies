# Program Interface

The `Program<TModel, TArgument>` interface is the compile-time contract that defines an Abies MVU application. It extends the Automaton kernel's transition function with two MVU-specific capabilities: rendering views and declaring subscriptions.

## Type Signature

```csharp
public interface Program<TModel, TArgument> : Automaton<TModel, Message, Command, TArgument>
{
    static abstract Document View(TModel model);
    static abstract Subscription Subscriptions(TModel model);
}
```

## Automaton Kernel Mapping

The `Program` interface inherits two members from the `Automaton<TState, TEvent, TEffect, TParameters>` kernel and adds two MVU-specific members:

| Automaton | MVU | Member | Signature |
|-----------|-----|--------|-----------|
| `TState` | `TModel` | — | The application state |
| `TEvent` | `Message` | — | User and system events |
| `TEffect` | `Command` | — | Side effects |
| `TParameters` | `TArgument` | — | Initialization parameters |
| `Initialize` | `Initialize` | From kernel | `static (TModel, Command) Initialize(TArgument argument)` |
| `Transition` | `Transition` | From kernel | `static (TModel, Command) Transition(TModel model, Message message)` |
| — | `View` | MVU-specific | `static Document View(TModel model)` |
| — | `Subscriptions` | MVU-specific | `static Subscription Subscriptions(TModel model)` |

The interface has exactly **four static abstract members** — two inherited from the Automaton kernel (`Initialize`, `Transition`) and two defined directly (`View`, `Subscriptions`).

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
await Abies.Browser.Runtime.Run<Counter, CounterModel, Unit>();
```

### Server (ASP.NET Core)

```csharp
app.MapAbies<Counter, CounterModel, Unit>("/", new RenderMode.InteractiveServer());
```

See [Runtime](runtime.md) for the full API reference of both hosting models.

## Design Principles

### Pure Functions Only

All four members are pure functions. Side effects are expressed as `Command` values returned from `Initialize` and `Transition`. The runtime's interpreter executes commands and feeds any resulting messages back into the loop.

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
