# Virtual DOM Benchmarks

This document describes the benchmarking infrastructure for the Abies Virtual DOM diffing algorithm.

## Overview

The Abies framework uses a Virtual DOM diffing algorithm to compute minimal patches between UI states. Performance of this algorithm is critical because it runs on every UI update.

## Benchmark Scenarios

We measure the following scenarios:

| Benchmark | Description | Use Case |
|-----------|-------------|----------|
| `SmallDomDiff` | 2-3 element tree with mixed changes | Typical component updates |
| `MediumDomDiff` | 15-20 element tree with scattered changes | Page section updates |
| `LargeDomDiff` | 150+ element tree (data table) | Worst-case scenario |
| `AttributeOnlyDiff` | Attribute changes only, no structural changes | Style/class updates |
| `TextOnlyDiff` | Text content changes only | Dynamic content updates |
| `NodeAdditionDiff` | Adding new child nodes | List append operations |
| `NodeRemovalDiff` | Removing child nodes | List item deletion |

## Running Benchmarks Locally

```bash
# Run all benchmarks
dotnet run --project Abies.Benchmarks -c Release

# Run specific benchmark
dotnet run --project Abies.Benchmarks -c Release -- --filter "*SmallDom*"

# Quick run with fewer iterations
dotnet run --project Abies.Benchmarks -c Release -- --job short
```

## Benchmark Results

### Latest Results

Benchmark results are automatically published to GitHub Pages after each merge to `main`:

📊 **[View Interactive Charts](https://picea.github.io/Abies/dev/bench/)**

### Performance Targets

| Metric | Target | Rationale |
|--------|--------|-----------|
| Small DOM diff | < 500 ns | 60fps requires < 16.6ms per frame |
| Medium DOM diff | < 1 μs | Allow for complex layouts |
| Large DOM diff | < 5 μs | Data tables must remain responsive |
| Memory per operation | < 5 KB | Minimize GC pressure |

## Quality Gates

The CI pipeline enforces the following quality gates:

- **Warning threshold**: 150% - CI will comment on PR if performance degrades by 50%
- **Failure threshold**: 200% - CI will fail if performance degrades by 100%

## CI/CD Integration

Benchmarks run automatically on:
- Every push to `main` that modifies `Abies/` or `Abies.Benchmarks/`
- Every PR targeting `main` with changes to these paths

### How It Works

1. GitHub Actions runs benchmarks using BenchmarkDotNet
2. Results are exported to JSON format
3. `github-action-benchmark` compares results against historical data
4. Charts are published to GitHub Pages
5. Performance regressions trigger PR comments or workflow failures

## Understanding Results

### Key Metrics

- **Mean**: Average execution time
- **Error**: 99.9% confidence interval margin
- **StdDev**: Standard deviation showing consistency
- **Gen0/Gen1**: GC collections per 1000 operations
- **Allocated**: Memory allocated per operation

### Interpreting Changes

When evaluating a performance change:

1. **< 5% change**: Within noise margin, ignore
2. **5-20% change**: Worth investigating
3. **> 20% change**: Significant - review the code changes

## Adding New Benchmarks

1. Add a new method in `Abies.Benchmarks/DomDiffingBenchmarks.cs`:

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

```
Abies.Benchmarks/
├── DomDiffingBenchmarks.cs    # Virtual DOM benchmarks
├── UrlParsingBenchmarks.cs    # URL parsing benchmarks  
├── Program.cs                  # Entry point
└── Abies.Benchmarks.csproj    # Project file

.github/workflows/
└── benchmark.yml              # CI workflow

docs/
└── benchmarks.md              # This file
```

## Related Documentation

- [ADR-020: Benchmark Quality Gates](../adr/ADR-020-benchmark-quality-gates.md)
- [ADR-016: Virtual DOM Keyed Diffing](../adr/ADR-016-keyed-dom-diffing.md)
- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/articles/overview.html)
- [github-action-benchmark](https://github.com/benchmark-action/github-action-benchmark)
