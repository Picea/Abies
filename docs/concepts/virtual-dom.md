# Virtual DOM

Abies uses a virtual DOM to efficiently update the UI. This document explains how the virtual DOM works, the binary batch protocol, and the performance optimizations that make Abies fast.

## What is a Virtual DOM?

The virtual DOM is an in-memory representation of the UI. Instead of manipulating the real DOM directly, Abies:

1. Builds a virtual DOM tree from your View function
2. Compares it to the previous virtual DOM (diffing)
3. Calculates minimal changes needed (patches)
4. Serializes patches into a binary batch
5. Applies only those changes to the platform

```text
┌──────────────────────────────────────────────────────────────┐
│                    Virtual DOM Flow                          │
│                                                              │
│  View(model)     Diff          Binary Batch     Platform     │
│      │            │                │               │         │
│      ▼            ▼                ▼               ▼         │
│  ┌───────┐   ┌─────────┐   ┌────────────┐   ┌─────────┐    │
│  │ VDOM  │──▶│ Compare │──▶│  Serialize │──▶│ Apply   │    │
│  │ Tree  │   │ Old/New │   │  to Binary │   │ Patches │    │
│  └───────┘   └─────────┘   └────────────┘   └─────────┘    │
└──────────────────────────────────────────────────────────────┘
```

## Platform-Agnostic Design

The virtual DOM is entirely platform-agnostic. The `Apply` delegate is the boundary:

| Platform | How Patches Are Applied |
| -------- | ---------------------- |
| **Browser** | Binary batch → JS `DataView` reader → DOM mutations |
| **Server** | Binary batch → WebSocket → client-side JS replay |
| **Tests** | Patches captured in a `List<Patch>` for assertions |

The same diff algorithm and binary serializer are used regardless of platform.

## Why Virtual DOM?

### 1. Declarative UI

You describe what the UI should look like, not how to change it:

```csharp
public static Document View(Model model)
    => new("App",
        model.IsLoggedIn
            ? UserDashboard(model.User)
            : LoginForm());
```

### 2. Performance

Direct DOM manipulation is expensive. The virtual DOM:

- Batches all changes into a single binary transfer
- Minimizes actual DOM operations via keyed diffing with LIS
- Avoids unnecessary reflows through bulk innerHTML operations

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

Abies supports five node types:

### Element Nodes

Standard HTML elements with attributes and children:

```csharp
div([class_("container"), id("main")], [
    h1([], [text("Title")]),
    p([], [text("Content")])
])
```

### Text Nodes

Plain text content:

```csharp
text("Hello, World!")
```

### Raw HTML Nodes

Pre-rendered HTML strings (use carefully):

```csharp
rawHtml("<strong>Bold</strong>")
```

### Memo Nodes

Cached nodes that skip diffing when the key hasn't changed:

```csharp
memo(myKey, () => ExpensiveView(data))
```

### Lazy Memo Nodes

Like memo nodes, but the view function is deferred — the node is only evaluated if the key changes:

```csharp
lazy(myKey, () => ExpensiveView(data), id: "item-42")
```

This is the primary performance optimization for list rendering. When the memo key matches between renders, Abies skips both node construction AND subtree diffing entirely.

## The Diff Algorithm

Abies compares old and new virtual DOM trees to find changes. The algorithm is inspired by Elm's VirtualDom and Inferno.

### 1. Reference Equality Bailout — O(1)

```csharp
if (ReferenceEquals(oldNode, newNode)) return;
```

Cached or reused nodes are detected instantly.

### 2. Memo Key Comparison

For memo and lazy memo nodes, the key is compared using `MemoKeyEquals()` — a generic method that avoids boxing overhead for value type keys:

```csharp
// Uses EqualityComparer<TKey>.Default — JIT-optimized, no boxing
if (oldLazy.MemoKeyEquals(newLazy))
{
    MemoHits++;  // Skip evaluation AND diffing entirely
    return;
}
```

### 3. Attribute Diffing

#### Same-Order Fast Path

Most renders don't change attribute order or count. When old and new attributes have the same count and names match positionally, Abies compares them in-place — avoiding dictionary allocation entirely:

```csharp
// O(n) positional comparison — no dictionary needed
for (int i = 0; i < oldAttrs.Length; i++)
{
    if (!newAttrs[i].Equals(oldAttrs[i]))
        patches.Add(new UpdateAttribute(...));
}
```

