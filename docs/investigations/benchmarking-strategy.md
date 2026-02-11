# Benchmarking Strategy for Abies Framework

**Date**: 2026-02-11
**Author**: AI Agent (based on deep research)
**Status**: Recommendation

## Executive Summary

After analyzing how Blazor/ASP.NET Core, React, and other major frameworks approach performance benchmarking, this document recommends a **dual-layer benchmarking strategy**:

1. **Primary (Source of Truth)**: js-framework-benchmark for real-world E2E performance
2. **Secondary (Development Guidance)**: BenchmarkDotNet micro-benchmarks for algorithm comparison and allocation tracking

**Key Finding**: Micro-benchmark improvements that don't appear in E2E benchmarks are likely false positives caused by overhead in areas the micro-benchmark doesn't measure (JS interop, rendering, GC).

---

## Research Findings

### How Blazor/ASP.NET Core Benchmarks

The ASP.NET team uses **Microsoft Crank** for continuous benchmarking:

```bash
crank --config blazor.benchmarks.yml --scenario blazorWebInteractiveWebAssembly --profile aspnet-perf-lin
```

Key characteristics:

- **Lighthouse-based metrics**: Uses Lighthouse for Blazor scenarios (TTI, LCP, script bootup time)
- **Real browser execution**: Actual Chrome running actual scenarios
- **Multiple machine profiles**: Tests on various CPU/memory configurations
- **Continuous tracking**: Results at <https://aka.ms/aspnet/benchmarks>

**Blazor doesn't use BenchmarkDotNet micro-benchmarks for framework performance**. Their micro-benchmarks are for lower-level .NET runtime components (GC, LINQ, etc.), not UI framework behavior.

### How js-framework-benchmark Works

Stefan Krause's benchmark uses Chrome's Performance Log API to measure:

```text
Duration = EventDispatch (start) → Paint (end)
```

This captures the **complete user-perceived latency**:

1. JavaScript execution (model update, diff algorithm)
2. DOM mutations (createElement, setAttribute, appendChild)
3. Style calculation
4. Layout
5. Paint

**Critical insight from Stefan Krause's original article**:
> "Framework hooks (componentDidMount, $postDigest) do NOT include rendering time. To get a somewhat fair comparison the complete duration should be taken, since that is how long the user has to wait for the screen update."

### Why Micro-benchmarks Miss Real-World Costs

Our `memory.instructions.md` documents a case where **micro-benchmarks were misleading**:

| Optimization             | BenchmarkDotNet | js-framework-benchmark | Conclusion |
| ------------------------ | --------------- | ---------------------- | ---------- |
| PatchType enum + pooling | 11-20% faster   | 2-5% SLOWER            | Rejected   |

**Root cause analysis**:

1. Micro-benchmarks run in-process without JS interop overhead
2. Object pooling adds Rent/Return overhead in WASM that exceeds allocation savings
3. The "savings" measured were swamped by unmeasured costs

---

## What Micro-benchmarks Are Good For

Despite limitations, micro-benchmarks remain valuable for:

### 1. Algorithm Selection

```csharp
// Compare LIS implementations
[Benchmark] public void PatientSort_LIS() => ComputeLISPatientSort(arr);
[Benchmark] public void BinarySearch_LIS() => ComputeLISBinarySearch(arr);
```

### 2. Allocation Tracking

```csharp
[MemoryDiagnoser]
public class KeyedDiffingBenchmarks
{
    [Benchmark]
    public void Swap1k() => Operations.Diff(_old, _new);
    // Allocated: 672 B (good) vs 50 KB (regression!)
}
```

### 3. Development-Time Feedback

- Faster iteration than E2E (seconds vs minutes)
- Catches obvious regressions early
- Enables A/B testing of implementations

### 4. Regression Detection

- CI can fail builds on allocation increases
- Catches hot-path performance degradations

---

## What Micro-benchmarks Miss

### 1. JS Interop Overhead

