# Program API Reference

The `Program` interface defines the complete MVU lifecycle for an Abies application.

## Interface Definition

```csharp
public interface Program<TModel, in TArgument> 
{
    static abstract (TModel, Command) Initialize(Url url, TArgument argument);
    static abstract (TModel model, Command command) Update(Message message, TModel model);
    static abstract Document View(TModel model);
    static abstract Message OnUrlChanged(Url url);
    static abstract Message OnLinkClicked(UrlRequest urlRequest);
    static abstract Subscription Subscriptions(TModel model);
    static abstract Task HandleCommand(Command command, Func<Message, ValueTuple> dispatch);
}
```

## Type Parameters

| Parameter | Description |
| --------- | ----------- |
| `TModel` | The type of the application's model (state) |
| `TArgument` | The type of the initialization argument passed at startup |

## Methods

### Initialize

Creates the initial model and startup commands.

```csharp
static abstract (TModel, Command) Initialize(Url url, TArgument argument);
```

**Parameters:**

- `url` — The initial URL when the application starts
- `argument` — Initialization data passed from the host

**Returns:** A tuple of the initial model and any startup command

**Example:**

```csharp
public static (Model, Command) Initialize(Url url, Unit argument)
{
    var route = Route.FromUrl(url);
    var model = new Model(Route: route, Count: 0, IsLoading: true);
    return (model, new LoadInitialDataCommand());
}
```

### Update

Handles messages and produces new state.

```csharp
static abstract (TModel model, Command command) Update(Message message, TModel model);
```

**Parameters:**

- `message` — The message to handle
- `model` — The current model state

**Returns:** A tuple of the new model and any command to execute

**Example:**

```csharp
public static (Model, Command) Update(Message message, Model model)
    => message switch
    {
        Increment => (model with { Count = model.Count + 1 }, Commands.None),
        Decrement => (model with { Count = model.Count - 1 }, Commands.None),
        Reset => (model with { Count = 0 }, Commands.None),
        _ => (model, Commands.None)
    };
```

### View

Renders the model to a virtual DOM document.

```csharp
static abstract Document View(TModel model);
```

**Parameters:**

- `model` — The current model state

**Returns:** A `Document` representing the page

**Example:**

```csharp
public static Document View(Model model)
    => new("My App",
        div([class_("app")], [
            h1([], [text($"Count: {model.Count}")]),
            button([onclick(new Increment())], [text("+")])
        ]));
```

### OnUrlChanged

Creates a message when the browser URL changes (e.g., back/forward navigation).

```csharp
static abstract Message OnUrlChanged(Url url);
```

**Parameters:**

- `url` — The new URL

**Returns:** A message to dispatch

**Example:**

```csharp
public static Message OnUrlChanged(Url url)
    => new UrlChangedMessage(url);
```

### OnLinkClicked

Creates a message when a link is clicked.

```csharp
static abstract Message OnLinkClicked(UrlRequest urlRequest);
```

**Parameters:**

- `urlRequest` — Either `Internal(Url)` or `External(string)`

**Returns:** A message to dispatch

**Example:**

```csharp
public static Message OnLinkClicked(UrlRequest urlRequest)
    => urlRequest switch
    {
        UrlRequest.Internal @internal => new NavigateTo(@internal.Url),
        UrlRequest.External external => new OpenExternal(external.Url),
        _ => throw new NotImplementedException()
    };
```

### Subscriptions

Declares external event sources based on current state.

```csharp
static abstract Subscription Subscriptions(TModel model);
```

**Parameters:**

- `model` — The current model state

**Returns:** Active subscriptions

**Example:**

```csharp
public static Subscription Subscriptions(Model model)
    => model.TimerRunning
        ? Every(TimeSpan.FromSeconds(1), _ => new TimerTick())
        : SubscriptionModule.None;
```

### HandleCommand

Executes side effects and dispatches result messages.

```csharp
static abstract Task HandleCommand(Command command, Func<Message, ValueTuple> dispatch);
```

**Parameters:**

- `command` — The command to execute
- `dispatch` — Function to dispatch result messages

**Example:**

```csharp
public static async Task HandleCommand(Command command, Func<Message, ValueTuple> dispatch)
{
    switch (command)
    {
        case LoadData:
            try
            {
                var data = await httpClient.GetFromJsonAsync<Data>("/api/data");
                dispatch(new DataLoaded(data));
            }
            catch (Exception ex)
            {
                dispatch(new LoadFailed(ex.Message));
            }
            break;
            
        case Commands.None:
            break;
    }
}
```

## Complete Example

```csharp
using Abies;
using Abies.Html;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

public record Model(int Count, bool IsLoading);

public record Increment : Message;
public record Decrement : Message;
public record Reset : Message;

public class CounterProgram : Program<Model, Unit>
{
    public static (Model, Command) Initialize(Url url, Unit argument)
        => (new Model(0, false), Commands.None);

    public static (Model, Command) Update(Message message, Model model)
        => message switch
        {
            Increment => (model with { Count = model.Count + 1 }, Commands.None),
            Decrement => (model with { Count = model.Count - 1 }, Commands.None),
            Reset => (model with { Count = 0 }, Commands.None),
            _ => (model, Commands.None)
        };

    public static Document View(Model model)
        => new("Counter",
            div([class_("counter")], [
                button([onclick(new Decrement())], [text("-")]),
                span([], [text(model.Count.ToString())]),
                button([onclick(new Increment())], [text("+")]),
                button([onclick(new Reset())], [text("Reset")])
            ]));

    public static Message OnUrlChanged(Url url) 
        => new NoOp();

    public static Message OnLinkClicked(UrlRequest request) 
        => new NoOp();

    public static Subscription Subscriptions(Model model) 
        => SubscriptionModule.None;

    public static Task HandleCommand(Command command, Func<Message, ValueTuple> dispatch) 
        => Task.CompletedTask;
}

public record NoOp : Message;
```

## Running a Program

```csharp
// Program.cs
using Abies;

await Runtime.Run<CounterProgram, Arguments, Model>(new Arguments());
```

## See Also

- [Element Interface](./element.md) — Reusable components
- [Command API](./command.md) — Side effect handling
- [Message Interface](./message.md) — Event types
- [Concepts: MVU Architecture](../concepts/mvu-architecture.md) — Deep dive
