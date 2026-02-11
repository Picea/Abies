---
applyTo: '**'
---

# Agent Memory

This file contains important reminders and learned preferences for the AI assistant.

## Pull Request Guidelines

**See `.github/instructions/pr.instructions.md` for comprehensive PR guidelines.**

Key reminders:
- Always run `dotnet format --verify-no-changes` before submitting
- Follow the PR template at `.github/pull_request_template.md`
- Use Conventional Commits format for PR titles

## Benchmark Suite

The project has benchmark suites in `Abies.Benchmarks/`:
- `DomDiffingBenchmarks.cs` - Virtual DOM diffing performance
- `RenderingBenchmarks.cs` - HTML rendering performance
- `EventHandlerBenchmarks.cs` - Event handler creation performance

## Performance Optimizations Applied

The following Toub-inspired optimizations have been applied:
1. **Atomic counter for CommandIds** - Replaced `Guid.NewGuid().ToString()` with atomic counter
2. **SearchValues fast-path** - Skip HtmlEncode when no special chars present
3. **FrozenDictionary cache** - Cache event attribute names to avoid interpolation
4. **StringBuilder pooling** - Pool StringBuilders for HTML rendering
5. **Index string cache** - Pre-allocate "__index:{n}" strings for keyed diffing

## Known Performance Trade-offs

### Source-Generated JSON (PR #38)
- **What**: Switched to source-generated JSON serialization for .NET 10 WASM trim-safety
- **Trade-off**: Accepted 10-20% regression in event handler creation benchmarks
- **Why**: Required for .NET 10 WASM compatibility - cannot use reflection-based JSON in trimmed builds
- **Impact**: Event handler creation is still fast enough, and WASM bundle size/startup time improvements outweigh this cost

### ❌ REJECTED: Direct DOM Commands (2026-02-10)

**Hypothesis**: Replace innerHTML parsing with direct DOM API calls (createElement, setAttribute, appendChild) to eliminate the ~4.8% parseHtmlFragment overhead observed in profiling.

**Implementation**: Created ElementData/AttributeData/ChildData records in C# that serialize to JSON, with corresponding JavaScript handlers that recursively call createElement() instead of innerHTML.

**Benchmark Results** (Abies v1.0.152):

| Benchmark | HTML Strings (baseline) | Direct DOM | Regression |
|-----------|------------------------|------------|------------|
| 01_run1k | 109.4ms (76.6ms script) | 114.6ms (85.7ms script) | +4.8% total, **+12% script** |
| 09_clear1k | 91.7ms (86.2ms script) | 107.8ms (102.5ms script) | **+17.5% total, +19% script** |

**Root Cause of Failure**:
1. **JSON serialization overhead** - Creating and serializing nested ElementData objects is MORE expensive than serializing HTML strings
2. **Object allocation pressure** - ElementData/AttributeData/ChildData creates more GC pressure than StringBuilder-based HTML rendering
3. **Recursive JS execution** - Recursive createElement() calls are slower than browser's highly optimized innerHTML HTML parser
4. **Clear benchmark especially hurt** - The initial 1000-row render is much slower, so clear (which includes re-creation) suffers most

**Why Blazor's Approach Works**:
- Blazor uses a **binary protocol** (RenderBatch), not JSON
- Blazor writes directly to a binary buffer - no intermediate objects
- The binary data is processed very efficiently in JavaScript
- JSON's text serialization overhead negates the innerHTML parsing savings

**Conclusion**: 
- HTML string rendering via innerHTML is the correct approach for Abies
- Browser HTML parsers are highly optimized and very hard to beat
- Do NOT revisit this approach unless moving to a binary protocol like Blazor's RenderBatch
- The ~4.8% parseHtmlFragment overhead is acceptable and cannot be eliminated with JSON-based approaches

### ✅ APPLIED: WASM Single-Thread Optimization (2026-02-10)

**Hypothesis**: Since WASM is single-threaded, concurrent data structures add unnecessary overhead.

