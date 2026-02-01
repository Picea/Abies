# Tutorial 2: Todo List

This tutorial teaches you to manage collections: adding, removing, updating, and filtering items.

**Prerequisites:** [Tutorial 1: Counter App](./01-counter-app.md)

**Time:** 30 minutes

## What You'll Build

A todo list application with:

- Add new todos
- Toggle completion status
- Delete todos
- Filter by status (all, active, completed)
- Clear all completed

## Step 1: Define the Model

A todo list needs:

- A list of todo items
- A filter for viewing
- Input for the new todo

```csharp
public record TodoItem(string Id, string Text, bool Completed);

public enum Filter { All, Active, Completed }

public record Model(
    List<TodoItem> Todos,
    Filter CurrentFilter,
    string NewTodoText
);
```

Key design decisions:

- Each `TodoItem` has a unique `Id` for identification
- `Filter` is an enum for the three view states
- `NewTodoText` tracks the input field

## Step 2: Define Messages

What actions can the user take?

```csharp
// Input handling
public record UpdateNewTodoText(string Text) : Message;
public record AddTodo : Message;

// Todo item actions
public record ToggleTodo(string Id) : Message;
public record DeleteTodo(string Id) : Message;

// Filtering
public record SetFilter(Filter Filter) : Message;

// Bulk actions
public record ClearCompleted : Message;
```

## Step 3: Initialize the Model

Start with an empty list:

```csharp
public static (Model, Command) Initialize(Url url, Arguments argument)
    => (new Model(
        Todos: new List<TodoItem>(),
        CurrentFilter: Filter.All,
        NewTodoText: ""
    ), Commands.None);
```

## Step 4: Implement Update

Handle each message:

```csharp
public static (Model model, Command command) Update(Message message, Model model)
    => message switch
    {
        UpdateNewTodoText update => 
            (model with { NewTodoText = update.Text }, Commands.None),
        
        AddTodo when !string.IsNullOrWhiteSpace(model.NewTodoText) =>
            (model with 
            { 
                Todos = [..model.Todos, new TodoItem(
                    Id: Guid.NewGuid().ToString(),
                    Text: model.NewTodoText.Trim(),
                    Completed: false
                )],
                NewTodoText = ""
            }, Commands.None),
        
        AddTodo => // Empty text, do nothing
            (model, Commands.None),
        
        ToggleTodo toggle =>
            (model with 
            { 
                Todos = model.Todos
                    .Select(t => t.Id == toggle.Id 
                        ? t with { Completed = !t.Completed } 
                        : t)
                    .ToList()
            }, Commands.None),
        
        DeleteTodo delete =>
            (model with 
            { 
                Todos = model.Todos
                    .Where(t => t.Id != delete.Id)
                    .ToList()
            }, Commands.None),
        
        SetFilter set =>
            (model with { CurrentFilter = set.Filter }, Commands.None),
        
        ClearCompleted =>
            (model with 
            { 
                Todos = model.Todos
                    .Where(t => !t.Completed)
                    .ToList()
            }, Commands.None),
        
        _ => (model, Commands.None)
    };
```

Notice:

- **Guard clause**: `AddTodo when !string.IsNullOrWhiteSpace(...)` prevents empty todos
- **Collection spread**: `[..model.Todos, newItem]` creates a new list with the item added
- **LINQ for updates**: `Select` and `Where` create new collections immutably

## Step 5: Build the View

Break the view into logical sections:

```csharp
public static Document View(Model model)
{
    var filteredTodos = model.CurrentFilter switch
    {
        Filter.Active => model.Todos.Where(t => !t.Completed).ToList(),
        Filter.Completed => model.Todos.Where(t => t.Completed).ToList(),
        _ => model.Todos
    };
    
    var activeCount = model.Todos.Count(t => !t.Completed);
    var completedCount = model.Todos.Count(t => t.Completed);
    
    return new("Todo List",
        div([class_("todo-app")], [
            // Header with input
            Header(model.NewTodoText),
            
            // Todo list
            TodoList(filteredTodos),
            
            // Footer with filters and count
            Footer(model.CurrentFilter, activeCount, completedCount)
        ]));
}
```

### Header Component

