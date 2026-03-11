# Option API Reference

The `Option<T>` type represents values that may or may not be present.

## Usage

```csharp
using Automaton;
```

> **Note:** `Option<T>` is defined in the `Automaton` kernel package, not in `Picea.Abies` itself. Abies applications use it for domain modeling.

## Overview

`Option<T>` is a `readonly struct` that makes optionality explicit in the type system. Unlike nullable reference types, `Option<T>` forces callers to handle both the `Some` and `None` cases — there is no way to accidentally access a missing value without an explicit decision.

The type is stack-allocated with zero GC pressure. A `bool` discriminator replaces virtual dispatch.

## Algebraic Structure

```
Option<T> ≅ 1 + T    (coproduct / sum type)
Map/Select  : (T → U) → Option<T> → Option<U>              (functor)
Bind        : (T → Option<U>) → Option<T> → Option<U>      (monad)
Match       : (T → R) × (() → R) → Option<T> → R           (catamorphism)
```

## Type Definition

```csharp
public readonly struct Option<T>
{
    public static Option<T> None { get; }
    public static Option<T> Some(T value);

    public bool IsSome { get; }
    public bool IsNone { get; }
    public T Value { get; } // Throws InvalidOperationException if None

    // Functor
    public Option<TNew> Map<TNew>(Func<T, TNew> f);
    public Option<TNew> Select<TNew>(Func<T, TNew> selector); // LINQ alias

    // Monad
    public Option<TNew> Bind<TNew>(Func<T, Option<TNew>> f);
    public Option<TNew> SelectMany<TIntermediate, TNew>(
        Func<T, Option<TIntermediate>> bind,
        Func<T, TIntermediate, TNew> project); // LINQ alias

    // Catamorphism
    public TOut Match<TOut>(Func<T, TOut> some, Func<TOut> none);
    public void Switch(Action<T> some, Action none);

    // Try-pattern
    public bool TryGetValue(out T value);

    // Defaults
    public T DefaultValue(T fallback);
    public T DefaultWith(Func<T> fallback);

    // Filter
    public Option<T> Where(Func<T, bool> predicate);

    // Conversion
    public Result<T, TError> ToResult<TError>(TError error);

    // Implicit conversion
    public static implicit operator Option<T>(T value); // T → Some(T)
}
```

## Factory Class

```csharp
public static class Option
{
    public static Option<T> Some<T>(T value);  // Type-inferred creation
    public static Option<T> None<T>();         // Type-inferred None
}
```

## Creating Options

### Some

```csharp
// Via static method on Option<T>
Option<string> name = Option<string>.Some("Alice");
Option<int> count = Option<int>.Some(42);

// Via factory class (type inference)
Option<string> name = Option.Some("Alice");
var count = Option.Some(42);

// Via implicit conversion
Option<int> fromValue = 42;  // Some(42)
```

### None

```csharp
Option<string> name = Option<string>.None;
Option<int> count = Option<int>.None;

// Via factory class
var empty = Option.None<string>();
```

## Pattern Matching

### Match (Catamorphism)

The primary way to consume an `Option<T>` — forces exhaustive handling of both cases:

```csharp
string message = maybeUser.Match(
    some: user => $"Hello, {user.Name}!",
    none: () => "No user found"
);
```

### Switch (Side Effects)

```csharp
maybeUser.Switch(
    some: user => Console.WriteLine($"Found: {user.Name}"),
    none: () => Console.WriteLine("Not found")
);
```

### TryGetValue (Try-Pattern)

```csharp
if (maybeUser.TryGetValue(out var user))
{
    Console.WriteLine($"Found: {user.Name}");
}
```

### C# Pattern Matching

```csharp
var message = maybeUser switch
{
    { IsSome: true, Value: var user } => $"Hello, {user.Name}!",
    _ => "No user found"
};
```

### In Transition Functions

```csharp
public static (Model, Command) Transition(Model model, Message message)
    => message switch
    {
        UserLoaded(var maybeUser) => maybeUser.Match(
            some: user => (model with { User = user }, Commands.None),
            none: () => (model with { Error = "User not found" }, Commands.None)
        ),
        _ => (model, Commands.None)
    };
```

## Transformations

### Map (Functor)

Transform the contained value if present:

```csharp
Option<string> maybeName = maybeUser.Map(u => u.Name);
// Some(User("Alice")) → Some("Alice")
// None                → None
```

### Bind (Monad)

Chain computations that may fail:

```csharp
Option<Address> maybeAddress = maybeUser.Bind(u => u.Address);
// Some(User(Address(...))) → Some(Address(...))
// Some(User(no address))   → None
// None                     → None
```

### Where (Filter)

Keep value only if predicate matches:

```csharp
Option<User> maybeAdult = maybeUser.Where(u => u.Age >= 18);
// Some(User(Age=25)) → Some(User(Age=25))
// Some(User(Age=15)) → None
// None               → None
```

## Default Values

### DefaultValue

Return the contained value or a fallback:

```csharp
string name = maybeName.DefaultValue("Anonymous");
```

### DefaultWith

Return the contained value or a lazily-evaluated fallback:

```csharp
string name = maybeName.DefaultWith(() => GenerateGuestName());
```

## LINQ Query Syntax

`Option<T>` supports LINQ query comprehensions via `Select` and `SelectMany`:

```csharp
var result =
    from user in maybeUser
    from address in user.Address
    select $"{user.Name} lives at {address.City}";
// Only produces Some if both user and address are present
```

This desugars to:

```csharp
maybeUser.SelectMany(
    user => user.Address,
    (user, address) => $"{user.Name} lives at {address.City}"
);
```

## Conversion to Result

Convert an `Option<T>` to a `Result<T, TError>`, providing an error for the `None` case:

```csharp
Result<User, string> result = maybeUser.ToResult("User not found");
// Some(user) → Ok(user)
// None       → Err("User not found")
```

## In View Functions

Handle optional data in views:

```csharp
public static Document View(Model model)
    => new("App",
        model.CurrentUser.Match(
            some: user => UserProfile(user),
            none: () => LoginPrompt()
        ));

static Node UserProfile(User user)
    => div([], [
        h1([], [text($"Welcome, {user.Name}!")]),
        p([], [text($"Email: {user.Email}")])
    ]);

static Node LoginPrompt()
    => div([], [
        h1([], [text("Please log in")]),
        a([href("/login")], [text("Login")])
    ]);
```

## Model Design

Use `Option<T>` for fields that may or may not have a value:

```csharp
public record Model(
    Option<User> CurrentUser,
    Option<Article> SelectedArticle,
    bool IsLoading = false
);

// Initialize with None
var initialModel = new Model(
    CurrentUser: Option<User>.None,
    SelectedArticle: Option<Article>.None
);
```

## Best Practices

### 1. Prefer Match Over Value

```csharp
// ❌ Avoid — throws on None
var user = maybeUser.Value;

// ✅ Better — exhaustive
var name = maybeUser.Match(
    some: u => u.Name,
    none: () => "Anonymous"
);
```

### 2. Use TryGetValue for Conditional Logic

```csharp
// ✅ Familiar try-pattern
if (maybeUser.TryGetValue(out var user))
{
    // Use user
}
```

### 3. Use Implicit Conversion for Concise Construction

```csharp
// ✅ Implicit conversion from T to Option<T>
Option<int> score = 42;  // Same as Option.Some(42)
```

### 4. Leverage LINQ for Chaining

```csharp
// ✅ Compose optional computations
var city =
    from user in maybeUser
    from addr in user.Address
    from city in addr.City
    select city;
```

### 5. Keep Domain Pure

Use `Option<T>` in domain logic, convert at boundaries:

```csharp
// Domain layer
Option<User> FindUser(int id);

// API boundary converts to HTTP response
var result = FindUser(id);
if (result.TryGetValue(out var user))
    return Ok(user);
else
    return NotFound();
```

### 6. Avoid Nesting

```csharp
// ❌ Avoid
Option<Option<User>> nested;

// ✅ Flatten with Bind
Option<User> flat = outer.Bind(inner => inner);
```

## Comparison with Nullable

| Feature | `Option<T>` | `T?` |
| ------- | ----------- | ---- |
| Explicit handling | Required (Match/TryGetValue) | Optional |
| Reference types | ✅ | ✅ |
| Value types | ✅ | ✅ (wraps in Nullable<T>) |
| Allocation | None (readonly struct) | None |
| GC pressure | Zero | Zero |
| Pattern matching | Match / LINQ / TryGetValue | `is not null` |
| Compiler enforcement | Always | With nullable enabled |
| Monadic composition | Map / Bind / LINQ | Manual null checks |
| Result conversion | `ToResult(error)` | Manual |

## See Also

- [Concepts: Pure Functions](../concepts/pure-functions.md)
