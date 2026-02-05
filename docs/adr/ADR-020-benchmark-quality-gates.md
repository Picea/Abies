# ADR-020: Benchmark Quality Gates

## Status

Accepted

## Context

The Abies framework relies on efficient Virtual DOM diffing for responsive UI updates. Performance regressions in the diffing algorithm directly impact user experience - a 2x slowdown in diffing could cause noticeable frame drops.

Without automated performance monitoring:
- Performance regressions can slip through code review unnoticed
- Developers lack visibility into the performance impact of their changes
- Historical performance data is not preserved for analysis
- There's no objective standard for acceptable performance levels

## Decision

We implement automated benchmark quality gates using:

1. **BenchmarkDotNet** for reliable .NET benchmarking with statistical rigor
2. **github-action-benchmark** for CI integration, historical tracking, and quality gates
3. **GitHub Pages** for hosting interactive benchmark result charts

### Quality Gate Thresholds

| Threshold | Value | Action |
|-----------|-------|--------|
| Warning | 150% of baseline | Comment on PR with performance comparison |
| Failure | 200% of baseline | Fail the PR check |

### Benchmark Scope

Benchmarks cover seven key scenarios:

1. **SmallDomDiff** - Typical component updates (2-3 elements)
2. **MediumDomDiff** - Page section updates (15-20 elements)
3. **LargeDomDiff** - Data table updates (150+ elements)
4. **AttributeOnlyDiff** - Style/class changes only
5. **TextOnlyDiff** - Text content changes
6. **NodeAdditionDiff** - List append operations
7. **NodeRemovalDiff** - List item deletions

### Workflow Triggers

Benchmarks run on:
- Push to `main` affecting `Abies/` or `Abies.Benchmarks/`
- PR targeting `main` affecting these paths

## Consequences

### Positive

- **Automated regression detection**: Performance issues caught before merge
- **Historical tracking**: Trend analysis over time
- **Developer awareness**: Clear feedback on performance impact of changes
- **Objective standards**: Defined thresholds remove subjective debates
- **Documentation**: Performance characteristics are documented and visible

### Negative

- **CI time increase**: Benchmarks add ~3-5 minutes to CI pipeline
- **False positives**: Statistical noise may occasionally trigger warnings
- **Threshold tuning**: Initial thresholds may need adjustment based on experience
- **GitHub Pages dependency**: Requires GitHub Pages to be enabled for the repository

### Mitigations

- Benchmarks only run when relevant code changes
- BenchmarkDotNet provides statistical analysis to reduce noise
- Thresholds are configurable in the workflow file
- Benchmark results are also stored as artifacts for local analysis

## Alternatives Considered

### 1. Manual performance testing

**Rejected**: Not scalable, easy to forget, no historical data.

### 2. Benchmark on every commit

**Rejected**: Too slow, wastes CI resources on unrelated changes.

### 3. Different threshold values

**Considered**: 200%/300% thresholds would catch only severe regressions. The chosen 150%/200% thresholds balance sensitivity with noise tolerance.

### 4. Self-hosted benchmark runner

**Rejected**: Adds operational complexity. GitHub-hosted runners provide sufficient consistency for relative comparisons.

## Implementation

- Workflow: `.github/workflows/benchmark.yml`
- Benchmarks: `Abies.Benchmarks/DomDiffingBenchmarks.cs`
- Documentation: `docs/benchmarks.md`

### Viewing Results

| Location | URL/Path | Content |
|----------|----------|---------|
| GitHub Pages | https://picea.github.io/Abies/dev/bench/ | Interactive charts with historical trends |
| PR Comments | Automatic on regression | Performance comparison vs baseline |
| Workflow Artifacts | Actions → Run → Artifacts | HTML, CSV, JSON reports |
| Local | `BenchmarkDotNet.Artifacts/results/` | Full reports after local run |

## References

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [github-action-benchmark](https://github.com/benchmark-action/github-action-benchmark)
- [GitHub Issue #16](https://github.com/Picea/Abies/issues/16)
- [ADR-016: Keyed DOM Diffing](ADR-016-keyed-dom-diffing.md)
