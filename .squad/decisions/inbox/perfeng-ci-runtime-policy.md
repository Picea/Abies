### 2026-04-01T00:00:00Z: CI Runtime Policy — Staged Fast/Full Lanes
**By:** Maurice Cornelius Gerardus Petrus Peters (via Performance Engineer)
**What:** Adopt a staged CI policy with a <10 minute fast-feedback PR lane, a push/main full-confidence lane, and nightly deep validation lane. Keep js-framework-benchmark as the authoritative performance gate with a 5% regression threshold.
**Why:** Recent run history shows PR time-to-feedback is dominated by long-running E2E and benchmark workflows. A staged policy preserves signal while reducing median PR wait time and runner cost.

Proposed policy points:
- PR fast lane: metadata checks, changed-files lint/format, targeted unit/integration, secrets scan, lightweight static analysis, and conditional performance smoke (selected js-framework-benchmark scenarios only for perf-sensitive changes).
- Push/main full lane: full E2E suite, full js-framework-benchmark suite + baseline compare, packaging, container/image scans, full SCA, and publication tasks.
- Nightly lane: authenticated DAST (ZAP), full template security smoke, dependency drift/vuln audits, and longer stress/perf trend checks.
- False-confidence controls: path-based required checks, merge queue, post-merge failure alerts tied to owning area, and rollback/escalation playbook.
- Cache/artifact strategy: centralize setup and restore caches for .NET, npm, Playwright, WASM publish outputs, and template package feed artifacts to remove duplicated work across workflows.
