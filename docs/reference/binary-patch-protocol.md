---
description: 'Binary Patch Protocol — Reference for developers maintaining the DOM patching system'
---

# Binary Patch Protocol

This document describes the binary protocol used to transmit DOM patches from the .NET Abies runtime to the JavaScript renderer. It is intended for maintainers and contributors working on the rendering pipeline.

**Audience:** Framework maintainers, performance optimization contributors, framework archeologists

**Related:** [Virtual DOM Algorithm](./virtual-dom-algorithm.md), [JavaScript Interop](./js-interop.md)

---

## Overview

After each MVU render cycle, Abies produces a **render batch** — a sequence of DOM mutations (add child, remove child, replace element, update attribute, etc.). Instead of serializing these as JSON, which adds payload overhead and parsing cost, Abies uses a **binary protocol** optimized for:

- **Compact representation** — Enum values as bytes, strings deduplicated in a string table
- **Fast parsing** — Fixed-size entries, O(1) lookups, no parsing ambiguity
- **Zero-copy transfer** — JavaScript reads directly from allocated buffers using `DataView`

## Architecture

### High-Level Flow

```
C# (Abies Runtime)
    ↓
    MVU render cycle produces patches
    ↓
    RenderBatchWriter serializes to binary
    ↓
    MemoryView (Span<byte>) transferred to JavaScript
    ↓
JavaScript (abies.js)
    ↓
    BinaryReader parses patches
    ↓
    DOM operations executed
```

### Memory Layout

The binary batch is a contiguous byte array with three regions:

```
[Header (8 bytes)]
[Patch Entries (20 bytes each × N)]
[String Table (variable)]
```

---

## Header (Offset 0, 8 bytes)

| Offset | Size | Name | Type | Description |
|--------|------|------|------|-------------|
| 0 | 4 | PatchCount | int32 | Number of patch entries (N) |
| 4 | 4 | StringTableOffset | int32 | Byte offset where string table begins |

**Example:**
```
00 00 00 03 18 00 00 00
  └─ 3 patches        └─ string table starts at byte 18 (after 3 entries × 16 bytes + 8 byte header)
```

---

## Patch Entries (Offset 8, 20 bytes each)

Each entry describes one DOM operation.

| Offset | Size | Name | Type | Description |
|--------|------|------|------|-------------|----------|
| 0 | 4 | Type | BinaryPatchType (int32) | Enum: operation type |
| 4 | 4 | Field1 | StringTableIndex (int32) | Index into string table (or -1 for null) |
| 8 | 4 | Field2 | StringTableIndex (int32) | Index into string table (or -1 for null) |
| 12 | 4 | Field3 | StringTableIndex (int32) | Index into string table (or -1 for null) |
| 16 | 4 | Field4 | StringTableIndex (int32) | Index into string table (or -1 for null) |

**Field semantics depend on patch type:**

| Type | Field1 | Field2 | Field3 | Note |
|------|--------|--------|--------|------|
| CreateElement | tagName | null | null | Create `<div>`, `<span>`, etc |
| CreateText | text | null | null | Create text node |
| AddChild | parentId | childId | null | Insert child into parent |
| RemoveChild | parentId | childId | null | Remove child from parent |
| ReplaceChild | parentId | oldChildId | newChildId | Replace one child with another |
| ClearChildren | parentId | null | null | Remove all children |
| UpdateAttribute | elementId | attrName | attrValue | Set `element.setAttribute(name, value)` |
| RemoveAttribute | elementId | attrName | null | Remove attribute |
| UpdateText | textNodeId | text | null | Update text content |
| MoveChild | parentId | childId | beforeId | Move child before another (or end if beforeId=-1) |
| SetChildrenHtml | parentId | html | null | Bulk replace children with concatenated HTML |

---

## String Table (Offset StringTableOffset, variable)

Strings are stored in a deduplicated table to compress repeated values (tag names, attribute names, etc.).

### Format

```
[LEB128 length] [UTF-8 bytes] [LEB128 length] [UTF-8 bytes] ...
```

**LEB128** (Little Endian Base 128): Variable-length integer encoding.

- Values 0–127: Single byte
- Values 128+: Multiple bytes with continuation bit set

