# Getting Started

This guide uses the `Abies.Counter` sample to show the minimal setup for an
Abies application.

## Prerequisites

- .NET 10 SDK (preview is fine; see `global.json`)
- A browser with WebAssembly support

## Run the sample

```bash
dotnet run --project Abies.Counter
```

This starts a minimal counter app that already implements the full `Program`
interface.

## The minimal structure

Abies apps implement `Program<TModel, TArguments>` and then call
`Runtime.Run` to start the loop.

```csharp
using Abies;
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
    public static Subscription Subscriptions(Model model) => new();
    public static Task HandleCommand(Command command, Func<Message, System.ValueTuple> dispatch)
        => Task.CompletedTask;
}
```

Notes:
- `Initialize`, `Update`, and `View` are the core MVU loop.
- `HandleCommand` runs side effects. The minimal example has none.
- `OnUrlChanged` and `OnLinkClicked` are required for navigation support.

## Next steps

Continue with the [MVU Walkthrough](./mvu-walkthrough.md) to understand the
message flow and how to structure larger apps.
