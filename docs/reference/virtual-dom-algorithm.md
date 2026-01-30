# Virtual DOM Algorithm

This document describes the internal implementation of Abies' virtual DOM diffing and patching algorithm.

## Overview

The virtual DOM is an in-memory representation of the UI. When the model changes:

1. A new virtual DOM tree is created from `View(model)`
2. The new tree is compared against the previous tree (diffing)
3. A minimal set of patches is computed
4. Patches are applied to the real DOM via JavaScript interop

This approach minimizes expensive DOM operations.

## Tree Structure

### Node Types

```csharp
public record Node(string Id);
public record Element(string Id, string Tag, Attribute[] Attributes, Node[] Children) : Node(Id);
public record Text(string Id, string Value) : Node(Id);
public record RawHtml(string Id, string Html) : Node(Id);
public record Empty() : Node("");
```

Every node has a unique `Id` used to track it across renders.

### Attributes

```csharp
public record Attribute(string Id, string Name, string Value);
public record Handler(
    string Name,
    string CommandId,
    Message? Command,
    string Id,
    Func<object?, Message>? WithData = null,
    Type? DataType = null
) : Attribute(Id, $"data-event-{Name}", CommandId);
```

Handlers are special attributes that register event callbacks.

## Diffing Algorithm

### Top-Level Comparison

```csharp
public static List<Patch> Diff(Node? oldNode, Node newNode)
{
    if (oldNode is null)
        return [new AddRoot((Element)newNode)];
    
    DiffInternal(oldNode, newNode, null, patches);
    return patches;
}
```

### Node Comparison

```csharp
private static void DiffInternal(Node oldNode, Node newNode, Element? parent, List<Patch> patches)
{
    // Same reference = no change
    if (ReferenceEquals(oldNode, newNode))
        return;
    
    // Text nodes
    if (oldNode is Text oldText && newNode is Text newText)
    {
        if (oldText.Value != newText.Value)
            patches.Add(new UpdateText(oldText, newText.Value, newText.Id));
        return;
    }
    
    // Elements with same tag
    if (oldNode is Element oldEl && newNode is Element newEl && oldEl.Tag == newEl.Tag)
    {
        DiffAttributes(oldEl, newEl, patches);
        DiffChildren(oldEl, newEl, patches);
        return;
    }
    
    // Type or tag mismatch = replace
    patches.Add(new ReplaceChild(oldElement, newElement));
}
```

### Attribute Diffing

```csharp
private static void DiffAttributes(Element oldElement, Element newElement, List<Patch> patches)
{
    var oldMap = oldElement.Attributes.ToDictionary(a => a.Name);
    
    foreach (var newAttr in newElement.Attributes)
    {
        if (oldMap.TryGetValue(newAttr.Name, out var oldAttr))
        {
            oldMap.Remove(newAttr.Name);
            if (!newAttr.Equals(oldAttr))
            {
                if (oldAttr is Handler && newAttr is Handler)
                    patches.Add(new UpdateHandler(newElement, (Handler)oldAttr, (Handler)newAttr));
                else
                    patches.Add(new UpdateAttribute(oldElement, newAttr, newAttr.Value));
            }
        }
        else
        {
            patches.Add(new AddAttribute(newElement, newAttr));
        }
    }
    
    // Remove attributes not in new
    foreach (var remaining in oldMap.Values)
    {
        patches.Add(new RemoveAttribute(oldElement, remaining));
    }
}
```

### Child Diffing

```csharp
private static void DiffChildren(Element oldParent, Element newParent, List<Patch> patches)
{
    var oldChildren = oldParent.Children;
    var newChildren = newParent.Children;
    var shared = Math.Min(oldChildren.Length, newChildren.Length);
    
    // Check for keyed children
    if (HasKeyedChildren(oldChildren) || HasKeyedChildren(newChildren))
    {
        // Key-based reconciliation
        var oldKeys = BuildKeySequence(oldChildren);
        var newKeys = BuildKeySequence(newChildren);
        
        if (!oldKeys.SequenceEqual(newKeys))
        {
            // Keys changed - replace all children
            RemoveAllChildren(oldParent, patches);
            AddAllChildren(newParent, patches);
            return;
        }
    }
    
    // Diff matching children
    for (int i = 0; i < shared; i++)
        DiffInternal(oldChildren[i], newChildren[i], oldParent, patches);
    
    // Remove extra old children
    for (int i = oldChildren.Length - 1; i >= shared; i--)
        patches.Add(new RemoveChild(oldParent, oldChildren[i]));
    
    // Add new children
    for (int i = shared; i < newChildren.Length; i++)
        patches.Add(new AddChild(newParent, newChildren[i]));
}
```

## Patch Types

| Patch | Description |
| ----- | ----------- |
| `AddRoot(element)` | Set root element |
| `ReplaceChild(old, new)` | Replace element with another |
| `AddChild(parent, child)` | Append child element |
| `RemoveChild(parent, child)` | Remove child element |
| `UpdateAttribute(el, attr, value)` | Update attribute value |
| `AddAttribute(el, attr)` | Add new attribute |
| `RemoveAttribute(el, attr)` | Remove attribute |
| `AddHandler(el, handler)` | Add event handler |
| `RemoveHandler(el, handler)` | Remove event handler |
| `UpdateHandler(el, old, new)` | Update event handler |
| `UpdateText(node, text, newId)` | Update text content |
| `AddText(parent, text)` | Add text node |
| `RemoveText(parent, text)` | Remove text node |
| `AddRaw(parent, raw)` | Add raw HTML |
| `RemoveRaw(parent, raw)` | Remove raw HTML |
| `ReplaceRaw(old, new)` | Replace raw HTML |
| `UpdateRaw(node, html, newId)` | Update raw HTML content |

