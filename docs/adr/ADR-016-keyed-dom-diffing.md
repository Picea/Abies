# ADR-016: ID-Based DOM Diffing for Dynamic Lists

## Status

Accepted (Revised)

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

Use the existing `id:` parameter for both DOM identity and diffing. **No separate `key` concept is needed.**

The element's `id` already serves as a unique identifier for:

1. **DOM patching**: `getElementById(id)` finds the correct element
2. **Diffing**: The algorithm matches elements by `id` before falling back to position

### Solution: Explicit IDs for Dynamic Elements

For elements created inside helper functions that may be called multiple times, pass an explicit `id` parameter based on a stable identifier:

```csharp
private static Node NavLink(string url, string label, bool active)
{
    // Generate stable ID from URL
    var stableId = $"nav-{url.Replace("/", "-").TrimStart('-').TrimEnd('-')}";
    if (stableId == "nav-") stableId = "nav-home";
    
    return li([class_("nav-item")], [
        a([class_(active ? "nav-link active" : "nav-link"), href(url)], [text(label)])
    ], id: stableId);
}
```

This ensures:

- Each element has a unique DOM ID
- `getElementById` correctly finds each specific element
- DOM diffing matches elements by ID, not position
- DOM patches are applied to the correct elements

### Why Not a Separate `key` Attribute?

An earlier design considered adding a `key()` helper that creates `data-key` attributes separate from `id`. This was rejected because:

1. **Redundancy**: If you need stable identity for diffing, you also need it for patching—so `id` already solves both problems.
2. **Cognitive overhead**: Developers would need to understand when to use `key` vs `id` vs both.
3. **Simplicity**: One concept (`id`) is easier to understand and use correctly than two.

The `id` attribute is already part of the HTML spec and has well-understood semantics. Using it for diffing is a natural extension.

## Consequences

### Positive

- **Correct DOM updates**: Elements are correctly added, removed, and updated when lists change
- **No duplicate/stale content**: UI accurately reflects application state
- **Single concept**: Developers only need to understand `id:`, not `id:` + `key()`
- **Better performance for reordering**: ID-based diffing can reuse existing DOM nodes when items are reordered

### Negative

- **Developer responsibility**: Developers must remember to use explicit IDs for dynamic lists
- **ID generation logic**: The pattern for generating stable IDs needs to be applied consistently
- **ID pollution**: All keyed elements will have `id` attributes in the DOM (but this is often desirable for testing/accessibility)

### Neutral

- **No change to compile-time ID generation**: Praefixum still generates IDs for static elements automatically

## Implementation Guidelines

1. **Use explicit `id:` parameter when**:
   - A helper function creates elements that will be called multiple times
   - Elements are rendered in a loop or from a collection
   - The list content may change based on state

2. **Choose stable IDs based on**:
   - Database IDs for entities (e.g., `article-{slug}`)
   - URLs for navigation links (e.g., `nav-home`, `nav-settings`)
   - Slugs for articles (e.g., `comment-{id}`)
   - Any value that uniquely identifies the item and doesn't change

3. **Avoid using array indices as IDs**: They don't provide stable identity when items are reordered or removed.

4. **Static elements don't need explicit IDs**: Praefixum handles these automatically.

## Examples

### Navigation Links

```csharp
private static Node NavLink(string url, string label, bool active)
{
    var stableId = url switch
    {
        "/" => "nav-home",
        _ => $"nav-{url.Trim('/').Replace("/", "-")}"
    };
    
    return li([class_("nav-item")], [
        a([class_(active ? "nav-link active" : "nav-link"), href(url)], [text(label)])
    ], id: stableId);
}
```

### Article List

```csharp
articles.Select(article =>
    div([class_("article-preview")], [
        // article content
    ], id: $"article-{article.Slug}")
)
```

### Comments

```csharp
comments.Select(comment =>
    div([class_("card")], [
        // comment content  
    ], id: $"comment-{comment.Id}")
)
```

## Related ADRs

- [ADR-003: Virtual DOM](ADR-003-virtual-dom.md) - DOM diffing algorithm details
- [ADR-014: Compile-Time Unique IDs](ADR-014-compile-time-ids.md) - Praefixum ID generation