#### Dictionary Fallback

When attribute order or count changes, Abies falls back to dictionary-based diffing. Dictionaries are pooled (Stack<T>) to avoid allocation.

### 4. Children Diffing — Three-Phase Keyed Reconciliation

Children diffing is the most complex part. Abies uses a three-phase approach:

```text
Phase 1: Head Skip — Skip matching prefix
  [A, B, C, D, E]  old
  [A, B, X, Y, E]  new
   ^  ^              skip A, B (matching head)

Phase 2: Tail Skip — Skip matching suffix
  [A, B, C, D, E]  old
  [A, B, X, Y, E]  new
                ^   skip E (matching tail)

Phase 3: Middle Reconciliation
  [C, D]  old middle
  [X, Y]  new middle
  → build key maps, detect reorder vs membership change
```

#### Small Count Fast Path (≤ 8 children)

For elements with ≤ 8 children, Abies uses O(n²) linear scan with `stackalloc` instead of dictionary allocation. This is faster because dictionary overhead exceeds scan cost for small n.

#### LIS Algorithm for Minimal DOM Moves

When children are reordered (same set of keys, different order), Abies computes the Longest Increasing Subsequence (LIS) to determine the minimum number of DOM moves:

```text
Old order: [A, B, C, D, E]  (swap B↔D)
New order: [A, D, C, B, E]

LIS: [A, C, E]  (don't move these)
Move: D before C, B after C  (only 2 moves instead of 3)
```

The LIS algorithm uses patience sorting with binary search — O(n log n) time with ArrayPool to avoid allocation.

### Fast Paths

| Scenario | Optimization | Patches Emitted |
| -------- | ------------ | --------------- |
| Clear all children | `ClearChildren` | 1 |
| Add all children (0→N) | `SetChildrenHtml` | 1 (single innerHTML) |
| Append children | `AppendChildrenHtml` | 1 (single insertAdjacentHTML) |
| Complete replacement | `ClearChildren` + `SetChildrenHtml` | 2 |
| Reorder (same keys) | LIS → minimal `MoveChild` | LIS complement |
| Void elements | Skip children diff entirely | 0 |

## Patch Types

The diff produces patches that are serialized via the binary batch protocol:

| Patch | Description |
| ----- | ----------- |
| `AddRoot` | Set the root element |
| `ReplaceChild` | Replace a child element |
| `AddChild` | Add a new child |
| `RemoveChild` | Remove a child |
| `ClearChildren` | Remove all children |
| `SetChildrenHtml` | Set all children via innerHTML |
| `AppendChildrenHtml` | Append children via insertAdjacentHTML |
| `MoveChild` | Move a child to a new position |
| `AddAttribute` | Add a new attribute |
| `UpdateAttribute` | Change an attribute value |
| `RemoveAttribute` | Remove an attribute |
| `AddHandler` | Attach an event handler |
| `UpdateHandler` | Replace an event handler |
| `RemoveHandler` | Detach an event handler |
| `UpdateText` | Change text content |
| `AddHeadElement` | Add a `<head>` element (title, meta, link) |
| `UpdateHeadElement` | Update a `<head>` element |
| `RemoveHeadElement` | Remove a `<head>` element |

## The Binary Batch Protocol

Patches are serialized into a compact binary format for efficient transfer to JavaScript — inspired by Blazor's `RenderBatch` protocol.

### Binary Format

```text
Header (8 bytes):
  PatchCount:        int32 (4 bytes)
  StringTableOffset: int32 (4 bytes)

Patch Entries (20 bytes each):
  Type:   int32 (4 bytes)  — BinaryPatchType enum
  Field1: int32 (4 bytes)  — string table index (-1 = null)
  Field2: int32 (4 bytes)  — string table index (-1 = null)
  Field3: int32 (4 bytes)  — string table index (-1 = null)
    Field4: int32 (4 bytes)  — string table index (-1 = null)

String Table:
  LEB128 length prefix + UTF-8 bytes for each string
  Strings are deduplicated — identical values share one slot
```

### Why Binary?

The binary protocol replaced an earlier JSON-based approach:

| Aspect | JSON | Binary Batch |
| ------ | ---- | ------------ |
| Create 1k rows | 107ms | **89ms** (−17%) |
| Script time | 75ms | **58ms** (−23%) |
| String dedup | None | Yes (element IDs reused) |
| Transfer | Text serialization | MemoryView (zero-copy) |

