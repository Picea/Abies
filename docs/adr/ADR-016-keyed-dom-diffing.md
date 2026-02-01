# ADR-016: Keyed DOM Diffing for Dynamic Lists

## Status
Accepted

## Date
2026-02-01

## Context

When rendering dynamic lists in the virtual DOM (such as navigation links, article lists, or any collection that changes based on state), the DOM diffing algorithm needs to correctly identify which elements have been added, removed, or moved.

The Abies framework uses the Praefixum source generator to create compile-time unique IDs for DOM elements. However, these IDs are based on the **call-site** in source code, not on the content or identity of the elements. This works well for static UI elements but causes problems with dynamic lists.

### The Problem

Consider a helper function that creates navigation links:

```csharp
private static Node NavLink(string url, string label, bool active) =>
    li([class_("nav-item")], [
        a([class_(active ? "nav-link active" : "nav-link"), href(url)], [text(label)])
    ]);
```

When this function is called multiple times:
```csharp
NavLink("/", "Home", true);
NavLink("/login", "Sign in", false);
NavLink("/register", "Sign up", false);
```

All three `li` elements receive the **same ID** because they all originate from the same call-site (line 2 of the function). This causes two problems:

1. **DOM diffing matches wrong elements**: Position-based matching updates the wrong elements when the list content changes.

2. **JavaScript getElementById returns wrong element**: When applying patches, `document.getElementById(id)` returns only the first matching element, so other elements aren't updated.

### Real-World Bug

After user registration, the navigation should change from:
- Home | Sign in | Sign up

To:
- Home | New Article | Settings | username

But because all `li` elements shared the same ID, the old "Sign in" and "Sign up" links remained visible alongside the new authenticated links.

## Decision

We implement two complementary solutions:

### 1. Key Attribute for DOM Diffing

Add a `key` attribute helper function that creates `data-key` attributes:

```csharp
public static DOM.Attribute key(string value, ...)
    => attribute("data-key", value, id);
```

The DOM diffing algorithm already supports keyed children - when elements have `data-key` or `key` attributes, it uses key-based matching instead of position-based matching.

### 2. Explicit IDs for Dynamic Elements

For elements created inside helper functions that may be called multiple times, pass an explicit `id` parameter based on a stable identifier:

```csharp
private static Node NavLink(string url, string label, bool active)
{
    // Generate stable ID from URL
    var stableId = $"nav-{url.Replace("/", "-").TrimStart('-').TrimEnd('-')}";
    
    return li([class_("nav-item"), key(url)], [
        a([class_(active ? "nav-link active" : "nav-link"), href(url)], [text(label)])
    ], id: stableId);
}
```

This ensures:
- Each element has a unique DOM ID
- `getElementById` correctly finds each specific element
- DOM patches are applied to the correct elements

## Consequences

### Positive

- **Correct DOM updates**: Elements are correctly added, removed, and updated when lists change
- **No duplicate/stale content**: UI accurately reflects application state
- **Better performance for reordering**: Keyed diffing can reuse existing DOM nodes when items are reordered

### Negative

- **Developer responsibility**: Developers must remember to use keys and explicit IDs for dynamic lists
- **ID generation logic**: The pattern for generating stable IDs needs to be applied consistently

### Neutral

- **No change to compile-time ID generation**: Praefixum still generates IDs for static elements automatically

## Implementation Guidelines

1. **Use `key()` for any list that may change**: Articles, comments, tags, navigation links, etc.

2. **Use explicit `id:` parameter when**:
   - A helper function creates elements that will be called multiple times
   - The function is used within loops or dynamic contexts

3. **Choose stable keys based on**:
   - Database IDs for entities
   - URLs for navigation links
   - Slugs for articles
   - Any value that uniquely identifies the item and doesn't change

4. **Avoid using array indices as keys**: They don't provide stable identity when items are reordered or removed.

## Related ADRs

- [ADR-003: Virtual DOM](ADR-003-virtual-dom.md) - DOM diffing algorithm details
- [ADR-014: Compile-Time Unique IDs](ADR-014-compile-time-ids.md) - Praefixum ID generation

## References

- [React Keys Documentation](https://react.dev/learn/rendering-lists#keeping-list-items-in-order-with-key) - Similar concept in React
- [Elm Keyed Nodes](https://package.elm-lang.org/packages/elm/html/latest/Html-Keyed) - Elm's approach to keyed virtual DOM