**Implementation**:
- Changed `ConcurrentQueue<T>` → `Stack<T>` for all object pools in `Operations.cs`
- Changed `ConcurrentDictionary<TKey, TValue>` → `Dictionary<TKey, TValue>` for handler registries in `Runtime.cs`
- Removed unused `System.Collections.Concurrent` using directives

**Benchmark Results** (js-framework-benchmark):

| Benchmark | Before (ConcurrentQueue) | After (Stack) | Change |
|-----------|-------------------------|---------------|--------|
| 01_run1k | 105.0 / 73.9 ms | 104.1 / 73.2 ms | **-0.9%** ✅ |
| 05_swap1k | 115.6 / 94.6 ms | 116.2 / 94.7 ms | ~0% (within variance) |
| 09_clear1k | 90.5 / 85.0 ms | 90.3 / 85.0 ms | **-0.2%** ✅ |

**Why Stack<T> over Queue<T>**:
- LIFO pattern is more cache-friendly (recently used items reused first)
- Simpler implementation than Queue<T>'s circular buffer
- Single backing array vs Queue's head/tail pointers

**Key Insight**: The optimization is small (~1%) but the change is correct - we shouldn't pay for thread-safety we don't need. The real win is code simplicity and correctness.

### ❌ REJECTED: PatchType Enum + PatchData Pooling (2026-02-10)

**Hypothesis**: Reduce JSON payload size and allocation overhead by:
1. Using `PatchType : byte` enum instead of string type names (e.g., `"Type":1` vs `"Type":"AddChild"`)
2. Pooling `PatchData` objects to reduce allocations
3. Using `Stack<T>` instead of `ConcurrentQueue<T>` for pools in single-threaded WASM

**Implementation**:
- Created `PatchType` enum with byte-sized values 0-10
- Changed `PatchData` from `record` to `sealed class` with mutable properties
- Added `_patchDataPool` and `_patchDataListPool` for object reuse
- Changed all pool data structures from `ConcurrentQueue<T>` to `Stack<T>`
- Updated JavaScript to use numeric switch cases

**Benchmark Results** (js-framework-benchmark):

| Benchmark | Baseline Total | Baseline Script | With Changes Total | With Changes Script | Δ Total | Δ Script |
|-----------|----------------|-----------------|-------------------|---------------------|---------|----------|
| 01_run1k | 105.0 ms | 73.9 ms | 110.3 ms | 77.8 ms | **+5.0%** | **+5.3%** |
| 05_swap1k | 115.6 ms | 94.6 ms | 120.0 ms | 95.7 ms | **+3.8%** | **+1.2%** |
| 09_clear1k | 90.5 ms | 85.0 ms | 92.9 ms | 87.2 ms | **+2.7%** | **+2.6%** |

**Observations**:
- BenchmarkDotNet micro-benchmarks showed **11-20% improvement** in DOM diffing
- js-framework-benchmark showed **2-5% regression** in real-world scenarios
- The discrepancy suggests micro-benchmarks don't capture real-world overhead

**Root Cause Analysis**:
1. **Pooling overhead in WASM** - Object pooling adds Rent/Return overhead that exceeds allocation savings in single-threaded WASM
2. **Possible JSON enum serialization overhead** - System.Text.Json may have overhead converting enum values
3. **record vs class** - Source-generated serializers may have optimized paths for records

**Stack vs ConcurrentQueue**: Testing showed minimal difference (~1-2%), not the root cause.

**Conclusion**:
- Object pooling does NOT provide benefits in WASM for small, frequently-created objects like PatchData
- WASM's single-threaded nature and different allocation characteristics make pooling counterproductive
- Keep the existing `record PatchData` with string `Type` property
- Do NOT attempt PatchData pooling again - the overhead exceeds the benefit

## Build System Issues & Fixes

### NETSDK1152 - Duplicate abies.js in Publish Output

**Problem**: Projects referencing Abies get duplicate `wwwroot/abies.js` files during `dotnet publish`:
- One from `Abies/wwwroot/abies.js` (via `<Content>` with `CopyToPublishDirectory`)
- One from the consuming project's local copy (via `SyncAbiesJs` target)

