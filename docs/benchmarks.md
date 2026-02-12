# Performance Benchmarks# Rendering Engine Benchmarks



This document describes the benchmarking infrastructure for the Abies framework, covering both micro-benchmarks (BenchmarkDotNet) and end-to-end benchmarks (js-framework-benchmark).This document describes the benchmarking infrastructure for the Abies rendering engine, including DOM diffing, HTML rendering, and event handler creation.



## Benchmarking Strategy## Overview



Abies uses a **dual-layer benchmarking strategy**:The Abies framework uses a Virtual DOM diffing algorithm to compute minimal patches between UI states. Performance of this algorithm is critical because it runs on every UI update.



| Layer | Tool | Purpose | Trust Level |## Benchmark Categories

| --- | --- | --- | --- |

| **Primary (E2E)** | js-framework-benchmark | Real-world user-perceived performance | Source of truth |The benchmark suite covers three categories:

| **Secondary (Micro)** | BenchmarkDotNet | Algorithm comparison, allocation tracking | Development guidance |

### DOM Diffing (`Abies.Benchmarks.Diffing/`)

**Key principle**: Micro-benchmark improvements that don't appear in E2E benchmarks are likely false positives. Always validate with E2E benchmarks before shipping performance changes.

Measures the Virtual DOM diffing algorithm performance.

See [Benchmarking Strategy Investigation](./investigations/benchmarking-strategy.md) for the full analysis behind this approach.

### Rendering (`Abies.Benchmarks.Rendering/`)

## E2E Benchmarks (js-framework-benchmark)

Measures HTML string rendering performance.

