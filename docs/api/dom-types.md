# DOM Types API Reference

The `Abies.DOM` namespace contains the virtual DOM types used internally by Abies.

## Usage

```csharp
using Abies.DOM;
```

## Overview

The virtual DOM is an immutable tree representation of the UI. Abies uses this tree to compute minimal updates to the real DOM via diffing.

> **Note:** Most applications use `Html.Elements`, `Html.Attributes`, and `Html.Events` rather than these types directly. These types are documented for advanced use cases and framework understanding.

## Document

Represents a complete HTML document:

```csharp
public record Document(string Title, Node Body);
```

**Properties:**

| Property | Type | Description |
| -------- | ---- | ----------- |
| `Title` | `string` | The document title (`<title>` tag) |
| `Body` | `Node` | The root node of the body content |

**Usage in View:**

```csharp
public static Document View(Model model)
    => new("My App", div(h1(text("Hello!"))));
```

## Node

Base type for all virtual DOM nodes:

```csharp
public record Node(string Id);
```

**Properties:**

| Property | Type | Description |
| -------- | ---- | ----------- |
| `Id` | `string` | Unique identifier for DOM operations |

IDs are auto-generated and used internally for diffing.

## Element

Represents an HTML element:

```csharp
public record Element(
    string Id, 
    string Tag, 
    Attribute[] Attributes, 
    params Node[] Children
) : Node(Id);
```

**Properties:**

| Property | Type | Description |
| -------- | ---- | ----------- |
| `Id` | `string` | Unique identifier |
| `Tag` | `string` | HTML tag name (`div`, `span`, etc.) |
| `Attributes` | `Attribute[]` | Element attributes |
| `Children` | `Node[]` | Child nodes |

**Direct Creation (Advanced):**

```csharp
var element = new Element(
    Id: Guid.NewGuid().ToString(),
    Tag: "div",
    Attributes: [new Attribute("id", "class", "container")],
    Children: [new Text("id", "Hello")]
);
```

## Text

Represents a text node:

```csharp
public record Text(string Id, string Value) : Node(Id);
```

**Properties:**

| Property | Type | Description |
| -------- | ---- | ----------- |
| `Id` | `string` | Unique identifier |
| `Value` | `string` | The text content |

**Implicit Conversions:**

```csharp
// String to Text
Text t = "Hello, World!";

// Text to String
string s = t;
```

## RawHtml

Represents unescaped HTML content:

```csharp
public record RawHtml(string Id, string Html) : Node(Id);
```

**Properties:**

| Property | Type | Description |
| -------- | ---- | ----------- |
| `Id` | `string` | Unique identifier |
| `Html` | `string` | Raw HTML string (not escaped) |

> **Warning:** Use with caution. Raw HTML is not sanitized and can be a security risk (XSS) if the content comes from untrusted sources.

## Empty

Represents an empty node (no content):

```csharp
public record Empty() : Node("");
```

Used as a placeholder when no content should be rendered.

## Attribute

Represents an element attribute:

```csharp
public record Attribute(string Id, string Name, string Value);
```

**Properties:**

| Property | Type | Description |
| -------- | ---- | ----------- |
| `Id` | `string` | Unique identifier |
| `Name` | `string` | Attribute name |
| `Value` | `string` | Attribute value |

## Handler

Represents an event handler:

```csharp
public record Handler(
    string Name,
    string CommandId,
    Message? Command,
    string Id,
    Func<object?, Message>? WithData = null,
    Type? DataType = null
) : Attribute(Id, $"data-event-{Name}", CommandId);
```

**Properties:**

| Property | Type | Description |
| -------- | ---- | ----------- |
| `Name` | `string` | Event name (e.g., `click`, `input`) |
| `CommandId` | `string` | Unique ID for command dispatch |
| `Command` | `Message?` | Message to dispatch |
| `WithData` | `Func<object?, Message>?` | Function to create message from event data |
| `DataType` | `Type?` | Type of event data for deserialization |

Handlers are rendered as `data-event-{name}` attributes and intercepted by JavaScript.

## Rendering

### Render.Html

Converts virtual DOM to HTML string:

```csharp
string html = Render.Html(node);
```

**Example:**

```csharp
var element = new Element("1", "div", 
    [new Attribute("2", "class", "container")],
    [new Text("3", "Hello")]
);

string html = Render.Html(element);
// <div id="1" class="container"><span id="3">Hello</span></div>
```

## Diffing (Internal)

### Operations.Diff

Computes patches between two virtual DOM trees:

```csharp
List<Patch> patches = Operations.Diff(oldNode, newNode);
```

Returns a list of `Patch` operations to transform `oldNode` into `newNode`.

### Patch Types

| Patch | Description |
| ----- | ----------- |
| `AddRoot` | Set root element |
| `ReplaceChild` | Replace element with another |
| `AddChild` | Add child element |
| `RemoveChild` | Remove child element |
| `UpdateAttribute` | Update attribute value |
| `AddAttribute` | Add new attribute |
| `RemoveAttribute` | Remove attribute |
| `AddHandler` | Add event handler |
| `RemoveHandler` | Remove event handler |
| `UpdateHandler` | Update event handler |
| `UpdateText` | Update text content |
| `AddText` | Add text node |
| `RemoveText` | Remove text node |
| `AddRaw` | Add raw HTML |
| `RemoveRaw` | Remove raw HTML |
| `ReplaceRaw` | Replace raw HTML |
| `UpdateRaw` | Update raw HTML content |

### Operations.Apply

Applies a patch to the real DOM:

```csharp
await Operations.Apply(patch);
```

Uses JavaScript interop to modify the browser DOM.

## Keyed Children

For list rendering, use `data-key` to enable efficient reordering:

```csharp
static Element<Model, Unit> TodoList(List<Todo> todos)
    => ul(todos.Select(todo =>
        li(
            attribute("data-key", todo.Id.ToString()),
            text(todo.Title)
        )
    ).ToArray());
```

Keyed children are matched by key during diffing, enabling:
- Efficient reordering without full re-render
- Preservation of element state (focus, animations)
- Correct event handler mapping

## Performance Considerations

### Object Pooling

The diff algorithm uses object pooling for internal collections to reduce GC pressure:

```csharp
// Internal implementation
private static readonly ConcurrentQueue<List<Patch>> _patchListPool = new();
private static readonly ConcurrentQueue<Dictionary<string, Attribute>> _attributeMapPool = new();
```

### Early Exits

The differ checks for:
- Reference equality (`ReferenceEquals`)
- Empty collections
- Identical attribute arrays

### Complexity

| Operation | Complexity |
| --------- | ---------- |
| Diff (same structure) | O(n) |
| Diff (different structure) | O(n) |
| Attribute diff | O(n) |
| Apply patch | O(1) per patch |

## See Also

- [HTML Elements API](./html-elements.md)
- [HTML Attributes API](./html-attributes.md)
- [HTML Events API](./html-events.md)
- [Concepts: Virtual DOM](../concepts/virtual-dom.md)
- [ADR-003: Virtual DOM](../adr/ADR-003-virtual-dom.md)
