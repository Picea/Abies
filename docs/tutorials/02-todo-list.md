# Tutorial 2: Todo List

Build a todo list application that introduces text input handling, list rendering, keyed diffing, and memoization.

**Prerequisites:** [Tutorial 1: Counter App](01-counter-app.md)

**Time:** 25 minutes

**What you'll learn:**

- Handling text input with `oninput`
- Rendering lists with keyed elements
- Memoizing list items with `lazy` for performance
- Modeling complex state with nested records
- Filtering and derived views

## The Model

A todo app needs a list of items, each with a description and completion status, plus an input field for new items:

```csharp
using Abies.DOM;
using Abies.Subscriptions;
using Automaton;
using static Abies.Html.Attributes;
using static Abies.Html.Elements;
using static Abies.Html.Events;

namespace MyTodo;

/// <summary>A single todo item.</summary>
public record TodoItem(Guid Id, string Text, bool Completed);

/// <summary>Which items to show.</summary>
public enum Filter { All, Active, Completed }

/// <summary>Complete application state.</summary>
public record Model(
    string Input,
    IReadOnlyList<TodoItem> Items,
    Filter ActiveFilter);
```

**Design decisions:**

- Each `TodoItem` has a `Guid Id` for stable identity across renders (important for keyed diffing)
- `IReadOnlyList<T>` enforces immutability at the type level
- `Filter` is a simple enum — no need for a record when there's no associated data

## Messages

Think about every interaction the user can perform:

```csharp
public interface TodoMessage : Message;

/// <summary>The text in the input field changed.</summary>
public record InputChanged(string Value) : TodoMessage;

/// <summary>The user pressed Enter or clicked Add.</summary>
public record AddTodo : TodoMessage;

/// <summary>The user toggled a todo's completion.</summary>
public record ToggleTodo(Guid Id) : TodoMessage;

/// <summary>The user clicked delete on a todo.</summary>
public record RemoveTodo(Guid Id) : TodoMessage;

/// <summary>The user changed the active filter.</summary>
public record SetFilter(Filter Filter) : TodoMessage;

/// <summary>The user clicked "Clear completed".</summary>
public record ClearCompleted : TodoMessage;
```

## Initialize

```csharp
public sealed class TodoApp : Program<Model, Unit>
{
    public static (Model, Command) Initialize(Unit argument) =>
        (new Model(
            Input: "",
            Items: [],
            ActiveFilter: Filter.All
        ), Commands.None);
```

## Transition

The transition function handles every message with a pure state transformation:

```csharp
    public static (Model, Command) Transition(Model model, Message message) =>
        message switch
        {
            InputChanged msg =>
                (model with { Input = msg.Value }, Commands.None),

            AddTodo when !string.IsNullOrWhiteSpace(model.Input) =>
                (model with
                {
                    Input = "",
                    Items = model.Items.Append(
                        new TodoItem(Guid.NewGuid(), model.Input.Trim(), false)
                    ).ToList()
                }, Commands.None),

            AddTodo =>
                (model, Commands.None),  // empty input — do nothing

            ToggleTodo msg =>
                (model with
                {
                    Items = model.Items
                        .Select(item => item.Id == msg.Id
                            ? item with { Completed = !item.Completed }
                            : item)
                        .ToList()
                }, Commands.None),

            RemoveTodo msg =>
                (model with
                {
                    Items = model.Items
                        .Where(item => item.Id != msg.Id)
                        .ToList()
                }, Commands.None),

            SetFilter msg =>
                (model with { ActiveFilter = msg.Filter }, Commands.None),

            ClearCompleted =>
                (model with
                {
                    Items = model.Items
                        .Where(item => !item.Completed)
                        .ToList()
                }, Commands.None),

            _ => (model, Commands.None)
        };
```

**Key patterns:**

