# Picea.Abies.Conduit

Domain and read-model layer for the Conduit RealWorld sample used by Abies.

This project is part of the full Conduit sample application and is used by frontend, API, and test projects.

## Purpose

- Defines core Conduit domain types and behavior
- Hosts read-model abstractions shared across Conduit services
- Serves as the common layer for the RealWorld implementation

## Related Projects

| Project | Role |
| --- | --- |
| `Picea.Abies.Conduit.App` | MVU frontend application logic |
| `Picea.Abies.Conduit.Wasm.Host` | WASM host for browser rendering |
| `Picea.Abies.Conduit.Server` | Server host for server rendering |
| `Picea.Abies.Conduit.Api` | REST API backend |
| `Picea.Abies.Conduit.AppHost` | Aspire orchestration |
| `Picea.Abies.Conduit.Testing.E2E` | Playwright end-to-end test suite |

## Run Locally

For local URLs and ports, use [Development Ports](../docs/reference/development-ports.md).

Recommended full-stack run:

```bash
dotnet run --project Picea.Abies.Conduit.AppHost
```

Alternative split run:

```bash
dotnet run --project Picea.Abies.Conduit.Api
dotnet run --project Picea.Abies.Conduit.Wasm.Host
```

## Specification

Conduit follows the RealWorld specification:

- https://docs.realworld.show/

## Testing

Run Conduit E2E tests:

```bash
dotnet test --project Picea.Abies.Conduit.Testing.E2E/Picea.Abies.Conduit.Testing.E2E.csproj -c Debug -v minimal
```

## Documentation

- Root docs index: [docs/index.md](../docs/index.md)
- Tracing tutorial: [docs/tutorials/08-tracing.md](../docs/tutorials/08-tracing.md)
- Benchmark docs: [docs/benchmarks.md](../docs/benchmarks.md)
