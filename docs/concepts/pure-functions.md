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

### Transition is Pure

Transition takes a model and message, returning a new model and command. It never performs I/O.

```csharp
// ✅ Pure Transition
public static (Model, Command) Transition(Model model, Message msg)
    => msg switch
    {
        Increment => (model with { Count = model.Count + 1 }, Commands.None),
        DataLoaded d => (model with { Data = d.Items }, Commands.None),
        _ => (model, Commands.None)
    };

// ❌ Impure Transition (DON'T DO THIS)
public static (Model, Command) Transition(Model model, Message msg)
{
    if (msg is Save)
    {
        File.WriteAllText("data.json", model.ToString()); // Side effect!
    }
    return (model, Commands.None);
}
```

> **Note:** In the Picea kernel, this function is called `Transition`. MVU literature often calls it "Update" — they are the same concept.

## Why Purity Matters

### 1. Testability

Pure functions are trivially testable. No mocking, no setup, no teardown.

```csharp
[Fact]
public void Increment_IncreasesCount()
{
    var model = new Model(Count: 5);

    var (result, _) = Transition(model, new Increment());

    Assert.Equal(6, result.Count);
}
```

### 2. Predictability

Given the same model, View always produces the same UI. Given the same message and model, Transition always produces the same result.

```csharp
// This is guaranteed to be true:
var (result1, _) = Transition(model, new Increment());
var (result2, _) = Transition(model, new Increment());
Assert.Equal(result1, result2);
```

### 3. Debuggability

You can replay message sequences to recreate any state:

```csharp
var initialModel = Program.Initialize(args);
var messages = GetRecordedMessages(); // From logging

var finalModel = messages.Aggregate(
    initialModel,
    (model, msg) => Transition(model, msg).model
);
```

### 4. Platform Independence

Pure functions don't depend on the runtime platform. The same View and Transition run identically in the browser (WASM), on the server (Kestrel), and in tests.

### 5. Caching

Pure function results can be cached (memoized) safely. Abies uses this with `lazy()` — when the memo key hasn't changed, the view function isn't even called.

## How Abies Enforces Purity

### 1. Command Pattern

Side effects aren't performed in Transition — they're described as Commands:

```csharp
public static (Model, Command) Transition(Model model, Message msg)
    => msg switch
    {
        FetchData => (model with { IsLoading = true }, new LoadDataCommand()),
        _ => (model, Commands.None)
    };
```

### 2. Immutable Models

Using records makes mutation impossible:

```csharp
public record Model(int Count, string Name);

// Can't mutate, must create new
var newModel = model with { Count = model.Count + 1 };
```

### 3. Virtual DOM

View returns data (virtual DOM), not actual DOM mutations. The runtime handles the impure rendering through the Apply delegate.

### 4. Interpreter Boundary

The interpreter is the only place where impurity lives:

```csharp
// Pure side (your code):
Transition(model, msg) => (newModel, new FetchDataCommand());

// Impure side (interpreter):
interpreter = async cmd => {
    var data = await httpClient.GetAsync(...);  // Impurity isolated here
    return Ok([new DataLoaded(data)]);
};
```

## Handling Impurity

Sometimes you need randomness, timestamps, or other "impure" values. The pattern is to move impurity to the edges.

### Pattern 1: Push Impurity to Initialization

```csharp
public static (Model, Command) Initialize(Unit _)
    => (new Model(Id: Guid.NewGuid().ToString()), Commands.None);

// From then on, Transition uses the ID purely
case SaveData:
    return (model, new SaveCommand(model.Id, model.Data));
```

### Pattern 2: Include Values in Messages (via Interpreter)

```csharp
// Interpreter captures impurity:
case GetCurrentTime:
    return Ok([new TimeReceived(DateTime.UtcNow)]);

// Transition uses the time purely:
case TimeReceived t:
    return (model with { LastUpdated = t.Time }, Commands.None);
```

### Pattern 3: Subscriptions for External Events

```csharp
public static Subscription Subscriptions(Model model)
    => Every(TimeSpan.FromSeconds(1), now => new TimerTick(now));
```

## Common Mistakes

### 1. Calling APIs in Transition

```csharp
// ❌ WRONG
case Refresh:
    var data = await api.GetData(); // Can't await in Transition!
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
- **Transition** transforms model + message to new model + command (pure)
- **Interpreter** performs actual side effects (impure, isolated at the boundary)

This separation makes your code:

- ✅ Easy to test
- ✅ Easy to reason about
- ✅ Easy to debug
- ✅ Predictable and reliable
- ✅ Platform-agnostic (same code runs in browser and server)

## See Also

- [MVU Architecture](./mvu-architecture.md) — The overall pattern
- [Commands and Effects](./commands-effects.md) — Handling impurity
- [Render Modes](./render-modes.md) — How purity enables multi-platform execution
