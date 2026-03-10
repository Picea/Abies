# Composition and Node Types

Abies uses function composition instead of component classes. This document explains how to build reusable UI pieces and the virtual DOM types that underpin them.

## No Component Classes

Unlike Blazor or React, Abies has **no component classes**. There are no lifecycle methods, no component state, no `ShouldRender()`. Instead, you compose UIs from pure functions:

```csharp
// A reusable "component" is just a function
static Node ArticlePreview(Article article) =>
    div([class_("article-preview")], [
        h2([], [text(article.Title)]),
        p([], [text(article.Description)]),
        a([href($"/article/{article.Slug}")], [text("Read more...")])
    ]);

// Used by composing function calls
public static Document View(Model model) =>
    new("Articles",
        div([], model.Articles.Select(ArticlePreview).ToArray()));
```

This is simpler, more testable, and naturally pure.

## The Node Type Hierarchy

Every virtual DOM tree is built from these types:

```text
Node(Id)
├── Element(Id, Tag, Attributes, Children)   — HTML element
├── Text(Id, Value)                          — text content
├── RawHtml(Id, Html)                        — unescaped HTML
├── Memo<TKey>(Id, Key, CachedNode)          — eager memoization
├── LazyMemo<TKey>(Id, Key, Factory)          — lazy memoization
└── Empty()                                  — sentinel (renders nothing)
```