## Comparison with Other Frameworks

### Why React, Vue, and Elm Use a Separate `key`

Most frontend frameworks use a **separate `key` attribute** rather than reusing the element's `id`:

| Framework | Approach                | Key Attribute                      |
| --------- | ----------------------- | ---------------------------------- |
| React     | Separate `key` prop     | `key={item.id}`                    |
| Vue       | Separate `:key` binding | `:key="item.id"`                   |
| Elm       | Separate `Keyed.node`   | `(id, element)` tuple              |
| Svelte    | Keyed `{#each}` block   | `{#each items as item (item.id)}`  |

There are several reasons for this design:

#### 1. HTML `id` Must Be Globally Unique

The W3C HTML spec requires that `id` attributes be **unique across the entire document**. Using them for keying would mean:

- You'd need globally unique IDs even for simple list items
- If you render the same component twice on a page, you'd have ID collisions
- Accessibility tools and `document.getElementById()` would break

```jsx
// React's approach: key is local to siblings, id can be reused
<div id="item-card">
  <ul>
    {items.map(item => <li key={item.id}>...</li>)}
  </ul>
</div>
<div id="item-card">  {/* Can reuse id in different context */}
  <ul>
    {items.map(item => <li key={item.id}>...</li>)}  {/* Same keys, fine! */}
  </ul>
</div>
```

#### 2. `key` Is Framework-Internal, Not Rendered

React explicitly **does not render `key` to the DOM**:

> "Your components won't receive `key` as a prop. It's only used as a hint by React itself."

This separation has benefits:

- Keys don't pollute the DOM
- No interference with CSS `#id` selectors
- No accessibility side effects
- Cleaner generated HTML

#### 3. `id` Serves a Different Purpose

In React/Vue, `id` is for:

- CSS targeting (`#my-element`)
- JavaScript DOM selection (`getElementById`)
- Accessibility (ARIA references like `aria-labelledby`)
- Anchor links (`#section`)

Mixing identity concerns with diffing concerns would create confusion.

#### 4. Explicit Opt-In for Performance

Keyed diffing has overhead—the algorithm must build lookup tables and track matching elements. Elm requires explicit `Keyed.node` usage:

```elm
Keyed.node "ul" [] (List.map viewKeyedPresident presidents)

viewKeyedPresident president =
  ( president.name, lazy viewPresident president )  -- (key, element) tuple
```

This makes keying an explicit optimization rather than default behavior.

### Why Abies Can Unify `id` and Keying

Abies takes a different approach because of **Praefixum**—the compile-time ID generator:

| Aspect        | React/Vue/Elm                   | Abies                            |
| ------------- | ------------------------------- | -------------------------------- |
| ID generation | Manual at call site             | Automatic at compile time        |
| ID uniqueness | Developer responsibility        | Guaranteed by source generator   |
| Key vs ID     | Separate concepts               | Unified (ID *is* the key)        |
| Opt-in keying | Explicit (`key=`, `Keyed.node`) | Implicit (every element has ID)  |

Since every element in Abies **already has a guaranteed-unique ID** from Praefixum, we can reuse it for diffing without the problems that plague manual ID schemes.

#### The Key Insight

Abies uses IDs **internally for patching** (via `getElementById`), not for CSS or accessibility. The IDs are:

1. Generated automatically by Praefixum
2. Guaranteed unique within the application
3. Already required for the patching system to work
4. Not typically used for CSS styling (class-based styling is preferred)

This means the "ID must be globally unique" constraint is already satisfied, making a separate `key` concept redundant.

#### The Tradeoffs

Abies's unified approach requires:

1. **Compile-time infrastructure**: Praefixum source generator
2. **Override mechanism**: `id:` parameter for dynamic content
3. **All elements must have IDs**: Slight memory overhead

Most frameworks don't have (1), so they can't safely assume every element has a stable, unique identity.

### Summary

| Why others use separate `key`       | Why Abies can unify `id` + key       |
| ----------------------------------- | ------------------------------------ |
| HTML `id` must be globally unique   | Praefixum generates unique IDs       |
| `key` shouldn't pollute DOM         | IDs are already needed for patching  |
| Explicit opt-in for performance     | Every element already has ID anyway  |
| `id` has CSS/a11y/DOM purposes      | Abies IDs are internal, not for CSS  |

