# Performance Guide

Best practices for building fast Abies applications.

## Overview

Abies is designed for performance:

- Immutable records enable fast equality checks
- Virtual DOM diffing minimizes DOM updates
- Object pooling reduces garbage collection
- WebAssembly provides near-native speed

This guide covers optimization techniques for when you need maximum performance.

## Virtual DOM Optimization

### Use Stable IDs for Dynamic Lists

For lists that can reorder, add, or remove items, use the `id:` parameter:

```csharp
// ❌ Without stable IDs - position-based diffing, inefficient
ul([], todos.Select(todo => 
    li([], [text(todo.Title)])
).ToArray())

// ✅ With stable IDs - keyed diffing, efficient
ul([], todos.Select(todo => 
    li([], [text(todo.Title)], id: $"todo-{todo.Id}")
).ToArray())
```

Stable IDs enable:

- Efficient list reordering (move DOM nodes instead of recreating)
- Preserved element state (focus, scroll position)
- Fewer DOM operations

#### Why `id:` Instead of a Separate `key` Attribute?

Unlike React, Vue, or Elm, Abies doesn't use a separate `key` attribute. Instead, the element's `id` serves double duty:

| Framework  | Keying Approach           | Key Attribute             |
| ---------- | ------------------------- | ------------------------- |
| React      | Separate `key` prop       | `key={item.id}`           |
| Vue        | Separate `:key` binding   | `:key="item.id"`          |
| Elm        | Separate `Keyed.node`     | `(id, element)` tuple     |
| **Abies**  | **Unified `id:`**         | `id: $"item-{item.Id}"`   |

**Why can Abies do this when others can't?**

1. **Praefixum generates unique IDs**: The compile-time source generator ensures every element has a guaranteed-unique ID.

2. **IDs are already required for patching**: Abies uses `getElementById` to apply DOM patches, so every element needs an ID anyway.

3. **No HTML ID collision**: Abies IDs are internal and unique within the app, unlike HTML IDs which must be globally unique and are used for CSS/accessibility.

4. **Simpler mental model**: One concept (`id:`) instead of two (`id:` for patching + `key` for diffing).

For full details, see [ADR-016: ID-Based DOM Diffing](../adr/ADR-016-keyed-dom-diffing.md).

### Avoid Unnecessary Nesting

Flatten element hierarchy where possible:

```csharp
// ❌ Deep nesting
div(
    div(
        div(
            span(text("Hello"))
        )
    )
)

// ✅ Flat structure
div(
    span(text("Hello"))
)
```

### Reuse Static Elements

Extract static parts to avoid recreation:

```csharp
// ❌ Recreated every render
public static Document View(Model model)
    => new("App", div(
        header(
            nav(
                a(href("/"), text("Home")),
                a(href("/about"), text("About"))
            )
        ),
        DynamicContent(model)
    ));

// ✅ Static parts extracted
static readonly Node StaticHeader = header(
    nav(
        a(href("/"), text("Home")),
        a(href("/about"), text("About"))
    )
);

public static Document View(Model model)
    => new("App", div(
        StaticHeader,
        DynamicContent(model)
    ));
```

## Update Optimization

### Early Exit on No Change

Skip updates when model hasn't changed:

```csharp
public static (Model, Command) Update(Message msg, Model model)
    => msg switch
    {
        SetFilter filter when filter.Value == model.Filter 
            => (model, Commands.None),  // No change
        
        SetFilter filter 
            => (model with { Filter = filter.Value }, Commands.None),
        
        _ => (model, Commands.None)
    };
```

### Batch Related Updates

Combine multiple state changes:

```csharp
// ❌ Multiple separate updates
case FormSubmitted:
    return (model with { IsSubmitting = true }, new SubmitCommand());
// Then later:
case SubmitSucceeded:
    return (model with { IsSubmitting = false, Result = success.Data }, Commands.None);

// ✅ Single combined update
case SubmitSucceeded success:
    return (model with 
    { 
        IsSubmitting = false, 
        Result = success.Data,
        Errors = Array.Empty<string>()
    }, Commands.None);
```

### Lazy Computation

Defer expensive calculations:

```csharp
public record Model(
    IReadOnlyList<Article> AllArticles,
    string Filter,
    Lazy<IReadOnlyList<Article>> FilteredArticles
);

public static (Model, Command) Update(Message msg, Model model)
    => msg switch
    {
        FilterChanged changed =>
        {
            var filtered = new Lazy<IReadOnlyList<Article>>(() =>
                model.AllArticles
                    .Where(a => a.Title.Contains(changed.Filter))
                    .ToList()
            );
            return (model with { Filter = changed.Filter, FilteredArticles = filtered }, Commands.None);
        },
        _ => (model, Commands.None)
    };
```

## View Optimization

### Conditional Rendering

Only render visible content:

```csharp
static Element<Model, Unit> TabContent(Model model)
    => model.ActiveTab switch
    {
        Tab.Articles => ArticleList(model.Articles),
        Tab.Users => UserList(model.Users),
        Tab.Settings => SettingsPanel(model.Settings),
        _ => div()
    };

// Don't render all tabs and hide with CSS:
// ❌ div(style("display: none"), ExpensiveComponent())
```

### Virtualized Lists

For very long lists, only render visible items:

```csharp
static Element<Model, Unit> VirtualList(Model model)
{
    var visibleStart = model.ScrollTop / ItemHeight;
    var visibleEnd = visibleStart + model.ViewportHeight / ItemHeight + 1;
    var visibleItems = model.Items
        .Skip(visibleStart)
        .Take(visibleEnd - visibleStart);
    
    return div(
        style($"height: {model.Items.Count * ItemHeight}px; position: relative;"),
        visibleItems.Select((item, i) => 
            div(
                style($"position: absolute; top: {(visibleStart + i) * ItemHeight}px;"),
                attribute("data-key", item.Id.ToString()),
                ItemView(item)
            )
        ).ToArray()
    );
}
```

