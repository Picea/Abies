# MVU Walkthrough

Abies implements the Model-View-Update (MVU) pattern. This walkthrough shows
how data flows through your program and where side effects live.

## 1) Model

The model is the full application state. Keep it immutable and replace it on
updates.

```csharp
public record Model(int Count, bool IsLoading);
```

## 2) Messages

Messages describe events that can change state. They are plain records
implementing `Abies.Message`.

```csharp
public record Increment : Message;
public record Decrement : Message;
public record Loaded(int Count) : Message;
```

## 3) Update

`Update` is a pure function. It returns the new model and a `Command` describing
any side effects to run.

```csharp
public static (Model model, Command command) Update(Message message, Model model)
    => message switch
    {
        Increment => (model with { Count = model.Count + 1 }, Commands.None),
        Decrement => (model with { Count = model.Count - 1 }, Commands.None),
        Loaded loaded => (model with { Count = loaded.Count, IsLoading = false }, Commands.None),
        _ => (model, Commands.None)
    };
```

## 4) View

`View` renders the model into a virtual DOM tree. You use the helpers in
`Abies.Html.Elements`, `Attributes`, and `Events`.

```csharp
public static Document View(Model model)
    => new("Counter",
        div([], [
            button([onclick(new Decrement())], [text("-")]),
            text(model.Count.ToString()),
            button([onclick(new Increment())], [text("+")])
        ]));
```

## 5) Commands and effects

Commands are run by the runtime, not in `Update`. This keeps the update loop
pure and testable.

```csharp
public record LoadCount : Command;

public static async Task HandleCommand(Command command, Func<Message, System.ValueTuple> dispatch)
{
    switch (command)
    {
        case LoadCount:
            var count = await Task.FromResult(42);
            dispatch(new Loaded(count));
            break;
    }
}
```

## 6) Runtime loop

`Runtime.Run` wires everything together:
- It initializes the model
- It renders the first view
- It dispatches messages from event handlers
- It applies virtual DOM patches after each update

See [Program and Runtime](./runtime-program.md) for the full lifecycle.
