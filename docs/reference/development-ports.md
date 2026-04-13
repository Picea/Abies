# Development Ports

Canonical local URLs and ports for Abies development.

If any guide shows a hardcoded URL, prefer this page as source of truth.

## Core Services

| Service | Default URL | Where used |
| --- | --- | --- |
| Conduit API | http://localhost:5179 | Local API runs, E2E tests, OTLP proxy smoke test |
| Conduit WASM frontend | http://localhost:5209 | Local browser app runs |
| Aspire Dashboard | https://localhost:17195 | Traces, logs, metrics in AppHost runs |
| js-framework-benchmark server | http://localhost:8080 | E2E framework benchmark harness |

## Notes

- AppHost-managed runs can emit dynamic endpoints in startup logs. Use those values when they differ.
- HTTPS may be required depending on host configuration.
- For tests, prefer environment-driven URLs where possible to avoid hardcoded assumptions.

## Related Docs

- [Contributing](../../CONTRIBUTING.md)
- [Performance Benchmarks](../benchmarks.md)
- [Tutorial 8: Distributed Tracing](../tutorials/08-tracing.md)
- [Debugging Guide](../guides/debugging.md)
