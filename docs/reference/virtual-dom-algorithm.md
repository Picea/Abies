# Virtual DOM Diff Algorithm

Abies implements a virtual DOM diff algorithm inspired by Elm's VirtualDom and Inferno's keyed reconciliation. The algorithm is a pure function with no browser or JavaScript dependencies — it works identically for WASM, server-side rendering, and test environments.

All diffing logic lives in `Abies/Diff.cs` in the static class `Operations`.

## Node Type Hierarchy

The virtual DOM is an Abstract Syntax Tree (AST) of immutable record types:

```
Node(Id: string)
├── Element(Id, Tag, Attributes, Children)
├── Text(Id, Value)
├── RawHtml(Id, Html)
├── Empty()
├── Memo<TKey>(Id, Key, CachedNode) : MemoNode
└── LazyMemo<TKey>(Id, Key, Factory, CachedNode?) : LazyMemoNode
```

Every node carries a string `Id` for stable identity across renders. IDs are generated at compile time by the Praefixum source generator, ensuring deterministic, zero-cost identification.

### Memoization Nodes

**`MemoNode` (interface)** — Eager memoization. The `CachedNode` is always materialized. During diffing, if the memo key equals the previous key (via `EqualityComparer<TKey>.Default` to avoid boxing), the cached node is reused without re-diffing the subtree.

**`LazyMemoNode` (interface)** — Lazy memoization. The `Factory` function is only called if the key differs. This is the equivalent of Elm's `lazy` function — the true performance benefit is that the node content is never even *constructed* when the key matches.

Both use generic `EqualityComparer<TKey>` for key comparison to avoid boxing overhead for value-type keys. The `MemoKeyEquals` method on each interface enables polymorphic key comparison in the diff algorithm without reflection (trim-safe for WASM).

### Attribute Types

```
Attribute(Id, Name, Value)
└── Handler(EventName, CommandId, Command, Id, WithData?, Deserializer?)
```

`Handler` extends `Attribute` and renders as `data-event-{EventName}="{CommandId}"` in the DOM. The `CommandId` links the DOM attribute to the registered event handler in the `HandlerRegistry`.

## Algorithm Overview

The `Operations.Diff(oldNode, newNode)` method computes the minimal set of `Patch` instructions to transform an old virtual DOM tree into a new one:

```
Operations.Diff(old, new)
  │
  ├── Reference equality bailout ──────── O(1)
  │
  ├── LazyMemo key comparison ─────────── skip evaluation AND diffing
  │
  ├── Memo key comparison ─────────────── skip subtree diffing
  │
  ├── Text/RawHtml value comparison ───── O(1) per node
  │
  ├── Element (same tag)
  │   ├── DiffAttributes
  │   ├── Void element skip ───────────── no children to diff
  │   └── DiffChildren
  │       ├── Head/tail skip
  │       ├── Fast paths (clear, add-all, append)
  │       ├── Small count (≤8): O(n²) linear scan
  │       └── Keyed reconciliation
  │           ├── Reorder: LIS algorithm O(n log n)
  │           └── Membership change: add/remove/diff sets
  │
  └── Element (different tag) ─────────── replace
```

### Step 1: Reference Equality Bailout

```csharp
if (ReferenceEquals(oldNode, newNode))
    return;
```

If both nodes are the exact same reference (e.g., a cached node reused across renders), there is nothing to diff. This is an O(1) check that short-circuits entire subtrees.

### Step 2: Memo Node Comparison

**LazyMemo** — The most impactful optimization. If both old and new are `LazyMemoNode` and their keys match (via `MemoKeyEquals`), the diff skips both *evaluation* and *diffing* entirely. The factory function is never called:

```csharp
if (oldNode is LazyMemoNode oldLazy && newNode is LazyMemoNode newLazy)
{
    if (oldLazy.MemoKeyEquals(newLazy))
    {
        MemoHits++;  // Simple increment — WASM is single-threaded
        return;      // Skip evaluation AND diffing
    }
    MemoMisses++;
    // Keys differ — evaluate and diff the results
}
```

**Memo** — Similar but for eager memos. If keys match, skip diffing the `CachedNode` subtree.

The diff algorithm maintains counters (`MemoHits`, `MemoMisses`) for diagnostics.

### Step 3: Text and RawHtml Comparison

Text and RawHtml nodes are compared by value. If the value or ID changed, an `UpdateText` or `UpdateRaw` patch is emitted.

### Step 4: Element Diffing

If both nodes are `Element`:

