# Testing

Abies is designed to keep domain logic pure, which makes tests fast and
predictable.

## Unit tests

`Abies.Tests` contains tests for routing, parsing, and DOM behavior.

```bash
dotnet test Abies.Tests
```

## Integration tests

`Abies.Conduit.IntegrationTests` runs deterministic tests against the Conduit
UI logic by injecting a fake `HttpClient`.

```bash
dotnet test Abies.Conduit.IntegrationTests
```

## What to test where

- `Update` logic: unit tests (pure functions)
- `View` output: deterministic DOM tests
- Side effects: integration tests with fake services
