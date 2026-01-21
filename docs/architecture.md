# Abies Architecture

This document explains the core architecture of Abies and the MVU pattern it
implements.

## The MVU pattern

Abies uses Model-View-Update (MVU):

```
Model --(View)--> Virtual DOM --(Diff/Patch)--> Real DOM
   ^                   |
   |                   v
 Update <--- Messages/Events
   |
 Commands (side effects)
```

### Model

The model is the immutable state of your app.

```csharp
public record Model(List<Article> Articles, bool IsLoading);
```

### Messages

Messages are events that cause state transitions.

```csharp
public record ArticlesLoaded(List<Article> Articles) : Message;
public record LoadFailed(string Error) : Message;
```

### Update

Update is a pure function from `(Message, Model)` to `(Model, Command)`.

```csharp
public static (Model model, Command command) Update(Message message, Model model)
    => message switch
    {
        ArticlesLoaded loaded => (model with { Articles = loaded.Articles, IsLoading = false }, Commands.None),
        _ => (model, Commands.None)
    };
```

### View

View converts model state to a virtual DOM tree.

```csharp
public static Document View(Model model)
    => new("Articles",
        div([], [
            model.IsLoading ? text("Loading...") : ArticleList(model.Articles)
        ]));
```

### Commands

Commands represent side effects (HTTP, storage, timers). They are executed by
`HandleCommand`, not by `Update`.

```csharp
public record LoadArticles : Command;

public static async Task HandleCommand(Command command, Func<Message, System.ValueTuple> dispatch)
{
    if (command is LoadArticles)
    {
        var articles = await api.GetArticles();
        dispatch(new ArticlesLoaded(articles));
    }
}
```

## Program interface

Every application implements `Program<TModel, TArguments>`. See
[Program and Runtime](./runtime-program.md) for the full lifecycle.

## Virtual DOM

Abies renders a virtual DOM tree and computes patches between renders. It
updates the real DOM through JS interop. See
[Virtual DOM Algorithm](./virtual_dom_algorithm.md) for details.

## Routing

Routing is built on parser combinators and template routes.
See [Routing](./routing.md).

## Components

For reusable UI blocks, implement `Element<TModel, TArgument>`.
See [Components](./components.md).