```csharp
// Micro-benchmark measures only this:
var patches = Operations.Diff(old, new);

// Real execution includes:
// - JSON serialization of patches
// - JSRuntime.InvokeAsync call
// - WASM → JS memory boundary crossing
// - JavaScript patch application
// - Browser re-layout/paint
```

### 2. Browser Rendering Pipeline

- Style calculation
- Layout (reflow)
- Paint
- Composite

### 3. GC Pressure at Scale

- Micro-benchmarks measure single operations
- Real apps trigger GC from cumulative allocations
- Major GC pauses not captured

### 4. Event Loop Scheduling

- JavaScript task queue behavior
- requestAnimationFrame timing
- Long task detection

---

## Recommended Benchmarking Strategy

### Primary: js-framework-benchmark (Source of Truth)

**When to run**: Before merging ANY performance-related PR

**Key metrics to track**:

| Benchmark   | What It Tests      | Target        |
| ----------- | ------------------ | ------------- |
| 01_run1k    | Initial render     | ≤1.05x Blazor |
| 05_swap1k   | Keyed diffing/LIS  | ≤1.5x Blazor  |
| 09_clear1k  | Cleanup efficiency | ≤2x Blazor    |
| First Paint | Startup perceived  | ≤Blazor       |
| Ready Memory| WASM size          | ≤Blazor       |

**Run commands**:

```bash
# Build Abies for benchmark
cd js-framework-benchmark-fork/frameworks/keyed/abies/src
rm -rf bin obj && dotnet publish -c Release
cp -R bin/Release/net10.0/publish/wwwroot/* ../bundled-dist/

# Run benchmarks
cd ../../../..
npm start &  # Start server
cd webdriver-ts
npm run bench -- --headless --framework abies-v1.0.152-keyed
```

### Secondary: BenchmarkDotNet (Development Guidance)

**When to run**: During development iteration

**Use cases**:

1. Comparing algorithm implementations
2. Tracking allocations per operation
3. Quick feedback loop

**Key benchmarks to maintain**:

| Benchmark              | Purpose              |
| ---------------------- | -------------------- |
| DomDiffingBenchmarks   | Algorithm comparison |
| KeyedDiffingBenchmarks | LIS validation       |
| RenderingBenchmarks    | HTML generation cost |
| EventHandlerBenchmarks | Handler creation     |

**Run command**:

```bash
dotnet run -c Release --project Abies.Benchmarks -- --filter "*Swap1k*"
```

---

## Decision Framework

### When to Trust Micro-benchmarks

✅ **Trust when**:

- Comparing two algorithms with same input/output
- Measuring allocations (MemoryDiagnoser)
- Testing isolated pure functions
- Validating specific optimizations (e.g., SearchValues skip)

### When to Require E2E Validation

⚠️ **Always validate with js-framework-benchmark when**:

- Changing interop patterns (JSON format, binary batching)
- Adding object pooling
- Changing data structures
- Any optimization claiming >5% improvement

### When Micro-benchmarks Are Misleading

❌ **Distrust when**:

- Optimization involves allocation/deallocation patterns
- Change affects GC behavior
- Optimization trades CPU for memory (or vice versa)
- Test doesn't include realistic data sizes

---

## Local Benchmarking Workflow

A convenience script is provided for running benchmarks locally before pushing to a PR.

### Quick Start

```bash
# Fast feedback during development (micro-benchmarks)
./scripts/run-benchmarks.sh --micro

# Quick mode for even faster iteration (fewer iterations)
./scripts/run-benchmarks.sh --micro --quick

# Full validation before merging (E2E benchmarks)
./scripts/run-benchmarks.sh --e2e

# Compare E2E results against baseline
./scripts/run-benchmarks.sh --e2e --compare

# Update baseline after intentional changes
./scripts/run-benchmarks.sh --e2e --update-baseline

# Run everything (both micro and E2E)
./scripts/run-benchmarks.sh --all
```

### Prerequisites

