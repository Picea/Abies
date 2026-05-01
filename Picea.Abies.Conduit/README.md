# Picea.Abies.Conduit

Conduit is the Abies reference implementation of the RealWorld specification.

- RealWorld spec: https://docs.realworld.show/
- RealWorld upstream: https://github.com/gothinkster/realworld

This project contains the core Conduit domain and read-model contracts used by the API, app logic, and hosts.

## Project Layout

Conduit is split across several projects. Keep the domain model and read model contracts in this project, and keep host-specific behavior in host projects.

| Project | Purpose |
| --- | --- |
| `Picea.Abies.Conduit` | Domain model and read-model contracts |
| `Picea.Abies.Conduit.App` | MVU application program, model, messages, commands, views |
| `Picea.Abies.Conduit.Api` | RealWorld REST API |
| `Picea.Abies.Conduit.Wasm` | Browser WASM app entry point |
| `Picea.Abies.Conduit.Wasm.Host` | ASP.NET Core host for InteractiveWasm |
| `Picea.Abies.Conduit.Server` | ASP.NET Core host for InteractiveServer |
| `Picea.Abies.Conduit.AppHost` | .NET Aspire local orchestration |
| `Picea.Abies.Conduit.Testing.E2E` | Playwright end-to-end test suite |

## Running Locally

### Recommended: AppHost (full stack)

Run the full Conduit stack (API, database infrastructure, server host, wasm host, dashboard) through Aspire:

```bash
dotnet run --project Picea.Abies.Conduit.AppHost/Picea.Abies.Conduit.AppHost.csproj
```

### API + WASM directly

Run API and WASM frontend in separate terminals:

```bash
# Terminal 1
dotnet run --project Picea.Abies.Conduit.Api --no-build --urls http://localhost:5179

# Terminal 2
dotnet run --project Picea.Abies.Conduit.Wasm --no-build
```

### Optional: server host directly

```bash
dotnet run --project Picea.Abies.Conduit.Server
```

## Test Strategy

Conduit follows layered testing.

| Layer | Project | Focus |
| --- | --- | --- |
| Unit and integration | `Picea.Abies.Conduit.Tests` | MVU transitions, routing, interpreter behavior |
| API integration | `Picea.Abies.Conduit.Api.Tests` | Endpoint behavior and API contracts |
| Render-mode E2E | `Picea.Abies.Conduit.Testing.E2E` | User journeys across WASM, server, static, and auto modes |

Baseline commands:

```bash
dotnet test Picea.Abies.Conduit.Tests
dotnet test Picea.Abies.Conduit.Api.Tests
dotnet test Picea.Abies.Conduit.Testing.E2E/Picea.Abies.Conduit.Testing.E2E.csproj -c Debug -v minimal
```

All user journeys should be covered by integration tests and E2E tests.

## Related Documentation

- [Conduit tutorial walkthrough](../docs/tutorials/07-real-world-app.md)
- [Conduit E2E fixture architecture](../docs/guides/conduit-e2e-fixture-architecture.md)
- [Testing guide](../docs/guides/testing.md)
- [Project structure](../docs/getting-started/project-structure.md)
- [Render modes](../docs/concepts/render-modes.md)
