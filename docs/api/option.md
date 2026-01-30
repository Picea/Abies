# Option API Reference

The `Option<T>` type represents values that may or may not be present.

## Usage

```csharp
using Abies;
```

## Overview

`Option<T>` is an alternative to null that makes optionality explicit in the type system. All consumers must handle both the `Some` and `None` cases.

## Types

### Option Interface

```csharp
public interface Option<T>;
```

The base interface for optional values.

### Some Record

```csharp
public readonly record struct Some<T>(T Value) : Option<T>;
```

Represents a present value.

**Properties:**

| Property | Type | Description |
| -------- | ---- | ----------- |
| `Value` | `T` | The contained value |

### None Struct

```csharp
public readonly struct None<T> : Option<T>;
```

Represents an absent value. Uses a struct to avoid allocation.

## Creating Options

### Some

Wrap a value:

```csharp
Option<string> name = new Some<string>("Alice");
Option<int> count = new Some<int>(42);
```

### None

Create an absent value:

```csharp
Option<string> name = new None<string>();
Option<int> count = new None<int>();
```

### From Nullable

Convert nullable to Option:

```csharp
public static Option<T> FromNullable<T>(T? value) where T : class
    => value is not null ? new Some<T>(value) : new None<T>();

public static Option<T> FromNullable<T>(T? value) where T : struct
    => value.HasValue ? new Some<T>(value.Value) : new None<T>();
```

## Pattern Matching

Always handle both cases:

```csharp
string DisplayUser(Option<User> maybeUser) => maybeUser switch
{
    Some<User> some => $"User: {some.Value.Name}",
    None<User> => "No user found",
    _ => throw new InvalidOperationException()
};
```

### In Update Functions

```csharp
public static (Model, Command) Update(Message msg, Model model)
    => msg switch
    {
        UserLoaded loaded => loaded.User switch
        {
            Some<User> some => (model with { User = some.Value }, Commands.None),
            None<User> => (model with { Error = "User not found" }, Commands.None),
            _ => (model, Commands.None)
        },
        _ => (model, Commands.None)
    };
```

## Common Patterns

### Default Values

```csharp
T GetOrDefault<T>(Option<T> option, T defaultValue) => option switch
{
    Some<T> some => some.Value,
    _ => defaultValue
};

// Usage
var name = GetOrDefault(maybeUser.Name, "Anonymous");
```

### Map

Transform the contained value:

```csharp
Option<U> Map<T, U>(Option<T> option, Func<T, U> transform) => option switch
{
    Some<T> some => new Some<U>(transform(some.Value)),
    _ => new None<U>()
};

// Usage
Option<string> maybeName = Map(maybeUser, u => u.Name);
```

### Bind (FlatMap)

Chain optional computations:

```csharp
Option<U> Bind<T, U>(Option<T> option, Func<T, Option<U>> transform) => option switch
{
    Some<T> some => transform(some.Value),
    _ => new None<U>()
};

// Usage
Option<Address> maybeAddress = Bind(maybeUser, u => u.Address);
```

### Filter

Keep value only if predicate matches:

```csharp
Option<T> Filter<T>(Option<T> option, Func<T, bool> predicate) => option switch
{
    Some<T> some when predicate(some.Value) => option,
    _ => new None<T>()
};

// Usage
var maybeAdult = Filter(maybeUser, u => u.Age >= 18);
```

## In View Functions

Handle optional data in views:

```csharp
public static Document View(Model model)
    => new("App", model.CurrentUser switch
    {
        Some<User> some => UserProfile(some.Value),
        None<User> => LoginPrompt(),
        _ => div()
    });

static Element<Model, Unit> UserProfile(User user)
    => div(
        h1(text($"Welcome, {user.Name}!")),
        p(text($"Email: {user.Email}"))
    );

static Element<Model, Unit> LoginPrompt()
    => div(
        h1(text("Please log in")),
        a(href("/login"), text("Login"))
    );
```

## API Integration

Return Option from API calls:

```csharp
async Task<Option<User>> FetchUser(int id)
{
    var response = await httpClient.GetAsync($"/api/users/{id}");
    if (!response.IsSuccessStatusCode)
        return new None<User>();
    
    var user = await response.Content.ReadFromJsonAsync<User>();
    return user is not null 
        ? new Some<User>(user) 
        : new None<User>();
}
```

## Model Design

Use Option for optional fields:

```csharp
public record Model(
    Option<User> CurrentUser,
    Option<Article> SelectedArticle,
    bool IsLoading = false
);

// Initialize with None
var initialModel = new Model(
    CurrentUser: new None<User>(),
    SelectedArticle: new None<Article>()
);
```

## Best Practices

### 1. Prefer Option Over Null

```csharp
// ❌ Avoid
public User? MaybeUser { get; }

// ✅ Better
public Option<User> MaybeUser { get; }
```

### 2. Handle Both Cases

```csharp
// ❌ Avoid - throws on None
var user = ((Some<User>)maybeUser).Value;

// ✅ Better - pattern match
var message = maybeUser switch
{
    Some<User> some => $"Hello, {some.Value.Name}",
    None<User> => "Hello, Guest",
    _ => "Hello"
};
```

### 3. Keep Domain Pure

Use Option in domain logic, convert at boundaries:

```csharp
// Domain layer
Option<User> FindUser(int id);

// API boundary converts to HTTP response
if (FindUser(id) is Some<User> some)
    return Ok(some.Value);
else
    return NotFound();
```

### 4. Avoid Nesting

```csharp
// ❌ Avoid
Option<Option<User>> nested;

// ✅ Flatten
Option<User> flat = Bind(outer, inner => inner);
```

## Comparison with Nullable

| Feature | `Option<T>` | `T?` |
| ------- | ----------- | ---- |
| Explicit handling | Required | Optional |
| Reference types | ✅ | ✅ |
| Value types | ✅ | ✅ |
| Allocation (None) | None (struct) | None |
| Pattern matching | Excellent | Good |
| Compiler enforcement | Always | With nullable enabled |

## See Also

- [Concepts: Pure Functions](../concepts/pure-functions.md)
- [ADR-010: Option Type](../adr/ADR-010-option-type.md)
