---
name: required-check-with-conditional-heavy-work
description: To cut CI cost without touching branch protection, keep the job required on all PRs and gate only its expensive steps behind a should-run condition
metadata:
  type: feedback
---

The `Benchmark` workflow starts on every PR — so it always reports and branch
protection stays satisfied — but its expensive js-framework-benchmark run is
gated behind a `should-run` step that fires only on perf-relevant path changes
or the `performance` / `ui` labels. Every heavy step carries
`if: steps.should-run.outputs.run == 'true'`.

**Why:** Adopted 2026-04-01. Removing a job from PRs to save runner time means
editing branch protection and losing a required check, which is a much bigger
decision than it looks and is easy to forget to reverse. Keeping the check
required and skipping its body gets nearly all the savings with none of the
protection risk.

**How to apply:** Reach for this whenever asked to "take X off PRs for speed".
It is the cheap answer, and it does not need anyone to touch repository
settings. It does *not* apply to checks whose absence is itself the risk — see
[[compensating-controls-when-moving-checks-off-pr]] for what to add when work
genuinely moves off the PR path.

Verified 2026-09-02: `.github/workflows/benchmark.yml` still uses the
`should-run` gate with path- and label-driven conditions.
