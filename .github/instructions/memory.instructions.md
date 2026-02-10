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

### Latest Benchmark Results (2026-02-09) - AFTER LIS FIX

**Abies v1.0.151 vs Blazor WASM v10.0.0:**

| Benchmark | Abies (Before) | Abies (After) | Blazor | Improvement |
|-----------|----------------|---------------|--------|-------------|
| 01_run1k | 103.2ms | TBD | 87.6ms | - |
| 02_replace1k | 132.9ms | TBD | 102.4ms | - |
| 03_update10th | 132.1ms | TBD | 94.7ms | - |
| 04_select1k | 115.2ms | TBD | 82.9ms | - |
| **05_swap1k** | **326.6ms** | **121.6ms** | **94.2ms** | **✅ 2.7x faster!** |
| 06_remove-one | 65.3ms | TBD | 55.6ms | - |
| 07_create10k | 924.8ms | TBD | 810.7ms | - |
| 08_append1k | 135.8ms | TBD | 102.9ms | - |
| 09_clear1k | 159.6ms | TBD | 44.6ms | - |

**🎉 LIS Algorithm Fix (2026-02-09):**
- **Swap benchmark**: 326.6ms → 121.6ms (**2.7x faster**)
- Now only **1.29x** slower than Blazor (was 3.47x)
- Root cause: `ComputeLISInto` had bug where `k=0` tracking never incremented for first element

**Allocation Optimization (2026-02-09):**
Applied the following optimizations to reduce GC pressure:
1. `ComputeLISInto` - Uses ArrayPool for `result` and `p` arrays instead of allocating new arrays
2. `inLIS` bool array - Replaced `HashSet<int>` with `ArrayPool<bool>.Shared` for LIS membership
3. `PatchDataList` pooling - Added `_patchDataListPool` to reuse List<PatchData> in ApplyBatch

**GC Impact:**
- Swap benchmark GC: 10.4% → 9.4% (10% reduction)
- Clear benchmark GC: 18.1% → 12.2% (33% reduction)
- Clear benchmark time: 173.2ms → 159.6ms (8% improvement)

**Remaining Hotspots:**
- Patch records (MoveChild, RemoveChild, etc.) are allocated on every diff
- PatchData records are created for JSON serialization
- JSON serialization itself allocates strings

**Key Findings:**
1. **Swap (05_swap1k)** is 3.47x slower
2. **Clear (09_clear1k)** is 3.58x slower - improved by 8% with allocation optimizations

**Size Comparison:**
- Abies compressed: 1,225 KB
- Abies uncompressed: 3,938 KB
- First paint: 4,811ms

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
