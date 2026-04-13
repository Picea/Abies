# Performance Guide

Best practices for building fast Abies applications.

## Overview

Abies is designed for performance:

- Binary batch protocol for efficient DOM patching
- Virtual DOM diffing with head/tail skip and LIS algorithm
- Lazy memoization to skip unchanged subtrees
- Object pooling to reduce garbage collection
- WebAssembly provides near-native speed

Current benchmark results change over time and are published in [Performance Benchmarks](../benchmarks.md). Treat that page as the canonical source for latest numbers.

## Memoization (Most Important)

The single most impactful optimization is `lazy()`. When the memo key hasn't changed, the entire subtree is skipped during diffing:

```csharp
// ❌ Without memoization — diffs every row every render
ul([], todos.Select(todo =>
    li([], [text(todo.Title)])
).ToArray())

// ✅ With lazy() — skips unchanged rows entirely
ul([], todos.Select(todo =>
    lazy((todo.Id, todo.Title, todo.Completed), () =>
        li([], [text(todo.Title)]))
).ToArray())
```

### How It Works

1. Each `lazy()` call has a compile-time unique ID (Praefixum)
2. On re-render, the key is compared against the previous render's key
3. If equal → the factory function is **never called** and the subtree is **never diffed**
4. The view cache returns the same object reference, enabling `ReferenceEquals` bailout

### Choose Keys Carefully

Include everything that affects the output:

```csharp
// ❌ Key doesn't include selection state — won't re-render on select
lazy(todo.Id, () => TodoItem(todo, isSelected))

// ✅ Key includes all data that affects the view
lazy((todo.Id, todo.Title, todo.Completed, isSelected), () =>
    TodoItem(todo, isSelected))
```

### `lazy()` vs `memo()`

| Function | When to use | Key match behavior |
| -------- | ----------- | ------------------ |
| `lazy(key, factory)` | Expensive subtrees | Skips factory call AND diffing |
| `memo(key, node)` | Cheap to construct, expensive to diff | Skips diffing only |

## Stable IDs for Dynamic Lists

For lists that can reorder, add, or remove items, use the `id:` parameter:

```csharp
// ❌ Position-based diffing — recreates all nodes on reorder
ul([], todos.Select(todo =>
    li([], [text(todo.Title)])
).ToArray())

// ✅ Keyed diffing — moves DOM nodes efficiently
ul([], todos.Select(todo =>
    li([], [text(todo.Title)], id: $"todo-{todo.Id}")
).ToArray())
```

Keyed diffing uses the LIS (Longest Increasing Subsequence) algorithm to compute the minimum number of DOM moves.

## Transition Optimization

### Early Exit on No Change

```csharp
public static (Model, Command) Transition(Model model, Message msg)
    => msg switch
    {
        SetFilter filter when filter.Value == model.Filter
            => (model, Commands.None),  // No change, no re-render

        SetFilter filter
            => (model with { Filter = filter.Value }, Commands.None),

        _ => (model, Commands.None)
    };
```

### Use `record struct` for Hot-Path Messages

Messages created on every user interaction should be value types:

```csharp
// ✅ Stack-allocated, zero GC pressure
public interface CounterMessage : Message
{
    record struct Increment : CounterMessage;
    record struct Decrement : CounterMessage;
}

// ❌ Heap-allocated on every click
public record Increment : Message;
```

### Immutable Collections

```csharp
// ❌ Creates new list by copying
model with { Items = [..model.Items, newItem] }  // O(n) copy

// ✅ For large collections, consider ImmutableList
model with { Items = model.Items.Add(newItem) }  // O(log n) structural sharing
```

## View Optimization

### Extract Static Elements

```csharp
// ❌ Recreated every render
public static Document View(Model model) =>
    new("App", div([], [
        header([], [
            nav([], [
                a([href("/")], [text("Home")]),
                a([href("/about")], [text("About")])
            ])
        ]),
        DynamicContent(model)
    ]));

// ✅ Static parts extracted (created once)
static readonly Node StaticHeader = header([], [
    nav([], [
        a([href("/")], [text("Home")]),
        a([href("/about")], [text("About")])
    ])
]);

public static Document View(Model model) =>
    new("App", div([], [
        StaticHeader,  // Same reference every render
        DynamicContent(model)
    ]));
```

