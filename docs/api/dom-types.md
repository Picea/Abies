# DOM Types API

The `Abies.DOM` namespace contains the virtual DOM types that Abies uses internally to represent UI trees, compute diffs, and apply patches.

## Usage

```csharp
using Abies.DOM;
```

> **Note:** Most applications use `Html.Elements`, `Html.Attributes`, and `Html.Events` rather than these types directly. These types are documented for advanced use cases, testing, and framework understanding.

## Document

Represents a complete page with title, body, and optional managed head elements:

```csharp
public record Document(string Title, Node Body, params HeadContent[] Head);
```

| Property | Type | Description |
|----------|------|-------------|
| `Title` | `string` | The page title (`<title>` tag) |
| `Body` | `Node` | The root node of the body content |
| `Head` | `HeadContent[]` | Optional managed `<head>` elements (meta, link, script, base) |

Returned by `Program<TModel, TArgument>.View(model)`:

```csharp
public static Document View(Model model) =>
    new("My App",
        div([], [h1([], [text("Hello!")])]),
        Head.meta("description", "My Abies application"),
        Head.canonical("/"));
```

## HeadContent

Represents managed elements in the `<head>`. Each variant has a stable `Key` for diffing between renders.

```csharp
public interface HeadContent
{
    string Key { get; }
    string ToHtml();

    sealed record Meta(string Name, string Content) : HeadContent;
    sealed record MetaProperty(string Property, string Content) : HeadContent;
    sealed record Link(string Rel, string Href, string? Type = null) : HeadContent;
    sealed record Script(string Type, string Content) : HeadContent;
    sealed record Base(string Href) : HeadContent;
}
```

| Variant | Key Pattern | Description |
|---------|-------------|-------------|
| `Meta` | `"meta:{Name}"` | `<meta name="..." content="...">` |
| `MetaProperty` | `"property:{Property}"` | `<meta property="..." content="...">` (Open Graph etc.) |
| `Link` | `"link:{Rel}:{Href}"` | `<link rel="..." href="...">` |
| `Script` | `"script:{Type}"` | `<script type="...">...</script>` (typically JSON-LD) |
| `Base` | `"base"` | `<base href="...">` |

Managed elements are tagged with `data-abies-head` in the real DOM so they never conflict with user-defined head elements.

### Head Factory Functions

The `Head` static class provides convenience factories:

```csharp
using static Abies.Head;

Head.meta(name, content)      // <meta name="..." content="...">
Head.og(property, content)    // <meta property="og:..." content="...">
Head.twitter(name, content)   // <meta name="twitter:..." content="...">
Head.canonical(href)          // <link rel="canonical" href="...">
Head.stylesheet(href)         // <link rel="stylesheet" href="...">
Head.preload(href, asType)    // <link rel="preload" href="..." as="...">
Head.jsonLd(data)             // <script type="application/ld+json">...</script>
Head.link(rel, href, type?)   // <link rel="..." href="...">
Head.property(prop, content)  // <meta property="..." content="...">
Head.@base(href)              // <base href="...">
```

## Node

Base type for all virtual DOM nodes:

```csharp
public record Node(string Id);
```

Every node carries a stable `Id` for efficient diffing and DOM addressing. IDs are generated at compile time by the Praefixum source generator.

## Node Type Hierarchy

```
Node(Id)
├── Element(Id, Tag, Attributes, Children)
├── Text(Id, Value)
├── RawHtml(Id, Html)
├── Memo<TKey>(Id, Key, CachedNode)
├── LazyMemo<TKey>(Id, Key, Factory, CachedNode?)
└── Empty()
```

## Element

An HTML element with a tag name, attributes, and children:

```csharp
public record Element(
    string Id,
    string Tag,
    Attribute[] Attributes,
    params Node[] Children
) : Node(Id);
```

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique identifier |
| `Tag` | `string` | HTML tag name (`"div"`, `"span"`, `"input"`, etc.) |
| `Attributes` | `Attribute[]` | Plain attributes and event handlers |
| `Children` | `Node[]` | Child nodes |

## Text

A text node:

```csharp
public record Text(string Id, string Value) : Node(Id);
```

Supports implicit conversions:

```csharp
Text t = "Hello";         // string → Text (uses string as both Id and Value)
string s = someTextNode;  // Text → string
```

## RawHtml

Unescaped HTML content — inserted as-is with no encoding:

```csharp
public record RawHtml(string Id, string Html) : Node(Id);
```

> **Warning:** Raw HTML is not sanitized. Use with caution for content from untrusted sources (XSS risk).

## Empty

A sentinel node that renders nothing:

```csharp
public record Empty() : Node("");
```

Used internally as a placeholder (e.g., when diffing against a missing node).

## Memo\<TKey\>

An eagerly-evaluated memoized node. When diffing, if the key equals the previous key, the subtree diff is skipped:

```csharp
public record Memo<TKey>(string Id, TKey Key, Node CachedNode) : Node(Id), MemoNode
    where TKey : notnull;
```

Created via the `memo(key, node)` function in `Html.Elements`.

## LazyMemo\<TKey\>

A lazily-evaluated memoized node. The factory function is only called if the key differs from the previous render:

```csharp
public record LazyMemo<TKey>(string Id, TKey Key, Func<Node> Factory, Node? CachedNode = null)
    : Node(Id), LazyMemoNode
    where TKey : notnull;
```

