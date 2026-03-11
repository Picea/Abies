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

1. **Virtual DOM nodes** (`Node`, `Element`, `Text`, `RawHtml`, `Memo<TKey>`, `LazyMemo<TKey>`) representing the desired DOM structure
2. **Diff algorithm** that compares old and new virtual DOM trees with keyed reconciliation
3. **Binary batch generation** producing a compact binary buffer of minimal changes
4. **Patch application** via a single JavaScript interop call to update the real DOM

Core types in `Picea.Abies.DOM`:

```csharp
public record Node(string Id);
public record Element(string Id, string Tag, Attribute[] Attributes, params Node[] Children) : Node(Id);
public record Text(string Id, string Value) : Node(Id);
public record RawHtml(string Id, string Html) : Node(Id);
public sealed record Memo<TKey>(string Id, TKey Key, Node Inner) : Node(Id);
public sealed record LazyMemo<TKey>(string Id, TKey Key, Func<Node> Render) : Node(Id);
```

Patch types are defined as `BinaryPatchType` enum values in `RenderBatchWriter.cs` and include:

- **Root/structure**: `AddRoot`, `ClearChildren`
- **Child operations**: `AddChild`, `RemoveChild`, `ReplaceChild`, `MoveChild`
- **Attribute operations**: `AddAttribute`, `RemoveAttribute`, `UpdateAttribute`
- **Handler operations**: `AddHandler`, `RemoveHandler`, `UpdateHandler`
- **Text operations**: `UpdateText`, `AddText`, `RemoveText`
- **Raw HTML operations**: `AddRaw`, `RemoveRaw`, `ReplaceRaw`, `UpdateRaw`
- **Batch operations**: `SetChildrenHtml`, `AppendChildrenHtml`
- **Head management**: `SetHead`

The diffing algorithm (`Diff.cs`):

1. Compares nodes recursively by type and position
2. Uses stable element IDs for efficient updates
3. Supports full keyed reconciliation with LIS (Longest Increasing Subsequence) for optimal move operations
4. Head/tail skip optimization to avoid dictionary construction for common prefix/suffix matches
5. Memo and LazyMemo nodes enable skipping unchanged subtrees via key equality

All patches are written into a binary buffer via `RenderBatchWriter` and transferred to JavaScript in a single `ApplyBinaryBatch` interop call.

## Consequences

### Positive

- **Declarative views**: View functions describe desired state, not mutations
- **Efficient updates**: Only changed parts of the DOM are touched
- **Testable rendering**: Virtual DOM can be asserted without a browser
- **Handler coordination**: Event handlers are registered/unregistered atomically with DOM updates
- **Memory efficiency**: Object pooling reduces GC pressure
- **Optimal reordering**: LIS-based keyed reconciliation minimizes DOM move operations
- **Batch efficiency**: Binary batching transfers all patches in a single interop call

### Negative

- **Complexity**: Diffing and patching logic is non-trivial to maintain
- **String-based IDs**: Elements require unique IDs, adding overhead
- **Full VDOM rebuild**: MVU architecture requires rebuilding the entire virtual DOM on every render

### Neutral

- Text nodes are wrapped in `<span>` elements for stable IDs
- The diff algorithm is O(n) for typical trees, O(n log n) for keyed reordering

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
- [ADR-016: Keyed DOM Diffing](./ADR-016-keyed-dom-diffing.md)

## References

- [Elm VirtualDom](https://github.com/elm/virtual-dom)
- [How Virtual DOM Works](https://teropa.info/blog/2015/03/02/change-and-its-detection-in-javascript-frameworks.html)
- [React Reconciliation](https://reactjs.org/docs/reconciliation.html)
- [`Picea.Abies/Diff.cs`](../../Picea.Abies/Diff.cs) - Diff algorithm implementation
- [`Picea.Abies/DOM/Patch.cs`](../../Picea.Abies/DOM/Patch.cs) - Patch type definitions
- [`Picea.Abies/RenderBatchWriter.cs`](../../Picea.Abies/RenderBatchWriter.cs) - Binary batch writer

## Changelog

- **2026-03 (v2 migration)**: Updated to reflect current state after Picea migration.
  - Added `Memo<TKey>` and `LazyMemo<TKey>` to node types
  - Updated patch type list to include all current types (including `SetChildrenHtml`, `AppendChildrenHtml`, `SetHead`)
  - Replaced per-patch async interop description with binary batching architecture
  - Removed outdated "keyed list limitation" consequence — now has full LIS keyed reconciliation
  - Updated file references from `Abies/DOM/Operations.cs` → `Picea.Abies/Diff.cs`
  - Added link to ADR-016 (Keyed DOM Diffing)
