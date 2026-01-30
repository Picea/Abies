# Components (Elements)

Abies provides the `Element` interface for building reusable UI components. This document explains how to create and use components effectively.

## What is an Element?

An Element is a self-contained UI component with its own:

- **Model** — Component state
- **Update** — State transitions
- **View** — Rendering logic
- **Subscriptions** — Event sources

```csharp
public interface Element<TModel, in TArgument>
{
    public static abstract TModel Initialize(TArgument argument);
    public static abstract (TModel model, Command command) Update(Message message, TModel model);
    public static abstract Node View(TModel model);
    public static abstract Subscription Subscriptions(TModel model);
}
```

## Element vs Program

| Aspect | Program | Element |
| ------ | ------- | ------- |
| Output | `Document` (full page) | `Node` (fragment) |
| Owns runtime | Yes | No |
| Entry point | Yes | No |
| Use case | Application root | Reusable components |

## Creating an Element

### Step 1: Define the Model

```csharp
public record AccordionModel(
    string Title,
    string Content,
    bool IsExpanded
);
```

### Step 2: Define Messages

```csharp
public record ToggleAccordion : Message;
```

### Step 3: Define Input

```csharp
public record AccordionInput(string Title, string Content, bool InitiallyExpanded = false);
```

### Step 4: Implement the Element

```csharp
public class Accordion : Element<AccordionModel, AccordionInput>
{
    public static AccordionModel Initialize(AccordionInput input)
        => new(input.Title, input.Content, input.InitiallyExpanded);

    public static (AccordionModel model, Command command) Update(Message message, AccordionModel model)
        => message switch
        {
            ToggleAccordion => (model with { IsExpanded = !model.IsExpanded }, Commands.None),
            _ => (model, Commands.None)
        };

    public static Node View(AccordionModel model)
        => div([class_("accordion")], [
            button([
                class_("accordion-header"),
                onclick(new ToggleAccordion())
            ], [
                text(model.Title),
                span([class_(model.IsExpanded ? "icon-up" : "icon-down")], [])
            ]),
            model.IsExpanded
                ? div([class_("accordion-content")], [text(model.Content)])
                : text("")
        ]);

    public static Subscription Subscriptions(AccordionModel model)
        => SubscriptionModule.None;
}
```

## Using Elements

Elements are not automatically wired by the runtime. You integrate them manually into your Program.

### Embedding in Parent Model

```csharp
public record PageModel(
    AccordionModel Faq1,
    AccordionModel Faq2,
    AccordionModel Faq3
);
```

### Routing Messages

```csharp
// Create wrapper messages
public record Faq1Message(Message Inner) : Message;
public record Faq2Message(Message Inner) : Message;
public record Faq3Message(Message Inner) : Message;

// Route in Update
public static (PageModel, Command) Update(Message msg, PageModel model)
    => msg switch
    {
        Faq1Message m => 
            UpdateNested(m.Inner, model.Faq1, model, (m, faq) => m with { Faq1 = faq }),
        Faq2Message m => 
            UpdateNested(m.Inner, model.Faq2, model, (m, faq) => m with { Faq2 = faq }),
        Faq3Message m => 
            UpdateNested(m.Inner, model.Faq3, model, (m, faq) => m with { Faq3 = faq }),
        _ => (model, Commands.None)
    };

static (PageModel, Command) UpdateNested<T>(
    Message msg, 
    T componentModel, 
    PageModel pageModel,
    Func<PageModel, T, PageModel> update) where T : ...
{
    var (newComponentModel, cmd) = Accordion.Update(msg, componentModel);
    return (update(pageModel, newComponentModel), cmd);
}
```

### Rendering with Message Wrapping

```csharp
public static Document View(PageModel model)
    => new("FAQ Page",
        div([class_("faq-page")], [
            h1([], [text("Frequently Asked Questions")]),
            WrapView(Accordion.View(model.Faq1), m => new Faq1Message(m)),
            WrapView(Accordion.View(model.Faq2), m => new Faq2Message(m)),
            WrapView(Accordion.View(model.Faq3), m => new Faq3Message(m))
        ]));

static Node WrapView(Node node, Func<Message, Message> wrap)
    => node.MapMessages(wrap);  // Transforms all messages in the node tree
```

## Page Components Pattern

The Conduit sample uses a simplified pattern where pages are like elements:

```csharp
// Each page is a static class with MVU functions
public static class HomePage
{
    public record Model(List<Article> Articles, bool IsLoading);
    
    public interface Message : Abies.Message { /* ... */ }
    
    public static Model Init() => new([], true);
    
    public static (Model, Command) Update(Message msg, Model model) => /* ... */;
    
    public static Node View(Model model) => /* ... */;
}

// Main program delegates to page
public static (AppModel, Command) Update(Message msg, AppModel model)
    => msg switch
    {
        HomePage.Message homeMsg when model.Page is Page.Home home =>
        {
            var (newPage, cmd) = HomePage.Update(homeMsg, home.Model);
            return (model with { Page = new Page.Home(newPage) }, cmd);
        },
        // ...
    };
```