- **Different tag** → emit `ReplaceChild` (or `AddRoot` at the root level)
- **Same tag** → `DiffAttributes` then `DiffChildren`

**Void element optimization**: Elements whose tag is in `HtmlSpec.VoidElements` (e.g., `<input>`, `<br>`, `<img>`, `<hr>`, `<meta>`, `<source>`) skip `DiffChildren` entirely, avoiding unnecessary work.

## Attribute Diffing

Attribute diffing (`DiffAttributes`) handles three attribute types differently:

| Patch Type | When |
|---|---|
| `AddAttribute` | New attribute not in old |
| `RemoveAttribute` | Old attribute not in new |
| `UpdateAttribute` | Same name, different value |
| `AddHandler` | New event handler |
| `RemoveHandler` | Old handler removed |
| `UpdateHandler` | Same event, different handler |

### Same-Order Fast Path

Most renders don't change attribute order or count. When old and new have the same count, the algorithm compares them positionally first:

```csharp
if (oldAttrs.Length == newAttrs.Length)
{
    var sameOrder = true;
    for (int i = 0; i < oldAttrs.Length; i++)
    {
        if (oldAttrs[i].Name != newAttrs[i].Name)
        {
            sameOrder = false;
            break;
        }
    }

    if (sameOrder)
    {
        // Compare positionally — no dictionary needed
    }
}
```

This avoids dictionary allocation and hash computation overhead for the common case.

### Dictionary Fallback

When attribute order or count differs, the algorithm builds a `Dictionary<string, Attribute>` from the old attributes and looks up each new attribute. The dictionary is rented from an object pool (`Stack<Dictionary<...>>`) and returned after use.

## Children Diffing

Children diffing (`DiffChildren`) is the most complex part of the algorithm. It uses a multi-phase approach with several fast paths.

### Key Generation

Each child is assigned a key for identity matching:

1. **Explicit key** — `data-key` or `key` attribute on the element (set via `key()` in the HTML DSL)
2. **Element ID** — The Praefixum-generated ID (default)
3. **Index string** — For non-keyed children, a cached index string (`"__index:0"`, `"__index:1"`, etc.) from a pre-allocated 256-entry cache

The index string cache avoids string interpolation allocation for the 99% case where elements have fewer than 256 children.

### Phase 1: Head/Tail Skip

Before building key maps, the algorithm skips common prefix (head) and suffix (tail) elements:

```
Old: [A, B, C, D, E]
New: [A, B, X, Y, E]
         ↑        ↑
     headSkip  tailSkip

Middle to diff: old=[C, D], new=[X, Y]
```

This is highly effective for:
- **Append-only** patterns (chat messages, logs) — the entire old list is head-skipped
- **Single item changes** — head + tail skip leaves a tiny middle
- **Prepend** patterns — the entire old list is tail-skipped

Head and tail elements are still diffed recursively (they might have changed attributes or children), but they don't participate in the expensive keyed reconciliation.

### Phase 2: Fast Paths

After head/tail skip, several fast paths avoid the full keyed reconciliation:

| Condition | Fast Path | Patch Type |
|---|---|---|
| Old non-empty, new empty | Clear all children | `ClearChildren` |
| Old empty, new non-empty | Set all children via innerHTML | `SetChildrenHtml` |
| All old matched in head skip, new has extras | Append via insertAdjacentHTML | `AppendChildrenHtml` |
| Middle empty after skip | Done (head/tail covered everything) | — |

The `SetChildrenHtml` and `AppendChildrenHtml` patches are critical performance optimizations — they replace N individual `AddChild` patches with a single bulk DOM operation.

**MaterializeChildren**: Before emitting `SetChildrenHtml` or `AppendChildrenHtml`, the algorithm materializes any `LazyMemoNode` or `MemoNode` wrappers in the children array. This ensures that `RegisterHandlers` (which registers command IDs) and `Render.HtmlChildren` (which renders `data-event-*` attributes) see the same concrete nodes with the same `CommandId`s.

### Phase 3: Small Count Fast Path

For middle sections with ≤8 children on both sides, the algorithm uses an O(n²) linear scan with `stackalloc` instead of building dictionaries:

```csharp
private const int SmallChildCountThreshold = 8;

// stackalloc for tracking matched indices — no heap allocation
Span<int> oldMatched = stackalloc int[oldLength];
Span<int> newMatched = stackalloc int[newLength];
```

This eliminates dictionary allocation and hashing overhead, which dominates for small n. The threshold of 8 was chosen based on benchmarks.