### Memoization

Cache expensive view computations:

```csharp
public class ViewCache
{
    private Node? _cachedNav;
    private string? _lastUsername;
    
    public Node GetNav(Model model)
    {
        if (_cachedNav is not null && _lastUsername == model.Username)
            return _cachedNav;
        
        _cachedNav = RenderNav(model);
        _lastUsername = model.Username;
        return _cachedNav;
    }
    
    private static Node RenderNav(Model model) => nav(/* ... */);
}
```

## Command Optimization

### Debounce Rapid Commands

For search inputs, debounce API calls:

```csharp
public record Model(
    string SearchQuery,
    CancellationTokenSource? SearchCts
);

public static (Model, Command) Update(Message msg, Model model)
    => msg switch
    {
        SearchChanged changed =>
        {
            // Cancel previous search
            model.SearchCts?.Cancel();
            
            var cts = new CancellationTokenSource();
            return (
                model with { SearchQuery = changed.Query, SearchCts = cts },
                new DebouncedSearchCommand(changed.Query, cts.Token)
            );
        },
        _ => (model, Commands.None)
    };

// In HandleCommand:
case DebouncedSearchCommand search:
    await Task.Delay(300, search.Token);  // Debounce
    if (search.Token.IsCancellationRequested) return;
    var results = await api.Search(search.Query);
    dispatch(new SearchResults(results));
    break;
```

### Parallel Commands

Execute independent commands in parallel:

```csharp
case LoadDashboardCommand:
    var articlesTask = api.GetArticles();
    var usersTask = api.GetUsers();
    var statsTask = api.GetStats();
    
    await Task.WhenAll(articlesTask, usersTask, statsTask);
    
    dispatch(new DashboardLoaded(
        articlesTask.Result,
        usersTask.Result,
        statsTask.Result
    ));
    break;
```

### Cache API Responses

Avoid redundant API calls:

```csharp
public record Model(
    Dictionary<string, Article> ArticleCache
);

public static (Model, Command) Update(Message msg, Model model)
    => msg switch
    {
        ViewArticle view when model.ArticleCache.ContainsKey(view.Slug)
            => (model with { CurrentArticle = model.ArticleCache[view.Slug] }, Commands.None),
        
        ViewArticle view
            => (model, new LoadArticleCommand(view.Slug)),
        
        ArticleLoaded loaded =>
        {
            var cache = new Dictionary<string, Article>(model.ArticleCache)
            {
                [loaded.Article.Slug] = loaded.Article
            };
            return (model with { ArticleCache = cache, CurrentArticle = loaded.Article }, Commands.None);
        },
        
        _ => (model, Commands.None)
    };
```

## Memory Optimization

### Use Structs for Small Types

```csharp
// ✅ Struct for small, frequently created types
public readonly record struct Point(int X, int Y);

// ❌ Class creates heap allocations
public record Point(int X, int Y);
```

### Avoid Closures in Hot Paths

```csharp
// ❌ Creates closure for each item
items.Select(item => RenderItem(item, model.Theme))

// ✅ Pass data explicitly
items.Select(item => RenderItem(item, model.Theme))

static Element<Model, Unit> RenderItem(Item item, Theme theme)
    => div(/* use item and theme directly */);
```

### Pool Large Collections

For frequently modified collections:

```csharp
// Use ArrayPool for temporary buffers
var buffer = ArrayPool<Article>.Shared.Rent(1000);
try
{
    // Use buffer
}
finally
{
    ArrayPool<Article>.Shared.Return(buffer);
}
```

## Build Optimization

### AOT Compilation

Enable Ahead-of-Time compilation for faster startup:

```xml
<PropertyGroup>
    <RunAOTCompilation>true</RunAOTCompilation>
</PropertyGroup>
```

### Trimming

Enable IL trimming to reduce bundle size:

```xml
<PropertyGroup>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>link</TrimMode>
</PropertyGroup>
```

### Compression

Enable Brotli compression:

```xml
<PropertyGroup>
    <BlazorEnableCompression>true</BlazorEnableCompression>
</PropertyGroup>
```

## Measuring Performance

### Browser DevTools

Use the Performance tab to identify bottlenecks:

1. Open DevTools (F12)
2. Go to Performance tab
3. Click Record
4. Perform actions
5. Stop recording
6. Analyze flame chart

### Custom Timing

Add timing to critical paths:

```csharp
public static Document View(Model model)
{
    var sw = Stopwatch.StartNew();
    var result = RenderView(model);
    Console.WriteLine($"View: {sw.ElapsedMilliseconds}ms");
    return result;
}
```

### OpenTelemetry

Abies includes OpenTelemetry instrumentation:

```csharp
// Traces are automatically created for:
// - Runtime lifecycle
// - Message processing
// - Command handling

// View in Aspire dashboard or Jaeger
```

## Performance Checklist

Before deploying:

- [ ] Use keys for dynamic lists
- [ ] Avoid deep nesting
- [ ] Extract static elements
- [ ] Debounce rapid inputs
- [ ] Cache API responses
- [ ] Enable AOT compilation
- [ ] Enable trimming
- [ ] Enable compression
- [ ] Test with realistic data volumes
- [ ] Profile in production build

## See Also

- [Concepts: Virtual DOM](../concepts/virtual-dom.md) — How diffing works
- [API: DOM Types](../api/dom-types.md) — Internal DOM operations
- [Guide: Deployment](./deployment.md) — Production builds
