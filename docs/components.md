# Components (Elements)

For reusable UI blocks, Abies exposes the `Element<TModel, TArgument>`
interface. An element has its own model and update logic, just like a program,
but renders to a `Node` instead of a full `Document`.

## Element interface

```csharp
public interface Element<TModel, in TArgument>
{
    public static abstract Node View(TModel model);
    public static abstract (TModel model, Command command) Update(Message message, TModel model);
    public static abstract TModel Initialize(TArgument argument);
    public static abstract Subscription Subscriptions(TModel model);
}
```

## Example

```csharp
public record CardModel(string Title, string Description);

public record CardInput(string Title, string Description);

public record ToggleDetails : Message;

public class ArticleCard : Element<CardModel, CardInput>
{
    public static CardModel Initialize(CardInput input)
        => new(input.Title, input.Description);

    public static (CardModel model, Command command) Update(Message message, CardModel model)
        => (model, Commands.None);

    public static Node View(CardModel model)
        => div([class_("card")], [
            h2([], [text(model.Title)]),
            p([], [text(model.Description)])
        ]);

    public static Subscription Subscriptions(CardModel model) => new();
}
```

## How elements are used

Elements are not wired automatically by the runtime. They are a pattern for
organizing view logic in a large application. The Conduit sample uses element
pages under `Abies.Conduit/Page/*` and composes them in the main view.

If you adopt elements, keep their models small and pass in the minimum data
needed for rendering. Let the top-level `Program` own routing and global state.
