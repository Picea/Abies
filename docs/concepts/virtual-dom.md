# Virtual DOM

Abies uses a virtual DOM to efficiently update the browser. This document explains how the virtual DOM works and why it matters.

## What is a Virtual DOM?

The virtual DOM is an in-memory representation of the UI. Instead of manipulating the browser DOM directly, Abies:

1. Builds a virtual DOM tree from your View function
2. Compares it to the previous virtual DOM (diffing)
3. Calculates minimal changes needed (patches)
4. Applies only those changes to the real DOM

```text
┌────────────────────────────────────────────────────────────┐
│                    Virtual DOM Flow                        │
│                                                            │
│   View(model)      Diff           Patch         Browser    │
│       │             │               │               │      │
│       ▼             ▼               ▼               ▼      │
│   ┌───────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐  │
│   │ VDOM  │───▶│ Compare │───▶│ Changes │───▶│  DOM    │  │
│   │ Tree  │    │ Old/New │    │  Only   │    │         │  │
│   └───────┘    └─────────┘    └─────────┘    └─────────┘  │
└────────────────────────────────────────────────────────────┘
```

## Why Virtual DOM?

### 1. Declarative UI

You describe what the UI should look like, not how to change it:

```csharp
// Just describe the desired state
public static Document View(Model model)
    => new("App",
        model.IsLoggedIn
            ? UserDashboard(model.User)
            : LoginForm());
```

### 2. Performance

Direct DOM manipulation is expensive. The virtual DOM:

- Batches changes together
- Minimizes actual DOM operations
- Avoids unnecessary reflows

### 3. Simplicity

No manual DOM bookkeeping:

```csharp
// Without VDOM: track and update individual elements
if (nameChanged) document.getElementById("name").textContent = newName;
if (emailChanged) document.getElementById("email").textContent = newEmail;
// ... endless updates

// With VDOM: just return the whole view
return View(newModel); // Abies figures out what changed
```

## Node Types

Abies supports three node types:

### Element Nodes

Standard HTML elements with attributes and children:

```csharp
div([class_("container"), id("main")], [
    h1([], [text("Title")]),
    p([], [text("Content")])
])
```

Internal representation:

```csharp
public record Element(
    string Id,
    string Tag,
    Attributes Attributes,
    List<Node> Children
) : Node;
```

### Text Nodes

Plain text content:

```csharp
text("Hello, World!")
```

Internal representation:

```csharp
public record Text(string Id, string Value) : Node;
```

### Raw HTML Nodes

Pre-rendered HTML strings (use carefully):

```csharp
rawHtml("<strong>Bold</strong>")
```

Internal representation:

```csharp
public record RawHtml(string Id, string Html) : Node;
```

## Attributes

Attributes are divided into three categories:

### Regular Attributes

Standard HTML attributes:

```csharp
div([
    id("main"),
    class_("container"),
    Attr("data-custom", "value")
], [...])
```

### Properties

JavaScript properties set directly:

```csharp
input([
    type("checkbox"),
    checked_(true),
    value_(model.Text)
], [])
```

### Event Handlers

Functions that dispatch messages:

```csharp
button([
    onclick(new ButtonClicked()),
    onmouseover(new MouseEntered())
], [text("Click me")])
```

## The Diff Algorithm

Abies compares old and new virtual DOM trees to find changes.

### Element Comparison

```csharp
// If tags differ → replace entire element
// If tags match → compare attributes and children
```

### Attribute Comparison

Attributes are compared by name, not by internal ID:

```csharp
// Old: div([class_("red"), id("box")], [...])
// New: div([class_("blue"), id("box")], [...])
// Patches: UpdateAttribute("class", "red" → "blue")
```

### Child Comparison

Children are compared in order by default:

```csharp
// Old: [Child A, Child B, Child C]
// New: [Child A, Child D, Child C]
// Patches: Replace child at index 1
```

### Keyed Lists (ADR-016)

For dynamic lists, use the `id:` parameter to provide stable element identity:

```csharp
ul([], [
    ..model.Items.Select(item =>
        li([], [text(item.Name)], id: $"item-{item.Id}")
    )
])
```

The element's `id` is used by the diff algorithm to match elements across renders.
When the set of IDs changes, Abies correctly identifies which elements to add or remove.
When IDs are reordered, Abies replaces the entire list to preserve consistency.

## Patch Types

The diff produces patches that are applied to the real DOM:

| Patch | Description |
| ----- | ----------- |
| `AddRoot` | Set the root element |
| `ReplaceChild` | Replace a child element |
| `AddChild` | Add a new child |
| `RemoveChild` | Remove a child |
| `AddAttribute` | Add a new attribute |
| `UpdateAttribute` | Change an attribute value |
| `RemoveAttribute` | Remove an attribute |
| `AddHandler` | Attach an event handler |
| `UpdateHandler` | Replace an event handler |
| `RemoveHandler` | Detach an event handler |
| `UpdateText` | Change text content |