**Example:**

```
03 64 69 76    05 63 6C 61 73 73    01 78
└─ len=3 ──┘  └─ "div" ─┘  └─ len=5 ──┘  └─ "class" ─┘  └─ len=1 ─┘  └─ "x"
```

Decodes to strings: `["div", "class", "x"]`

### String Table Index

Patch entries reference strings by **zero-based index**:
- Index 0 → first string
- Index 1 → second string
- Index -1 → null (no string)

---

## Patch Types (BinaryPatchType Enum)

For the authoritative list, see `Picea.Abies/RenderBatchWriter.cs`.

| Value | Name | Purpose |
|-------|------|----------|
| 0 | AddRoot | Initialize root element |
| 1 | ReplaceChild | Replace one child with another |
| 2 | AddChild | Append child to parent |
| 3 | RemoveChild | Remove child from parent |
| 4 | ClearChildren | Remove all children |
| 5 | SetChildrenHtml | Bulk set children from HTML string |
| 6 | MoveChild | Reorder child within parent |
| 7 | UpdateAttribute | Set attribute on element |
| 8 | AddAttribute | Add new attribute |
| 9 | RemoveAttribute | Remove attribute from element |
| 10 | AddHandler | Register event handler |
| 11 | RemoveHandler | Unregister event handler |
| 12 | UpdateHandler | Update event handler |
| 13 | UpdateText | Change text node content |
| 14 | AddText | Add text node |
| 15 | RemoveText | Remove text node |
| 16 | AddRaw | Add raw HTML |
| 17 | RemoveRaw | Remove raw HTML |
| 18 | ReplaceRaw | Replace raw HTML |
| 19 | UpdateRaw | Update raw HTML |
| 20 | AddHeadElement | Add element to `<head>` |
| 21 | UpdateHeadElement | Update `<head>` element |
| 22 | RemoveHeadElement | Remove `<head>` element |
| 23 | AppendChildrenHtml | Append children from HTML string |

---

## Example: Create and Update

### Scenario

Render a button with dynamic text and classes:

```javascript
<button id="btn-1" class="primary" disabled>Click me</button>
```

### Binary Encoding

**Header:**
```
00 00 00 05  // 5 patches
2C 00 00 00  // string table at byte 88 (8 + 5*16)
```

**Patches (16 bytes each):**

```
// Patch 0: CreateElement "button"
00 00 00 00 | 00 00 00 00 | FF FF FF FF | FF FF FF FF

// Patch 1: SetAttribute id = "btn-1"
06 00 00 00 | 01 00 00 00 | 02 00 00 00 | FF FF FF FF

// Patch 2: SetAttribute class = "primary"
06 00 00 00 | 01 00 00 00 | 03 00 00 00 | FF FF FF FF

// Patch 3: SetAttribute disabled
06 00 00 00 | 01 00 00 00 | 04 00 00 00 | FF FF FF FF

// Patch 4: CreateText "Click me"
01 00 00 00 | 05 00 00 00 | FF FF FF FF | FF FF FF FF
```

**String Table (20 bytes):**
```
06 62 75 74 74 6F 6E     // "button" (len=6)
02 69 64                 // "id" (len=2)
08 62 74 6E 2D 31        // "btn-1" (len=8)
05 63 6C 61 73 73        // "class" (len=5)
07 70 72 69 6D 61 72 79  // "primary" (len=7)
08 43 6C 69 63 6B 20 6D 65 // "Click me" (len=8)
```

---

## JavaScript Parsing

The JavaScript side (`abies.js`) includes a `BinaryReader` that:

1. Reads header (patch count, string table offset)
2. Decodes string table into an array
3. Iterates patches, decoding fields as string table indices
4. Patches are applied immediately (typically via `addEventListener`, `setAttribute`, `appendChild`, etc.)

### Pseudocode