```csharp
static Node Header(string newTodoText) =>
    header([class_("header")], [
        h1([], [text("todos")]),
        input([
            class_("new-todo"),
            placeholder("What needs to be done?"),
            value(newTodoText),
            oninput(data => new UpdateNewTodoText(data?.Value ?? "")),
            onkeydown(data => data?.Key == "Enter" ? new AddTodo() : null)
        ], [])
    ]);
```

Note: The `onkeydown` handler only dispatches `AddTodo` when Enter is pressed.

### Todo List Component

```csharp
static Node TodoList(List<TodoItem> todos) =>
    ul([class_("todo-list")], 
        todos.Select(TodoItemView).ToArray());

static Node TodoItemView(TodoItem todo) =>
    li([class_(todo.Completed ? "completed" : "")], [
        div([class_("view")], [
            input([
                class_("toggle"),
                type("checkbox"),
                todo.Completed ? checked_("checked") : null,
                onclick(new ToggleTodo(todo.Id))
            ], []),
            label([], [text(todo.Text)]),
            button([
                class_("destroy"),
                onclick(new DeleteTodo(todo.Id))
            ], [])
        ])
    ]);
```

Note: We use `Select` to transform each `TodoItem` into a `Node`.

### Footer Component

```csharp
static Node Footer(Filter currentFilter, int activeCount, int completedCount) =>
    footer([class_("footer")], [
        span([class_("todo-count")], [
            text($"{activeCount} item{(activeCount == 1 ? "" : "s")} left")
        ]),
        ul([class_("filters")], [
            FilterButton("All", Filter.All, currentFilter),
            FilterButton("Active", Filter.Active, currentFilter),
            FilterButton("Completed", Filter.Completed, currentFilter)
        ]),
        completedCount > 0
            ? button([class_("clear-completed"), onclick(new ClearCompleted())], 
                [text("Clear completed")])
            : text("")
    ]);

static Node FilterButton(string label, Filter filter, Filter current) =>
    li([], [
        a([
            class_(filter == current ? "selected" : ""),
            href("#"),
            onclick(new SetFilter(filter))
        ], [text(label)])
    ]);
```

## Step 6: Complete Program

Here's the full implementation:

