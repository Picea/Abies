# Security Expert memory

- [PR-blocking security minimum](pr-blocking-security-minimum.md) — secrets, HIGH/CRITICAL SCA, and one SAST signal never move off the PR path.
- [Security gate ownership map](security-gate-ownership-map.md) — which workflow owns which gate; extend the owner rather than adding a parallel scan.
- [Path filter with nightly compensation](path-filter-with-nightly-compensation.md) — narrowing a heavy scan is only safe alongside an enforced full scheduled scan.