**Solution**: Use dual MSBuild target approach in consuming projects:

```xml
<!-- Copy the canonical abies.js before build/publish -->
<Target Name="SyncAbiesJs" BeforeTargets="Build;ComputeFilesToPublish" 
        Inputs="..\Abies\wwwroot\abies.js" 
        Outputs="wwwroot\abies.js">
  <Copy SourceFiles="..\Abies\wwwroot\abies.js" 
        DestinationFiles="wwwroot\abies.js" />
</Target>

<!-- Remove the Abies project's copy from publish to avoid NETSDK1152 -->
<Target Name="RemoveDuplicateAbiesJs" AfterTargets="ComputeFilesToPublish">
  <ItemGroup>
    <!-- Use Identity metadata (full path) to identify and remove Abies project's copy -->
    <ResolvedFileToPublish Remove="@(ResolvedFileToPublish)" 
      Condition="'%(ResolvedFileToPublish.RelativePath)' == 'wwwroot\abies.js' 
                 AND $([System.String]::new('%(ResolvedFileToPublish.Identity)').Contains('\Abies\wwwroot\abies.js'))" />
    <ResolvedFileToPublish Remove="@(ResolvedFileToPublish)" 
      Condition="'%(ResolvedFileToPublish.RelativePath)' == 'wwwroot/abies.js' 
                 AND $([System.String]::new('%(ResolvedFileToPublish.Identity)').Contains('/Abies/wwwroot/abies.js'))" />
  </ItemGroup>
</Target>
```