## Stateless Components

For components without state, use simple functions:

```csharp
// Stateless component - just a function
public static Node Avatar(string url, string alt, string size = "md")
    => img([
        src(url),
        Attr("alt", alt),
        class_($"avatar avatar-{size}")
    ], []);

// Usage
Avatar("/images/user.jpg", "User profile", "lg")
```

```csharp
// Stateless component with children
public static Node Card(string title, params Node[] children)
    => div([class_("card")], [
        div([class_("card-header")], [text(title)]),
        div([class_("card-body")], children)
    ]);

// Usage
Card("My Card",
    p([], [text("Card content here")]),
    button([onclick(new Action())], [text("Click")])
)
```

## Component Best Practices

### 1. Keep Models Small

Pass only what's needed:

```csharp
// ❌ Too much data
public record CardModel(Article Article, User Author, List<Comment> Comments);

// ✅ Minimal data
public record CardModel(string Title, string Preview, string AuthorName);
```

### 2. Use Initialization Arguments

Configure components at creation:

```csharp
public record DropdownInput(
    List<string> Options,
    string? InitialSelection,
    bool AllowEmpty = true
);

public static DropdownModel Initialize(DropdownInput input)
    => new(
        Options: input.Options,
        Selected: input.InitialSelection,
        IsOpen: false,
        AllowEmpty: input.AllowEmpty
    );
```

### 3. Expose Subscriptions

Let parent compose subscriptions:

```csharp
public static Subscription Subscriptions(DropdownModel model)
    => model.IsOpen
        ? OnClickOutside(() => new CloseDropdown())
        : SubscriptionModule.None;

// Parent composes
public static Subscription Subscriptions(PageModel model)
    => Batch([
        Dropdown.Subscriptions(model.Dropdown),
        // Other subscriptions
    ]);
```

### 4. Document Component API

```csharp
/// <summary>
/// A collapsible accordion component.
/// </summary>
/// <example>
/// var model = Accordion.Initialize(new AccordionInput("FAQ", "Answer here"));
/// var view = Accordion.View(model);
/// </example>
public class Accordion : Element<AccordionModel, AccordionInput>
{
    // ...
}
```

## When to Use Elements

**Use Elements when:**

- Component has its own state
- Component is used in multiple places
- Component has complex update logic
- Component needs subscriptions

**Use Functions when:**

- Component is stateless
- Component is simple/presentational
- Component doesn't need its own messages

## Example: Modal Component

```csharp
public record ModalModel(
    string Title,
    Node Content,
    bool IsOpen,
    bool ShowCloseButton
);

public record ModalInput(string Title, Node Content, bool ShowCloseButton = true);

public interface ModalMessage : Message
{
    public record Open : ModalMessage;
    public record Close : ModalMessage;
    public record ClickedOutside : ModalMessage;
}

public class Modal : Element<ModalModel, ModalInput>
{
    public static ModalModel Initialize(ModalInput input)
        => new(input.Title, input.Content, false, input.ShowCloseButton);

    public static (ModalModel, Command) Update(Message msg, ModalModel model)
        => msg switch
        {
            ModalMessage.Open => (model with { IsOpen = true }, Commands.None),
            ModalMessage.Close => (model with { IsOpen = false }, Commands.None),
            ModalMessage.ClickedOutside => (model with { IsOpen = false }, Commands.None),
            _ => (model, Commands.None)
        };

    public static Node View(ModalModel model)
        => model.IsOpen
            ? div([class_("modal-overlay"), onclick(new ModalMessage.ClickedOutside())], [
                div([class_("modal"), onclick(StopPropagation)], [
                    div([class_("modal-header")], [
                        text(model.Title),
                        model.ShowCloseButton
                            ? button([class_("close"), onclick(new ModalMessage.Close())], [text("×")])
                            : text("")
                    ]),
                    div([class_("modal-body")], [model.Content])
                ])
              ])
            : text("");

    public static Subscription Subscriptions(ModalModel model)
        => model.IsOpen
            ? OnKeyDown(k => k == "Escape" ? new ModalMessage.Close() : null)
            : SubscriptionModule.None;
}
```

## Summary

Elements provide a pattern for reusable, stateful components:

- **Initialize** — Create component state from input
- **Update** — Handle component messages
- **View** — Render component to nodes
- **Subscriptions** — Declare event sources

Key points:

- ✅ Elements are patterns, not magic
- ✅ Parent integrates elements manually
- ✅ Use functions for stateless components
- ✅ Keep component models minimal

## See Also

- [MVU Architecture](./mvu-architecture.md) — How components fit in MVU
- [Tutorial: Real-World App](../tutorials/07-real-world-app.md) — Component patterns in practice
- [API: Element Interface](../api/element.md) — Interface reference