### Phase 4: Keyed Reconciliation

For larger middle sections, the algorithm builds `Dictionary<string, int>` maps (rented from object pools) and determines whether the change is a **reorder** (same keys, different order) or a **membership change** (some keys added/removed).

#### Reorder Detection

```csharp
var isReorder = oldMiddleLength == newMiddleLength &&
                AreKeysSameSet(oldMiddleKeys, newKeyToIndex);
```

If all old keys exist in new keys and the counts match, it's a pure reorder.

#### LIS Algorithm (Longest Increasing Subsequence)

For reorders, the algorithm computes the Longest Increasing Subsequence of old indices in new order. Elements in the LIS don't need to be moved — only elements outside the LIS require DOM `insertBefore` operations.

The LIS algorithm uses **patience sorting with binary search** (O(n log n)):

```csharp
private static void ComputeLISInto(ReadOnlySpan<int> arr, Span<bool> inLIS)
{
    // result[j] = index in arr of smallest ending value for LIS of length j+1
    // p[i] = predecessor index for position i in the LIS chain
    var result = ArrayPool<int>.Shared.Rent(len);
    var p = ArrayPool<int>.Shared.Rent(len);

    for (int i = 0; i < len; i++)
    {
        // Binary search to find position where val fits
        int lo = 0, hi = lisLen;
        while (lo < hi)
        {
            var mid = (lo + hi) >> 1;
            if (arr[result[mid]] < val) lo = mid + 1;
            else hi = mid;
        }
        // ... update result, p, lisLen
    }

    // Mark LIS positions by following predecessor chain backwards
}
```

A separate `ComputeLISIntoSmall` variant uses `stackalloc` instead of `ArrayPool` for small arrays.

After computing the LIS, elements NOT in the LIS are moved to their correct positions by iterating in reverse and emitting `MoveChild` patches with `insertBefore` semantics.

#### Membership Change

When keys differ between old and new:

1. **Remove** — old keys not in new → `RemoveChild` / `RemoveText` / `RemoveRaw` (iterate backwards)
2. **Diff** — keys present in both → recursive `DiffInternal`
3. **Add** — new keys not in old → `AddChild` / `AddText` / `AddRaw`

**Complete replacement fast path**: When NO keys overlap (e.g., replacing an entire list), emit `ClearChildren` + `SetChildrenHtml` for 2 bulk operations instead of N+M individual patches.

## Patch Types

All patch types are `readonly struct` implementing the `Patch` marker interface, ensuring zero allocation on the hot path:

### Tree Mutations

| Patch | Purpose | Fields |
|---|---|---|
| `AddRoot` | Set root element (initial render) | `Element` |
| `ReplaceChild` | Replace element with another | `OldElement`, `NewElement` |
| `AddChild` | Append child to parent | `Parent`, `Child` |
| `RemoveChild` | Remove child from parent | `Parent`, `Child` |
| `ClearChildren` | Remove all children | `Parent`, `OldChildren` |
| `SetChildrenHtml` | Set all children via innerHTML | `Parent`, `Children` |
| `AppendChildrenHtml` | Append children via insertAdjacentHTML | `Parent`, `Children` |
| `MoveChild` | Move child to new position | `Parent`, `Child`, `BeforeId?` |

### Attribute Mutations

| Patch | Purpose | Fields |
|---|---|---|
| `AddAttribute` | Add new attribute | `Element`, `Attribute` |
| `RemoveAttribute` | Remove attribute | `Element`, `Attribute` |
| `UpdateAttribute` | Change attribute value | `Element`, `Attribute`, `Value` |

### Handler Mutations

| Patch | Purpose | Fields |
|---|---|---|
| `AddHandler` | Add event handler | `Element`, `Handler` |
| `RemoveHandler` | Remove event handler | `Element`, `Handler` |
| `UpdateHandler` | Replace event handler | `Element`, `OldHandler`, `NewHandler` |

### Text Mutations

| Patch | Purpose | Fields |
|---|---|---|
| `AddText` | Add text node | `Parent`, `Child` |
| `RemoveText` | Remove text node | `Parent`, `Child` |
| `UpdateText` | Change text content | `Parent`, `Node`, `Text`, `NewId` |

### Raw HTML Mutations

| Patch | Purpose | Fields |
|---|---|---|
| `AddRaw` | Add raw HTML node | `Parent`, `Child` |
| `RemoveRaw` | Remove raw HTML node | `Parent`, `Child` |
| `ReplaceRaw` | Replace raw HTML node | `OldNode`, `NewNode` |
| `UpdateRaw` | Update raw HTML content | `Node`, `Html`, `NewId` |

