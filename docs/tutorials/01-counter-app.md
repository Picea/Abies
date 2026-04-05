# Tutorial 1: Counter App

Build a counter application from scratch to learn the fundamentals of Abies's Model-View-Update (MVU) architecture.

**Prerequisites:** [Installation](../getting-started/installation.md) complete

**Time:** 15 minutes

**What you'll learn:**

- The four-function `Program` interface
- Immutable models with `record` types
- Messages as discriminated unions
- The `Transition` function (pure state machine)
- Virtual DOM rendering with `View`
- Running an Abies program in the browser

## Create the Project

```bash
# Create a new class library for the program logic
dotnet new classlib -n MyCounter
cd MyCounter

# Add the Abies framework reference
dotnet add package Picea.Abies --prerelease
```

## Step 1: Define the Model

The **model** is the entire state of your application. For a counter, that's a single integer:

```csharp
using Picea.Abies;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using Automaton;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Events;

namespace MyCounter;

/// <summary>
/// The complete application state — just a count.
/// </summary>
public record Model(int Count);
```

**Key points:**

- Models are **immutable** C# `record` types
- Use `with` expressions to create updated copies: `model with { Count = 5 }`
- The model is the **single source of truth** — there is no other mutable state

## Step 2: Define the Messages

Messages describe **what happened**. They are events, not commands — they say "the user clicked increment", not "please add one".

```csharp
/// <summary>All messages implement the Abies Message marker interface.</summary>
public interface CounterMessage : Message;

/// <summary>The user clicked the increment button.</summary>
public record Increment : CounterMessage;

/// <summary>The user clicked the decrement button.</summary>
public record Decrement : CounterMessage;

/// <summary>The user clicked the reset button.</summary>
public record Reset : CounterMessage;
```

**Key points:**

- Messages implement `Message` (or a sub-interface for organization)
- Each message is an immutable `record` — no behavior, just data
- Name messages after what happened, not what should happen

## Step 3: Write the Transition Function

The `Transition` function is the heart of MVU. Given the current model and a message, it returns the new model and optionally a command to execute:

```csharp
public sealed class Counter : Program<Model, Unit>
{
    public static (Model, Command) Initialize(Unit argument) =>
        (new Model(Count: 0), Commands.None);

    public static (Model, Command) Transition(Model model, Message message) =>
        message switch
        {
            Increment => (model with { Count = model.Count + 1 }, Commands.None),
            Decrement => (model with { Count = model.Count - 1 }, Commands.None),
            Reset     => (model with { Count = 0 }, Commands.None),
            _         => (model, Commands.None)
        };
```

**Key points:**

- `Transition` is a **pure function** — no side effects, no I/O, no mutation
- It returns a tuple of `(newModel, command)` — use `Commands.None` when there's no side effect
- Pattern matching on messages makes the state machine exhaustive and readable
- The `_` catch-all handles framework messages (like navigation) that this program doesn't need

