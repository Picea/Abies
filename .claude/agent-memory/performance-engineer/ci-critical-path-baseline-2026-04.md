---
name: ci-critical-path-baseline-2026-04
description: Historical 2026-04-01 measurement of PR time-to-feedback — Benchmark ~10-11 min, E2E ~9-29 min, with pr-validation identified as the mixed lane
metadata:
  type: project
---

Measured on 2026-04-01, when the staged CI lane policy was designed:

- PR critical path was dominated by `Benchmark` (~10–11 min) and `E2E`
  (~9–29 min, high variance).
- `PR Validation` had grown into a mixed lane — policy checks alongside
  expensive template smoke tests, bundle publish and security scans — and was
  identified as the single biggest fast-lane extraction opportunity.
- Security and template scanning was duplicated between `pr-validation.yml` and
  the standalone `template-security.yml`, `secrets-scan.yml`, `trivy.yml`,
  `semgrep.yml` and `zap-baseline.yml` workflows, inflating runner spend and
  queue pressure.

**Why:** This is the evidence base behind the "CI Runtime Policy — staged
fast/full/nightly lanes" decision. Keeping the numbers means a future
"CI is slow" complaint can be compared against a known starting point instead of
re-measured from scratch.

**How to apply:** Treat these as a dated baseline, **not** current state — they
have not been re-measured since 2026-04-01 and the lane policy has been applied
in the meantime. Re-measure before quoting them. The duplication observation in
particular was partly resolved: `cd.yml` no longer runs SCA on pull requests.
Related: [[security-gate-ownership-map]] in the Security Expert's memory.
