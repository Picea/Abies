# Render StringBuilder Pool Cap Validation (2026-04-02)

## Summary

Validated a Render pipeline optimization in `Picea.Abies/Render.cs`:

- Keep thread-safe pool: `ConcurrentStack<StringBuilder>`
- Increase pool retention cap: `MaxPooledStringBuilderCapacity` from `8192` to `4 * 1024 * 1024`

Goal: reduce large-render StringBuilder growth churn while keeping server-side thread safety.

## Why this change

For 1000-row renders, output size is much larger than 8 KB. With an 8 KB pool cap, grown builders were not returned to the pool. This caused repeated growth allocations on each render.

## Method

Power state and session controls:

- Machine on AC power
- Same-session A/B baseline against `main`
- Same benchmark harness and browser flow

Benchmark layers used:

1. BenchmarkDotNet microbenchmarks in `Picea.Abies.Benchmarks`
2. End-to-end `js-framework-benchmark` in `../js-framework-benchmark-fork`

## Microbenchmark results

`RenderingBenchmarks.Render1kBenchmarkRows`

- Before cap increase: 660.8 us, 2.44 MB allocated
- After cap increase (thread-safe pool retained): 356.8 us, 1.22 MB allocated
- Change: about 46% faster, about 50% less allocation

Attribution split (new benchmark variant):

- `Render1kBenchmarkRows`: 326.9 us, 1.21 MB
- `Render1kBenchmarkRowsNoHandlers`: 304.6 us, 1.12 MB

Interpretation:

- Handler attributes are a small part of the remaining cost.
- Most remaining allocation is base HTML emission plus final string materialization.

## End-to-end results (same-session full suite vs main baseline)

All 9 duration benchmarks were run using `js-framework-benchmark`.

| Benchmark | Current Total | Main Total | Total Delta | Current Script | Main Script | Script Delta |
| --- | --- | --- | --- | --- | --- | --- |
| Create 1,000 rows | 123.6 ms | 122.4 ms | +0.98% | 96.5 ms | 96.3 ms | +0.21% |
| Replace 1,000 rows | 129.9 ms | 127.4 ms | +1.96% | 103.7 ms | 101.1 ms | +2.57% |
| Update every 10th row x16 | 83.9 ms | 81.5 ms | +2.94% | 65.3 ms | 63.3 ms | +3.16% |
| Select row | 14.1 ms | 13.8 ms | +2.17% | 8.3 ms | 8.5 ms | -2.35% |
| Swap rows | 33.0 ms | 32.8 ms | +0.61% | 11.8 ms | 12.0 ms | -1.67% |
| Remove row | 20.3 ms | 20.3 ms | 0.00% | 5.2 ms | 5.4 ms | -3.70% |
| Create 10,000 rows | 1195.4 ms | 1201.4 ms | -0.50% | 927.2 ms | 926.6 ms | +0.06% |
| Append 1,000 rows | 135.7 ms | 135.1 ms | +0.44% | 104.7 ms | 104.2 ms | +0.48% |
| Clear 1,000 rows x8 | 19.4 ms | 18.9 ms | +2.65% | 17.3 ms | 16.6 ms | +4.22% |

Geometric mean (total medians): `1.0124x` (+1.24% vs main).

Interpretation:

- 7 of 9 total-duration benchmarks are slower, 1 is neutral, 1 is faster.
- All deltas are below the 5% regression threshold.
- The optimization clearly improves Render microbenchmarks but does not translate into net E2E wins across the full suite.

## Conclusions

1. The optimization is safe for both WASM and server rendering because thread-safe pooling is preserved.
2. The cap increase provides measurable allocation and CPU wins in Render microbenchmarks.
3. Full-suite E2E impact is mixed: geometric mean is +1.24% slower vs main, but still within the project's 5% threshold.

## Files changed in this validation

- `Picea.Abies/Render.cs`
- `Picea.Abies.Benchmarks/RenderingBenchmarks.cs`