Created via the `lazy(key, factory)` function in `Html.Elements`. This is the equivalent of Elm's `lazy` function — the primary performance optimization tool.

## Attribute

A plain HTML attribute:

```csharp
public record Attribute(string Id, string Name, string Value);
```

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique identifier |
| `Name` | `string` | Attribute name (e.g., `"class"`, `"href"`) |
| `Value` | `string` | Attribute value |

## Handler

An event handler attribute. Renders as `data-event-{EventName}="{CommandId}"` in the DOM:

```csharp
public record Handler(
    string EventName,
    string CommandId,
    Message? Command,
    string Id,
    Func<object?, Message>? WithData = null,
    Func<string, object?>? Deserializer = null
) : Attribute(Id, $"data-event-{EventName}", CommandId);
```

| Property | Type | Description |
|----------|------|-------------|
| `EventName` | `string` | The DOM event name (e.g., `"click"`, `"input"`) |
| `CommandId` | `string` | Unique ID linking to the runtime's `HandlerRegistry` |
| `Command` | `Message?` | Static message to dispatch (for simple handlers) |
| `WithData` | `Func<object?, Message>?` | Factory for data-carrying handlers |
| `Deserializer` | `Func<string, object?>?` | Trim-safe JSON deserializer for event data |

Handlers support two modes:

- **Static message** — `Command` is set, dispatched as-is (e.g., `onclick(new Increment())`)
- **Data-carrying** — `WithData` is set, called with deserialized event data (e.g., `oninput(e => new TextChanged(e.Value))`)

Event delegation: the browser registers a single listener per event type at the document level. When an event fires, the `CommandId` from the target element's `data-event-{name}` attribute is used to look up the handler in the runtime's `HandlerRegistry`.

## Patch Types

Patches are the output of the diff algorithm — they describe the minimal set of mutations to transform the old DOM into the new DOM:

```csharp
public interface Patch;
```

All patches are `readonly struct` types implementing the `Patch` marker interface:

### Element Patches

| Patch | Description |
|-------|-------------|
| `AddRoot(Element)` | Set the root element (initial render) |
| `ReplaceChild(Element old, Element new)` | Replace an element with another |
| `AddChild(Element parent, Element child)` | Add a child element |
| `RemoveChild(Element parent, Element child)` | Remove a child element |
| `ClearChildren(Element parent, Node[] old)` | Remove all children |
| `SetChildrenHtml(Element parent, Node[] children)` | Replace all children at once |
| `AppendChildrenHtml(Element parent, Node[] children)` | Append children to existing |
| `MoveChild(Element parent, Element child, string? beforeId)` | Reorder a child |

### Attribute Patches

| Patch | Description |
|-------|-------------|
| `UpdateAttribute(Element, Attribute, string value)` | Update attribute value |
| `AddAttribute(Element, Attribute)` | Add new attribute |
| `RemoveAttribute(Element, Attribute)` | Remove attribute |

### Handler Patches

| Patch | Description |
|-------|-------------|
| `AddHandler(Element, Handler)` | Add event handler |
| `RemoveHandler(Element, Handler)` | Remove event handler |
| `UpdateHandler(Element, Handler old, Handler new)` | Update event handler |

### Text Patches

| Patch | Description |
|-------|-------------|
| `UpdateText(Element parent, Text node, string text, string newId)` | Update text content |
| `AddText(Element parent, Text child)` | Add text node |
| `RemoveText(Element parent, Text child)` | Remove text node |

### Raw HTML Patches

| Patch | Description |
|-------|-------------|
| `AddRaw(Element parent, RawHtml child)` | Add raw HTML node |
| `RemoveRaw(Element parent, RawHtml child)` | Remove raw HTML node |
| `ReplaceRaw(RawHtml old, RawHtml new)` | Replace raw HTML node |
| `UpdateRaw(RawHtml node, string html, string newId)` | Update raw HTML content |

### Head Patches

| Patch | Description |
|-------|-------------|
| `AddHeadElement(HeadContent)` | Add a managed head element |
| `UpdateHeadElement(HeadContent)` | Update a managed head element |
| `RemoveHeadElement(string key)` | Remove a managed head element by key |

## Diffing

### Operations.Diff

Computes the minimal patch set between two virtual DOM trees:

```csharp
List<Patch> patches = Operations.Diff(oldNode, newNode);
```

The diff algorithm runs in O(n) time (linear in the number of nodes) by walking both trees simultaneously. It uses:

- **Reference equality** (`ReferenceEquals`) for early exit when subtrees are identical
- **Memo key comparison** to skip entire subtrees for `Memo<TKey>` and `LazyMemo<TKey>` nodes
- **Identity-based child matching** for efficient reordering

### HeadDiff.Diff

Computes the delta between old and new head content arrays:

```csharp
IReadOnlyList<Patch> patches = HeadDiff.Diff(oldHead, newHead);
```

Uses `HeadContent.Key` for identity matching. Head patches flow through the same binary batch protocol as body patches.

## Rendering

### Render.Html

Converts a virtual DOM tree to an HTML string (used for server-side rendering):

```csharp
string html = Render.Html(node);
```

## See Also

- [UI Composition](element.md) — How to build reusable UI with functions and memoization
- [HTML Elements](html-elements.md) — Functions that create DOM nodes
- [HTML Attributes](html-attributes.md) — Functions that create attributes
- [HTML Events](html-events.md) — Functions that create event handlers
- [Runtime](runtime.md) — How the runtime uses diffing and patching
