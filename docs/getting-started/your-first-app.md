# Your First App

In this tutorial, you'll build a counter application from scratch. This teaches the fundamental MVU (Model-View-Update) pattern that powers every Abies application.

## What You'll Build

A simple counter with:
- A display showing the current count
- Buttons to increment and decrement
- The complete MVU loop

## The MVU Pattern

Before coding, understand the three pillars:

```text
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│    ┌─────────┐     ┌────────┐     ┌──────┐                 │
│    │  Model  │────▶│  View  │────▶│ DOM  │                 │
│    └─────────┘     └────────┘     └──────┘                 │
│         ▲                              │                    │
│         │                              │ User clicks        │
│         │                              ▼                    │
│    ┌─────────┐                   ┌─────────┐               │
│    │ Update  │◀──────────────────│ Message │               │
│    └─────────┘                   └─────────┘               │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

1. **Model**: Your application state (immutable record)
2. **View**: A pure function that renders Model → DOM
3. **Update**: A pure function that handles Message → (Model, Command)

## Step 1: Create the Project

Create a new console project:

```bash
mkdir MyCounter
cd MyCounter
dotnet new console
dotnet add package Abies
```

## Step 2: Define the Model

The model is your application state. For a counter, we need one number:

```csharp
public record Model(int Count);
```

That's it! The model is an immutable record. When the count changes, we create a new Model rather than mutating this one.

## Step 3: Define Messages

Messages describe events that can change state. For a counter:

```csharp
public record Increment : Message;
public record Decrement : Message;
```

Messages are also immutable records. They implement `Abies.Message`.

## Step 4: Write the Update Function

Update is a pure function: given a message and the current model, return the new model and any commands to execute.

```csharp
public static (Model model, Command command) Update(Message message, Model model)
    => message switch
    {
        Increment => (model with { Count = model.Count + 1 }, Commands.None),
        Decrement => (model with { Count = model.Count - 1 }, Commands.None),
        _ => (model, Commands.None)
    };
```

Notice:
- We use `model with { ... }` to create a new model with updated values
- We return `Commands.None` because there are no side effects
- We use pattern matching to handle different message types

## Step 5: Write the View Function

View is a pure function: given the model, return a virtual DOM tree.

```csharp
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

public static Document View(Model model)
    => new("Counter",
        div([], [
            button([onclick(new Decrement())], [text("-")]),
            text(model.Count.ToString()),
            button([onclick(new Increment())], [text("+")])
        ]));
```

The view:
- Returns a `Document` with a title and body
- Uses helper functions (`div`, `button`, `text`) from `Abies.Html.Elements`
- Attaches click handlers that dispatch messages

## Step 6: Implement the Program Interface

Now wire everything together by implementing `Program<Model, Arguments>`:

```csharp
using Abies;
using Abies.DOM;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

// Start the runtime
await Runtime.Run<Counter, Arguments, Model>(new Arguments());

// Arguments passed to Initialize (none needed for this app)
public record Arguments;

// Application state
public record Model(int Count);

// Messages
public record Increment : Message;
public record Decrement : Message;

// The application
public class Counter : Program<Model, Arguments>
{
    // Called once when the app starts
    public static (Model, Command) Initialize(Url url, Arguments argument)
        => (new Model(0), Commands.None);

    // Called when a message is dispatched
    public static (Model model, Command command) Update(Message message, Model model)
        => message switch
        {
            Increment => (model with { Count = model.Count + 1 }, Commands.None),
            Decrement => (model with { Count = model.Count - 1 }, Commands.None),
            _ => (model, Commands.None)
        };

    // Called after every update to render the UI
    public static Document View(Model model)
        => new("Counter",
            div([], [
                button([onclick(new Decrement())], [text("-")]),
                text(model.Count.ToString()),
                button([onclick(new Increment())], [text("+")])
            ]));

    // Required for navigation support (minimal implementation for now)
    public static Message OnUrlChanged(Url url) => new Increment();
    public static Message OnLinkClicked(UrlRequest urlRequest) => new Increment();
    
    // No subscriptions needed for this app
    public static Subscription Subscriptions(Model model) => SubscriptionModule.None;
    
    // No commands to handle in this app
    public static Task HandleCommand(Command command, Func<Message, ValueTuple> dispatch)
        => Task.CompletedTask;
}
```

## Step 7: Run It

```bash
dotnet run
```

Open your browser to `http://localhost:5000`. Click the buttons—the count updates!

## What Just Happened?

1. `Runtime.Run` called `Initialize` to get the initial model
2. `View` rendered the model to a virtual DOM
3. The runtime rendered the virtual DOM to the real DOM
4. When you clicked a button, the `onclick` handler dispatched a message
5. `Update` received the message and returned a new model
6. `View` rendered the new model
7. The runtime diffed the old and new virtual DOM, applying minimal patches

This loop continues forever. State only changes through messages. The view always reflects the current model.

## Key Takeaways

| Concept | Purpose |
| ------- | ------- |
| **Model** | Single source of truth for application state |
| **Message** | Describes an event that can change state |
| **Update** | Pure function: (Message, Model) → (Model, Command) |
| **View** | Pure function: Model → Document |
| **Command** | Describes side effects to execute |
| **Runtime** | Orchestrates the loop and handles side effects |

## Exercises

1. **Add a Reset button** that sets the count back to 0
2. **Display "Positive" or "Negative"** based on the count
3. **Add a step size**: increment/decrement by 5 instead of 1
4. **Prevent negative**: don't allow the count to go below 0

## Next Steps

- [Project Structure](./project-structure.md) — Understand how Abies projects are organized
- [Tutorial 2: Todo List](../tutorials/02-todo-list.md) — Work with lists and more complex state
