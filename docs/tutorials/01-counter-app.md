# Tutorial 1: Counter App

This tutorial reinforces the MVU fundamentals by building a counter with additional features.

**Prerequisites:** Complete [Your First App](../getting-started/your-first-app.md)

**Time:** 15 minutes

## What You'll Build

A counter application with:

- Increment and decrement buttons
- A reset button
- Step size control (increment by 1, 5, or 10)
- Keyboard shortcuts

## Starting Point

Create a new project or use the existing counter as a base:

```csharp
using Abies;
using Abies.DOM;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

await Runtime.Run<Counter, Arguments, Model>(new Arguments());

public record Arguments;
public record Model(int Count);

public record Increment : Message;
public record Decrement : Message;

public class Counter : Program<Model, Arguments>
{
    public static (Model, Command) Initialize(Url url, Arguments argument)
        => (new Model(0), Commands.None);

    public static (Model model, Command command) Update(Message message, Model model)
        => message switch
        {
            Increment => (model with { Count = model.Count + 1 }, Commands.None),
            Decrement => (model with { Count = model.Count - 1 }, Commands.None),
            _ => (model, Commands.None)
        };

    public static Document View(Model model)
        => new("Counter",
            div([], [
                button([onclick(new Decrement())], [text("-")]),
                text(model.Count.ToString()),
                button([onclick(new Increment())], [text("+")])
            ]));

    public static Message OnUrlChanged(Url url) => new Increment();
    public static Message OnLinkClicked(UrlRequest urlRequest) => new Increment();
    public static Subscription Subscriptions(Model model) => SubscriptionModule.None;
    public static Task HandleCommand(Command command, Func<Message, ValueTuple> dispatch)
        => Task.CompletedTask;
}
```

## Step 1: Add a Reset Button

First, add a message for reset:

```csharp
public record Reset : Message;
```

Handle it in Update:

```csharp
public static (Model model, Command command) Update(Message message, Model model)
    => message switch
    {
        Increment => (model with { Count = model.Count + 1 }, Commands.None),
        Decrement => (model with { Count = model.Count - 1 }, Commands.None),
        Reset => (model with { Count = 0 }, Commands.None),
        _ => (model, Commands.None)
    };
```

Add the button to the view:

```csharp
public static Document View(Model model)
    => new("Counter",
        div([], [
            button([onclick(new Decrement())], [text("-")]),
            text(model.Count.ToString()),
            button([onclick(new Increment())], [text("+")]),
            button([onclick(new Reset())], [text("Reset")])
        ]));
```

## Step 2: Add Step Size

Now let's add the ability to change how much we increment/decrement.

Expand the model:

```csharp
public record Model(int Count, int Step);
```

Update Initialize:

```csharp
public static (Model, Command) Initialize(Url url, Arguments argument)
    => (new Model(Count: 0, Step: 1), Commands.None);
```

Add a message to change step:

```csharp
public record SetStep(int Step) : Message;
```

Update the Update function:

```csharp
public static (Model model, Command command) Update(Message message, Model model)
    => message switch
    {
        Increment => (model with { Count = model.Count + model.Step }, Commands.None),
        Decrement => (model with { Count = model.Count - model.Step }, Commands.None),
        Reset => (model with { Count = 0 }, Commands.None),
        SetStep set => (model with { Step = set.Step }, Commands.None),
        _ => (model, Commands.None)
    };
```

Add step buttons to the view:

```csharp
public static Document View(Model model)
    => new("Counter",
        div([], [
            // Counter controls
            div([], [
                button([onclick(new Decrement())], [text("-")]),
                text(model.Count.ToString()),
                button([onclick(new Increment())], [text("+")]),
                button([onclick(new Reset())], [text("Reset")])
            ]),
            
            // Step size controls
            div([], [
                text($"Step: {model.Step}"),
                button([onclick(new SetStep(1))], [text("1")]),
                button([onclick(new SetStep(5))], [text("5")]),
                button([onclick(new SetStep(10))], [text("10")])
            ])
        ]));
```

## Step 3: Add Keyboard Shortcuts

Let's add keyboard support: arrow up/down to increment/decrement, 'r' to reset.

Add a message for key presses:

```csharp
public record KeyPressed(string Key) : Message;
```

Update the Update function to handle keys:

```csharp
public static (Model model, Command command) Update(Message message, Model model)
    => message switch
    {
        Increment => (model with { Count = model.Count + model.Step }, Commands.None),
        Decrement => (model with { Count = model.Count - model.Step }, Commands.None),
        Reset => (model with { Count = 0 }, Commands.None),
        SetStep set => (model with { Step = set.Step }, Commands.None),
        KeyPressed { Key: "ArrowUp" } => (model with { Count = model.Count + model.Step }, Commands.None),
        KeyPressed { Key: "ArrowDown" } => (model with { Count = model.Count - model.Step }, Commands.None),
        KeyPressed { Key: "r" or "R" } => (model with { Count = 0 }, Commands.None),
        _ => (model, Commands.None)
    };
```

