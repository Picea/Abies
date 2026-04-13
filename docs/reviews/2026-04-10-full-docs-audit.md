# Full Documentation Audit — 2026-04-10

This review covers consistency, freshness, and coverage gaps across the documentation set.

## Policy Applied

- Docs describe intended behavior.
- If implementation is not complete yet, docs and ADRs link to an issue that tracks delivery.
- One canonical local port/URL source is maintained in [Development Ports](../reference/development-ports.md).

## High-Priority Findings Addressed

1. Performance headline mismatch
- Updated [Performance Guide](../guides/performance.md) to point to [Performance Benchmarks](../benchmarks.md) as canonical metrics source.

2. OTel runtime API and config completeness mismatch
- Added intended-behavior issue links in:
  - [Tutorial 8: Tracing](../tutorials/08-tracing.md)
  - [Debugging Guide](../guides/debugging.md)
  - [ADR-015](../adr/ADR-015-tracing-verbosity.md)
- Tracking issues:
  - Runtime API completion: https://github.com/Picea/Abies/issues/214
  - Meta tag support completion: https://github.com/Picea/Abies/issues/212

3. ADR broken reference
- Updated [ADR-022](../adr/ADR-022-picea-ecosystem-migration.md) to reference tracking issue instead of missing local file.
- Tracking issue: https://github.com/Picea/Abies/issues/215

4. Contributing/security workflow docs drift
- Updated benchmark trigger docs in [CONTRIBUTING.md](../../CONTRIBUTING.md) to match workflow behavior.
- Updated SCA behavior in [Security Scanning Implementation](../security-scanning-implementation.md) to reflect fail-on-high/critical behavior.

## Standardization

- Added canonical port table: [Development Ports](../reference/development-ports.md)
- Linked in:
  - [Docs Index](../index.md)
  - [Tutorial 8: Tracing](../tutorials/08-tracing.md)
  - [Debugging Guide](../guides/debugging.md)
  - [README](../../README.md)

## Coverage Gaps Tracked

- Conduit project README coverage: https://github.com/Picea/Abies/issues/213

## Remaining Recommended Backlog

- Browser runtime API reference (public JS surface)
- Binary protocol contributor maintenance guide
- E2E fixture architecture guide
- InteractiveServer/InteractiveAuto lifecycle deep reference

## Notes

This document is a point-in-time audit snapshot and should be refreshed after issue #212, #214, and #215 are closed.
