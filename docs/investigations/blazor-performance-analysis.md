# Blazor WASM Performance Analysis

## Executive Summary

This document provides a comprehensive analysis of why Blazor WASM outperforms Abies in certain js-framework-benchmark operations, and documents potential optimization paths.

**Key Finding**: Blazor's primary advantage is its **zero-copy binary protocol** via shared WebAssembly memory, which eliminates serialization overhead entirely.

## Current Performance Gap

| Benchmark | Abies | Blazor | Gap | Root Cause |
|-----------|-------|--------|-----|------------|
| 01_run1k | 107.1ms | 88.5ms | 1.21x | JSON serialization overhead |
| 05_swap1k | 124.8ms | 95.2ms | 1.31x | JSON + patch building overhead |
| 09_clear1k | 85.1ms | 46.2ms | **1.84x** | Handler unregistration + JSON overhead |
| First Paint | 74.2ms | 75ms | **0.99x** ✅ | Optimized with placeholder |
| Size | 1,225 KB | 1,377 KB | **0.89x** ✅ | Smaller bundle |
| Memory | 34.3 MB | 41.1 MB | **0.83x** ✅ | Lower memory footprint |

## Blazor Architecture Deep Dive

### 1. SharedMemoryRenderBatch - Zero Serialization

**This is Blazor's #1 performance advantage.**

Blazor uses a `SharedMemoryRenderBatch` that passes a **raw memory pointer** to JavaScript. The JavaScript side reads the render batch data **directly from .NET's WASM heap** without any serialization.

From `Boot.WebAssembly.Common.ts`:
```typescript
Blazor._internal.renderBatch = (browserRendererId: number, batchAddress: Pointer) => {
    // Read directly from .NET memory heap - no serialization!
    const heapLock = monoPlatform.beginHeapLock();
    try {
        renderBatch(browserRendererId, new SharedMemoryRenderBatch(batchAddress));
    } finally {
        heapLock.release();
    }
};
```

From `SharedMemoryRenderBatch.ts`:
```typescript
export class SharedMemoryRenderBatch implements RenderBatch {
    constructor(private batchAddress: Pointer) {}

    // Read directly from memory at fixed offsets
    updatedComponents(): ArrayRange<RenderTreeDiff> {
        return platform.readStructField<Pointer>(this.batchAddress, 0) as any;
    }
}
```

### 2. Binary Protocol Format

Blazor's RenderBatch uses a compact binary format with fixed-size entries:

From `RenderBatchWriter.cs`:
```csharp
// Fixed-size entries for O(1) indexing
void Write(in RenderTreeEdit edit)
{
    _binaryWriter.Write((int)edit.Type);        // 4 bytes
    _binaryWriter.Write(edit.SiblingIndex);     // 4 bytes
    _binaryWriter.Write(edit.ReferenceFrameIndex); // 4 bytes
    WriteString(edit.RemovedAttributeName, allowDeduplication: true);
}
```

Entry sizes (from `OutOfProcessRenderBatch.ts`):
- Updated components: 4 bytes each (int32 pointer)
- Reference frames: 20 bytes each (1 int + 16 bytes type-specific)
- Disposed component IDs: 4 bytes each
- Disposed event handler IDs: 8 bytes each

### 3. Direct DOM Commands (Edit Types)

Blazor uses granular edit types that map directly to DOM operations:

```typescript
export enum EditType {
    prependFrame = 1,      // document.createElement + insertBefore
    removeFrame = 2,       // element.remove()
    setAttribute = 3,      // element.setAttribute()
    removeAttribute = 4,   // element.removeAttribute()
    updateText = 5,        // textNode.textContent = value
    stepIn = 6,            // Navigate into child
    stepOut = 7,           // Navigate to parent
    updateMarkup = 8,      // innerHTML for markup sections
    permutationListEntry = 9,  // For reordering
    permutationListEnd = 10,
}
```

### 4. DOM Operations

From `BrowserRenderer.ts`, Blazor's approach:

```typescript
private insertElement(batch, componentId, parent, childIndex, frames, frame, frameIndex) {
    const tagName = frameReader.elementName(frame)!;
    
    // Direct DOM creation - no HTML parsing
    let newDomElementRaw: Element;
    if (tagName === 'svg' || isSvgElement(parent)) {
        newDomElementRaw = document.createElementNS('http://www.w3.org/2000/svg', tagName);
    } else if (tagName === 'math' || isMathMLElement(parent)) {
        newDomElementRaw = document.createElementNS('http://www.w3.org/1998/Math/MathML', tagName);
    } else {
        newDomElementRaw = document.createElement(tagName);  // Direct API call
    }
    
    // ... apply attributes directly
}
```