Every node carries a string `Id` for stable identity across renders. IDs are generated at **compile time** by the [Praefixum](https://github.com/MCGPPeters/Praefixum) source generator, ensuring deterministic, zero-cost identification for efficient diffing.

### Element

An HTML element with tag, attributes, and children:

```csharp
new Element("abc123", "div",
    [new Attribute("a1", "class", "container")],
    [new Text("t1", "Hello")])
```

You never construct these directly — use the factory functions from `Abies.Html.Elements`:

```csharp
using static Abies.Html.Elements;
using static Abies.Html.Attributes;

div([class_("container")], [
    text("Hello")
])
```

### Text

```csharp
text("Hello, world!")  // HTML-encoded during rendering
```

### RawHtml

```csharp
raw("<em>Bold</em> move")  // Inserted as-is — NOT sanitized
```

⚠️ Use `raw()` with caution. Never pass user input to it without sanitization.

### Empty

A sentinel node that renders nothing. Used internally by the diff algorithm.

## Attributes and Event Handlers

Attributes come in two variants:

```text
Attribute(Id, Name, Value)             — plain HTML attribute
└── Handler(EventName, CommandId, ...)  — event binding
```

### Plain Attributes

```csharp
using static Abies.Html.Attributes;

div([class_("container"), id_("main")], [...])
input([type_("text"), placeholder("Enter name"), value_(model.Name)], [])
```

### Event Handlers

Event handlers dispatch messages into the MVU loop:

```csharp
// Static message (no event data needed)
button([onclick(new Increment())], [text("+1")])

// With event data (e.g., input value)
input([oninput<InputEventData>(e => new TextChanged(e.Value))], [])
```

Handlers render as `data-event-{eventName}="{commandId}"` attributes in the DOM. The runtime registers a single listener per event type at the document level (event delegation), looks up the `CommandId` from the target element's data attribute, and dispatches the corresponding message.

Common event attribute names are cached in a `FrozenDictionary` to avoid string interpolation at runtime.

## Memoization

Memoization is the primary performance optimization. When the memo key hasn't changed between renders, the entire subtree is skipped during diffing.

### `lazy()` — Deferred Evaluation

```csharp
// The factory function is NOT called if the key matches
lazy((row.Id, isSelected), () =>
    tr([class_(isSelected ? "danger" : "")], [
        td([], [text(row.Id.ToString())]),
        td([], [text(row.Label)])
    ])
)
```

How it works:

1. The runtime compares the key `(row.Id, isSelected)` against the previous render's key
2. If equal → **skip entirely** (no factory call, no diffing)
3. If different → call the factory, diff the result

**View cache optimization:** Abies maintains a cache keyed by compile-time ID. When `lazy()` is called with the same ID and matching key, the _exact same object reference_ is returned. This enables `ReferenceEquals` bailout in `DiffInternal` — an O(1) skip that avoids all key comparison, dictionary building, and subtree diffing. Inspired by Elm's `lazy` where JavaScript `===` skips VDOM construction entirely.

### `memo()` — Eager Memoization

```csharp
// The node is always created, but diffing is skipped if key matches
memo(row.Id, tr([...], [...]))
```

Use `lazy()` when the subtree is expensive to construct. Use `memo()` when the node is cheap to create but expensive to diff.

### Generic Key Comparison

Both `Memo<TKey>` and `LazyMemo<TKey>` use `EqualityComparer<TKey>.Default.Equals()` — this is JIT-optimized and avoids boxing for value types like `(int, bool)` tuples.

## The Document Type

The root of every view is a `Document`:

```csharp
public record Document(string Title, Node Body, params HeadContent[] Head);
```

- **`Title`** — The page title (rendered as `<title>` in the `<head>`)
- **`Body`** — The body content as a virtual DOM tree
- **`Head`** — Optional managed head elements (meta tags, stylesheets, scripts)

```csharp
public static Document View(Model model) =>
    new("My App",
        div([class_("app")], [
            h1([], [text("Welcome")])
        ]),
        new Meta("viewport", "width=device-width"),
        new Stylesheet("/css/app.css")
    );
```

## Composition Patterns

### Extract Functions

The simplest pattern — extract a function:

```csharp
static Node Header(string title, int count) =>
    header([class_("header")], [
        h1([], [text(title)]),
        span([class_("count")], [text($"{count} items")])
    ]);

static Node Footer(bool hasItems) =>
    footer([class_("footer")], [
        hasItems ? text("Clear completed") : text("")
    ]);

public static Document View(Model model) =>
    new("Todo",
        div([class_("app")], [
            Header("Todos", model.Items.Count),
            ItemList(model.Items, model.Filter),
            Footer(model.Items.Any())
        ]));
```

### Parameterize with Data

```csharp
static Node UserCard(User user, bool isCurrentUser) =>
    div([class_("user-card")], [
        img([src(user.AvatarUrl), alt(user.Name)]),
        h3([], [text(user.Name)]),
        isCurrentUser
            ? button([onclick(new EditProfile())], [text("Edit")])
            : button([onclick(new FollowUser(user.Id))], [text("Follow")])
    ]);
```

### Memoize Expensive Lists

```csharp
static Node ArticleList(IReadOnlyList<Article> articles, string? selectedSlug) =>
    div([class_("article-list")],
        articles.Select(article =>
            lazy((article.Slug, article.UpdatedAt, article.Slug == selectedSlug), () =>
                ArticlePreview(article, article.Slug == selectedSlug))
        ).ToArray());
```

### Higher-Order View Functions

```csharp
static Node WithLoading(bool isLoading, Func<Node> content) =>
    isLoading
        ? div([class_("loading")], [text("Loading...")])
        : content();

static Node WithError(string? error, Func<Node> content) =>
    error is not null
        ? div([class_("error")], [text(error)])
        : content();

// Usage
public static Document View(Model model) =>
    new("App",
        WithLoading(model.IsLoading, () =>
            WithError(model.Error, () =>
                ArticleList(model.Articles))));
```

### Layout Functions

```csharp
static Node Page(string title, Node content) =>
    div([class_("page")], [
        Header(title),
        main_([], [content]),
        Footer()
    ]);

static Node TwoColumn(Node sidebar, Node main) =>
    div([class_("two-column")], [
        div([class_("sidebar")], [sidebar]),
        div([class_("main")], [main])
    ]);
```

## Why Functions Instead of Components?

| Aspect | Function Composition | Component Classes |
| ------ | -------------------- | ----------------- |
| State | None (pure) | Local state, lifecycle |
| Testing | Direct function call | Requires rendering context |
| Reuse | Import and call | Instantiate and configure |
| Composition | Function composition | Component tree |
| Memoization | Explicit `lazy()`/`memo()` | `ShouldRender()` override |
| Learning curve | Just functions | Lifecycle, state, props, refs |

The trade-off: Abies can't skip rendering subtrees as aggressively as Blazor's component model (which uses `ShouldRender()`). The `lazy()` function provides similar performance benefits explicitly.

## See Also

- [Virtual DOM](./virtual-dom.md) — How nodes are diffed and patched
- [MVU Architecture](./mvu-architecture.md) — The overall pattern
- [Pure Functions](./pure-functions.md) — Why composition works