1. **.NET SDK 10.0+** - For building and running benchmarks
2. **Python 3.10+** - For result comparison scripts
3. **Node.js 20+** - For E2E benchmarks (js-framework-benchmark)
4. **js-framework-benchmark fork** - Clone to `../js-framework-benchmark-fork`:

   ```bash
   git clone https://github.com/nicknash/js-framework-benchmark.git ../js-framework-benchmark-fork
   cd ../js-framework-benchmark-fork && npm ci
   cd webdriver-ts && npm ci
   ```

### Workflow Before Pushing a Performance PR

1. **During development**: Run `--micro --quick` for fast iteration
2. **Before committing**: Run `--micro` for full micro-benchmark results
3. **Before pushing**: Run `--e2e --compare` to validate against baseline
4. **If regression detected**: Investigate and fix, or justify the regression
5. **If intentional change**: Run `--e2e --update-baseline` and commit new baseline

### Result Locations

| Type             | Location                         |
| ---------------- | -------------------------------- |
| Micro-benchmarks | `benchmark-results/local/micro/` |
| E2E benchmarks   | `benchmark-results/local/e2e/`   |
| E2E baseline     | `benchmark-results/baseline.json`|

---

## Proposed CI Integration

### Phase 1: Current State (Implemented)

- Document exact js-framework-benchmark steps in memory.instructions.md ✅
- Local benchmarking script for consistent developer workflow ✅
- Run manually before perf PR merges

### Phase 2: Semi-Automated

```yaml
# .github/workflows/benchmark.yml (future)
on:
  pull_request:
    paths:
      - 'Abies/DOM/**'
      - 'Abies/Runtime*.cs'
      - 'Abies/wwwroot/abies.js'

jobs:
  e2e-benchmark:
    runs-on: ubuntu-latest
    steps:
      - name: Build Abies
        run: dotnet publish -c Release
      - name: Run js-framework-benchmark
        run: npm run bench -- --headless --framework abies-keyed
      - name: Compare to baseline
        run: python scripts/compare-benchmark.py
```

### Phase 3: Fully Automated (Future)

- Bot comments benchmark results on PRs
- Block merge if regression detected
- Track historical trends

---

## Comparison with Other Frameworks

| Framework  | Primary Benchmark      | Micro-benchmarks | Notes                         |
| ---------- | ---------------------- | ---------------- | ----------------------------- |
| **Blazor** | Crank + Lighthouse     | None for UI      | Microsoft Crank infrastructure|
| **React**  | User-space profiling   | None official    | Recommends browser DevTools   |
| **Vue**    | js-framework-benchmark | Internal only    | Focus on E2E                  |
| **Svelte** | js-framework-benchmark | Compile-time     | AOT optimizations             |
| **Abies**  | js-framework-benchmark | BenchmarkDotNet  | Dual-layer (recommended)      |

---

## Conclusion

**Micro-benchmarks have their place but should NEVER be the final arbiter of performance decisions.**

The js-framework-benchmark, despite its quirks, measures what users actually experience: the time from clicking a button to seeing the result on screen. This includes all the "invisible" costs that micro-benchmarks miss.

Our previous experience with PatchType enum optimization proves this point:

- BenchmarkDotNet showed 11-20% improvement
- js-framework-benchmark showed 2-5% regression
- Shipping based on micro-benchmarks would have been wrong

**Recommendation**: Continue using both, but always validate micro-benchmark improvements with E2E benchmarks before merging.

---

## References

1. [ASP.NET Benchmarks Repository](https://github.com/aspnet/Benchmarks)
2. [Microsoft Crank](https://github.com/dotnet/crank)
3. [js-framework-benchmark](https://github.com/krausest/js-framework-benchmark)
4. [Stefan Krause: Benchmarking JS Frontend Frameworks](https://www.stefankrause.net/wp/?p=218)
5. [V8: The Cost of JavaScript in 2019](https://v8.dev/blog/cost-of-javascript-2019)
6. [How js-framework-benchmark measures duration](https://github.com/krausest/js-framework-benchmark/wiki/How-the-duration-is-measured)
