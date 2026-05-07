# Conduit E2E Fixture Architecture

This guide documents how `Picea.Abies.Conduit.Testing.E2E` is structured and how to add new user-journey coverage without introducing flaky setup.

## Goals

- Start infrastructure once, reuse it safely across test classes.
- Keep tests deterministic by seeding through API calls, not UI setup flows.
- Keep coverage organized by render mode (WASM, InteractiveServer, InteractiveAuto, Static, AppHost regression).

## Fixture Stack

The fixture model is layered:

1. `SharedInfra` singleton starts Aspire infrastructure once per test run.
2. Mode-specific fixtures start one frontend host per render mode and provide `CreatePageAsync()`.
3. Test classes share fixtures through TUnit `[ClassDataSource(..., Shared = SharedType.Keyed, Key = ...)]`.

### Shared infrastructure fixture

`Fixtures/ConduitInfraFixture.cs` starts:

- `conduit-api`
- `kurrentdb`
- `postgres`
- AppHost-managed frontend endpoints (`conduit-server`, `conduit-wasm`)

`SharedInfra.GetAsync()` uses `Lazy<Task<ConduitInfraFixture>>` so startup happens exactly once.

### Mode fixtures

| Fixture | Render mode / host | Notes |
| --- | --- | --- |
| `ConduitAppFixture` | InteractiveWasm | Self-hosted Kestrel + reverse proxy + WASM AppBundle |
| `ConduitServerFixture` | InteractiveServer | Self-hosted Kestrel + WebSocket sessions + reverse proxy |
| `ConduitAutoFixture` | InteractiveAuto | Self-hosted Kestrel with server-first handoff |
| `ConduitStaticFixture` | Static | Self-hosted Kestrel static render checks |
| `ConduitAppHostServerFixture` | AppHost `conduit-server` | Regression coverage against AppHost wiring |
| `ConduitAppHostWasmFixture` | AppHost `conduit-wasm` | Regression coverage against AppHost wiring |

All fixtures expose:

- `BaseUrl` for page navigation
- `ApiUrl` for deterministic seeding
- `CreatePageAsync()` for an isolated Playwright context per test

## Seeding Strategy

Seeding is API-first through `Helpers/ApiSeeder.cs`.

### Why API seeding

- Faster than UI setup
- Less flaky than multi-step UI prerequisites
- Keeps each test independent and order-agnostic

### Seeder capabilities

`ApiSeeder` currently supports:

- user registration and login
- article creation and comment creation
- follow/favorite operations
- profile updates
- read-after-write wait helpers (`WaitForProfileAsync`, `WaitForArticleAsync`, `WaitForArticleWithTitleAsync`, `WaitForArticleDeletedAsync`)

`SendWithRetryAsync` handles transient startup failures and retries 5xx/network errors.

## Test Class Pattern

Follow this pattern for new classes:

```csharp
[Category("E2E")]
[ClassDataSource<ConduitAppFixture>(Shared = SharedType.Keyed, Key = "Conduit")]
[NotInParallel("Conduit")]
public sealed class ExampleTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitAppFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public ExampleTests(ConduitAppFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _seeder = new ApiSeeder(_fixture.ApiUrl);
    }

    public async ValueTask DisposeAsync() => await _page.Context.DisposeAsync();
}
```

### Render-mode conventions

- WASM tests call `WaitForWasmReady()` before interactive assertions.
- InteractiveServer tests use `FillAndWaitForPatch(...)` where server-patch timing matters.
- InteractiveAuto tests validate functionality after handoff conditions are possible.
- Static tests validate rendered output only (no runtime interaction assumptions).

## Adding New User-Journey Coverage

Use this checklist when adding a RealWorld journey.

1. Identify the user journey and expected behavior from https://docs.realworld.show/.
2. Decide which render modes need the journey.
3. Place tests in the correct folder:
   - root: WASM baseline journeys
   - `Server/`: InteractiveServer equivalents
   - `Auto/`: InteractiveAuto equivalents
   - `Static/`: static-render behavior
   - `AppHost/`: AppHost-specific regressions
4. Seed prerequisites through `ApiSeeder` instead of UI flows.
5. Assert user-visible outcomes only (URL, shell state, form state, visible text).
6. Keep one journey per test method.
7. Add matching integration coverage in non-E2E projects for core transition/interpreter logic.

## Practical Example: Porting a Journey Across Modes

When adding a new journey such as "edit article":

1. Implement baseline in a WASM-oriented class (for example in the root folder).
2. Add InteractiveServer equivalent under `Server/` and use patch-aware helpers.
3. Add InteractiveAuto equivalent under `Auto/` if handoff behavior can affect the journey.
4. Add Static assertions under `Static/` only if the journey has meaningful static output expectations.

## Related Docs

- [Testing guide](./testing.md)
- [Conduit README](../../Picea.Abies.Conduit/README.md)
- [Render modes](../concepts/render-modes.md)
- [Real-World app tutorial](../tutorials/07-real-world-app.md)