**BUT** Blazor also uses innerHTML for markup frames:
```typescript
private insertMarkup(batch, parent, childIndex, markupFrame) {
    const markupContainer = createAndInsertLogicalContainer(parent, childIndex);
    const markupContent = batch.frameReader.markupContent(markupFrame);
    const parsedMarkup = parseMarkup(markupContent, ...);  // Uses innerHTML
}
```

## Abies Architecture (Current)

### 1. JSON Serialization (Major Overhead)

From `Operations.cs`:
```csharp
public static async Task ApplyBatch(List<Patch> patches)
{
    // Step 1: Convert to JSON-serializable format
    var patchDataList = RentPatchDataList();
    foreach (var patch in patches)
    {
        patchDataList.Add(ConvertToPatchData(patch));
    }
    
    // Step 2: JSON serialize (expensive!)
    var json = System.Text.Json.JsonSerializer.Serialize(
        patchDataList, 
        AbiesJsonContext.Default.ListPatchData
    );
    
    // Step 3: Send to JavaScript
    await Interop.ApplyPatches(json);
}
```

### 2. HTML String Rendering

From `Operations.cs`:
```csharp
case AddChild addChild => new PatchData
{
    Type = "AddChild",
    ParentId = addChild.Parent.Id,
    Html = Render.Html(addChild.Child)  // Generates full HTML string
},
```

For every AddChild/ReplaceChild operation:
1. Build HTML string in C# (StringBuilder, HtmlEncode, etc.)
2. Embed HTML string in JSON
3. Serialize JSON to string
4. Transmit string to JavaScript
5. Parse JSON in JavaScript
6. Parse HTML via `parseHtmlFragment()` (innerHTML)

### 3. Patch Types

```csharp
public record PatchData
{
    public string Type { get; init; } = "";  // "AddChild", "RemoveChild", etc.
    public string? ParentId { get; init; }
    public string? TargetId { get; init; }
    public string? ChildId { get; init; }
    public string? Html { get; init; }       // Full HTML for add/replace ops
    public string? AttrName { get; init; }
    public string? AttrValue { get; init; }
    public string? NewId { get; init; }
    public string? Text { get; init; }
    public string? BeforeId { get; init; }
}
```

## Root Cause Analysis

### Why Clear is 1.84x Slower

The clear operation (`09_clear1k`) shows the largest gap because:

1. **Handler Unregistration Overhead**:
   ```csharp
   case ClearChildren clearChildren:
       // Unregister handlers for all children being cleared
       foreach (var child in clearChildren.OldChildren)
       {
           if (child is Element element)
           {
               Runtime.UnregisterHandlers(element);
           }
       }
       break;
   ```
   This iterates through all 1000 children to unregister handlers.

2. **JSON Overhead Scales with Data**:
   - For 1000 rows, even a simple ClearChildren patch still requires JSON serialization
   - The initial 1000-row render (before clear) is slower, affecting clear timings

3. **Re-render After Clear**:
   - Clear benchmarks often involve rendering new content after clearing
   - Abies re-renders via HTML strings + JSON serialization

### Why Run1k/Swap1k are ~1.2-1.3x Slower

1. **JSON serialization overhead**: For 1000 elements, the JSON payload is significant
2. **HTML string generation**: Building HTML in C# adds CPU time
3. **Double parsing**: HTML string → JSON string → JSON.parse → innerHTML → DOM

## Data Flow Comparison

### Blazor (Binary Protocol)

```
.NET creates RenderBatch (binary)
    ↓
Pass pointer to JavaScript
    ↓
JavaScript reads binary directly from WASM heap
    ↓
Apply DOM operations
```

**Total overhead**: Memory allocation for batch + pointer transfer

### Abies (JSON Protocol)

```
.NET creates Patch objects
    ↓
Convert to PatchData records
    ↓
For each AddChild/ReplaceChild: Generate HTML string
    ↓
JSON.Serialize(List<PatchData>)
    ↓
Transfer JSON string to JavaScript
    ↓
JSON.parse()
    ↓
For each patch: Apply (innerHTML for add/replace)
```

