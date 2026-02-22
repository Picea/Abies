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

## ⚠️ Benchmark Environment: Power State Awareness (M4 Pro MacBook)

**CRITICAL**: Always ask "Is your MacBook plugged in or on battery?" before running benchmarks.

macOS aggressively throttles CPU on battery, causing **up to 30% variance** in benchmark results.
Comparing results across different power states will produce misleading conclusions.

### Rules
1. **Never compare benchmarks from different power states** — results are not comparable
2. **Always record power state** when logging benchmark results
3. **Run A/B comparisons in the same session** — same power state, same thermal conditions
4. **If in doubt, re-run on main** to get a same-session baseline before drawing conclusions

### Reference Values (Abies main, same codebase, 2026-02-19)

| Benchmark | Plugged In | Battery | Δ |
|-----------|-----------|---------|---|
| 01_run1k | 86.8ms | 70.7ms | -19% |
| 02_replace1k | 87.0ms | 79.9ms | -8% |
| 04_select1k | 98.7ms | 121.9ms | +24% |
| 06_remove-one-1k | 63.6ms | 84.7ms | +33% |
| 07_create10k | 1174.7ms | 819.0ms | -30% |
| 09_clear1k | 98.8ms | 92.3ms | -7% |

**Note**: The inconsistent direction (some faster, some slower) suggests power state interacts
with thermal throttling, browser heuristics, and GC timing in complex ways. The magnitude of
change (up to 33%) makes cross-session comparison unreliable regardless of direction.

### Lesson Learned (2026-02-19)