- **Guard clauses in pattern matching**: `AddTodo when !string.IsNullOrWhiteSpace(model.Input)` — only add if there's text
- **Immutable list operations**: `Select` to transform, `Where` to filter, `Append` to add — always producing new collections
- **Two `AddTodo` cases**: The first handles valid input, the second is the catch-all for empty input

## View

The view composes several helper functions:

```csharp
    public static Document View(Model model)
    {
        var filtered = model.Items.Where(item => model.ActiveFilter switch
        {
            Filter.Active => !item.Completed,
            Filter.Completed => item.Completed,
            _ => true
        }).ToList();

        var activeCount = model.Items.Count(item => !item.Completed);

        return new("Todo App",
            div([class_("todo-app")],
            [
                h1([], [text("Todos")]),
                InputSection(model.Input),
                TodoList(filtered),
                Footer(activeCount, model.ActiveFilter)
            ]));
    }
```

### The Input Section

```csharp
    static Node InputSection(string input) =>
        div([class_("input-section")],
        [
            input_([
                class_("new-todo"),
                placeholder("What needs to be done?"),
                value(input),
                oninput(data => new InputChanged(data?.Value ?? ""))
            ]),
            button([class_("add-btn"), onclick(new AddTodo())], [text("Add")])
        ]);
```

**The `oninput` handler** uses the factory overload: `oninput(Func<InputEventData?, Message>)`. When the user types, the runtime calls this function with the event data, and the resulting message flows into `Transition`. This is how you access the input value.

Compare the two `onclick` / `oninput` overloads:

| Overload | Use Case |
| --- | --- |
| `onclick(new Increment())` | No event data needed — message is fixed |
| `oninput(data => new InputChanged(data?.Value ?? ""))` | Need the input value from the event |

### The Todo List with Keyed Rendering

```csharp
    static Node TodoList(IReadOnlyList<TodoItem> items) =>
        ul([class_("todo-list")],
            items.Select(item =>
                lazy(item.Id, () => TodoItemView(item))
            ).ToArray());
```

**`lazy(key, factory)`** is the memoization primitive. It:

1. Uses `item.Id` as a stable identity key
2. Caches the virtual DOM node for each key
3. Only calls the `factory` function when the key is new or the item has changed
4. Enables the differ to **match old and new list items by key** instead of by position

Without `lazy`, reordering a 100-item list would re-render all 100 items. With `lazy`, only the items that actually changed are re-rendered.

### Individual Todo Item

```csharp
    static Node TodoItemView(TodoItem item) =>
        li([class_(item.Completed ? "todo completed" : "todo")],
        [
            input_([
                type_("checkbox"),
                checked_(item.Completed),
                onclick(new ToggleTodo(item.Id))
            ]),
            span([class_("text")], [text(item.Text)]),
            button([class_("delete"), onclick(new RemoveTodo(item.Id))], [text("×")])
        ]);
```

### Filter Footer

```csharp
    static Node Footer(int activeCount, Filter current) =>
        div([class_("footer")],
        [
            span([], [text($"{activeCount} item{(activeCount == 1 ? "" : "s")} left")]),
            div([class_("filters")],
            [
                FilterButton("All", Filter.All, current),
                FilterButton("Active", Filter.Active, current),
                FilterButton("Completed", Filter.Completed, current)
            ]),
            button([class_("clear"), onclick(new ClearCompleted())], [text("Clear completed")])
        ]);

    static Node FilterButton(string label, Filter filter, Filter current) =>
        button([
            class_(filter == current ? "filter-btn active" : "filter-btn"),
            onclick(new SetFilter(filter))
        ], [text(label)]);
```

### Subscriptions

```csharp
    public static Subscription Subscriptions(Model model) =>
        SubscriptionModule.None;
}
```

## Understanding Keyed Diffing

Consider a list of three todos: A, B, C. The user deletes B.

**Without keys (positional diffing):**

```
Old: [A, B, C]     →  Position 0: A→A (no change)
New: [A, C]         →  Position 1: B→C (update text, checkbox, handlers)
                    →  Position 2: C→∅ (remove)
```

