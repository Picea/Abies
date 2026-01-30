# Element API Reference

The `Element` interface defines reusable UI components with their own state and update logic.

## Interface Definition

```csharp
public interface Element<TModel, in TArgument>
{
    static abstract TModel Initialize(TArgument argument);
    static abstract (TModel model, Command command) Update(Message message, TModel model);
    static abstract Node View(TModel model);
    static abstract Subscription Subscriptions(TModel model);
}
```

## Type Parameters

| Parameter | Description |
| --------- | ----------- |
| `TModel` | The type of the element's internal state |
| `TArgument` | The type of initialization data passed when creating the element |

## Methods

### Initialize

Creates the initial element state from input arguments.

```csharp
static abstract TModel Initialize(TArgument argument);
```

**Parameters:**

- `argument` — Initialization data

**Returns:** The initial element model

**Example:**

```csharp
public static DropdownModel Initialize(DropdownInput input)
    => new(
        Options: input.Options,
        Selected: input.DefaultSelection,
        IsOpen: false
    );
```

### Update

Handles element-specific messages.

```csharp
static abstract (TModel model, Command command) Update(Message message, TModel model);
```

**Parameters:**

- `message` — The message to handle
- `model` — Current element state

**Returns:** New state and optional command

**Example:**

```csharp
public static (DropdownModel, Command) Update(Message msg, DropdownModel model)
    => msg switch
    {
        ToggleOpen => (model with { IsOpen = !model.IsOpen }, Commands.None),
        SelectOption opt => (model with { Selected = opt.Value, IsOpen = false }, Commands.None),
        _ => (model, Commands.None)
    };
```

### View

Renders the element to a virtual DOM node.

```csharp
static abstract Node View(TModel model);
```

**Parameters:**

- `model` — Current element state

**Returns:** A `Node` (not a full `Document`)

**Example:**

```csharp
public static Node View(DropdownModel model)
    => div([class_("dropdown")], [
        button([onclick(new ToggleOpen())], [text(model.Selected ?? "Select...")]),
        model.IsOpen
            ? ul([class_("dropdown-menu")], 
                model.Options.Select(opt => 
                    li([onclick(new SelectOption(opt))], [text(opt)])
                ).ToArray())
            : text("")
    ]);
```

### Subscriptions

Declares element-specific subscriptions.

```csharp
static abstract Subscription Subscriptions(TModel model);
```

**Parameters:**

- `model` — Current element state

**Returns:** Active subscriptions for this element

**Example:**

```csharp
public static Subscription Subscriptions(DropdownModel model)
    => model.IsOpen
        ? OnKeyDown(key => key == "Escape" ? new CloseDropdown() : null)
        : SubscriptionModule.None;
```

## Element vs Program

| Aspect | Element | Program |
| ------ | ------- | ------- |
| View output | `Node` | `Document` |
| Runtime integration | Manual | Automatic |
| Entry point | No | Yes |
| Subscriptions | Must be composed | Managed by runtime |
| Use case | Reusable components | Application root |

## Complete Example

```csharp
using Abies;
using Abies.Html;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

// Model
public record AccordionModel(
    string Title,
    string Content,
    bool IsExpanded
);

// Input for initialization
public record AccordionInput(
    string Title, 
    string Content, 
    bool StartExpanded = false
);

// Messages
public record ToggleAccordion : Message;

// Element implementation
public class Accordion : Element<AccordionModel, AccordionInput>
{
    public static AccordionModel Initialize(AccordionInput input)
        => new(input.Title, input.Content, input.StartExpanded);

    public static (AccordionModel, Command) Update(Message msg, AccordionModel model)
        => msg switch
        {
            ToggleAccordion => (model with { IsExpanded = !model.IsExpanded }, Commands.None),
            _ => (model, Commands.None)
        };

    public static Node View(AccordionModel model)
        => div([class_("accordion")], [
            div([
                class_("accordion-header"),
                onclick(new ToggleAccordion())
            ], [
                text(model.Title),
                span([class_(model.IsExpanded ? "chevron-up" : "chevron-down")], [])
            ]),
            model.IsExpanded
                ? div([class_("accordion-body")], [text(model.Content)])
                : text("")
        ]);

    public static Subscription Subscriptions(AccordionModel model)
        => SubscriptionModule.None;
}
```

## Using Elements in a Program

Elements are not automatically managed. You integrate them manually:

### 1. Embed in Parent Model

```csharp
public record PageModel(
    AccordionModel Faq1,
    AccordionModel Faq2
);
```

### 2. Create Wrapper Messages

```csharp
public record Faq1Msg(Message Inner) : Message;
public record Faq2Msg(Message Inner) : Message;
```

### 3. Route Messages

```csharp
public static (PageModel, Command) Update(Message msg, PageModel model)
    => msg switch
    {
        Faq1Msg m => 
        {
            var (newFaq, cmd) = Accordion.Update(m.Inner, model.Faq1);
            return (model with { Faq1 = newFaq }, cmd);
        },
        Faq2Msg m =>
        {
            var (newFaq, cmd) = Accordion.Update(m.Inner, model.Faq2);
            return (model with { Faq2 = newFaq }, cmd);
        },
        _ => (model, Commands.None)
    };
```

### 4. Render with Message Wrapping

```csharp
public static Document View(PageModel model)
    => new("FAQ",
        div([], [
            // You'd need to wrap messages from child views
            Accordion.View(model.Faq1),
            Accordion.View(model.Faq2)
        ]));
```

## Stateless Alternative

For components without internal state, use plain functions:

```csharp
// Stateless component
public static Node Avatar(string url, string name, string size = "md")
    => img([
        src(url),
        alt(name),
        class_($"avatar avatar-{size}")
    ], []);

// Usage
Avatar("/images/user.jpg", "John Doe", "lg")
```

## Best Practices

1. **Keep element models minimal** — Only include necessary state
2. **Use initialization arguments** — Configure elements at creation
3. **Expose subscriptions** — Let parents compose them
4. **Prefer functions for stateless UI** — Simpler and lighter

## See Also

- [Program API](./program.md) — Application root interface
- [Concepts: Components](../concepts/components.md) — Component patterns
- [Tutorial: Real-World App](../tutorials/07-real-world-app.md) — Elements in practice