**Key Insights**:
- Must use `%(Identity)` metadata (full file path) NOT `%(OriginalItemSpec)` (relative path)
- Need both Windows (`\`) and Unix (`/`) path separators for cross-platform compatibility
- `BeforeTargets="Build;ComputeFilesToPublish"` ensures local copy exists before publish resolution
- `AfterTargets="ComputeFilesToPublish"` allows removal BEFORE NETSDK1152 check runs

**Applied to**: Abies.Conduit, Abies.Counter, Abies.Presentation, Abies.SubscriptionsDemo

### dotnet format - Multi-Targeted Solution Issues

**Problem**: Running `dotnet format` on the solution creates merge conflict markers in some files (e.g., `Parser.cs`) due to the multi-targeted nature of the solution (net10.0 and potentially other targets).

**Workaround**: 
- **DO NOT** run `dotnet format` on the entire solution
- Instead, manually format only the files you changed
- If you accidentally run `dotnet format` and it corrupts files, revert with:
  ```bash
  git checkout -- <corrupted-files>
  ```

**Key Insight**: The formatter gets confused by multi-targeting and inserts erroneous merge conflict markers like `<<<<<<< TODO: Unmerged change from project 'Abies(net10.0)'`.

## js-framework-benchmark (Official Performance Testing)

### Setup

The [js-framework-benchmark](https://github.com/nicknash/js-framework-benchmark) is the standard benchmark for comparing frontend framework performance.

**Clone the fork alongside Abies** (same parent directory):
```bash
# From Abies parent directory
cd ..
git clone https://github.com/nicknash/js-framework-benchmark.git js-framework-benchmark-fork
```

Expected structure:
```
parent-directory/
├── Abies/                          # This repository
└── js-framework-benchmark-fork/    # Benchmark fork
    └── frameworks/keyed/abies/     # Abies framework entry
        ├── src/                    # Source (references Abies project)
        └── bundled-dist/           # Published WASM output
```

### Building Abies for Benchmark

**IMPORTANT**: Always do a clean rebuild when testing code changes!

```bash
cd ../js-framework-benchmark-fork/frameworks/keyed/abies/src

# Clean rebuild
rm -rf bin obj
dotnet publish -c Release

# Copy to bundled-dist
rm -rf ../bundled-dist/*
cp -R bin/Release/net10.0/publish/wwwroot/* ../bundled-dist/
```

### Running the Benchmark

1. **Install dependencies** (first time only):
```bash
cd ../js-framework-benchmark-fork
npm ci
cd webdriver-ts
npm ci
```

2. **Start the benchmark server**:
```bash
cd ../js-framework-benchmark-fork
npm run start
# Server runs on http://localhost:8080
```

3. **Run specific benchmarks** (in a new terminal):
```bash
cd ../js-framework-benchmark-fork/webdriver-ts

# Run swap rows benchmark (05_swap1k)
npm run selenium -- --headless --framework abies-v1.0.151-keyed --benchmark 05_swap1k

# Run all benchmarks for Abies
npm run selenium -- --headless --framework abies-v1.0.151-keyed

# Common benchmarks:
# - 01_run1k: Create 1000 rows
# - 02_replace1k: Replace all 1000 rows
# - 03_update10th1k: Update every 10th row
# - 04_select1k: Select a row
# - 05_swap1k: Swap two rows (LIS optimization target)
# - 06_remove1k: Remove a row
# - 07_create10k: Create 10,000 rows
```

4. **View results**:
```bash
# Results saved to:
ls webdriver-ts/results/abies-v1.0.151-keyed_*.json

# View specific result:
cat webdriver-ts/results/abies-v1.0.151-keyed_05_swap1k.json | jq .
```

### Comparison Frameworks
For reference, compare against:
- `vanillajs-keyed` - Baseline (raw DOM manipulation)
- `blazor-wasm-keyed` - .NET Blazor WASM (similar tech stack)

### Latest Benchmark Results (2026-02-11)

**Abies v1.0.152 vs Blazor WASM v10.0.0:**

| Benchmark | Abies | Blazor | Ratio |
|-----------|-------|--------|-------|
| 01_run1k | 107.1ms | 88.5ms | 1.21x |
| 05_swap1k | 124.8ms | 95.2ms | 1.31x |
| 09_clear1k | 85.1ms | 46.2ms | 1.84x |
| **First Paint** | **74.2ms** | **75ms** | **0.99x ✅** |
| Size (compressed) | 1,225 KB | 1,377 KB | 0.89x ✅ |
| Ready Memory | 34.3 MB | 41.1 MB | 0.83x ✅ |

### 🎉 Binary Batching Protocol Implementation (2026-02-11)

**Branch**: `perf/binary-render-batch`

Implemented Blazor-inspired binary batching protocol to eliminate JSON serialization overhead.

**Results with Binary Batching:**

| Benchmark | JSON (baseline) | Binary | Blazor | Binary vs JSON | Binary vs Blazor |
|-----------|----------------|--------|--------|----------------|------------------|
| 01_run1k | 107ms | **89.1ms** | 88.3ms | **17% faster** | **1.01x** ✅ |
| 05_swap1k | 124.8ms | **120.3ms** | 93.9ms | 4% faster | 1.28x |
| Script time (run1k) | 75ms | **57.8ms** | 57.3ms | **23% faster** | **1.01x** ✅ |

**Key Achievement**: Create 1000 rows is now **matching Blazor performance** (89.1ms vs 88.3ms)!

**Implementation Details**:
- `RenderBatchWriter.cs`: Binary batch writer with LEB128 string encoding and string table deduplication
- `JSType.MemoryView` with `Span<byte>` for zero-copy memory transfer
- JavaScript binary reader using `DataView` API
- Binary batching is always enabled; JSON batching has been removed

**Binary Format**:
```
Header (8 bytes):
  - PatchCount: int32 (4 bytes)
  - StringTableOffset: int32 (4 bytes)

Patch Entries (16 bytes each):
  - Type: int32 (4 bytes) - BinaryPatchType enum value
  - Field1: int32 (4 bytes) - string table index (-1 = null)
  - Field2: int32 (4 bytes) - string table index (-1 = null)
  - Field3: int32 (4 bytes) - string table index (-1 = null)

String Table:
  - LEB128 length prefix + UTF8 bytes for each string
  - String deduplication via Dictionary lookup
```

**JSType.MemoryView Lesson Learned**:
- JavaScript receives a `Span` wrapper object, NOT a raw `Uint8Array`
- Must call `span.slice()` to get a `Uint8Array` copy of the data
- The wrapper has methods: `slice()`, `copyTo()`, `set()`, `length`, `byteLength`

### 🔬 Deep Investigation: Why Blazor is Faster (2026-02-11)

**See `docs/investigations/blazor-performance-analysis.md` for full analysis.**

**Root Cause**: Blazor uses a **zero-copy binary protocol** via shared WASM memory.

**Key Architectural Differences**:

1. **Blazor: SharedMemoryRenderBatch**
   - Passes a raw memory pointer to JavaScript
   - JavaScript reads binary data directly from .NET WASM heap
   - No serialization whatsoever
   - Fixed-size entries enable O(1) indexing

2. **Abies: Binary Batching Protocol** (Implemented 2026-02-11)
   - Binary batch format written directly to pooled buffers
   - `JSType.MemoryView` transfers data to JS without copying
   - JavaScript reads binary data using `DataView` API
   - LEB128-encoded string table with deduplication
   - ~17% faster than previous JSON approach

> **Note**: The JSON protocol described here was removed in the binary batching implementation.
> See `RenderBatchWriter.cs` and `abies.js` for the current binary implementation.

**The Math** (for 1000-row operations):
- Blazor: Single pointer transfer → JS reads binary directly
- Abies: Binary batch → memory view transfer → JS reads binary directly

**Optimization Paths Completed**:
1. ~~**Short-term**: Optimize JSON format (compact arrays, shorter keys) - 10-20% potential~~ Skipped
2. ✅ **Medium-term**: Binary protocol implementation - **17% improvement achieved**
3. **Future**: Consider SharedMemoryRenderBatch pattern for even more efficiency
2. **Medium-term**: Binary protocol prototype - 40-60% potential
3. **Long-term**: Full SharedMemoryRenderBatch implementation

**Why Direct DOM Commands Failed** (earlier investigation):
- JSON serialization overhead for createElement/setAttribute **exceeded** innerHTML parsing savings
- Browser HTML parsers are highly optimized
- Direct DOM commands only help with binary protocols (like Blazor's)

**🎉 Clear Fast Path Optimization (2026-02-11):**
- **Clear benchmark**: 90.4ms → 85.1ms (**5.9% faster**)
- Added O(1) early exit when clearing all children (`newLength == 0`)
- Added O(n) early exit when adding all children (`oldLength == 0`)
- Avoids building expensive dictionaries for keyed diffing
- Removed dead code (redundant ClearChildren check)

**🎉 First Paint Fix (2026-02-10):**
- **First Paint**: 4,843ms → 74.2ms (**65x faster**)
- Now **faster** than Blazor (74.2ms vs 75ms)
- Root cause: Empty `<body>` vs Blazor's `<app>Loading...</app>` placeholder

**🎉 LIS Algorithm Fix (2026-02-09):**
- **Swap benchmark**: 326.6ms → 121.6ms (**2.7x faster**)
- Now only **1.31x** slower than Blazor (was 3.47x)
- Root cause: `ComputeLISInto` had bug where `k=0` tracking never incremented for first element

**Allocation Optimization (2026-02-09):**
Applied the following optimizations to reduce GC pressure:
1. `ComputeLISInto` - Uses ArrayPool for `result` and `p` arrays instead of allocating new arrays
2. `inLIS` bool array - Replaced `HashSet<int>` with `ArrayPool<bool>.Shared` for LIS membership
3. `PatchDataList` pooling - Added `_patchDataListPool` to reuse List<PatchData> in ApplyBatch

**Performance Priority List (Updated 2026-02-12):**

Based on extensive trace analysis and VDOM optimization research. Current state:
- **Create (01_run1k)**: 91.6ms total, 60.3ms script ✅ **MATCHES BLAZOR** (1.00x)
- **Swap (05_swap1k)**: 121.7ms total, 99.1ms script ⚠️ **GAP: 1.95x script time** (vs Blazor 50.8ms)
- **Replace (02_replace1k)**: 115.4ms total, 84.8ms script (1.18x vs Blazor)

**✅ P0 IMPLEMENTED: Head/Tail Skip for Keyed Diffing (2026-02-12)**
- **Implementation**: Added three-phase diff to `DiffChildrenCore`:
  1. Skip matching head (common prefix)
  2. Skip matching tail (common suffix)
  3. Only build key maps and run LIS on the middle section
- **Result**: No measurable improvement for swap benchmark
  - Swap only has 2 matching elements (positions 0 and 999), so 998 still need LIS
- **Benefit**: Prepares codebase for append-only fast path (chat, logs, feeds)
- **Code**: Added ~100 lines to `Operations.cs` in `DiffChildrenCore`

**P1 (MEDIUM - Requires Investigation): String Key Hashing Overhead**
- **Problem**: Using `Dictionary<string, int>` requires hashing 998 string keys
- **Hypothesis**: Int-based keys or array-based lookup would be faster
- **Action**: Profile dictionary operations vs array lookup for keyed diffing
- **Expected Impact**: 10-30% reduction in reorder scenarios

**P2 (LOW - Research Required): Blazor Diff Architecture Analysis**
- **Question**: Why is Blazor's swap 2x faster when both use LIS + binary protocol?
- **Action**: Examine Blazor's RenderTreeDiff.cs source code
- **Hypothesis**: Blazor may use int sequence numbers instead of string keys

**P3 (FUTURE - After P1-P2): Additional Optimizations from Research**
- **Memo Identity Bailout**: If `oldNode === newNode` (reference equality), skip diff entirely
  - Already have Memo nodes but need to verify they skip at DiffInternal level
- **Type Mismatch → Replace**: Different element tag = replace, don't morph
  - Check if this is already implemented
- **Append-Only Fast Path**: Head/tail skip now enables O(1) append detection
  - When `headSkip == oldLength`, all new items are appends

**Size Comparison:**
- Abies compressed: 1,225 KB (vs Blazor 1,377 KB - **11% smaller** ✅)
- Abies uncompressed: 3,938 KB
- First paint: 74.2ms ✅ (was 4,811ms before placeholder fix)

### Benchmark Command Reference

```bash
# Run full benchmark suite for Abies
cd /path/to/js-framework-benchmark-fork
npm run bench -- --headless keyed/abies

# Run specific benchmarks only
npm run bench -- --headless keyed/abies --benchmark 05_swap1k

# Run Blazor for comparison
npm run bench -- --headless keyed/blazor-wasm
```

## ✅ FIXED: LIS Algorithm Bug (2026-02-09)

### Problem (was)

The `ComputeLISInto` function in `Operations.cs` had a **critical bug** that caused it to compute an LIS of length 1 instead of 998 for the swap benchmark.

**Impact**: For a simple swap of 2 elements in 1000, the algorithm produced **999 MoveChild patches** instead of **2**.

### Solution Applied

Changed the algorithm from tracking `k` (length-1) to `lisLen` (actual length):

```csharp
// BEFORE (buggy):
var k = 0; // Length of longest LIS found - 1
if (k > 0 && arr[result[k]] < arrI) { ... }

// AFTER (fixed):
var lisLen = 0; // Actual length of LIS found
int lo = 0, hi = lisLen;
// Binary search in [0, lisLen)
...
result[lo] = i;
if (lo == lisLen) lisLen++;
```

### Benchmark Results

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Swap median | 326.6ms | 121.6ms | **2.7x faster** |
| Script time | ~300ms | 96.7ms | **3x faster** |
| DOM Moves | 999 | 2 | **99.8% reduction** |

### Root Cause

The bug is in the initial state handling. When `k = 0`:
1. First element: `result[0] = 0`, k stays 0 (should be 1)
2. Second element (998): Binary search with hi=0, `result[0] = 1` (overwrites!)
3. Third element (2): `arr[result[0]]=998 >= 2`, so `result[0] = 2` (overwrites again!)
4. ...continues overwriting result[0] forever...

The LIS only contains position 999 (the last element), giving length 1.

### Fix Required

Change the initial tracking from `k = 0` (treating as length-1) to `lisLen = 0` (actual length), and fix the loop logic:

```csharp
// BEFORE (buggy):
var k = 0; // Length of longest LIS found - 1
...
if (k > 0 && arr[result[k]] < arrI)

// AFTER (fixed):
var lisLen = 0; // Actual length of LIS found
...
int lo = 0, hi = lisLen;
// Binary search in [0, lisLen)
...
result[lo] = i;
if (lo == lisLen) lisLen++;
```

### Test Case

```csharp
// Swap: [0, 998, 2, 3, ..., 997, 1, 999]
// Expected LIS: [0, 2, 3, ..., 997, 999] (length 998)
// Expected moves: 2 (positions 1 and 998)
// Actual with bug: LIS length 1, moves 999
```

### Expected Performance Improvement

Fixing this bug should:
- **Swap benchmark**: 326ms → ~100ms (3x faster, matching Blazor)
- **Clear benchmark**: No change (doesn't use LIS)

## Profile Analysis (2026-02-09)

### Abies vs Blazor Comparison

| Metric | Abies Swap | Blazor Swap | Difference |
|--------|------------|-------------|------------|
| Total | 607ms | 294ms | 2.1x slower |
| GC | 9.4% | 11% | Similar |
| wasm-function | 86.5ms (14.2%) | 29.3ms (10%) | 3x slower |
| insertBefore | 18.9ms (3.1%) | 0.8ms (0.3%) | **24x slower** |
| parseHtmlFragment | 4.8% | NOT present | Abies-only |

### Key Architectural Differences

1. **Blazor uses direct DOM commands** - no innerHTML parsing
2. **Abies uses HTML string rendering** - generates HTML, then parses it
3. **insertBefore 24x slower** - due to 999 MoveChild patches (bug!)

### Optimization Priority

1. **Fix LIS bug** (HIGH) - 3x swap improvement expected
2. **Direct DOM commands** (MEDIUM) - eliminate parseHtmlFragment (4.8%)
3. **JSON serialization** (LOW) - after other fixes

## ✅ FIXED: First Paint Performance (2026-02-10)

### Problem

Abies had a **64x slower first paint** than Blazor (4,843ms vs 75ms). Investigation revealed this was because:
- Blazor's index.html has `<app>Loading...</app>` placeholder that renders immediately
- Abies had an empty `<body>` tag - nothing rendered until WASM fully loaded

### Solution

Added a loading placeholder to the js-framework-benchmark's Abies index.html that matches the expected DOM structure:

```html
<body>
    <div id="main">
        <div class="container">
            <div class="jumbotron">
                <div class="row">
                    <div class="col-md-6">
                        <h1>Abies keyed</h1>
                    </div>
                    <div class="col-md-6">
                        <div class="row">Loading...</div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</body>
```

### Results

| Metric | Before (no placeholder) | After (with placeholder) | Blazor |
|--------|------------------------|-------------------------|--------|
| **First Paint** | **4,843ms** | **74.2ms** | **75ms** |

✅ First paint is now **65x faster** and actually **slightly faster** than Blazor!

### Key Insight

First Paint (FP) measures when the browser first renders *anything* visible. Both Blazor and Abies take similar time (~4-5 seconds) to become fully interactive (load WASM, initialize, render app). The difference was that Blazor shows "Loading..." text immediately while Abies showed nothing.

This is a **UX improvement**, not a "cheat" - Blazor does exactly the same thing. The user sees feedback immediately rather than staring at a blank screen.

### File Location

The fix was applied to:
```
js-framework-benchmark-fork/frameworks/keyed/abies/src/wwwroot/index.html
```

After publishing, this gets copied to:
```
js-framework-benchmark-fork/frameworks/keyed/abies/bundled-dist/wwwroot/index.html
```