```javascript
function applyBinaryBatch(buffer) {
  const dv = new DataView(buffer);
  
  // Read header
  const patchCount = dv.getInt32(0, true);
  const stringTableOffset = dv.getInt32(4, true);
  
  // Decode string table
  const strings = decodeStringTable(buffer, stringTableOffset);
  
  // Apply patches
  for (let i = 0; i < patchCount; i++) {
    const offset = 8 + i * 16;
    const type = dv.getInt32(offset, true);
    const field1Index = dv.getInt32(offset + 4, true);
    const field2Index = dv.getInt32(offset + 8, true);
    const field3Index = dv.getInt32(offset + 12, true);
    
    const field1 = field1Index >= 0 ? strings[field1Index] : null;
    const field2 = field2Index >= 0 ? strings[field2Index] : null;
    const field3 = field3Index >= 0 ? strings[field3Index] : null;
    
    applyPatch(type, field1, field2, field3);
  }
}

function decodeStringTable(buffer, offset) {
  const strings = [];
  let pos = offset;
  
  while (pos < buffer.byteLength) {
    const [len, bytesRead] = decodeLEB128(buffer, pos);
    pos += bytesRead;
    
    const text = new TextDecoder().decode(buffer.slice(pos, pos + len));
    strings.push(text);
    pos += len;
  }
  
  return strings;
}
```

---

## Optimization: Fast Paths

### All-Children-Fast-Path (SetChildrenHtml)

When transitioning from 0 → N children (complete replacement), emit a single `SetChildrenHtml` patch instead of N `AddChild` patches:

- **Before:** `[AddChild parent/child-1, AddChild parent/child-2, ..., AddChild parent/child-N]` (N entries)
- **After:** `[SetChildrenHtml parent/concatenated-html]` (1 entry)

**JavaScript side:** `parent.innerHTML = html`

**Savings:** N-1 entries × 16 bytes = 16×(N-1) bytes, plus N DOM insertions reduced to 1 bulk operation.

### Head/Tail Skip (LIS Optimization)

When diffing keyed lists, skip unchanging prefix and suffix:

- Build key dictionaries only for the "middle" section that differs
- Run LIS algorithm on middle section only
- Emit fewer `MoveChild` patches

**Effect:** Reduces patch count for append-only or prepend-only operations.

---

## Memory Management

### Pooling

`RenderBatchWriter` instances are pooled to reuse allocations:

```csharp
private static readonly ObjectPool<RenderBatchWriter> _pool = ...;

public class RenderBatchWriter
{
    private readonly List<PatchData> _patches = new();  // Pooled
    private readonly StringBuilder _stringBuilder = new();  // Pooled
    private readonly Dictionary<string, int> _stringTable = new();  // Pooled
}
```

After a batch is transferred to JavaScript, the writer is returned to the pool — strings are cleared, patches list is cleared, but allocations are retained for reuse.

### Transfer

