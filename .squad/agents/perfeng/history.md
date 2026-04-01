# Performance Engineer — History

## About This File
Benchmark results, load test reports, profiling findings, and performance budgets. Read this before every session.

## Performance Budgets
*None yet — budgets per service will be established with the Architect.*

## Benchmark Baselines
| Benchmark | Metric | Baseline Value | Date |
|---|---|---|---|
| *None yet* | | | |

## Load Test History
| Date | Scenario | VUs | p50 | p95 | p99 | Throughput | Breaking Point |
|---|---|---|---|---|---|---|---|
| *None yet* | | | | | | | |

## Profiling Findings
*None yet — hot spots, allocation patterns, GC pressure observations tracked here.*

## Optimization Log
| Date | What | Before | After | Technique |
|---|---|---|---|---|
| *None yet* | | | | |

## Bottleneck Patterns
*None yet — reusable performance knowledge tracked here.*

## Learnings

### 2026-04-01: CI time-to-feedback bottlenecks and lane design
- Current PR critical path is dominated by `E2E` and `Benchmark`; recent runs show `Benchmark` often ~10-11 min and `E2E` ranging ~9-29 min.
- `Benchmark` workflow is required and currently starts on all PRs, but its heavy js-framework-benchmark execution is conditionally skipped unless perf-related triggers fire. This preserves branch protection while reducing unnecessary load.
- `PR Validation` has grown into a mixed lane (policy checks plus expensive template smoke tests, bundle publish, and security scans). It is the biggest opportunity for fast-lane extraction.
- There is duplicate security/template scanning between `pr-validation.yml` and standalone workflows (`template-security.yml`, `secrets-scan.yml`, `trivy.yml`, `semgrep.yml`, `zap-baseline.yml`), which increases runner spend and queue pressure.
- For regression signal quality, keep js-framework-benchmark threshold at 5% and maintain mainline E2E benchmark runs for baseline updates; do not replace E2E gating with micro-benchmarks.
- To avoid false confidence when moving checks off PR, use strict path-aware required checks, merge queue, nightly full-suite sweeps, and an auto-revert/escalation policy for post-merge failures.