## How Patching Works

Patches are applied via JavaScript interop:

```text
1. Abies generates list of patches
2. Patches serialized to JavaScript
3. JavaScript finds DOM elements by ID
4. Changes applied in order
5. Event handlers registered/unregistered
```

```csharp
// Example patch flow
var patches = Operations.Diff(oldVdom, newVdom);
await Operations.Apply(patches, jsRuntime);
```

## Element IDs

Every virtual DOM node has a stable ID:

```csharp
public record Element(
    string Id,      // Stable ID for finding in real DOM
    string Tag,
    Attributes Attributes,
    List<Node> Children
) : Node;
```

IDs are generated during tree construction and used to locate elements during patching.

## Performance Considerations

### 1. Use Stable IDs for Dynamic Lists (ADR-016)

Elements without stable IDs may not be matched correctly:

```csharp
// ❌ No stable ID: may have issues with dynamic lists
ul([], model.Items.Select(i => li([], [text(i.Name)])))

// ✅ Stable ID: efficient updates and correct matching
ul([], model.Items.Select(i => li([], [text(i.Name)], id: $"item-{i.Id}")))
```

#### Why `id:` Instead of a Separate `key` Attribute?

Unlike React (`key={...}`), Vue (`:key="..."`), or Elm (`Keyed.node`), Abies uses the element's `id:` parameter for both diffing and patching:

| Framework  | Keying Approach           |
| ---------- | ------------------------- |
| React      | Separate `key` prop       |
| Vue        | Separate `:key` binding   |
| Elm        | Separate `Keyed.node`     |
| **Abies**  | **Unified `id:`**         |

**Why can Abies unify these?**

1. **Praefixum generates unique IDs at compile time** — every element already has a guaranteed-unique ID
2. **IDs are required for patching** — Abies uses `getElementById` to apply DOM patches
3. **No HTML ID collisions** — Abies IDs are internal, not used for CSS or accessibility
4. **Simpler API** — developers learn one concept instead of two

For the full comparison with other frameworks, see [ADR-016: ID-Based DOM Diffing](../adr/ADR-016-keyed-dom-diffing.md).

### 2. Avoid Unnecessary Nesting

Deep trees take longer to diff:

```csharp
// ❌ Unnecessary nesting
div([], [div([], [div([], [div([], [text("Hi")])])])])

// ✅ Flat structure
div([], [text("Hi")])
```

### 3. Memoize Expensive Views

For static content, cache the result:

```csharp
static readonly Node _footer = footer([], [
    text("© 2024 My Company")
]);

public static Document View(Model model)
    => new("App", div([], [
        DynamicContent(model),
        _footer  // Reused, not rebuilt
    ]));
```

### 4. Keep Models Small

Smaller models mean smaller diffs:

```csharp
// ❌ Entire model changes frequently
public record Model(List<Article> Articles, DetailedStats Stats, /* ... */);

// ✅ Separate what changes
public record Model(Page CurrentPage);
public record HomePage(List<Article> Articles);  // Only changes on home
```

## Debugging Virtual DOM

### Logging Patches

Enable patch logging to see what changes:

```csharp
var patches = Operations.Diff(oldVdom, newVdom);
foreach (var patch in patches)
{
    Console.WriteLine($"Patch: {patch}");
}
```

### Inspect Node IDs

Check that IDs are stable across renders:

```csharp
var doc1 = View(model);
var doc2 = View(model with { Count = model.Count + 1 });
// Compare element IDs to verify stability
```

## Implementation Details

The full implementation lives in `Abies/DOM/Operations.cs`:

- `Operations.Diff` — Compares two virtual DOM trees
- `Operations.Apply` — Applies patches to the browser DOM
- `Patch` — Sum type representing all possible patches

Key design decisions:

1. **Ordered child comparison** — Position-based, not key-based reordering
2. **Name-based attribute comparison** — Stable even when attribute source changes
3. **Full replacement for keyed reorders** — Conservative but correct
4. **ID-based patching** — Every node has a stable identifier

## Summary

The virtual DOM provides:

- ✅ Declarative UI — Describe what, not how
- ✅ Efficient updates — Only change what's different
- ✅ Simple mental model — Just return the desired view
- ✅ Testable views — Pure functions returning data

Understanding the virtual DOM helps you:

- Structure views for performance
- Debug rendering issues
- Optimize large applications

## See Also

- [MVU Architecture](./mvu-architecture.md) — How View fits in MVU
- [HTML API](../api/html-elements.md) — Building virtual DOM trees
- [ADR-0004 Virtual DOM](../adr/0004-virtual-dom.md) — Design decisions