**Total overhead**: 
- Object allocation (Patch → PatchData)
- HTML string building
- JSON serialization
- String transfer
- JSON parsing
- HTML parsing (innerHTML)

## Optimization Paths

### Path 1: Shared Memory Binary Protocol (HIGH IMPACT, HIGH EFFORT)

**What**: Implement Blazor-style binary protocol using WASM shared memory.

**Implementation**:
1. Create a binary render batch format in C#
2. Allocate batch in WASM heap
3. Pass pointer to JavaScript
4. Create JavaScript readers for the binary format

**Estimated Impact**: Could close the gap significantly (potentially 40-60% improvement)

**Complexity**: High - requires understanding WASM memory model, implementing binary readers

### Path 2: Reduce JSON Payload Size (MEDIUM IMPACT, LOW EFFORT)

**What**: Optimize the JSON format to reduce payload size and parsing time.

**Options**:
a. Use shorter field names (`T` instead of `Type`, `P` instead of `ParentId`)
b. Use numeric type codes instead of strings
c. Use arrays instead of objects for patch data
d. Compress repeated strings (element IDs, tag names)

**Example**:
```javascript
// Current format (verbose)
{"Type":"AddChild","ParentId":"main_1","Html":"<div id='row_1'>...</div>"}

// Optimized (compact)
[1,"main_1","<div id='row_1'>...</div>"]  // Type 1 = AddChild
```

**Estimated Impact**: 10-20% improvement

**Complexity**: Low - mostly string/array changes

### Path 3: Batch Handler Operations (MEDIUM IMPACT, LOW EFFORT)

**What**: Reduce handler registration/unregistration overhead.

**Current Problem**: For ClearChildren, we iterate all 1000 children to unregister handlers.

**Solutions**:
a. Lazy unregistration (mark as disposed, clean up later)
b. Batch clear handler registry for parent scope
c. Use WeakMap in JavaScript for handler storage

**Estimated Impact**: 5-15% improvement for clear operations

**Complexity**: Low-Medium

### Path 4: Incremental Improvements (LOW IMPACT, LOW EFFORT)

**What**: Small optimizations that compound.

**Options**:
a. Cache element ID lookups in JavaScript
b. Use `requestAnimationFrame` batching for multiple patch calls
c. Pre-allocate patch arrays with expected capacity
d. Use `Span<byte>` for binary portion of data

**Estimated Impact**: 2-5% improvement each

**Complexity**: Low

## Rejected Approaches

### Direct DOM Commands (Without Binary Protocol)

**Status**: ❌ REJECTED (tested 2026-02-10)

**Why Failed**: JSON serialization overhead for createElement/setAttribute/appendChild commands **exceeded** the innerHTML parsing savings. The JSON overhead negated any benefit from avoiding innerHTML.

**Key Learning**: Direct DOM commands only work with a binary protocol (like Blazor's). With JSON serialization, innerHTML is actually faster because:
1. Browser HTML parsers are highly optimized
2. JSON serialization has similar or higher overhead than HTML generation
3. Object allocation for commands exceeds StringBuilder overhead

## Recommendations

### Short-term (Next Sprint)

1. **Profile the critical path**: Use browser DevTools to identify exactly where time is spent
   - Is it diffing? Patch building? JSON serialization? JS parsing? DOM operations?
   
2. **Optimize JSON format**: Implement compact array-based format
   - Expected: 10-15% improvement
   - Effort: 1-2 days

3. **Batch handler operations**: Reduce loop overhead in clear
   - Expected: 5-10% improvement for clear
   - Effort: 0.5-1 day

### Medium-term (Next Quarter)

4. **Investigate binary protocol**: Prototype binary batch format
   - Start with simple operations (ClearChildren, UpdateText)
   - Measure impact before full implementation
   - Effort: 2-3 weeks for prototype

### Long-term (Future)

5. **Full binary protocol**: If prototype shows promise, implement complete binary protocol
   - Similar to Blazor's RenderBatch
   - Effort: 1-2 months

## Conclusion

Blazor's performance advantage comes primarily from its **zero-serialization binary protocol**. Abies' current JSON-based approach has inherent overhead that cannot be eliminated without architectural changes.

The good news:
- Abies is **already competitive** for first paint, bundle size, and memory
- The gaps are addressable with incremental improvements
- A binary protocol could close the remaining performance gap

The path forward is clear: incremental JSON optimizations in the short term, with a binary protocol investigation for significant long-term gains.
