# Testing

Abies is designed to keep domain logic pure, which makes tests fast and
predictable.

## Unit tests

`Abies.Tests` contains tests for routing, parsing, and DOM behavior.

```bash
dotnet test Abies.Tests
```

## Integration tests (near-E2E)

`Abies.Conduit.IntegrationTests` runs deterministic tests against the Conduit
UI logic by injecting a fake `HttpClient`.

```bash
dotnet test Abies.Conduit.IntegrationTests
```

## E2E tests

`Abies.Conduit.E2E` contains Playwright tests that validate basic browser
behavior for the Conduit app.

```bash
dotnet test Abies.Conduit.E2E
```

## What to test where

- `Update` logic: unit tests (pure functions)
- `View` output: deterministic DOM tests
- Side effects: integration tests with fake services
- Full browser behavior: small E2E smoke suite