Two DOM operations, and item C is unnecessarily re-rendered.

**With keys (keyed diffing via `lazy`):**

```
Old: {id-a: A, id-b: B, id-c: C}
New: {id-a: A, id-c: C}

→ id-a: matched, no change
→ id-b: removed
→ id-c: matched, no change
```

One DOM operation, and item C keeps its existing DOM node.

> **Principle:** This is the same *reconciliation by key* algorithm used by React, Elm, and other virtual DOM frameworks. The insight, formalized by [Hunt & Szymanski (1977)](https://doi.org/10.1145/359460.359467), is that when list items have stable identities, computing the minimal edit script is O(n log n) instead of O(n²).

## Performance with Memoization

The `lazy` function uses the key to check a view cache (default capacity: 2,000 entries). If the key is found and the underlying data hasn't changed, the cached virtual DOM subtree is reused without calling the factory function.

For a list of 1,000 items where only one changed, this means:

- **Without `lazy`**: 1,000 factory calls → 1,000 virtual DOM subtrees → 1,000 diffs
- **With `lazy`**: 1 factory call → 1 virtual DOM subtree → 1 diff + 999 cache hits

## Testing

```csharp
[Fact]
public void AddTodo_AppendsItemToList()
{
    var model = new Model("", [], Filter.All)
        with { Input = "Buy groceries" };

    var (newModel, _) = TodoApp.Transition(model, new AddTodo());

    Assert.Single(newModel.Items);
    Assert.Equal("Buy groceries", newModel.Items[0].Text);
    Assert.False(newModel.Items[0].Completed);
    Assert.Empty(newModel.Input); // input cleared after add
}

[Fact]
public void AddTodo_WithEmptyInput_DoesNothing()
{
    var model = new Model("", [], Filter.All);

    var (newModel, _) = TodoApp.Transition(model, new AddTodo());

    Assert.Empty(newModel.Items);
}

[Fact]
public void ToggleTodo_FlipsCompletedState()
{
    var id = Guid.NewGuid();
    var items = new List<TodoItem> { new(id, "Test", false) };
    var model = new Model("", items, Filter.All);

    var (newModel, _) = TodoApp.Transition(model, new ToggleTodo(id));

    Assert.True(newModel.Items[0].Completed);
}

[Fact]
public void ClearCompleted_RemovesOnlyCompletedItems()
{
    var items = new List<TodoItem>
    {
        new(Guid.NewGuid(), "Done", true),
        new(Guid.NewGuid(), "Not done", false),
        new(Guid.NewGuid(), "Also done", true)
    };
    var model = new Model("", items, Filter.All);

    var (newModel, _) = TodoApp.Transition(model, new ClearCompleted());

    Assert.Single(newModel.Items);
    Assert.Equal("Not done", newModel.Items[0].Text);
}
```

## Exercises

1. **Double-click to edit** — Add an `Editing` state to `TodoItem` and handle `ondblclick` to enter edit mode. You'll need new messages for starting/finishing editing.

2. **Persist to localStorage** — Use commands and an interpreter to save/load todos from browser localStorage (you'll learn this pattern in [Tutorial 3](03-api-integration.md)).

3. **Drag and drop reordering** — Use subscriptions to handle drag events and reorder the list.

4. **"Select all" toggle** — Add a checkbox that toggles all items' completion state at once.

## Key Concepts

| Concept | In This Tutorial |
| --- | --- |
| `oninput(data => msg)` | Accessing input values from DOM events |
| `lazy(key, factory)` | Memoized rendering with stable keys |
| Keyed diffing | O(n log n) list reconciliation |
| Immutable collections | `Select`, `Where`, `Append` → new list |
| Guard patterns | `AddTodo when !string.IsNullOrWhiteSpace(...)` |
| Derived views | Filtering in `View`, not in the model |

## Next Steps

→ [Tutorial 3: API Integration](03-api-integration.md) — Learn commands and the interpreter pattern for side effects