### String Deduplication

Element IDs are frequently repeated (a patch references both parent ID and child ID). The string table deduplicates these — each unique string is stored once and referenced by index.

### LEB128 Encoding

String lengths use LEB128 (Little Endian Base 128) variable-length encoding:
- 1 byte for lengths 0–127
- 2 bytes for 128–16,383
- Compact for typical DOM values (IDs, attribute names, short HTML)

## Head Content Management

Abies manages `<head>` content (title, meta tags, links) through the same diff/patch pipeline:

```csharp
public static Document View(Model model)
    => new("My App",
        body: div([], [...]),
        head:
        [
            new MetaTag("description", "My Abies app"),
            new LinkTag("stylesheet", "/css/app.css")
        ]);
```

Head patches (`AddHeadElement`, `UpdateHeadElement`, `RemoveHeadElement`) flow through the same binary batch as body patches — a single interop call per render cycle.

## Keyed Lists

For dynamic lists, use the `id:` parameter to provide stable element identity:

```csharp
ul([], [
    ..model.Items.Select(item =>
        li([], [text(item.Name)], id: $"item-{item.Id}")
    )
])
```

### Why `id:` Instead of a Separate `key`?

Unlike React (`key={...}`), Vue (`:key="..."`), or Elm (`Keyed.node`), Abies uses the element's `id:` parameter for both diffing and patching:

| Framework | Keying Approach |
| --------- | --------------- |
| React | Separate `key` prop |
| Vue | Separate `:key` binding |
| Elm | Separate `Keyed.node` |
| **Abies** | **Unified `id:`** |

**Why?** Abies already needs unique IDs for DOM patching (finding elements by ID). Using the same ID for keyed diffing eliminates a concept — developers learn one thing instead of two.

## Performance Optimizations

### Object Pools

Abies pools frequently-used collections to avoid GC pressure:
- `List<Patch>` pool (Stack-based, LIFO for cache locality)
- `Dictionary<string, Attribute>` pool
- `Dictionary<string, int>` pool for key maps
- `ArrayPool<T>` for key sequences and LIS computation

### Void Element Optimization

Void elements (`<img>`, `<input>`, `<br>`, `<hr>`, `<meta>`, etc.) cannot have children per the HTML Living Standard §13.1.2. Abies skips the children diff entirely for these elements.

### MaterializeChildren

When emitting `SetChildrenHtml` or `AppendChildrenHtml` patches, lazy memo nodes must be evaluated so that both the handler registry and the HTML renderer see the same concrete nodes with the same CommandIds. `MaterializeChildren` ensures this consistency.

### Pre-allocated Index String Cache

```csharp
private static readonly string[] IndexStringCache = new string[256];
// Avoids $"__index:{i}" allocation for non-keyed children
```

## Debugging Virtual DOM

### Memo Diagnostics

Abies tracks memo hits and misses:

```csharp
Operations.MemoHits    // Subtree diffs skipped (key matched)
Operations.MemoMisses  // Subtree diffs performed (key changed)
```

### Patch Logging

In test mode, capture and inspect patches:

```csharp
var patches = new List<IReadOnlyList<Patch>>();
var runtime = await Runtime<Counter, CounterModel, Unit>.Start(
    apply: p => patches.Add(p),
    interpreter: _ => new ValueTask<Result<Message[], PipelineError>>(
        Result<Message[], PipelineError>.Ok([])));

await runtime.Dispatch(new CounterMessage.Increment());

// Inspect patches to see exactly what changed
foreach (var batch in patches)
    foreach (var patch in batch)
        Console.WriteLine(patch);
```

## Summary

The virtual DOM provides:

- ✅ Declarative UI — Describe what, not how
- ✅ Efficient updates — Binary batch protocol, keyed diffing with LIS
- ✅ Platform agnostic — Same diff runs in browser and server
- ✅ Performance optimized — Object pools, fast paths, memo nodes
- ✅ Head content management — Title, meta, links through same pipeline

## See Also

- [MVU Architecture](./mvu-architecture.md) — How View fits in MVU
- [Render Modes](./render-modes.md) — How patches reach the platform
- [Commands and Effects](./commands-effects.md) — Side effect handling