## Patch Application

Patches are applied via JavaScript interop:

```csharp
public static async Task Apply(Patch patch)
{
    switch (patch)
    {
        case AddRoot addRoot:
            await Interop.SetAppContent(Render.Html(addRoot.Element));
            Runtime.RegisterHandlers(addRoot.Element);
            break;
            
        case ReplaceChild replace:
            Runtime.UnregisterHandlers(replace.OldElement);
            await Interop.ReplaceChildHtml(replace.OldElement.Id, Render.Html(replace.NewElement));
            Runtime.RegisterHandlers(replace.NewElement);
            break;
            
        case UpdateAttribute update:
            await Interop.UpdateAttribute(update.Element.Id, update.Attribute.Name, update.Value);
            break;
            
        // ... other cases
    }
}
```

## Performance Optimizations

### Object Pooling

Lists and dictionaries are pooled to reduce allocations:

```csharp
private static readonly ConcurrentQueue<List<Patch>> _patchListPool = new();
private static readonly ConcurrentQueue<Dictionary<string, Attribute>> _attributeMapPool = new();

private static List<Patch> RentPatchList()
{
    if (_patchListPool.TryDequeue(out var list))
    {
        list.Clear();
        return list;
    }
    return new List<Patch>();
}

private static void ReturnPatchList(List<Patch> list)
{
    if (list.Count < 1000)  // Prevent memory bloat
        _patchListPool.Enqueue(list);
}
```

### Early Exits

The algorithm checks for common cases that don't require comparison:

```csharp
// Reference equality - same object
if (ReferenceEquals(oldAttrs, newAttrs))
    return;

// Both empty
if (oldAttrs.Length == 0 && newAttrs.Length == 0)
    return;
```

### ID Preservation

IDs are preserved across renders to enable accurate patching:

```csharp
private static Node PreserveIds(Node? oldNode, Node newNode)
{
    if (oldNode is Element oldEl && newNode is Element newEl && oldEl.Tag == newEl.Tag)
    {
        // Preserve attribute IDs
        var attrs = newEl.Attributes.Select(attr => 
        {
            var oldAttr = Array.Find(oldEl.Attributes, a => a.Name == attr.Name);
            return attr with { Id = oldAttr?.Id ?? attr.Id };
        }).ToArray();
        
        // Preserve child IDs recursively
        var children = newEl.Children.Zip(oldEl.Children)
            .Select(pair => PreserveIds(pair.Second, pair.First))
            .ToArray();
        
        return new Element(oldEl.Id, newEl.Tag, attrs, children);
    }
    return newNode;
}
```

## Keyed Reconciliation

Keys enable efficient list reordering:

```csharp
private static string? GetKey(Node node)
{
    if (node is not Element element)
        return null;
    
    foreach (var attr in element.Attributes)
    {
        if (attr.Name == "data-key" || attr.Name == "key")
            return attr.Value;
    }
    
    return null;
}

private static bool HasKeyedChildren(Node[] children)
    => children.Any(child => GetKey(child) is not null);
```

When key order changes, all children are replaced to preserve correct ordering:

```csharp
if (!oldKeys.SequenceEqual(newKeys))
{
    // Remove all old children
    for (int i = oldLength - 1; i >= 0; i--)
        patches.Add(new RemoveChild(oldParent, oldChildren[i]));
    
    // Add all new children
    for (int i = 0; i < newLength; i++)
        patches.Add(new AddChild(newParent, newChildren[i]));
}
```

## Rendering to HTML

The `Render.Html` function converts virtual DOM to HTML string:

```csharp
public static string Html(Node node)
{
    var sb = new StringBuilder();
    RenderNode(node, sb);
    return sb.ToString();
}

private static void RenderNode(Node node, StringBuilder sb)
{
    switch (node)
    {
        case Element element:
            sb.Append($"<{element.Tag} id=\"{element.Id}\"");
            foreach (var attr in element.Attributes)
                sb.Append($" {attr.Name}=\"{HtmlEncode(attr.Value)}\"");
            sb.Append('>');
            foreach (var child in element.Children)
                RenderNode(child, sb);
            sb.Append($"</{element.Tag}>");
            break;
            
        case Text text:
            sb.Append($"<span id=\"{text.Id}\">{HtmlEncode(text.Value)}</span>");
            break;
            
        case RawHtml raw:
            sb.Append($"<span id=\"{raw.Id}\">{raw.Html}</span>");
            break;
    }
}
```

## Complexity Analysis

| Operation | Time Complexity | Space Complexity |
| --------- | --------------- | ---------------- |
| Diff (same structure) | O(n) | O(h) |
| Diff (different structure) | O(n) | O(h) |
| Attribute comparison | O(a) | O(a) |
| Apply single patch | O(1) | O(1) |

Where:
- n = number of nodes
- h = tree height
- a = number of attributes

## See Also

- [API: DOM Types](../api/dom-types.md) — DOM type reference
- [Concepts: Virtual DOM](../concepts/virtual-dom.md) — Conceptual overview
- [ADR-003: Virtual DOM](../adr/ADR-003-virtual-dom.md) — Design decision