### Conditional Rendering

Only render visible content:

```csharp
static Node TabContent(Model model) =>
    model.ActiveTab switch
    {
        Tab.Articles => ArticleList(model.Articles),
        Tab.Users => UserList(model.Users),
        Tab.Settings => SettingsPanel(model.Settings),
        _ => div([], [])
    };

// Don't render all tabs and hide with CSS:
// ❌ div([style("display: none")], [ExpensiveComponent()])
```

## Command Optimization

### Debounce Rapid Commands

For search inputs, debounce in the interpreter:

```csharp
public static (Model, Command) Transition(Model model, Message msg)
    => msg switch
    {
        SearchChanged changed =>
            (model with { SearchQuery = changed.Query },
             new DebouncedSearch(changed.Query)),
        _ => (model, Commands.None)
    };

// In interpreter:
case DebouncedSearch search:
    await Task.Delay(300);  // Debounce
    var results = await api.Search(search.Query);
    return Ok([new SearchResults(results)]);
```

### Parallel Commands

```csharp
// In interpreter:
case LoadDashboard:
    var (articles, users, stats) = await (
        api.GetArticles(),
        api.GetUsers(),
        api.GetStats()
    ).WhenAll();
    return Ok([new DashboardLoaded(articles, users, stats)]);
```

## Build Optimization

### AOT Compilation

```xml
<PropertyGroup>
    <RunAOTCompilation>true</RunAOTCompilation>
</PropertyGroup>
```

### Trimming

```xml
<PropertyGroup>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>link</TrimMode>
</PropertyGroup>
```

### Compression

```xml
<PropertyGroup>
    <BlazorEnableCompression>true</BlazorEnableCompression>
</PropertyGroup>
```

## Measuring Performance

### js-framework-benchmark

The authoritative benchmark for comparing frontend framework performance:

```bash
# Build Abies for benchmark
cd js-framework-benchmark/frameworks/keyed/abies/src
rm -rf bin obj && dotnet publish -c Release
cp -R bin/Release/net10.0/publish/wwwroot/* ../bundled-dist/

# Run benchmarks
cd ../../../../webdriver-ts
npm run bench -- --headless --framework abies-keyed
```

### Browser DevTools

1. Open DevTools (F12) → Performance tab
2. Click Record
3. Perform actions
4. Stop recording
5. Analyze the flame chart — look for long script execution

### OpenTelemetry

Abies includes built-in tracing:

```javascript
// Enable debug verbosity to see DOM mutations
window.__otel.setVerbosity('debug');
```

> Intended behavior note: runtime `window.__otel` controls are tracked in [Issue #214](https://github.com/Picea/Abies/issues/214). Until implemented, use meta tags or URL parameters.

View traces in the Aspire dashboard.

## What the Framework Already Optimizes

You get these for free — no action needed:

| Optimization | What it does |
| ------------ | ------------ |
| Binary batch protocol | 17% faster than JSON for DOM patches |
| Head/tail skip | O(1) skip for common prefix/suffix in lists |
| LIS algorithm | Minimum DOM moves for reordering |
| Object pooling | Reuses arrays and dictionaries during diffing |
| Event delegation | Single listener per event type at document level |
| String table dedup | Binary patches share string references |
| FrozenDictionary cache | Event attribute names cached at startup |
| SetChildrenHtml | Batch innerHTML for initial render (33% faster) |
| View cache | `lazy()` returns same reference for ReferenceEquals bailout |

## Performance Checklist

- [ ] Use `lazy()` for list items and expensive subtrees
- [ ] Use `id:` parameter for dynamic lists
- [ ] Use `record struct` for hot-path messages
- [ ] Extract static elements
- [ ] Debounce rapid inputs
- [ ] Enable AOT compilation for production
- [ ] Enable trimming and compression
- [ ] Profile with browser DevTools on realistic data

## See Also

- [Virtual DOM](../concepts/virtual-dom.md) — How diffing and patching work
- [Composition](../concepts/components.md) — Memoization patterns
- [Deployment](./deployment.md) — Production build settings