```csharp
using Abies;
using Abies.DOM;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

await Runtime.Run<TodoApp, Arguments, Model>(new Arguments());

public record Arguments;

// Domain types
public record TodoItem(string Id, string Text, bool Completed);
public enum Filter { All, Active, Completed }

// Model
public record Model(List<TodoItem> Todos, Filter CurrentFilter, string NewTodoText);

// Messages
public record UpdateNewTodoText(string Text) : Message;
public record AddTodo : Message;
public record ToggleTodo(string Id) : Message;
public record DeleteTodo(string Id) : Message;
public record SetFilter(Filter Filter) : Message;
public record ClearCompleted : Message;

public class TodoApp : Program<Model, Arguments>
{
    public static (Model, Command) Initialize(Url url, Arguments argument)
        => (new Model(
            Todos: new List<TodoItem>(),
            CurrentFilter: Filter.All,
            NewTodoText: ""
        ), Commands.None);

    public static (Model model, Command command) Update(Message message, Model model)
        => message switch
        {
            UpdateNewTodoText update => 
                (model with { NewTodoText = update.Text }, Commands.None),
            
            AddTodo when !string.IsNullOrWhiteSpace(model.NewTodoText) =>
                (model with 
                { 
                    Todos = [..model.Todos, new TodoItem(
                        Id: Guid.NewGuid().ToString(),
                        Text: model.NewTodoText.Trim(),
                        Completed: false
                    )],
                    NewTodoText = ""
                }, Commands.None),
            
            AddTodo => (model, Commands.None),
            
            ToggleTodo toggle =>
                (model with 
                { 
                    Todos = model.Todos
                        .Select(t => t.Id == toggle.Id 
                            ? t with { Completed = !t.Completed } 
                            : t)
                        .ToList()
                }, Commands.None),
            
            DeleteTodo delete =>
                (model with 
                { 
                    Todos = model.Todos.Where(t => t.Id != delete.Id).ToList()
                }, Commands.None),
            
            SetFilter set =>
                (model with { CurrentFilter = set.Filter }, Commands.None),
            
            ClearCompleted =>
                (model with 
                { 
                    Todos = model.Todos.Where(t => !t.Completed).ToList()
                }, Commands.None),
            
            _ => (model, Commands.None)
        };

    public static Document View(Model model)
    {
        var filteredTodos = model.CurrentFilter switch
        {
            Filter.Active => model.Todos.Where(t => !t.Completed).ToList(),
            Filter.Completed => model.Todos.Where(t => t.Completed).ToList(),
            _ => model.Todos
        };
        
        var activeCount = model.Todos.Count(t => !t.Completed);
        var completedCount = model.Todos.Count(t => t.Completed);
        
        return new("Todo List",
            div([class_("todo-app")], [
                Header(model.NewTodoText),
                TodoList(filteredTodos),
                model.Todos.Count > 0 
                    ? Footer(model.CurrentFilter, activeCount, completedCount)
                    : text("")
            ]));
    }

    static Node Header(string newTodoText) =>
        header([class_("header")], [
            h1([], [text("todos")]),
            input([
                class_("new-todo"),
                placeholder("What needs to be done?"),
                value(newTodoText),
                oninput(data => new UpdateNewTodoText(data?.Value ?? ""))
            ], [])
        ]);

    static Node TodoList(List<TodoItem> todos) =>
        ul([class_("todo-list")], 
            todos.Select(TodoItemView).ToArray());

static Node TodoItemView(TodoItem todo) =>
    li([class_(todo.Completed ? "completed" : "")], [
        div([class_("view")], [
            input([
                class_("toggle"),
                type("checkbox"),
                onclick(new ToggleTodo(todo.Id))
            ], []),
            label([], [text(todo.Text)]),
            button([class_("destroy"), onclick(new DeleteTodo(todo.Id))], [])
        ])
    ], id: $"todo-{todo.Id}");  // Stable ID for efficient list diffing    static Node Footer(Filter currentFilter, int activeCount, int completedCount) =>
        footer([class_("footer")], [
            span([class_("todo-count")], [
                text($"{activeCount} item{(activeCount == 1 ? "" : "s")} left")
            ]),
            ul([class_("filters")], [
                FilterButton("All", Filter.All, currentFilter),
                FilterButton("Active", Filter.Active, currentFilter),
                FilterButton("Completed", Filter.Completed, currentFilter)
            ]),
            completedCount > 0
                ? button([class_("clear-completed"), onclick(new ClearCompleted())], 
                    [text("Clear completed")])
                : text("")
        ]);

    static Node FilterButton(string label, Filter filter, Filter current) =>
        li([], [
            a([
                class_(filter == current ? "selected" : ""),
                href("#"),
                onclick(new SetFilter(filter))
            ], [text(label)])
        ]);

    public static Message OnUrlChanged(Url url) => new SetFilter(Filter.All);
    public static Message OnLinkClicked(UrlRequest urlRequest) => new SetFilter(Filter.All);
    public static Subscription Subscriptions(Model model) => SubscriptionModule.None;
    public static Task HandleCommand(Command command, Func<Message, ValueTuple> dispatch)
        => Task.CompletedTask;
}
```

## What You Learned

| Concept | Application |
| ------- | ----------- |
| List management | Add, remove, update items immutably |
| Derived state | Filter todos in View, not in Model |
| Guard patterns | `AddTodo when !string.IsNullOrWhiteSpace(...)` |
| Component functions | Break View into reusable functions |
| Conditional rendering | Show/hide footer and clear button |

## Patterns to Remember

### Adding to a list

```csharp
Todos = [..model.Todos, newItem]
```

### Updating an item in a list

```csharp
Todos = model.Todos
    .Select(t => t.Id == targetId ? t with { ... } : t)
    .ToList()
```

### Removing from a list

```csharp
Todos = model.Todos.Where(t => t.Id != targetId).ToList()
```

### Filtering for display (in View, not Model)

```csharp
var filtered = model.Filter switch
{
    Filter.Active => model.Todos.Where(t => !t.Completed),
    // ...
};
```

## Exercises

1. **Edit todos**: Double-click to edit a todo's text
2. **Toggle all**: Add a checkbox to mark all as complete
3. **Persist todos**: Save to localStorage (requires a Command)
4. **Drag to reorder**: Allow reordering via drag and drop

## Next Tutorial

→ [Tutorial 3: API Integration](./03-api-integration.md) — Learn to fetch and persist data