The [js-framework-benchmark](https://github.com/krausest/js-framework-benchmark) measures what users actually experience: the time from clicking a button to seeing the result on screen, captured via Chrome's Performance Log API (`EventDispatch → Paint`).

### Event Handlers (`Abies.Benchmarks.Handlers/`)

### Latest Results

Measures event handler creation and registration performance.

| Benchmark | Abies | Blazor WASM | Ratio |

| --- | --- | --- | --- |## Benchmark Scenarios

| 01\_run1k (create 1,000 rows) | ~89 ms | ~88 ms | **1.01x** ✅ |

| 05\_swap1k (swap two rows) | ~121 ms | ~95 ms | 1.27x |We measure the following scenarios:

| 09\_clear1k (clear all rows) | ~85 ms | ~46 ms | 1.84x |

| First Paint | ~74 ms | ~75 ms | **0.99x** ✅ || Benchmark | Description | Use Case |

| Bundle size (compressed) | 1,225 KB | 1,377 KB | **0.89x** ✅ ||-----------|-------------|----------|

| Ready memory | 34.3 MB | 41.1 MB | **0.83x** ✅ || `SmallDomDiff` | 2-3 element tree with mixed changes | Typical component updates |

| `MediumDomDiff` | 15-20 element tree with scattered changes | Page section updates |

### Running E2E Benchmarks| `LargeDomDiff` | 150+ element tree (data table) | Worst-case scenario |

| `AttributeOnlyDiff` | Attribute changes only, no structural changes | Style/class updates |

```bash| `TextOnlyDiff` | Text content changes only | Dynamic content updates |

# Build Abies for benchmark| `NodeAdditionDiff` | Adding new child nodes | List append operations |

cd js-framework-benchmark-fork/frameworks/keyed/abies/src| `NodeRemovalDiff` | Removing child nodes | List item deletion |

rm -rf bin obj && dotnet publish -c Release

cp -R bin/Release/net10.0/publish/wwwroot/* ../bundled-dist/## Running Benchmarks Locally



# Start benchmark server```bash

cd ../../../../# Run all benchmarks

npm start &dotnet run --project Abies.Benchmarks -c Release



# Run benchmarks (in another terminal)# Run specific benchmark

cd webdriver-tsdotnet run --project Abies.Benchmarks -c Release -- --filter "*SmallDom*"

npm run bench -- --headless --framework abies-keyed --benchmark 01_run1k

npm run bench -- --headless --framework abies-keyed --benchmark 05_swap1k# Quick run with fewer iterations

npm run bench -- --headless --framework abies-keyed --benchmark 09_clear1kdotnet run --project Abies.Benchmarks -c Release -- --job short

``````



### Comparing Results## Benchmark Results



```bash### Latest Results

python3 scripts/compare-benchmark.py \

  --results-dir ../js-framework-benchmark-fork/webdriver-ts/results \Benchmark results are automatically published to GitHub Pages after each merge to `main`:

  --baseline benchmark-results/baseline.json \

  --threshold 5.0📊 **[View Interactive Charts](https://picea.github.io/Abies/dev/bench/)**

```

### Performance Targets

## Micro-Benchmarks (BenchmarkDotNet)

| Metric | Target | Rationale |

Micro-benchmarks provide fast development feedback for algorithm comparison and allocation tracking.|--------|--------|-----------|

| Small DOM diff | < 500 ns | 60fps requires < 16.6ms per frame |

### Benchmark Categories| Medium DOM diff | < 1 μs | Allow for complex layouts |

| Large DOM diff | < 5 μs | Data tables must remain responsive |

| Category | File | What It Measures || Memory per operation | < 5 KB | Minimize GC pressure |

| --- | --- | --- |

| DOM Diffing | `DomDiffingBenchmarks.cs` | Virtual DOM diffing algorithm |## Quality Gates

| Rendering | `RenderingBenchmarks.cs` | HTML string rendering |

| Event Handlers | `EventHandlerBenchmarks.cs` | Event handler creation |The CI pipeline enforces the following quality gates:

| URL Parsing | `UrlParsingBenchmarks.cs` | URL parsing performance |

- **Warning threshold**: 150% - CI will comment on PR if performance degrades by 50%

### Running Micro-Benchmarks- **Failure threshold**: 200% - CI will fail if performance degrades by 100%



```bash## CI/CD Integration

# Run all benchmarks

dotnet run --project Abies.Benchmarks -c ReleaseBenchmarks run automatically on:

- Every push to `main` that modifies `Abies/` or `Abies.Benchmarks/`

# Run specific benchmark- Every PR targeting `main` with changes to these paths

dotnet run --project Abies.Benchmarks -c Release -- --filter "*SmallDom*"

### How It Works

# Quick run with fewer iterations

dotnet run --project Abies.Benchmarks -c Release -- --job short1. GitHub Actions runs benchmarks using BenchmarkDotNet

```2. Results are exported to JSON format

3. `github-action-benchmark` compares results against historical data

### Benchmark Results4. Charts are published to GitHub Pages

5. Performance regressions trigger PR comments or workflow failures

Results are automatically published to GitHub Pages after each merge to `main`:

## Understanding Results

📊 **[View Interactive Charts](https://picea.github.io/Abies/dev/bench/)** — Separate charts for CPU and memory metrics with historical trends.

### Key Metrics

## Quality Gates

- **Mean**: Average execution time

The CI pipeline enforces the following quality gates:- **Error**: 99.9% confidence interval margin

- **StdDev**: Standard deviation showing consistency

### Micro-Benchmark Thresholds- **Gen0/Gen1**: GC collections per 1000 operations

- **Allocated**: Memory allocated per operation

| Metric | Warning | Failure |

| --- | --- | --- |### Interpreting Changes

| Throughput regression | 105% | 110% |

| Allocation increase | 110% | 120% |When evaluating a performance change:



### When to Require E2E Validation1. **< 5% change**: Within noise margin, ignore

2. **5-20% change**: Worth investigating

Always validate with js-framework-benchmark when:3. **> 20% change**: Significant - review the code changes



- Changing interop patterns (binary batching, serialization)## Adding New Benchmarks

- Adding or removing object pooling

- Changing data structures in hot paths1. Add a new method in `Abies.Benchmarks/DomDiffingBenchmarks.cs`:

- Any optimization claiming >5% improvement

```csharp

## CI/CD Integration/// <summary>

/// Description of what this benchmark measures.

Benchmarks run automatically on:/// </summary>

[Benchmark]

- Every push to `main` that modifies `Abies/` or `Abies.Benchmarks/`public void YourNewBenchmark()

- Every PR targeting `main` with changes to these paths{

    Operations.Diff(_yourOldNode, _yourNewNode);

### Workflow}

```

1. GitHub Actions runs BenchmarkDotNet benchmarks

2. Results are exported to JSON format2. Add setup logic in `[GlobalSetup]` method

3. `github-action-benchmark` compares results against historical data3. Run locally to verify

4. Charts are published to GitHub Pages (separate CPU and memory charts)4. Submit PR - CI will baseline the new benchmark

5. Performance regressions trigger PR comments or workflow failures

## Architecture

### E2E Benchmarks in CI

```text

E2E benchmarks also run on pushes to `main` via `benchmark.yml`:Abies.Benchmarks/

├── DomDiffingBenchmarks.cs    # Virtual DOM diffing benchmarks

- Builds Abies for the js-framework-benchmark harness├── RenderingBenchmarks.cs     # HTML rendering benchmarks

- Runs the standard benchmark suite├── EventHandlerBenchmarks.cs  # Event handler creation benchmarks

- Publishes results to GitHub Pages alongside micro-benchmark charts├── UrlParsingBenchmarks.cs    # URL parsing benchmarks  

├── Program.cs                 # Entry point

## Understanding Results└── Abies.Benchmarks.csproj    # Project file



### Key Metrics (Micro-Benchmarks).github/workflows/

└── benchmark.yml              # CI workflow

- **Mean**: Average execution time

- **Error**: 99.9% confidence interval margindocs/

- **StdDev**: Standard deviation showing consistency└── benchmarks.md              # This file

- **Gen0/Gen1**: GC collections per 1,000 operations```

- **Allocated**: Memory allocated per operation

## Related Documentation

### Interpreting Changes

- [ADR-020: Benchmark Quality Gates](../adr/ADR-020-benchmark-quality-gates.md)

| Change | Action |- [ADR-016: Virtual DOM Keyed Diffing](../adr/ADR-016-keyed-dom-diffing.md)

| --- | --- |- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/articles/overview.html)

| < 5% | Within noise margin, ignore |- [github-action-benchmark](https://github.com/benchmark-action/github-action-benchmark)

| 5–20% | Worth investigating |
| > 20% | Significant — review the code changes |

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
4. Submit PR — CI will baseline the new benchmark

## Architecture

```text
Abies.Benchmarks/
├── DomDiffingBenchmarks.cs    # Virtual DOM diffing benchmarks
├── RenderingBenchmarks.cs     # HTML rendering benchmarks
├── EventHandlerBenchmarks.cs  # Event handler creation benchmarks
├── UrlParsingBenchmarks.cs    # URL parsing benchmarks
├── Program.cs                 # Entry point
└── Abies.Benchmarks.csproj    # Project file

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

- [Benchmarking Strategy](./investigations/benchmarking-strategy.md) — Why E2E is the source of truth
- [Blazor Performance Analysis](./investigations/blazor-performance-analysis.md) — Deep comparison with Blazor
- [ADR-020: Benchmark Quality Gates](./adr/ADR-020-benchmark-quality-gates.md) — Quality gate design decision
- [ADR-016: Keyed DOM Diffing](./adr/ADR-016-keyed-dom-diffing.md) — LIS algorithm and keyed reconciliation
- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/articles/overview.html)
- [js-framework-benchmark](https://github.com/krausest/js-framework-benchmark)