Batches are transferred to JavaScript via `JSType.MemoryView` (a C# type that wraps a `Span<byte>`):

```csharp
await JS.InvokeVoidAsync("applyBinaryBatch", memoryView);
```

JavaScript receives the wrapper and calls `.slice()` to get a `Uint8Array` copy. This enables zero-copy transfer semantics — no explicit copying in C#, and JavaScript's copy is the cheapest way to hand data across the JS/WASM boundary.

---

## Performance Considerations

### Patch Emission

**Cost drivers:**
1. Patch count — More patches = larger batch
2. String table size — Deduplicated but still contributes to memory
3. LEB128 encoding — Variable-length strings can expand slightly

**Optimization strategies:**
- Batch multiple mutations into fewer patches (e.g., `SetChildrenHtml` instead of N `AddChild`)
- Reuse strings via deduplication (already done)
- Skip unchanged subtrees (via memoization)

### Parsing (JavaScript)

**Cost drivers:**
1. String table decode — O(n) where n = character count
2. Patch iteration — O(m) where m = patch count
3. DOM operations — O(1) each, but 1000s of them adds up

**Optimization strategies:**
- Stream patches; don't buffer entire batch (already done)
- Batch DOM mutations via `requestAnimationFrame` (already done)
- Use `innerHTML` for bulk insertions (via `SetChildrenHtml`)

### Network

**Typical sizes:**
- Simple render (10 patches): ~400 bytes
- List update (1000 patches): ~30–50 KB
- Full page re-render (5000+ patches): ~150–300 KB

Binary encoding is ~40–50% smaller than JSON for equivalent data.

---

## Debugging & Inspection

### Dumping a Batch (C#)

Add logging to `RenderBatchWriter`:

```csharp
public void Dump()
{
    Console.WriteLine($"[Batch] {_patches.Count} patches, {_stringTable.Count} strings");
    foreach (var (patch, i) in _patches.Select((p, i) => (p, i)))
    {
        Console.WriteLine($"  [{i}] {patch.Type} {patch.Field1} {patch.Field2} {patch.Field3}");
    }
}
```

### Dumping a Batch (JavaScript)

In `abies.js`, add logging to `BinaryReader`:

```javascript
function dumpBatch(buffer) {
  const dv = new DataView(buffer);
  const patchCount = dv.getInt32(0, true);
  const stringTableOffset = dv.getInt32(4, true);
  
  console.log(`[Batch] ${patchCount} patches, strings at offset ${stringTableOffset}`);
  
  for (let i = 0; i < patchCount; i++) {
    const offset = 8 + i * 16;
    const type = dv.getInt32(offset, true);
    const f1 = dv.getInt32(offset + 4, true);
    const f2 = dv.getInt32(offset + 8, true);
    const f3 = dv.getInt32(offset + 12, true);
    console.log(`  [${i}] type=${type} f1=${f1} f2=${f2} f3=${f3}`);
  }
}
```

### Browser DevTools

Set a breakpoint in `applyBinaryBatch()` and inspect:
- `buffer.byteLength` — Total batch size
- `patchCount` — Number of patches
- `strings.length` — Number of deduplicated strings

---

## Historical Context

### Why Binary?

Early Abies used JSON serialization. Profiling revealed:

| Metric | JSON | Binary | Improvement |
|--------|------|--------|-------------|
| Payload size (1000 rows) | ~60 KB | ~25 KB | **58% smaller** |
| JS parse time | 8–12 ms | 2–3 ms | **70% faster** |
| Total script time | 75 ms | 57 ms | **24% faster** |

See [investigations/blazor-performance-analysis.md](../investigations/blazor-performance-analysis.md) for full analysis.

### Future Considerations

**Potential optimizations:**
- **CBOR encoding** — Standard binary format (smaller than our LEB128 format, tool ecosystem)
- **Shared memory protocol** — Similar to Blazor's `SharedMemoryRenderBatch` (investigated but no measurable benefit due to interop overhead)
- **Incremental patching** — Only encode changes, not full batch (requires more complex differencinglogic)

---

## Contributing

When modifying the binary protocol:

1. **Update patch types** in `BinaryPatchType` enum (c#) and `PatchType` switch (JavaScript)
2. **Update `RenderBatchWriter`** to emit new patch type
3. **Update `abies.js` patch handler** to apply new patch type
4. **Add tests** in `Picea.Abies.Browser.Tests` and `Picea.Abies.Benchmarks`
5. **Update this document** with new patch type and field semantics
6. **Benchmark** the change — ensure no regressions

### Source Files

| File | Purpose | Language |
|------|---------|----------|
| `Picea.Abies/DOM/Operations.cs` | Patch emission, diffing logic | C# |
| `Picea.Abies/DOM/RenderBatchWriter.cs` | Binary serialization | C# |
| `Picea.Abies.Browser/wwwroot/abies.js` | Patch parsing, DOM application | JavaScript |
| `Picea.Abies.Browser.Tests/RenderBatchTests.cs` | Binary format tests | C# |
| `Picea.Abies.Benchmarks/RenderingBenchmarks.cs` | Performance validation | C# |

---

## See Also

- [Virtual DOM Algorithm](./virtual-dom-algorithm.md) — Diff and patch strategy
- [JavaScript Interop](./js-interop.md) — .NET ↔ JavaScript bridge
- [Performance Guide](../guides/performance.md) — Optimization techniques
- [Benchmarking Strategy](../investigations/benchmarking-strategy.md) — How we validate performance

## Implementation Notes

**Tracking Issue:** [#217: Binary Patch Protocol Maintenance Guide](https://github.com/Picea/Abies/issues/217)

Last updated: 2026-04-12
