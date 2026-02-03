# Pure Functions

Abies is built around pure functions. This document explains what purity means, why it matters, and how Abies enforces it.

## What is a Pure Function?

A pure function has two properties:

1. **Deterministic** — Same inputs always produce same outputs
2. **No side effects** — Doesn't modify external state or perform I/O

```csharp
// Pure: same inputs → same output, no side effects
static int Add(int a, int b) => a + b;

// Impure: depends on external state
static int AddToCounter(int a) => a + _counter++;

// Impure: performs I/O
static int GetFromApi(int id) => httpClient.Get(id).Result;
```

## Pure Functions in Abies

### View is Pure

View takes a model and returns a virtual DOM tree. Nothing else.

```csharp
// ✅ Pure View
public static Document View(Model model)
    => new("Counter",
        div([], [
            text($"Count: {model.Count}")
        ]));

// ❌ Impure View (DON'T DO THIS)
public static Document View(Model model)
{
    Console.WriteLine("Rendering..."); // Side effect!
    var time = DateTime.Now;           // Non-deterministic!
    return new("Counter", div([], [text($"{model.Count} at {time}")]));
}
```

### Update is Pure

Update takes a message and model, returns new model and command. It never performs I/O.

```csharp
// ✅ Pure Update
public static (Model, Command) Update(Message msg, Model model)
    => msg switch
    {
        Increment => (model with { Count = model.Count + 1 }, Commands.None),
        DataLoaded d => (model with { Data = d.Items }, Commands.None),
        _ => (model, Commands.None)
    };

// ❌ Impure Update (DON'T DO THIS)
public static (Model, Command) Update(Message msg, Model model)
{
    if (msg is Save)
    {
        File.WriteAllText("data.json", model.ToString()); // Side effect!
    }
    return (model, Commands.None);
}
```

## Why Purity Matters

### 1. Testability

Pure functions are trivially testable. No mocking, no setup, no teardown.

```csharp
[Fact]
public void Increment_IncreasesCount()
{
    var model = new Model(Count: 5);
    
    var (result, _) = Update(new Increment(), model);
    
    Assert.Equal(6, result.Count);
}

[Fact]
public void View_ShowsCount()
{
    var model = new Model(Count: 42);
    
    var document = View(model);
    
    Assert.Contains("42", RenderToString(document));
}
```

### 2. Predictability

Given the same model, View always produces the same UI. Given the same message and model, Update always produces the same result.

```csharp
// This is guaranteed to be true:
var (result1, _) = Update(new Increment(), model);
var (result2, _) = Update(new Increment(), model);
Assert.Equal(result1, result2);
```

### 3. Debuggability

You can replay message sequences to recreate any state:

```csharp
var initialModel = Program.Initialize(args);
var messages = GetRecordedMessages(); // From logging

var finalModel = messages.Aggregate(
    initialModel,
    (model, msg) => Update(msg, model).model
);
```

### 4. Parallelization

Pure functions can run in parallel without locks or synchronization because they don't share mutable state.

### 5. Caching

Pure function results can be cached (memoized) safely:

```csharp
var cache = new Dictionary<Model, Document>();

Document GetCachedView(Model model)
{
    if (!cache.TryGetValue(model, out var doc))
    {
        doc = View(model);
        cache[model] = doc;
    }
    return doc;
}
```

## How Abies Enforces Purity

### 1. Command Pattern

Side effects aren't performed in Update—they're described as Commands:

```csharp
// Update returns a command describing what to do
public static (Model, Command) Update(Message msg, Model model)
    => msg switch
    {
        FetchData => (model with { IsLoading = true }, new LoadDataCommand()),
        _ => (model, Commands.None)
    };

// Runtime handles the impure part
public static async Task HandleCommand(Command cmd, Dispatch dispatch)
{
    if (cmd is LoadDataCommand)
    {
        var data = await httpClient.GetAsync("/api/data"); // Impure, but isolated
        dispatch(new DataLoaded(data));
    }
}
```

### 2. Immutable Models

Using records makes mutation impossible:

```csharp
public record Model(int Count, string Name);

// Can't mutate, must create new
var newModel = model with { Count = model.Count + 1 };
```

### 3. Virtual DOM

View returns data (virtual DOM), not actual DOM mutations. The runtime handles the impure rendering.

```csharp
// Returns data structure, not side effects
public static Document View(Model model)
    => new("Title", div([], [text(model.Name)]));
```

## Handling Impurity

Sometimes you need randomness, timestamps, or other "impure" values. The pattern is to move impurity to the edges.

### Pattern 1: Push Impurity to Initialization

```csharp
// Impure: get random ID at startup
var model = new Model(Id: Guid.NewGuid().ToString());

// From then on, Update uses the ID purely
public static (Model, Command) Update(Message msg, Model model)
    => msg switch
    {
        // Uses model.Id purely
        SaveData => (model, new SaveCommand(model.Id, model.Data)),
        _ => (model, Commands.None)
    };
```

### Pattern 2: Include Values in Messages

```csharp
// Command triggers side effect
public record GetCurrentTime : Command;

// HandleCommand captures impurity
case GetCurrentTime:
    dispatch(new TimeReceived(DateTime.UtcNow));
    break;

// Update uses the time purely
case TimeReceived t:
    return (model with { LastUpdated = t.Time }, Commands.None);
```

### Pattern 3: Subscriptions for External Events

```csharp
public static Subscription Subscriptions(Model model)
    => Every(TimeSpan.FromSeconds(1), now => new TimerTick(now));
```

## Pure vs Impure Reference

| Pure | Impure |
| ---- | ------ |
| `a + b` | `Console.WriteLine(x)` |
| `model with { X = 1 }` | `model.X = 1` |
| `list.Where(x => x > 0)` | `await httpClient.GetAsync(url)` |
| `string.Concat(a, b)` | `File.ReadAllText(path)` |
| Pattern matching | Database queries |
| Creating records | Random number generation |
| Virtual DOM construction | DOM manipulation |

## Common Mistakes

### 1. Calling APIs in Update

```csharp
// ❌ WRONG
case Refresh:
    var data = await api.GetData(); // Can't await in Update!
    return (model with { Data = data }, Commands.None);

// ✅ RIGHT
case Refresh:
    return (model with { IsLoading = true }, new LoadDataCommand());
```

### 2. Using DateTime.Now in View

```csharp
// ❌ WRONG
public static Document View(Model model)
    => new("App", text($"Time: {DateTime.Now}")); // Different each render!

// ✅ RIGHT
public static Document View(Model model)
    => new("App", text($"Time: {model.CurrentTime}")); // Uses model
```

### 3. Mutating State

```csharp
// ❌ WRONG
case AddItem item:
    model.Items.Add(item.Value); // Mutating!
    return (model, Commands.None);

// ✅ RIGHT
case AddItem item:
    return (model with { Items = [..model.Items, item.Value] }, Commands.None);
```

## Summary

Pure functions are the foundation of Abies:

- **View** transforms model to virtual DOM (pure)
- **Update** transforms message + model to new model + command (pure)
- **HandleCommand** performs actual side effects (impure, isolated)

This separation makes your code:

- ✅ Easy to test
- ✅ Easy to reason about
- ✅ Easy to debug
- ✅ Predictable and reliable

## See Also

- [MVU Architecture](./mvu-architecture.md) — The overall pattern
- [Commands and Effects](./commands-effects.md) — Handling impurity
