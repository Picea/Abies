---
name: performance-engineering
description: Performance engineering reference for .NET 10 / C# 14 / Aspire — BenchmarkDotNet recipes with [MemoryDiagnoser], k6 and NBomber load tests against the Aspire AppHost, dotnet-trace/dotnet-counters/dotnet-gcdump/PerfView profiling, what to watch for (allocations, EF pitfalls, serialization, Aspire overhead, GC, concurrency), performance budget templates, and load test report formats. Use when designing benchmarks, investigating regressions, setting performance budgets, or running a load test pass.
---

# Performance Engineering Reference

The Performance Engineer's deep reference. Role and protocol live in `.claude/agents/performance-engineer.md`. The squad's principles around evidence-driven optimization live in `.claude/docs/decisions.md` (Definition of Done, Reviewer dimensions).

The team's two non-negotiables for any performance work:
1. **Measure.** No optimization without a profile.
2. **Benchmark.** No claimed improvement without BenchmarkDotNet evidence.

---

## BenchmarkDotNet — The Microbenchmark Standard

Every hot path has a benchmark. Every benchmark has a baseline. CI compares against baseline.

### Standard Setup

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;

[MemoryDiagnoser]                                   // ALWAYS — track allocations
[ThreadingDiagnoser]                                // for any concurrent code
[SimpleJob(RuntimeMoniker.Net100, baseline: true)]
public class ArticlePublishingBenchmarks
{
    private DraftArticle _draft = null!;
    private FakeArticleRepository _repo = null!;
    private FakeClock _clock = null!;

    [GlobalSetup]
    public void Setup()
    {
        _draft = ArticleFixtures.RealisticDraft();
        _repo = FakeArticleRepository.Empty();
        _clock = FakeClock.At(DateTimeOffset.Parse("2026-01-15T00:00:00Z"));
    }

    [Benchmark(Baseline = true)]
    public async Task<Result<PublishedArticle, PublishArticleError>> Publish_with_current_workflow() =>
        await PublishArticleWorkflow.Run(
            new PublishArticleCommand(_draft.Id, _draft.Author.Id),
            _repo, _clock, CancellationToken.None);
}
```

**Rules:**
- `[MemoryDiagnoser]` is non-negotiable. Allocation counts are part of the result, not optional.
- `[ThreadingDiagnoser]` for anything async or concurrent — surfaces `Thread.Sleep`, lock contention, work item churn.
- Mark the baseline benchmark with `Baseline = true` so percentage deltas are meaningful.
- **Realistic inputs.** Trivial inputs (single-character strings, empty collections) overstate gains from branch elimination. Use representative production-shaped data.
- **Avoid measurement noise.** Don't allocate inside the hot loop unless you're measuring the allocation. Use `[GlobalSetup]` for fixture construction.

### Output Interpretation

BenchmarkDotNet reports include:
- `Mean`, `Error`, `StdDev` — confidence in the measurement. Don't chase improvements smaller than `Error`.
- `Allocated` — bytes allocated per operation. The most actionable column for .NET. `0 B` is the goal for hot paths.
- `Gen0`, `Gen1`, `Gen2` — collections per 1000 ops. Gen2 collections in a hot path are a problem.

### Common Anti-Patterns

- **Benchmarking in `Debug` build.** Always run `Release`. BenchmarkDotNet refuses to run on `Debug` for a reason.
- **No warmup.** BenchmarkDotNet handles this; hand-rolled `Stopwatch` loops don't.
- **Single iteration.** Statistical noise dominates. BenchmarkDotNet runs many iterations and reports confidence intervals.
- **Benchmark result without `[MemoryDiagnoser]`.** A perf claim without an allocation column is incomplete.

---

## Load Testing — k6 and NBomber

Microbenchmarks measure *function-level* performance. Load tests measure *system-level* behavior under load. Both are necessary.

### k6 (JavaScript-based, scripted)

```javascript
// load/article-publish.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 50 },   // ramp up
    { duration: '2m',  target: 50 },   // sustain
    { duration: '1m',  target: 200 },  // spike
    { duration: '2m',  target: 200 },  // sustain spike
    { duration: '30s', target: 0 },    // ramp down
  ],
  thresholds: {
    'http_req_duration{endpoint:publish}': ['p(95)<500', 'p(99)<1500'],
    'http_req_failed': ['rate<0.01'],
  },
};