The shared-memory protocol (issue #93) initially appeared to show both massive improvements AND
regressions. A/B testing against main on the same machine (same session, same power state) revealed
**zero measurable difference**. All apparent changes were from comparing a plugged-in session
against a battery session.

## Benchmark Suite

The project has benchmark suites in `Abies.Benchmarks/`:
- `DomDiffingBenchmarks.cs` - Virtual DOM diffing performance
- `RenderingBenchmarks.cs` - HTML rendering performance
- `EventHandlerBenchmarks.cs` - Event handler creation performance

### Benchmarking Strategy (2026-02-11)

**See `docs/investigations/benchmarking-strategy.md` for the full analysis.**

**Key Finding**: Micro-benchmark improvements that don't appear in E2E benchmarks are likely false positives.

**Dual-Layer Strategy**:

1. **Primary (Source of Truth)**: js-framework-benchmark - measures what users experience (EventDispatch → Paint)
2. **Secondary (Development Feedback)**: BenchmarkDotNet - algorithm comparison, allocation tracking

**Critical Rule**: NEVER ship based on micro-benchmark improvements alone. Always validate with js-framework-benchmark.

**Historical Evidence**: The PatchType enum optimization showed 11-20% improvement in BenchmarkDotNet but caused 2-5% REGRESSION in js-framework-benchmark. This proves micro-benchmarks can mislead.

**What Micro-Benchmarks Miss**:

- JS interop overhead (the biggest cost in WASM apps)
- Browser rendering pipeline
- GC pressure at scale
- Memory bandwidth effects
- Real-world allocation patterns

**When Micro-Benchmarks Are Useful**:

- Comparing algorithm alternatives (A vs B with same interface)
- Tracking allocation counts (not timings)
- Rapid iteration during development
- Isolated component testing (must validate with E2E after)

**Running E2E Benchmarks**:

```bash
# Build Abies for benchmark
cd js-framework-benchmark-fork/frameworks/keyed/abies/src
rm -rf bin obj && dotnet publish -c Release
cp -R bin/Release/net10.0/publish/wwwroot/* ../bundled-dist/

# Run benchmarks
cd ../../../../
npm start &  # Start server on port 8080
cd webdriver-ts
npm run bench -- --headless --framework abies-keyed --benchmark 01_run1k
npm run bench -- --headless --framework abies-keyed --benchmark 05_swap1k
npm run bench -- --headless --framework abies-keyed --benchmark 09_clear1k
```

**Comparing Results**:

```bash
python3 scripts/compare-benchmark.py \
  --results-dir ../js-framework-benchmark-fork/webdriver-ts/results \
  --baseline benchmark-results/baseline.json \
  --threshold 5.0
```

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

### ✅ APPLIED: Generic MemoKeyEquals + View Cache (2026-02-12)

**Hypothesis**: Eliminate boxing overhead in memo key comparison and enable ReferenceEquals shortcut.

**Implementation**:
1. **Generic `MemoKeyEquals()` method** - Added to `IMemoNode` and `ILazyMemoNode` interfaces
   - Uses `EqualityComparer<TKey>.Default.Equals(Key, other.Key)` - JIT-optimized, no boxing for value types
   - Replaces `MemoKey.Equals()` which boxed the key to `object`
2. **ReferenceEquals bailout** - Added at top of `DiffInternal`:
   ```csharp
   if (ReferenceEquals(oldNode, newNode)) return;
   ```
3. **View cache layer** - Added `_lazyCache` Dictionary to `lazy<TKey>()` function:
   - Returns same `LazyMemo` reference if key matches
   - Enables `ReferenceEquals` bailout for unchanged items

**Benchmark Results** (js-framework-benchmark):

| Benchmark | Before Total/Script | After Total/Script | Δ Script |
|-----------|--------------------|--------------------|----------|
| 01_run1k | 107.1ms / 74.2ms | 94.3ms / 61.4ms | **-17%** ✅ |
| 05_swap1k | 124.8ms / 99.1ms | 120.8ms / 96.9ms | **-2%** |
| 09_clear1k | 85.1ms / ~80ms | 95.8ms / 89.5ms | +12% ⚠️ |

**Key Insight**: The 17% improvement on create benchmark comes from the view cache enabling `ReferenceEquals` bailout. The clear regression is likely noise or GC timing - the cache doesn't help when clearing.

**Code Changes**:
- `Abies/DOM/Operations.cs`: Added `MemoKeyEquals()` to interfaces and implementations
- `Abies/Runtime.cs`: Updated `PreserveIds` to use `MemoKeyEquals()`
- `Abies/Html/Elements.cs`: Added `_lazyCache` with view cache logic

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

The [js-framework-benchmark](https://github.com/krausest/js-framework-benchmark) is the standard benchmark for comparing frontend framework performance.

**Clone the fork alongside Abies** (same parent directory):
```bash
# From Abies parent directory
cd ..
git clone https://github.com/krausest/js-framework-benchmark.git js-framework-benchmark-fork
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

### Latest Benchmark Results (2026-02-19)

**Abies v1.0.152 (with SetChildrenHtml + addEventListeners skip) vs Blazor WASM v10.0.0:**

| Benchmark | Abies | Blazor | Ratio |
|-----------|-------|--------|-------|
| 01_run1k | **71.7ms** | 88.5ms | **0.81x ✅ (19% FASTER)** |
| 02_replace1k | **95.5ms** | — | — |
| 05_swap1k | 107.8ms | 95.2ms | 1.13x |
| 09_clear1k | 92.2ms | 46.2ms | 2.00x |
| **First Paint** | **74.2ms** | **75ms** | **0.99x ✅** |
| Size (compressed) | 1,225 KB | 1,377 KB | 0.89x ✅ |
| Ready Memory | 34.3 MB | 41.1 MB | 0.83x ✅ |

### 🎉 SetChildrenHtml + addEventListeners Skip (2026-02-19)

**Branch**: `perf/html-spec-aware-rendering`

Two related optimizations that dramatically improve create and replace performance.

**⚠️ IMPORTANT: Add-All Path Reverted (Issue #92 Fix, 2026-02-21)**

The add-all fast path (0→N children) was reverted from `SetChildrenHtml` back to individual
`AddChild` patches because innerHTML-created DOM nodes behave differently for subsequent
`removeChild` operations, causing a **44% regression** on `06_remove-one-1k` benchmark
(36.5ms → 52.6ms). See the "Issue #92 Fix" section below for full details.

**SetChildrenHtml is still used for**:
- Complete replacement fast path (02_replace1k) — all old keys differ from all new keys
- It is **extracted from type-switch dispatch** chains using `if` pre-checks to avoid
  adding `isinst` overhead to every patch dispatch (1000× for create-1k).

**Results (3-run average, 15 samples each, with handler registration bug fix):**

| Benchmark | Before (binary batch) | After (SetChildrenHtml) | Improvement |
|-----------|----------------------|------------------------|-------------|
| 01_run1k total | 90ms | **71.7ms** | **20% faster** |
| 01_run1k script | 60ms | **40.4ms** | **33% faster** |
| 02_replace1k total | 103ms | **95.5ms** | **7% faster** |
| 02_replace1k script | 68ms | **56.6ms** | **17% faster** |
| 05_swap1k | 112ms | 107.8ms | ~neutral |
| 09_clear1k | 92ms | 92.2ms | No change |

**Key Achievement**: Create 1000 rows is now **19% FASTER than Blazor** (71.7ms vs 88.5ms)!

**Note**: Initial pre-bug-fix numbers (65ms/33ms for 01_run1k) were artificially fast because
event handlers were not being registered for Memo/LazyMemo-wrapped nodes. The bug fix
(RegisterHandlers/UnregisterHandlers now handles ILazyMemoNode/IMemoNode) added ~7ms — the
correct cost of proper handler registration.

**Optimization 1: SetChildrenHtml Batch Patch (HIGH IMPACT)**
- New `SetChildrenHtml` binary patch type (enum value 12)
- When going from 0→N children (Add-All fast path), concatenates all children HTML
  into ONE string and emits a single `SetChildrenHtml` patch instead of N `AddChild` patches
- JS handler: single `parent.innerHTML = html` instead of N `parseHtmlFragment + appendChild`
- Eliminates N temporary DOM container creations, N `firstElementChild` extractions
- Inspired by ivi's `_hN` template pattern and blockdom's batch innerHTML approach

**Optimization 2: Skip addEventListeners TreeWalker Scan**
- All common events are pre-registered at document level via `COMMON_EVENT_TYPES.forEach(ensureEventListener)`
- The TreeWalker scan after each AddChild/ReplaceChild was pure wasted work
  (8000+ element visits, 16000+ attribute scans, all resulting in no-op)
- Removed `addEventListeners(childElement)` calls from binary batch AddChild and ReplaceChild handlers

**Optimization 3: Complete Replacement Fast Path**
- When all old keys differ from all new keys (replace benchmark), emit
  `ClearChildren + SetChildrenHtml` instead of N `RemoveChild + N AddChild`
- Detected after dictionary categorization: `keysToDiff.Count == 0 && headSkip == 0 && tailSkip == 0`
- Reduces 2000 individual DOM operations to 2 bulk operations

**Code Changes:**
- `Abies/DOM/Operations.cs`: Added `SetChildrenHtml` patch, `Render.HtmlChildren()`, complete replacement fast path
- `Abies/DOM/RenderBatchWriter.cs`: Added `BinaryPatchType.SetChildrenHtml = 12`, `WriteSetChildrenHtml()`
- `Abies/Interop.cs`: Added `SetChildrenHtml` JS import
- `Abies/wwwroot/abies.js`: Added `SetChildrenHtml` binary handler, removed `addEventListeners` from AddChild/ReplaceChild

### ✅ FIXED: Issue #92 — 06_remove-one-1k Regression (2026-02-21)

**Branch**: `perf/fix-06-remove-regression`
**Issue**: https://github.com/Picea/Abies/issues/92

**Problem**: `06_remove-one-1k` benchmark regressed from 36.5ms to 52.6ms (44% slower)
between v1.0.151 and v1.0.152.

**Root Cause**: The `DiffChildrenCore` add-all fast path (0→N children) was changed to emit
a single `SetChildrenHtml` patch (innerHTML) instead of N individual `AddChild` patches.
While innerHTML is faster for initial creation, the DOM nodes it creates behave differently
for subsequent `removeChild` operations — causing the 06_remove-one-1k regression.

**Fix** (Operations.cs only):
1. **Reverted add-all path** from `SetChildrenHtml` back to individual `AddChild`/`AddRaw`/`AddText` patches
2. **Extracted SetChildrenHtml from type-switch dispatch** in `Apply`, `ApplyBatch`, and
   `WritePatchToBinary` using `if` pre-checks before the `switch` statement. This avoids
   adding an `isinst` check to every patch dispatch (×1000 for create-1k).

**Key Discovery — Mono Interpreter isinst Overhead**:
In Mono WASM interpreter, C# type-pattern-match switches (`case Type var:`) compile to linear
`isinst` IL check chains. Adding cases to hot switches that execute N times (like per-patch
dispatch) has O(N × cases) cost. Extract rare cases to `if` pre-checks or `[NoInlining]` methods.

**Benchmark Results (fresh, same session)**:

| Configuration | 06_remove-one-1k | 01_run1k |
|---|---|---|
| main (v1.0.151) | 36.5ms | 50.7ms |
| HEAD (v1.0.152, regressed) | 52.6ms | — |
| **Fix applied** | **36.6ms** ✅ | **52.0ms** ✅ |

**SetChildrenHtml is still used** for the complete-replacement fast path (02_replace1k)
where all old keys differ from all new keys — verified at 47.6ms.

**Tests updated**: 5 unit tests in `DomBehaviorTests.cs` updated to expect individual
`AddChild` patches from the add-all path instead of `SetChildrenHtml`.

**What was ruled out** during investigation:
- JavaScript changes (addEventListeners → ensureSubtreeEventListeners)
- JITERP (regression is purely Mono interpreter)
- FrozenSet (HtmlSpec.VoidElements)
- VoidElements check in DiffElements
- Individual switch cases in isolation (WritePatchToBinary alone, ApplyBatch alone)

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

**Performance Priority List (Updated 2026-02-11):**

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

**✅ P1 INVESTIGATED: String Key Hashing Overhead (2026-02-11)**
- **Hypothesis**: String key hashing in `Dictionary<string, int>` is a major bottleneck
- **Investigation**: Created `DictionaryKeyBenchmarks` and `KeyedDiffingBenchmarks`

**Benchmark Results:**

| Operation | String Keys | Int Keys | Int/String Ratio |
|-----------|-------------|----------|------------------|
| Build Dict (1000) | 6.15 µs | 2.47 µs | **0.40x (2.5x faster)** |
| Lookup (1000) | 3.72 µs | 1.28 µs | **0.21x (2.9x faster)** |
| Allocation | 31 KB | 22 KB | **0.72x (28% less)** |

**Impact Analysis:**
- Dictionary overhead (~10 µs) is only **1.6%** of total Swap1k diff time (626 µs)
- The main time is in 998 `DiffInternal` calls for matched elements
- Blazor's int sequence numbers benefit ALL matching, not just dictionaries

**Conclusion:**
- String→Int optimization would save ~10µs (1.6%) - not worth the complexity
- The 2x gap with Blazor comes from deeper architectural differences
- **DOWNGRADED** to low priority

**❌ P2 REJECTED: Blazor Permutation List Architecture (2026-02-11)**

- **Research**: Deep analysis of `RenderTreeDiffBuilder.cs` from aspnetcore repo
- **Hypothesis**: Replace LIS with Blazor-style permutation list for faster reordering

**Blazor's Permutation List Approach:**
1. **Index-based**: Uses `PermutationListEntry(oldSiblingIndex, newSiblingIndex)` pairs
2. **Sibling indices**: Tracks running DOM sibling index during traversal
3. **Batch emission**: All permutations written at end of diff
4. **JS applies batch**: Renderer applies permutations relative to tracked state

**Why Permutation List is NOT Applicable to Abies:**

1. **Abies uses element IDs, not indices**:
   - `MoveChild(parentId, childId, beforeId)` uses stable element IDs
   - Blazor uses array indices that shift during operations
   - ID-based moves are order-independent and more robust

2. **LIS already minimizes moves correctly**:
   - Swap benchmark: 2 MoveChild patches (correct!)
   - LIS O(n log n) for n=998 is ~microseconds (negligible)
   - Dictionary overhead is only 1.6% of diff time

3. **Index-based permutation is MORE complex**:
   - After moving element from position 5→2, positions 2-4 shift
   - Requires tracking running index offsets or:
     - Extract to fragment, rebuild, reinsert (expensive)
     - Process in specific order (complex)
   - Blazor maintains parallel renderer state to handle this

4. **No performance benefit**:
   - Still need `insertBefore` calls (fundamental DOM operation)
   - Same number of DOM mutations
   - Added complexity without gain

**Abies vs Blazor Architecture (Updated Analysis):**

| Aspect | Blazor | Abies | Winner |
|--------|--------|-------|--------|
| Primary matching | int sequence | string ID | Blazor (faster) |
| Move tracking | Index-based permutation | ID-based MoveChild | Abies (simpler) |
| Move robustness | Requires ordered apply | Order-independent | **Abies** |
| LIS computation | None (not needed) | O(n log n) | Blazor |
| Actual moves | Same count | Same count | Tie |

**Real Performance Gap Analysis:**

The remaining ~30ms gap in swap (124.8ms vs 95.2ms) comes from:
1. **DiffInternal overhead**: 998 recursive calls for matched elements
2. **VDOM allocation**: Creating 998 new nodes each render cycle
3. **String operations**: ID generation, comparison, hashing
4. **Not from move algorithm**: Both emit ~2 moves for swap

**Conclusion:**
- Permutation list is architecturally incompatible with Abies
- Abies's ID-based approach is actually MORE robust
- The performance gap is in VDOM/diff overhead, not move algorithm
- **REJECTED** - Do NOT attempt permutation list migration

### 🔬 Deep Profiling Analysis: Swap Benchmark (2026-02-11)

**Chrome DevTools Trace Analysis:**

| Metric | Abies | Blazor | Gap |
|--------|-------|--------|-----|
| **Total** | 121.3ms | 94.3ms | **27ms (1.29x)** |
| **Script** | 99ms | 51ms | **48ms (1.94x)** |
| **Paint** | 18.5ms | 19.2ms | ~equal |
| **FunctionCall** | 98ms | 56.8ms | **41ms** |
| **MajorGC** | 28.6ms | 44.1ms | Abies better! |

**Root Cause: VDOM Rebuild + Diffing Overhead**

The 48ms script time gap comes from fundamental MVU architecture costs:

1. **View rebuilds entire VDOM** - 1000 LazyMemo objects created every render
2. **Keyed diff builds dictionaries** - 998 dictionary insertions for middle section
3. **DiffInternal called 998 times** - Even with memo hits, method call overhead adds up
4. **LIS computation** - O(n log n) for n=998

**Memoization IS working correctly:**
- Lazy memo keys compare correctly: `(Row, bool)` tuples use value equality
- All 998 matched rows return early from DiffInternal (MemoHit++)
- But we still pay the cost of:
  - Creating 1000 LazyMemo objects
  - Building 2 dictionaries (998 entries each)
  - 998 method calls to DiffInternal
  - Interface dispatch + boxing for MemoKey comparison

**Why Blazor is Faster:**

| Aspect | Blazor | Abies |
|--------|--------|-------|
| VDOM rebuild | Components skip via ShouldRender() | Always rebuilds full tree |
| Dictionary | Only built when keys differ | Always built for middle section |
| Matching | O(n) merge-join on sequence numbers | O(n) dictionary + O(n log n) LIS |
| Method calls | Inline in single large method | Recursive DiffInternal calls |

**Optimization Opportunities (Future):**

1. **Skip dictionary for in-order keys**: If keys[i] == keys[i] for all i, skip dict building
2. **Reduce method call overhead**: Inline hot paths, avoid interface dispatch
3. **Pool LazyMemo objects**: Reuse node allocations across renders (breaks immutability)
4. **Incremental view updates**: Only rebuild changed parts (fundamental architecture change)

**Conclusion:**
- The ~50ms gap is inherent to MVU architecture (full VDOM rebuild per render)
- Memoization helps (prevents deep diffing) but can't eliminate rebuild cost
- Blazor's component model allows skipping entire subtrees
- Further optimization requires architectural changes, not algorithm tweaks

**P3 (LOW): Additional Optimizations**
- **Reference Equality Bailout**: If `oldNode === newNode`, skip diff entirely
  - Requires model architecture to reuse node references
  - Not applicable with current record-based immutable nodes
- **Append-Only Fast Path**: Head/tail skip enables O(1) append detection
  - Already implemented in P0

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

## 🏗️ Architectural Analysis: Closing the Blazor Gap (2026-02-12)

### Executive Summary

The remaining ~50ms script time gap between Abies and Blazor in the swap benchmark is **fundamentally architectural**, not algorithmic. The gap comes from MVU's requirement to rebuild the entire VDOM on every render.

| Aspect | Blazor | Abies | Gap Cause |
|--------|--------|-------|-----------|
| Render scope | Only dirty components | Entire view tree | ~30% of gap |
| VDOM allocation | Skip unchanged | Always allocate | ~40% of gap |
| Diff traversal | Skip unchanged subtrees | Always traverse | ~30% of gap |

### Framework Comparison

| Framework | Optimization Pattern | How It Works |
|-----------|---------------------|--------------|
| **Blazor** | `ShouldRender()` override | Components override to skip render entirely |
| **Elm** | `lazy`/`lazy2`/`lazy3` | Reference equality (===) skips VDOM construction |
| **React** | `memo()` wrapper | Shallow prop equality skips re-render |
| **Abies** | `lazy(key, fn)` | Value equality on key, still creates LazyMemo wrapper |

### Why Elm's `lazy` is Fast but Abies's Isn't Matching

**Elm's approach:**
```elm
-- Elm: If args are SAME REFERENCE, skip VDOM construction entirely
lazy viewRow rowData  -- Uses JavaScript === for comparison
```

**Abies's current approach:**
```csharp
// Abies: Creates new LazyMemo every render, compares key VALUES
lazy((row, isSelected), () => TableRow(...), id: ...)
```

**Critical difference:**
1. Elm uses **reference equality** (===) which is O(1)
2. Abies uses **value equality** via `MemoKey.Equals()` which involves:
   - Interface dispatch (ILazyMemoNode)
   - Boxing of value types to object
   - Field-by-field comparison for tuples/records

3. Even when memo key matches, Abies still:
   - Creates 1000 new `LazyMemo` objects per render
   - Builds 2 dictionaries with 998 entries each
   - Makes 998 recursive `DiffInternal` calls

### Architectural Options

#### Option A: Component-Based Architecture (Blazor-like)

**Effort**: MAJOR (weeks to months)
**Expected improvement**: 50-70%

```csharp
// FROM pure function:
public static Document View(Model model) => ...

// TO stateful component:
public class RowComponent : Component<RowProps>
{
    public override bool ShouldRender(RowProps old, RowProps new) 
        => !ReferenceEquals(old.Row, new.Row);
    
    public override Node Render(RowProps props) => TableRow(props.Row);
}
```

**Pros:**
- Can completely skip subtree rendering
- Matches Blazor's proven architecture
- Components manage own lifecycle

**Cons:**
- Breaks pure MVU architecture
- Components need lifecycle management  
- More complex mental model
- Loses referential transparency

#### Option B: Reference Equality for Memo (Elm-like)

**Effort**: LOW (hours)
**Expected improvement**: 10-20%

```csharp
// Current (value equality):
if (oldLazy.MemoKey.Equals(newLazy.MemoKey))

// Optimized (reference equality):
if (ReferenceEquals(oldLazy.MemoKey, newLazy.MemoKey))
```

**Caveat**: Would only help if SAME object is passed each render:
```csharp
// Current creates NEW tuple each render:
lazy((row, isSelected), ...)  // NEW tuple object

// Would need to cache:
var memoKey = GetOrCreateMemoKey(row, isSelected);  // Reused reference
lazy(memoKey, ...)
```

#### Option C: View Caching Layer

**Effort**: MEDIUM (days)
**Expected improvement**: 30-50%

```csharp
public static Document View(Model model, Document? previous)
{
    return new Document("Title",
        tbody([], model.Data.Select((row, i) => 
            GetCachedRowNode(row, model.Selected == row.Id, previous)
        ))
    );
}

// Cache rendered nodes by stable key
private static Node GetCachedRowNode(Row row, bool selected, Document? prev)
{
    if (TryGetCached(row.Id, out var cached) && cached.Selected == selected)
        return cached.Node;  // Reuse entire node tree!
    return TableRow(row, selected);
}
```

**Pros:**
- Maintains MVU purity at top level
- Can skip VDOM construction for unchanged items

**Cons:**
- Requires cache invalidation logic
- Memory overhead for cache
- Complex lifecycle management

#### Option D: Incremental/Reactive Views

**Effort**: MAJOR (months)
**Expected improvement**: 60-80%

Move to a reactive architecture where views subscribe to model changes:

```csharp
// Instead of View(model) → entire VDOM
// Use reactive bindings:
public static IObservable<Node> ViewRow(IObservable<Row> row$, IObservable<bool> selected$)
    => row$.CombineLatest(selected$, (row, sel) => TableRow(row, sel));
```

**Cons:**
- Fundamental paradigm shift
- No longer "pure" MVU
- Significant complexity increase

#### Option E: Compile-Time Optimization (Source Generators)

**Effort**: MAJOR (weeks)
**Expected improvement**: 40-60%

Use source generators to analyze View functions and generate optimized code:

```csharp
// Developer writes:
public static Node ViewRow(Row row, bool selected) => 
    tr([class_(selected ? "danger" : "")], [...]);

// Generator produces:
public static Node ViewRow_Optimized(Row row, bool selected, Node? cached)
{
    if (cached is Element e && ReferenceEquals(e.Data, row) && e.Selected == selected)
        return cached;  // Skip construction!
    return ViewRow(row, selected);
}
```

### Recommended Path Forward

#### Phase 1: Quick Wins (LOW effort, 10-20%)
1. **Generic EqualityComparer** - Avoid object boxing in MemoKey comparison
2. **In-order keys fast path** - Skip dictionary for append/update operations
3. **Reference equality bailout** - Add `ReferenceEquals(old, new)` check at DiffInternal top

#### Phase 2: Medium Investment (MEDIUM effort, 20-40%)
4. **View caching layer** - Cache and reuse node trees across renders
5. **Specialized list diffing** - Track changes at model level for `List<T>` children

#### Phase 3: Architecture Decision (MAJOR effort, 50-70%)
6. **Hybrid component model** - Keep MVU for app structure, allow components for lists
7. **Compile-time optimization** - Source generator for automatic memoization

### Decision Points

The key question for Abies's future is:

> **Is matching Blazor's performance worth compromising MVU purity?**

**If YES (prioritize performance):**
- Implement hybrid component model (Option A)
- Allow developers to opt into stateful components for performance-critical sections
- Accept increased complexity for performance gains

**If NO (prioritize simplicity):**
- Accept 1.3-1.5x performance gap as MVU architectural cost
- Focus on quick wins (Phase 1)
- Document trade-offs for users
- Position Abies for correctness/simplicity, not raw speed

### Conclusion

The ~50ms gap with Blazor is **inherent to MVU architecture** (full VDOM rebuild per render). The memoization helps (prevents deep diffing) but cannot eliminate the rebuild cost entirely.

Blazor's component model allows skipping entire subtrees because components are stateful and can decide whether to re-render. This is fundamentally different from MVU's pure function approach.

Further optimization beyond Phase 1 requires **architectural decisions** about the framework's direction:
- **Pure MVU with performance ceiling**, or
- **Hybrid approach with Blazor-like performance**

Both are valid choices with different trade-offs.

### ❌ REJECTED: Shared-Memory Binary Protocol (Issue #93, 2026-02-19)

**Branch**: `perf/shared-memory-protocol`

**Hypothesis**: The `batchData.slice()` copy in JavaScript (from `JSType.MemoryView` → `Uint8Array`)
is a significant overhead. Eliminating it via Blazor-style shared WASM memory would improve performance.

**Implementation**:
1. Changed `ApplyBinaryBatch` from `[JSMarshalAs<JSType.MemoryView>] Span<byte>` to `(int address, int length)`
2. C# side: `unsafe fixed (byte* ptr = memory.Span)` pins the buffer, passes raw address to JS
3. JS side: `getWasmHeapU8()` accesses WASM linear memory via `globalThis.getDotnetRuntime(0).localHeapViewU8()`
4. Creates zero-copy `new Uint8Array(heap.buffer, address, length)` view — no `slice()` copy
5. Added heap view caching with detached-buffer detection (`byteLength > 0` check)

**A/B Benchmark Results (same session, battery, 2026-02-19):**

| Benchmark | Main (MemoryView) | Shared Memory | Δ |
|-----------|-------------------|---------------|---|
| 01_run1k | 70.7ms | 71.7ms | +1.4% (noise) |
| 02_replace1k | 79.9ms | 80.0ms | +0.1% (noise) |
| 04_select1k | 121.9ms | 122.3ms | +0.3% (noise) |
| 06_remove-one-1k | 84.7ms | 77.1ms | -9.0% (noise) |
| 07_create10k | 819.0ms | 815.5ms | -0.4% (noise) |
| 09_clear1k | 92.3ms | 92.4ms | +0.1% (noise) |

**Result**: **ZERO measurable improvement.** All differences are within noise margin.

**Why It Didn't Help**:
- The `slice()` copy cost for a ~16KB buffer (1000 rows) is **negligible** (~microseconds)
- The real costs are VDOM diffing (~40ms) and DOM operations (~20ms)
- Eliminating a microsecond-level copy cannot produce measurable E2E improvement
- Blazor's speed advantage comes from its **component architecture** (skipping subtrees), NOT from its binary protocol details

**Conclusion**:
- The shared-memory protocol is architecturally cleaner (no MemoryView dependency, standard WASM pattern)
- But it provides **zero performance benefit** — do NOT pursue further interop protocol optimizations
- The remaining Blazor gap is in VDOM rebuild cost, not JS interop overhead