> **Why "Transition" instead of "Update"?** Abies models your program as a *state machine* (an [Automaton](https://en.wikipedia.org/wiki/Finite-state_machine)). In automata theory, the function that takes `(state, input) → (state, output)` is called a *transition function*. This naming makes the formal semantics explicit.

## Step 4: Write the View

The `View` function renders the model as a virtual DOM tree. It's called after every state transition:

```csharp
    public static Document View(Model model) =>
        new("Counter",
            div([class_("counter")],
            [
                h1([], [text("Abies Counter")]),
                div([class_("controls")],
                [
                    button([class_("btn"), onclick(new Decrement())], [text("−")]),
                    span([class_("count")], [text(model.Count.ToString())]),
                    button([class_("btn"), onclick(new Increment())], [text("+")])
                ]),
                button([class_("reset"), onclick(new Reset())], [text("Reset")])
            ]));
```

**Key points:**

- `View` returns a `Document(title, body)` — the title updates the browser tab
- HTML elements are plain C# function calls: `div(attributes, children)`
- Static imports (`using static Abies.Html.Elements`) keep the syntax clean
- `onclick(new Increment())` attaches a message to the click event — when the button is clicked, this message flows into `Transition`
- The view is a **pure function of the model** — same model always produces the same DOM

## Step 5: Declare Subscriptions

Subscriptions connect your program to external event sources (timers, WebSockets, etc.). Our counter doesn't need any:

```csharp
    public static Subscription Subscriptions(Model model) =>
        SubscriptionModule.None;
}
```

## The Complete Program

Here's the full counter in one file:

```csharp
using Picea.Abies;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using Automaton;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Events;

namespace MyCounter;

public record Model(int Count);

public interface CounterMessage : Message;
public record Increment : CounterMessage;
public record Decrement : CounterMessage;
public record Reset : CounterMessage;

public sealed class Counter : Program<Model, Unit>
{
    public static (Model, Command) Initialize(Unit argument) =>
        (new Model(Count: 0), Commands.None);

    public static (Model, Command) Transition(Model model, Message message) =>
        message switch
        {
            Increment => (model with { Count = model.Count + 1 }, Commands.None),
            Decrement => (model with { Count = model.Count - 1 }, Commands.None),
            Reset     => (model with { Count = 0 }, Commands.None),
            _         => (model, Commands.None)
        };

    public static Document View(Model model) =>
        new("Counter",
            div([class_("counter")],
            [
                h1([], [text("Abies Counter")]),
                div([class_("controls")],
                [
                    button([class_("btn"), onclick(new Decrement())], [text("−")]),
                    span([class_("count")], [text(model.Count.ToString())]),
                    button([class_("btn"), onclick(new Increment())], [text("+")])
                ]),
                button([class_("reset"), onclick(new Reset())], [text("Reset")])
            ]));

    public static Subscription Subscriptions(Model model) =>
        SubscriptionModule.None;
}
```

## Step 6: Create a Browser Host

The program logic is platform-independent. To run it in the browser, create a WASM host:

```bash
cd ..
dotnet new web -n MyCounter.Wasm
cd MyCounter.Wasm
dotnet add reference ../MyCounter/MyCounter.csproj
dotnet add package Picea.Abies.Browser --prerelease
```

Replace `Program.cs` with:

```csharp
using MyCounter;

await Picea.Abies.Browser.Runtime.Run<Counter, Model, Unit>();
```

That's it — one line. The runtime:

1. Calls `Initialize` to get the initial model
2. Calls `View` to render the initial DOM
3. Listens for user events (clicks, inputs, etc.)
4. Dispatches messages to `Transition` when events occur
5. Diffs the old and new virtual DOM trees
6. Applies the minimal set of patches to the real DOM
7. Repeats from step 3

## Step 7: Add the HTML Shell

Create `wwwroot/index.html`:

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <title>Abies Counter</title>
    <style>
        body { font-family: system-ui; display: flex; justify-content: center; padding: 4rem; }
        .counter { text-align: center; }
        .controls { display: flex; align-items: center; gap: 1rem; margin: 2rem 0; }
        .btn { font-size: 1.5rem; width: 3rem; height: 3rem; cursor: pointer; }
        .count { font-size: 3rem; min-width: 4rem; }
        .reset { cursor: pointer; padding: 0.5rem 1rem; }
    </style>
</head>
<body>
    <div id="app"></div>
    <script src="_framework/dotnet.js"></script>
</body>
</html>
```

## Step 8: Run It

```bash
dotnet run
```

Open the URL shown in the terminal. You should see a counter with increment, decrement, and reset buttons.

## Understanding the Data Flow

Every interaction follows the same cycle:

```
┌─────────────────────────────────────────────────┐
│                                                 │
│   User clicks "+"                               │
│        │                                        │
│        ▼                                        │
│   onclick(new Increment())                      │
│        │                                        │
│        ▼                                        │
│   Transition(Model(Count: 3), Increment)        │
│        │                                        │
│        ▼                                        │
│   returns (Model(Count: 4), Commands.None)      │
│        │                                        │
│        ▼                                        │
│   View(Model(Count: 4))                         │
│        │                                        │
│        ▼                                        │
│   Diff old DOM ↔ new DOM                        │
│        │                                        │
│        ▼                                        │
│   Apply patches (update "3" → "4")              │
│                                                 │
└─────────────────────────────────────────────────┘
```

This cycle is the same for every Abies application, no matter how complex. The only things that change are the model, messages, and view.

## Testing the Transition Function

Because `Transition` is a pure function, it's trivially testable:

```csharp
[Fact]
public void Increment_IncreasesCount()
{
    var model = new Model(Count: 5);

    var (newModel, command) = Counter.Transition(model, new Increment());

    Assert.Equal(6, newModel.Count);
    Assert.Equal(Commands.None, command);
}

[Fact]
public void Reset_SetsCountToZero()
{
    var model = new Model(Count: 42);

    var (newModel, _) = Counter.Transition(model, new Reset());

    Assert.Equal(0, newModel.Count);
}
```

No mocks, no setup, no teardown. Pure functions are the easiest code to test.

## Exercises

1. **Add a step size** — Add an input field that lets the user choose how much to increment/decrement by. You'll need a new message for the input change and an `oninput` event handler.

2. **Add bounds** — Prevent the counter from going below 0 or above 100. Handle this in `Transition` by returning the unchanged model when the limit is reached.

3. **Add keyboard shortcuts** — Use `SubscriptionModule.Create` to listen for keyboard events (you'll learn more about subscriptions in [Tutorial 6](06-subscriptions.md)).

## Key Concepts

| Concept | In This Tutorial |
| --- | --- |
| Model | `record Model(int Count)` — immutable state |
| Message | `Increment`, `Decrement`, `Reset` — what happened |
| Transition | Pattern match → new model + command |
| View | Pure function → virtual DOM tree |
| Program | `Program<Model, Unit>` — the four-function contract |
| Commands.None | No side effects needed |

## Next Steps

→ [Tutorial 2: Todo List](02-todo-list.md) — Learn about lists, text input, and keyed rendering