export default function () {
  const res = http.post(
    `${__ENV.BASE_URL}/articles/${__ENV.ARTICLE_ID}/publish`,
    null,
    { tags: { endpoint: 'publish' } }
  );
  check(res, { 'status is 200': (r) => r.status === 200 });
  sleep(1);
}
```

Run against the Aspire AppHost:
```bash
dotnet run --project src/<Root>.AppHost &
sleep 30  # wait for startup
BASE_URL=http://localhost:5000 ARTICLE_ID=... k6 run load/article-publish.js
```

### NBomber (.NET-native, code-first)

```csharp
var publishScenario = Scenario.Create("article_publish", async ctx =>
    {
        using var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var response = await http.PostAsync($"/articles/{articleId}/publish", null);
        return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
    })
    .WithLoadSimulations(
        Simulation.RampingInject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
        Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(2)));

NBomberRunner.RegisterScenarios(publishScenario).Run();
```

**Rules:**
- Always run load tests against the **Aspire AppHost**, never against a stubbed HTTP server. The whole point is to exercise the real service-to-service path with real telemetry.
- Define thresholds (p95, p99, error rate) explicitly. Failed thresholds fail the build in CI.
- Ramp up gradually. Step-load is unrealistic and triggers JIT/connection-pool effects that don't reflect real traffic.

---

## Profiling — Diagnose, Don't Guess

When something is slow, profile to find out **where**. Don't guess.

### `dotnet-counters` — Live Snapshot

```bash
dotnet-counters monitor -p <PID> System.Runtime "Microsoft.AspNetCore.Hosting" "<Root>.Articles"
```
Shows GC pressure, threadpool starvation, request rate, custom event-counters from your `EventSource` instances.

### `dotnet-trace` — Sampling Profiler

```bash
dotnet-trace collect -p <PID> --profile cpu-sampling --duration 00:00:30
```
Outputs `.nettrace`. Open in PerfView or Visual Studio. Look for:
- Hot methods (where CPU is spent).
- Async stalls (threadpool starvation, lock contention).
- Unexpected method invocations on hot paths (logging, exception construction, reflection).

### `dotnet-gcdump` — Heap Snapshot

```bash
dotnet-gcdump collect -p <PID>
```
Outputs `.gcdump`. Open in Visual Studio or PerfView. Look for:
- Unexpectedly large object retention.
- LOH (Large Object Heap) pressure.
- Reference chains keeping objects alive (dictionaries, event handlers, statics).

### Aspire Dashboard

The Aspire dashboard (`http://localhost:18888` by default) shows traces, metrics, and logs end-to-end. **Check it first** — most performance investigations start by reading a slow request's trace and finding which span is dominant. Deep tools (`dotnet-trace`, PerfView) come second.

---

## What to Watch For (Allocation & Performance Catalog)

### Allocations

- **String concatenation in loops.** Use `StringBuilder` or interpolated string handlers (`DefaultInterpolatedStringHandler`) for hot paths.
- **`string.Format` / `$"..."` on hot paths.** Allocates the formatted string even if the result isn't used (e.g., debug logging without a level check). Use the logging source generators (`LoggerMessage` / `[LoggerMessage]`).
- **Boxing.** Storing value types in `object` references, dictionaries with `object` keys/values, `params object[]`. Use generic collections, source generators, or strongly-typed APIs.
- **LINQ on hot paths.** Each `.Where(...).Select(...)` creates iterator state machines. For hot paths with known shapes, hand-roll a loop or use `Span<T>`.
- **Closures capturing locals.** A lambda that captures a local creates a closure object. On hot paths, use static lambdas with state parameters: `dict.GetOrAdd(key, static (k, state) => ..., state)`.
- **Array allocations in loops.** Pool with `ArrayPool<T>.Shared.Rent` / `Return` (always in `try/finally`).

### EF Core Pitfalls

- **N+1 queries.** Iterate over a collection, hit the database in each iteration. Always `Include` relationships you'll access, or split into bulk queries.
- **Lazy loading.** Forbidden in this codebase. Reviewer flags it. Eager-load explicitly.
- **Fetching whole entities for projection.** If you only need three fields, project: `.Select(x => new { x.Id, x.Name, x.Status })`.
- **Tracking when not modifying.** `AsNoTracking()` on read queries — saves change-tracker overhead and memory.
- **`SaveChanges` in loops.** Batch updates with a single `SaveChanges` per logical unit of work.