Abies's innovation is recognizing that once you have compile-time guaranteed IDs, the separate `key()` concept becomes redundant—the ID already serves both purposes.

## Runtime Integration: PreserveIds and Key-Based Matching

The `PreserveIds` function in `Runtime.cs` prepares the new virtual DOM tree for diffing by preserving attribute IDs from matching elements. This enables `DiffAttributes` to emit `UpdateAttribute` patches instead of `RemoveAttribute`/`AddAttribute` pairs.

### Critical Invariant: Key-Based Matching in PreserveIds

**The `PreserveIds` function MUST use key-based matching, not positional matching.**

An earlier bug in `PreserveIds` used positional matching for children:

```csharp
// ❌ BUG: Positional matching breaks keyed diffing
var oldChild = i < oldElement.Children.Length ? oldElement.Children[i] : null;
children[i] = PreserveIds(oldChild, newElement.Children[i]);
```

This caused the navigation bar bug where authenticated and unauthenticated links would mix together. When transitioning from:

- `[nav-home, nav-login, nav-register]` (unauthenticated)

To:

- `[nav-home, nav-editor, nav-settings, nav-profile-me]` (authenticated)

The positional matching would incorrectly pair:

- `nav-login` (old[1]) → `nav-editor` (new[1]) — IDs would be swapped!
- `nav-register` (old[2]) → `nav-settings` (new[2]) — IDs would be swapped!

This broke keyed diffing because the algorithm would see the same IDs and think no changes occurred.

### The Fix: Key-Based Child Matching

The corrected implementation uses the element's ID as the matching key:

```csharp
// ✅ CORRECT: Key-based matching preserves keyed diffing integrity
var oldChildrenById = new Dictionary<string, Node>();
foreach (var child in oldElement.Children)
{
    if (child is Element childElem)
        oldChildrenById[childElem.Id] = child;
}

var children = new Node[newElement.Children.Length];
for (int i = 0; i < newElement.Children.Length; i++)
{
    var newChild = newElement.Children[i];
    Node? matchingOldChild = null;
    
    if (newChild is Element newChildElem && oldChildrenById.TryGetValue(newChildElem.Id, out var oldMatch))
        matchingOldChild = oldMatch;
    
    children[i] = PreserveIds(matchingOldChild, newChild);
}
```

Additionally, `PreserveIds` now only preserves the parent element's ID when the old and new elements have **matching IDs**:

```csharp
// Only preserve IDs for elements with matching keys
if (oldElement.Tag == newElement.Tag && oldElement.Id == newElement.Id)
{
    return new Element(oldElement.Id, newElement.Tag, attrs, children);
}
```

This ensures that:

1. Elements with different IDs are treated as different elements (correct for keyed diffing)
2. Only matching elements have their attribute IDs preserved (for efficient updates)
3. The diffing algorithm receives unmodified IDs for non-matching elements

### Testing the Invariant

The `MainUpdateJourneyTests` in the integration tests verify that navigation transitions work correctly:

- `Register_Success_UpdatesNavigationToShowAuthenticatedLinks`
- `Login_Success_UpdatesNavigationToShowAuthenticatedLinks`
- `Register_FullJourney_NavigationUpdatesAfterApiCall`

These tests verify that after login/registration:

- "Sign in" and "Sign up" links are removed
- "New Article", "Settings", and username links appear
- No stale or duplicate elements remain

## References

- [React Keys Documentation](https://react.dev/learn/rendering-lists#keeping-list-items-in-order-with-key) - Similar concept (React uses separate `key`, but we chose to use `id`)
- [React Reconciliation Algorithm](https://legacy.reactjs.org/docs/reconciliation.html) - How React's diffing works
- [Elm Keyed Nodes](https://package.elm-lang.org/packages/elm/html/latest/Html-Keyed) - Elm's approach to keyed virtual DOM
- [Elm Guide: Html.Keyed](https://guide.elm-lang.org/optimization/keyed.html) - Why Elm uses explicit keying
