# ADR-003: Virtual DOM Implementation

**Status:** Accepted  
**Date:** 2024-01-15  
**Decision Makers:** Abies Core Team  
**Supersedes:** None  
**Superseded by:** None

## Context

Web applications need to synchronize application state with the browser's DOM. Direct DOM manipulation is:

1. Error-prone (forgetting to update elements)
2. Performance-sensitive (unnecessary reflows)
3. Hard to test (requires browser environment)
4. Difficult to reason about (imperative mutations)

Abies needed a strategy to render declarative views to the DOM while:

- Minimizing DOM operations for performance
- Keeping the view function pure
- Working within WebAssembly's constraints
- Supporting event handler binding

## Decision

We implement a **Virtual DOM** system inspired by Elm's approach. The system consists of:

1. **Virtual DOM nodes** (`Node`, `Element`, `Text`, `RawHtml`) representing the desired DOM structure
2. **Diff algorithm** that compares old and new virtual DOM trees
3. **Patch generation** producing a list of minimal changes
4. **Patch application** via JavaScript interop to update the real DOM

Core types in `Abies.DOM`:

```csharp
public record Node(string Id);
public record Element(string Id, string Tag, Attribute[] Attributes, params Node[] Children) : Node(Id);
public record Text(string Id, string Value) : Node(Id);
public record RawHtml(string Id, string Html) : Node(Id);
```

Patch types include:

- `AddRoot`, `ReplaceChild`, `AddChild`, `RemoveChild`
- `AddAttribute`, `RemoveAttribute`, `UpdateAttribute`
- `AddHandler`, `RemoveHandler`, `UpdateHandler`
- `UpdateText`, `AddText`, `RemoveText`
- `AddRaw`, `RemoveRaw`, `ReplaceRaw`, `UpdateRaw`

The diffing algorithm (`Operations.Diff`):

1. Compares nodes recursively by type and position
2. Uses stable element IDs for efficient updates
3. Supports keyed children for list reordering (via `data-key` attribute)
4. Pools patch lists and attribute maps to reduce allocations

## Consequences

### Positive

- **Declarative views**: View functions describe desired state, not mutations
- **Efficient updates**: Only changed parts of the DOM are touched
- **Testable rendering**: Virtual DOM can be asserted without a browser
- **Handler coordination**: Event handlers are registered/unregistered atomically with DOM updates
- **Memory efficiency**: Object pooling reduces GC pressure

### Negative

- **Complexity**: Diffing and patching logic is non-trivial to maintain
- **Keyed list limitation**: Changing key order replaces entire child list (conservative strategy)
- **String-based IDs**: Elements require unique IDs, adding overhead
- **JavaScript bridge**: Each patch requires async interop call

### Neutral

- Text nodes are wrapped in `<span>` elements for stable IDs
- The diff algorithm is O(n) for typical trees

## Alternatives Considered

### Alternative 1: Direct DOM Manipulation

Mutate the DOM directly from C#:

- Simpler initial implementation
- Error-prone as applications grow
- Hard to optimize incrementally
- No declarative view abstraction

Rejected because it undermines the pure View function goal.

### Alternative 2: Full DOM Replacement

Replace entire app DOM on every update:

- Trivial to implement
- Extremely slow for non-trivial UIs
- Loses focus state, scroll position, etc.

Rejected for obvious performance reasons.

### Alternative 3: Blazor-style Render Tree

Use Blazor's `RenderTreeBuilder` approach:

- More complex diffing with component lifecycle
- Heavier runtime
- Not aligned with Elm-style simplicity

Rejected to keep the system minimal and Elm-like.

### Alternative 4: Fine-grained Reactivity (Solid-style)

Track dependencies at the expression level:

- Very efficient updates
- Complex dependency tracking
- Different mental model from MVU

Rejected because it doesn't fit the MVU architecture.

## Related Decisions

- [ADR-001: Model-View-Update Architecture](./ADR-001-mvu-architecture.md)
- [ADR-011: JavaScript Interop Strategy](./ADR-011-javascript-interop.md)

## References

- [Elm VirtualDom](https://github.com/elm/virtual-dom)
- [How Virtual DOM Works](https://teropa.info/blog/2015/03/02/change-and-its-detection-in-javascript-frameworks.html)
- [React Reconciliation](https://reactjs.org/docs/reconciliation.html)
- [`Abies/DOM/Operations.cs`](../../Abies/DOM/Operations.cs) - Authoritative implementation
