# Performance Engineer memory

- [CI critical path baseline 2026-04](ci-critical-path-baseline-2026-04.md) — dated timings behind the staged-lane policy; re-measure before quoting.
- [Required check with conditional heavy work](required-check-with-conditional-heavy-work.md) — keep the job required, skip its body; saves runner time without touching branch protection.
- [E2E gating is not replaceable by micro-benchmarks](e2e-gating-is-not-replaceable-by-microbenchmarks.md) — js-framework-benchmark at 5% stays the authoritative gate.
- [Compensating controls when moving checks off PR](compensating-controls-when-moving-checks-off-pr.md) — the four-part package that has to travel with any de-PR'd check.
