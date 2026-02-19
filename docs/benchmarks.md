# Performance Benchmarks

This document describes the benchmarking infrastructure for the Abies framework, covering both micro-benchmarks (BenchmarkDotNet) and end-to-end benchmarks (js-framework-benchmark).

## Benchmarking Strategy

Abies uses a **dual-layer benchmarking strategy**:

| Layer | Tool | Purpose | Trust Level |
| --- | --- | --- | --- |
| **Primary (E2E)** | js-framework-benchmark | Real-world user-perceived performance | Source of truth |
| **Secondary (Micro)** | BenchmarkDotNet | Algorithm comparison, allocation tracking | Development guidance |

**Key principle**: Micro-benchmark improvements that don't appear in E2E benchmarks are likely false positives. Always validate with E2E benchmarks before shipping performance changes.

See [Benchmarking Strategy Investigation](./investigations/benchmarking-strategy.md) for the full analysis behind this approach.

## E2E Benchmarks (js-framework-benchmark)

The [js-framework-benchmark](https://github.com/krausest/js-framework-benchmark) measures what users actually experience: the time from clicking a button to seeing the result on screen, captured via Chrome's Performance Log API (`EventDispatch → Paint`).

### Latest Results

| Benchmark | Abies | Blazor WASM | Ratio |
| --- | --- | --- | --- |
| 01\_run1k (create 1,000 rows) | ~72 ms | ~88 ms | **0.81x** ✅ |
| 02\_replace1k (replace 1,000 rows) | ~96 ms | — | — |
| 05\_swap1k (swap two rows) | ~108 ms | ~95 ms | 1.13x |
| 09\_clear1k (clear all rows) | ~92 ms | ~46 ms | 2.00x |
| First Paint | ~74 ms | ~75 ms | **0.99x** ✅ |
| Bundle size (compressed) | 1,225 KB | 1,377 KB | **0.89x** ✅ |
| Ready memory | 34.3 MB | 41.1 MB | **0.83x** ✅ |

Key optimizations contributing to the create benchmark lead:
- **SetChildrenHtml batch patch** — single `parent.innerHTML` instead of N individual `AddChild` patches
- **Skip addEventListeners TreeWalker scan** — events are pre-registered at document level
- **Complete Replacement fast path** — `ClearChildren + SetChildrenHtml` for full list replacements

### Running E2E Benchmarks

```bash
# Build Abies for benchmark
cd js-framework-benchmark-fork/frameworks/keyed/abies/src
rm -rf bin obj && dotnet publish -c Release
cp -R bin/Release/net10.0/publish/wwwroot/* ../bundled-dist/

# Start benchmark server
cd ../../../../
npm start &

# Run benchmarks (in another terminal)
cd webdriver-ts
npm run bench -- --headless --framework abies-keyed --benchmark 01_run1k
npm run bench -- --headless --framework abies-keyed --benchmark 05_swap1k
npm run bench -- --headless --framework abies-keyed --benchmark 09_clear1k
```

### Comparing Results

```bash
python3 scripts/compare-benchmark.py \
  --results-dir ../js-framework-benchmark-fork/webdriver-ts/results \
  --baseline benchmark-results/baseline.json \
  --threshold 5.0
```

## Micro-Benchmarks (BenchmarkDotNet)

Micro-benchmarks provide fast development feedback for algorithm comparison and allocation tracking.

### Benchmark Categories

| Category | File | What It Measures |
| --- | --- | --- |
| DOM Diffing | `DomDiffingBenchmarks.cs` | Virtual DOM diffing algorithm |
| Rendering | `RenderingBenchmarks.cs` | HTML string rendering |
| Event Handlers | `EventHandlerBenchmarks.cs` | Event handler creation |
| URL Parsing | `UrlParsingBenchmarks.cs` | URL parsing performance |

### Benchmark Scenarios

| Benchmark | Description | Use Case |
|-----------|-------------|----------|
| `SmallDomDiff` | 2-3 element tree with mixed changes | Typical component updates |
| `MediumDomDiff` | 15-20 element tree with scattered changes | Page section updates |
| `LargeDomDiff` | 150+ element tree (data table) | Worst-case scenario |
| `AttributeOnlyDiff` | Attribute changes only, no structural changes | Style/class updates |
| `TextOnlyDiff` | Text content changes only | Dynamic content updates |
| `NodeAdditionDiff` | Adding new child nodes | List append operations |
| `NodeRemovalDiff` | Removing child nodes | List item deletion |

### Performance Targets

| Metric | Target | Rationale |
|--------|--------|-----------|
| Small DOM diff | < 500 ns | 60fps requires < 16.6ms per frame |
| Medium DOM diff | < 1 μs | Allow for complex layouts |
| Large DOM diff | < 5 μs | Data tables must remain responsive |
| Memory per operation | < 5 KB | Minimize GC pressure |

### Running Micro-Benchmarks

```bash
# Run all benchmarks
dotnet run --project Abies.Benchmarks -c Release

# Run specific benchmark
dotnet run --project Abies.Benchmarks -c Release -- --filter "*SmallDom*"

# Quick run with fewer iterations
dotnet run --project Abies.Benchmarks -c Release -- --job short
```

### Benchmark Results

Results are automatically published to GitHub Pages after each merge to `main`:

📊 **[View Interactive Charts](https://picea.github.io/Abies/dev/bench/)** — Separate charts for CPU and memory metrics with historical trends.

## Quality Gates

The CI pipeline enforces the following quality gates:

### Micro-Benchmark Thresholds

| Metric | Warning | Failure |
| --- | --- | --- |
| Throughput regression | 105% | 110% |
| Allocation increase | 110% | 120% |

### When to Require E2E Validation

Always validate with js-framework-benchmark when:

- Changing interop patterns (binary batching, serialization)
- Adding or removing object pooling
- Changing data structures in hot paths
- Any optimization claiming >5% improvement

## CI/CD Integration

Benchmarks run automatically on:

- Every push to `main` that modifies `Abies/` or `Abies.Benchmarks/`
- Every PR targeting `main` with changes to these paths

### Workflow

1. GitHub Actions runs BenchmarkDotNet benchmarks
2. Results are exported to JSON format
3. `github-action-benchmark` compares results against historical data
4. Charts are published to GitHub Pages (separate CPU and memory charts)
5. Performance regressions trigger PR comments or workflow failures

### E2E Benchmarks in CI

E2E benchmarks also run on pushes to `main` via `benchmark.yml`:

- Builds Abies for the js-framework-benchmark harness
- Runs the standard benchmark suite
- Publishes results to GitHub Pages alongside micro-benchmark charts

## Understanding Results

### Key Metrics

- **Mean**: Average execution time
- **Error**: 99.9% confidence interval margin
- **StdDev**: Standard deviation showing consistency
- **Gen0/Gen1**: GC collections per 1,000 operations
- **Allocated**: Memory allocated per operation

### Interpreting Changes

| Change | Action |
| --- | --- |
| < 5% | Within noise margin, ignore |
| 5-20% | Worth investigating |
| > 20% | Significant - review the code changes |

### What Micro-Benchmarks Miss

- JS interop overhead (the biggest cost in WASM apps)
- Browser rendering pipeline (style, layout, paint)
- GC pressure at scale
- Event loop scheduling

## Adding New Benchmarks

1. Add a new method in the appropriate benchmark file:

```csharp
/// <summary>
/// Description of what this benchmark measures.
/// </summary>
[Benchmark]
public void YourNewBenchmark()
{
    Operations.Diff(_yourOldNode, _yourNewNode);
}
```

2. Add setup logic in `[GlobalSetup]` method
3. Run locally to verify
4. Submit PR - CI will baseline the new benchmark

## Architecture

```text
Abies.Benchmarks/
├── DomDiffingBenchmarks.cs    # Virtual DOM diffing benchmarks
├── RenderingBenchmarks.cs     # HTML rendering benchmarks
├── EventHandlerBenchmarks.cs  # Event handler creation benchmarks
├── UrlParsingBenchmarks.cs    # URL parsing benchmarks
├── Program.cs                 # Entry point
└── Abies.Benchmarks.csproj   # Project file

benchmark-results/
├── baseline.json              # E2E benchmark baseline
└── local/                     # Local benchmark results

scripts/
├── compare-benchmark.py       # E2E result comparison
├── convert-e2e-results.py     # Convert results for GitHub Pages
└── run-benchmarks.sh          # Local benchmarking convenience script

.github/workflows/
└── benchmark.yml              # CI workflow (micro + E2E)
```

## Related Documentation

- [Benchmarking Strategy](./investigations/benchmarking-strategy.md) - Why E2E is the source of truth
- [Blazor Performance Analysis](./investigations/blazor-performance-analysis.md) - Deep comparison with Blazor
- [ADR-020: Benchmark Quality Gates](./adr/ADR-020-benchmark-quality-gates.md) - Quality gate design decision
- [ADR-016: Keyed DOM Diffing](./adr/ADR-016-keyed-dom-diffing.md) - LIS algorithm and keyed reconciliation
- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/articles/overview.html)
- [js-framework-benchmark](https://github.com/krausest/js-framework-benchmark)