### Head Element Mutations

| Patch | Purpose | Fields |
|---|---|---|
| `AddHeadElement` | Add managed `<head>` element | `Content` |
| `UpdateHeadElement` | Update managed `<head>` element | `Content` |
| `RemoveHeadElement` | Remove managed `<head>` element | `Key` |

All patches address DOM nodes by their stable string `Id` rather than positional paths. This enables O(1) element lookup via `getElementById` and makes patches order-independent.

## Performance Optimizations

The diff algorithm is heavily optimized for WASM's single-threaded, memory-constrained environment.

### Object Pools

Several data structures are pooled using `Stack<T>` (safe because WASM is single-threaded):

| Pool | Type | Purpose |
|---|---|---|
| `PatchListPool` | `Stack<List<Patch>>` | Intermediate patch lists |
| `AttributeMapPool` | `Stack<Dictionary<string, Attribute>>` | Attribute diff fallback |
| `KeyIndexMapPool` | `Stack<Dictionary<string, int>>` | Keyed children reconciliation |
| `IntListPool` | `Stack<List<int>>` | Keys to add/remove indices |
| `IntPairListPool` | `Stack<List<(int,int)>>` | Old→new index pairs for diffing |

Pools have size limits (e.g., `PatchListPool` rejects lists with >1000 entries) to prevent memory bloat.

### ArrayPool

Key arrays and LIS working storage use `ArrayPool<T>.Shared` to avoid allocation:

```csharp
var oldKeysArray = ArrayPool<string>.Shared.Rent(oldLength);
var newKeysArray = ArrayPool<string>.Shared.Rent(newLength);
try {
    // ... diff using rented arrays
} finally {
    Array.Clear(oldKeysArray, 0, oldLength);  // prevent string reference leaks
    ArrayPool<string>.Shared.Return(oldKeysArray);
}
```

### stackalloc

For small child counts (≤8), `stackalloc` is used for matching indices and LIS computation, eliminating all heap allocation:

```csharp
Span<int> oldMatched = stackalloc int[oldLength];
Span<int> newMatched = stackalloc int[newLength];
Span<int> oldIndices = stackalloc int[newLength];
Span<bool> inLIS = stackalloc bool[newLength];
```

### Index String Cache

A pre-allocated 256-entry cache avoids string interpolation for non-keyed children:

```csharp
private const int IndexStringCacheSize = 256;
private static readonly string[] IndexStringCache = InitializeIndexStringCache();

private static string GetIndexString(int index) =>
    (uint)index < IndexStringCacheSize ? IndexStringCache[index] : $"__index:{index}";
```

### Memo Diagnostics

Internal counters track memo cache effectiveness:

```csharp
internal static int MemoHits;    // Subtree diff skipped
internal static int MemoMisses;  // Subtree diff required
```

Simple increment (no `Interlocked`) because WASM is single-threaded.

## Complexity Summary

| Operation | Complexity | Notes |
|---|---|---|
| Reference equality | O(1) | Short-circuits unchanged subtrees |
| Memo key comparison | O(1) | Skips entire subtree evaluation + diffing |
| Text/RawHtml comparison | O(n) | String comparison |
| Attribute diff (same order) | O(n) | Positional comparison, no dictionary |
| Attribute diff (different order) | O(n) | Dictionary-based, pooled |
| Children diff (head/tail skip) | O(k) | k = length of matching prefix + suffix |
| Children diff (small, ≤8) | O(n²) | Linear scan with stackalloc |
| Children diff (keyed, reorder) | O(n log n) | LIS with patience sorting |
| Children diff (keyed, membership) | O(n) | Dictionary-based set operations |

## Source Files

| File | Role |
|---|---|
| `Abies/Diff.cs` | Complete diff algorithm (`Operations` class) |
| `Abies/DOM/Node.cs` | Node type hierarchy (`Node`, `Element`, `Text`, `RawHtml`, `MemoNode`, `LazyMemoNode`) |
| `Abies/DOM/Patch.cs` | Patch type definitions (all `readonly struct`) |
| `Abies/DOM/Attribute.cs` | `Attribute` and `Handler` records |
| `Abies/RenderBatchWriter.cs` | Binary serialization of patches |
| `Abies/Render.cs` | HTML string rendering for bulk patches |
| `Abies/Head.cs` | `HeadContent` types and `HeadDiff` |