### Serialization

- **`System.Text.Json` over `Newtonsoft.Json`.** STJ is faster, allocates less, and is the BCL default. Reviewer flags new uses of Newtonsoft.
- **Source-generated serializers (`[JsonSerializable]`).** Avoids reflection at runtime. Mandatory for AOT, beneficial elsewhere.
- **Avoid `JsonDocument.Parse` if you only need one value.** Use `Utf8JsonReader` for streaming reads.

### Aspire / OTEL Overhead

- **OTEL exporter backpressure.** If the exporter can't keep up, span batching falls behind. Check the dashboard's "spans dropped" metric.
- **Custom span tag explosion.** High-cardinality tags (user IDs, request IDs as tag values) blow up index size. Use them sparingly.
- **Logging level too verbose in production.** `Information` baseline, `Debug` only for targeted investigation.

### GC

- **Server GC for production, Workstation GC for desktop tools.** Server GC is the default for ASP.NET Core; verify it's on.
- **Background GC.** On by default; verify.
- **Large Object Heap.** Objects > ~85KB go to LOH. LOH compaction is expensive. For very large objects (buffers), pool aggressively.
- **Pinning.** `fixed` blocks and `GCHandle.Alloc(..., GCHandleType.Pinned)` block GC compaction. Use sparingly.

### Concurrency

- **Lock contention.** Profile with `dotnet-trace --profile gc-collect` and look at `Lock.Contention` events.
- **`async void`.** Forbidden except in event handlers. Reviewer flags.
- **`Thread.Sleep` in async code.** `await Task.Delay(...)` instead.
- **Threadpool starvation.** Long-running `Task.Run` work can block the threadpool. For long work, use a dedicated thread (`new Thread(...) { IsBackground = true }`).

---

## Performance Budgets

Set at design time with the architect. Format:

```markdown
# Performance Budget — Article Publishing

## Latency
- p50 < 50ms
- p95 < 200ms
- p99 < 500ms

## Throughput
- Sustained: 200 req/sec on staging hardware
- Burst: 1000 req/sec for 30s

## Resource
- Per-request allocations: < 4 KB
- Per-request DB queries: ≤ 2

## Startup
- Cold start: < 3s (CI environment)

## Validated By
- BenchmarkDotNet: `ArticlePublishingBenchmarks`
- k6 load test: `load/article-publish.js`
- Aspire dashboard verification on staging
```

Budgets are enforced in CI. A regression that crosses a budget threshold blocks merge.

---

## Load Test Report Format

```markdown
# Load Test — Article Publishing

**Date:** 2026-01-15
**Build:** abc1234
**Environment:** Aspire AppHost on CI runner (4 vCPU, 8 GB RAM)
**Tool:** k6 v0.50

## Scenario
Ramp 0→200 RPS over 30s, sustain 200 RPS for 2 min, spike to 500 RPS for 30s.

## Results
| Metric | Target | Actual | Verdict |
|---|---|---|---|
| p50 latency | < 50ms | 32ms | ✅ |
| p95 latency | < 200ms | 187ms | ✅ |
| p99 latency | < 500ms | 612ms | ❌ |
| Error rate | < 1% | 0.03% | ✅ |
| Throughput | 200 RPS | 198 RPS | ✅ (within noise) |

## Observations
- p99 latency exceeded budget during the 500 RPS spike.
- Aspire dashboard shows database connection pool saturation at the spike.
- GC pressure within bounds; no LOH growth.

## Recommended Actions
1. Increase DB connection pool max size (currently 50, recommended 100).
2. Add a regression benchmark for the publish workflow's DB-acquisition phase.
3. Re-test with the updated pool size.

## Reproduction
```bash
dotnet run --project src/<Root>.AppHost &
BASE_URL=http://localhost:5000 k6 run load/article-publish.js
```
```

---

## What This Skill Does NOT Cover

- **The decision of *whether* to optimize** — that's an architect/reviewer judgment based on whether a budget was violated or a hot path was identified. Profile first, optimize after.
- **Code review-side checks** for common anti-patterns — `code-review` skill.
- **Functional-DDD patterns** that affect allocation (smart constructors, Result types) — `functional-ddd` skill.
- **CI integration** of benchmark and load-test jobs — `cicd-pipelines` skill.
