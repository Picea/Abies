---
name: benchmark
description: Dispatch the performance-engineer subagent to set up or run a BenchmarkDotNet harness on a target hot path, capture baseline+delta, and write a structured artifact. Use when the user types `/benchmark [target]`.
disable-model-invocation: true
allowed-tools: Read, Glob, Grep, Bash(git*), Bash(dotnet*)
---

# /benchmark — performance benchmark

Dispatches the `performance-engineer` subagent to design, run, and report a benchmark for a specified hot path. Writes the result to `.squad/decisions/inbox/perf-benchmark-<target>.md` (verdict `INFO` if delivering a measurement, or `NEEDS-CHANGES` if a regression is found).

## Arguments

- `target` — required. Identifies the hot path being benchmarked. Forms accepted:
  - Method name (`Article.Publish`)
  - File path (`src/Articles/PublishCommand.cs`)
  - Bounded context (`articles`)
  - PR number or branch name (e.g. `pr-142` to baseline-vs-delta a change)

## Procedure

1. **Map target → code.** Resolve the `target` argument to:
   - The hot path under measurement (one or more methods/types).
   - The existing benchmark project (typically `tests/<Context>.Benchmarks/`).
   - Any baseline measurements already on file (look for prior `perf-benchmark-*` decisions in `.claude/docs/decisions.md`).

2. **Build the brief** for the performance-engineer. Include:
   - The hot path and the existing benchmark project (or a request to scaffold one if none exists).
   - References to load: `.claude/agents/performance-engineer.md`, `.claude/skills/performance-engineering/SKILL.md`, `.claude/docs/decisions.md` (perf budgets), `.claude/docs/decision-schema.md`.
   - The current branch and commit; if comparing to a baseline, also the base commit.
   - The performance budget if one exists (mean, P95, allocations) — pulled from `decisions.md` or `tech-stack.md`.

3. **Dispatch the `performance-engineer` subagent.** The agent runs `dotnet run -c Release --project <BenchmarkProject>` (or equivalent) and captures BenchmarkDotNet output. It writes a decision drop with the canonical schema.

4. **Surface the result.** Short summary:
   - Mean / P95 / allocations at HEAD.
   - Delta vs. baseline (if applicable).
   - Pass/fail vs. budget (if a budget exists).
   - Path to full report (which includes raw BenchmarkDotNet table).

5. The scribe-decision-merger merges to `decisions.md` on `SubagentStop`.

## What the performance-engineer produces

- **Setup**: the benchmark code, with `[MemoryDiagnoser]` and the relevant config.
- **Methodology**: warm-up, iterations, JIT considerations, what was held constant.
- **Raw results**: BenchmarkDotNet's output table, quoted verbatim. Do not paraphrase numbers.
- **Interpretation**: what changed, by how much, and what it means.
- **Recommendation**: PASS (within budget), NEEDS-CHANGES (regression), or INFO (baseline established).

## What this skill does NOT do

- Does not write production code (only benchmark code, owned by performance-engineer).
- Does not modify performance budgets in `decisions.md`. Budget changes route through `/decide` or architect.
- Does not run load tests (k6/NBomber). Those are explicitly different from BenchmarkDotNet micro-benchmarks; ask the performance-engineer if you need that scope.

## Failure modes

- **Target argument missing:** ask before dispatching.
- **No benchmark project exists for the target:** the performance-engineer scaffolds one, with a `NEEDS-CHANGES` verdict if a baseline is required before merge per the budget rules in `decisions.md`.
- **`dotnet` not available:** report and exit; benchmarking requires the SDK.
- **Release build fails:** the agent reports the failure as a blocker rather than running the benchmark.
- **Comparing to a baseline that's stale or missing:** the agent records the missing-baseline as `INFO`, not `BLOCKED`, and recommends establishing one.