Add a subscription for keyboard events:

```csharp
public static Subscription Subscriptions(Model model) =>
    SubscriptionModule.OnKeyDown(data => new KeyPressed(data?.Key ?? ""));
```

Now pressing arrow keys and 'r' controls the counter!

## Step 4: Visual Feedback

Let's add visual feedback when the count is positive, negative, or zero:

```csharp
public static Document View(Model model)
{
    var countClass = model.Count switch
    {
        > 0 => "positive",
        < 0 => "negative",
        _ => "zero"
    };
    
    return new("Counter",
        div([], [
            div([class_(countClass)], [
                button([onclick(new Decrement())], [text("-")]),
                span([], [text(model.Count.ToString())]),
                button([onclick(new Increment())], [text("+")])
            ]),
            button([onclick(new Reset())], [text("Reset")]),
            div([], [
                text($"Step: {model.Step} "),
                button([onclick(new SetStep(1))], [text("1")]),
                button([onclick(new SetStep(5))], [text("5")]),
                button([onclick(new SetStep(10))], [text("10")])
            ]),
            p([], [text("Use ↑/↓ arrows or click buttons. Press 'r' to reset.")])
        ]));
}
```

## Final Code

Here's the complete application:

```csharp
using Abies;
using Abies.DOM;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

await Runtime.Run<Counter, Arguments, Model>(new Arguments());

public record Arguments;
public record Model(int Count, int Step);

// Messages
public record Increment : Message;
public record Decrement : Message;
public record Reset : Message;
public record SetStep(int Step) : Message;
public record KeyPressed(string Key) : Message;

public class Counter : Program<Model, Arguments>
{
    public static (Model, Command) Initialize(Url url, Arguments argument)
        => (new Model(Count: 0, Step: 1), Commands.None);

    public static (Model model, Command command) Update(Message message, Model model)
        => message switch
        {
            Increment => (model with { Count = model.Count + model.Step }, Commands.None),
            Decrement => (model with { Count = model.Count - model.Step }, Commands.None),
            Reset => (model with { Count = 0 }, Commands.None),
            SetStep set => (model with { Step = set.Step }, Commands.None),
            KeyPressed { Key: "ArrowUp" } => (model with { Count = model.Count + model.Step }, Commands.None),
            KeyPressed { Key: "ArrowDown" } => (model with { Count = model.Count - model.Step }, Commands.None),
            KeyPressed { Key: "r" or "R" } => (model with { Count = 0 }, Commands.None),
            _ => (model, Commands.None)
        };

    public static Document View(Model model)
    {
        var countClass = model.Count switch
        {
            > 0 => "positive",
            < 0 => "negative",
            _ => "zero"
        };
        
        return new("Counter",
            div([], [
                div([class_(countClass)], [
                    button([onclick(new Decrement())], [text("-")]),
                    span([], [text(model.Count.ToString())]),
                    button([onclick(new Increment())], [text("+")])
                ]),
                button([onclick(new Reset())], [text("Reset")]),
                div([], [
                    text($"Step: {model.Step} "),
                    button([onclick(new SetStep(1))], [text("1")]),
                    button([onclick(new SetStep(5))], [text("5")]),
                    button([onclick(new SetStep(10))], [text("10")])
                ]),
                p([], [text("Use ↑/↓ arrows or click buttons. Press 'r' to reset.")])
            ]));
    }

    public static Message OnUrlChanged(Url url) => new Increment();
    public static Message OnLinkClicked(UrlRequest urlRequest) => new Increment();
    
    public static Subscription Subscriptions(Model model) =>
        SubscriptionModule.OnKeyDown(data => new KeyPressed(data?.Key ?? ""));
    
    public static Task HandleCommand(Command command, Func<Message, ValueTuple> dispatch)
        => Task.CompletedTask;
}
```

## What You Learned

| Concept | Application |
| ------- | ----------- |
| Expanding state | Added `Step` to the model |
| Pattern matching | Used nested patterns like `KeyPressed { Key: "ArrowUp" }` |
| Subscriptions | Added keyboard event subscription |
| Conditional rendering | Changed class based on count value |

## Exercises

1. **Add bounds**: Limit count to -100 to 100
2. **Add history**: Track the last 5 count values
3. **Add undo**: Allow undoing the last action
4. **Add styling**: Apply CSS based on count magnitude

## Next Tutorial

→ [Tutorial 2: Todo List](./02-todo-list.md) — Learn to manage lists of items
