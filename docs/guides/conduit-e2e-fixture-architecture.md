# Conduit E2E Fixture Architecture

This guide documents how `Picea.Abies.Conduit.Testing.E2E` structures fixtures, seeds test data, and adds new user-journey coverage with minimal flakiness.

## Why this architecture exists

Conduit E2E tests need to validate user-visible behavior across multiple render modes while keeping startup cost manageable.

- Start backend infrastructure once per test run
- Reuse browser + app fixture setup per render mode
- Seed state through API instead of UI workflows
- Keep each test isolated with unique data

## Fixture layers

Conduit E2E uses a layered fixture model:

1. **Shared backend infrastructure** (`SharedInfra` + `ConduitInfraFixture`)
   - Starts Aspire stack once (API + backing services)
   - Exposes shared URLs (`ApiUrl`, `ServerUrl`, `WasmUrl`)
2. **Render-mode fixtures** (one per mode)
   - `ConduitServerFixture` (`InteractiveServer`)
   - `ConduitAppFixture` (`InteractiveWasm`)
   - `ConduitAutoFixture` (`InteractiveAuto`)
   - `ConduitStaticFixture` (`Static`)
   - Each starts its own Kestrel host and Playwright browser
3. **AppHost regression fixtures**
   - `ConduitAppHostServerFixture`
   - `ConduitAppHostWasmFixture`
   - Reuse AppHost-hosted frontends directly for regression coverage

## Test class pattern

Conduit tests use TUnit shared class fixtures:

```csharp
[Category("E2E")]
[ClassDataSource<ConduitAppFixture>(Shared = SharedType.Keyed, Key = "Conduit")]
[NotInParallel("Conduit")]
public sealed class FeedTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitAppFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public FeedTests(ConduitAppFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _seeder = new ApiSeeder(_fixture.ApiUrl);
    }

    public async ValueTask DisposeAsync() => await _page.Context.DisposeAsync();
}
```

Key points:

- Use the fixture matching your render-mode target
- Create a **new browser context per test** (`CreatePageAsync`) for isolation
- Instantiate `ApiSeeder` with `fixture.ApiUrl`
- Dispose page context in `DisposeAsync`

## Seeding strategy (API-first)

Use `ApiSeeder` for setup instead of building data through UI flows.

Common helpers:

- `RegisterUserAsync(...)`
- `LoginUserAsync(...)`
- `CreateArticleAsync(...)`
- `AddCommentAsync(...)`
- `FollowUserAsync(...)`
- `FavoriteArticleAsync(...)`
- `UpdateUserAsync(...)`

For eventual consistency in read models, wait explicitly:

- `WaitForProfileAsync(...)`
- `WaitForArticleAsync(...)`
- `WaitForArticleWithTitleAsync(...)`
- `WaitForArticleDeletedAsync(...)`

This keeps tests deterministic and avoids brittle `Task.Delay`-style waits.

## Adding a new user-journey test

Use this checklist when adding coverage:

1. **Pick the render mode**
   - Add to existing mode test file/folder (`Server/`, `Auto/`, root WASM tests, or `Static/`)
2. **Seed preconditions with `ApiSeeder`**
   - Create users/articles/comments through API
   - Use unique identifiers (`Guid.NewGuid():N`) to avoid collisions
3. **Wait for read visibility**
   - Call the relevant `WaitFor*` helper after writes
4. **Drive only the behavior under test through UI**
   - Navigate and interact with Playwright locators
5. **Assert user-visible outcomes**
   - Use `Assertions.Expect(...)` on visible UI state
6. **Keep tests independent**
   - No dependency on execution order or shared mutable state

## Render-mode-specific notes

- **InteractiveServer**: use `FillAndWaitForPatch(...)` helpers when input synchronization matters.
- **InteractiveWasm / InteractiveAuto**: call `WaitForWasmReady(...)` after navigation when the test depends on client takeover.
- **Static**: validate rendered HTML output only; no interactive behavior.

## Suggested template for new journeys

```csharp
[Test]
public async Task Journey_WhenCondition_ShouldShowExpectedOutcome()
{
    var username = $"journey{Guid.NewGuid():N}"[..20];
    var email = $"{username}@test.com";
    var user = await _seeder.RegisterUserAsync(username, email, "password123");

    var article = await _seeder.CreateArticleAsync(
        user.Token,
        $"Title {Guid.NewGuid():N}"[..30],
        "Description",
        "Body");
    await _seeder.WaitForArticleAsync(article.Slug);

    await _page.GotoAsync("/");
    await _page.WaitForWasmReady();

    await Assertions.Expect(_page.Locator(".article-preview").Filter(new() { HasText = article.Title }))
        .ToBeVisibleAsync();
}
```

## Related files

- `Picea.Abies.Conduit.Testing.E2E/Fixtures/ConduitInfraFixture.cs`
- `Picea.Abies.Conduit.Testing.E2E/Fixtures/ConduitServerFixture.cs`
- `Picea.Abies.Conduit.Testing.E2E/Fixtures/ConduitAppFixture.cs`
- `Picea.Abies.Conduit.Testing.E2E/Fixtures/ConduitAutoFixture.cs`
- `Picea.Abies.Conduit.Testing.E2E/Fixtures/ConduitStaticFixture.cs`
- `Picea.Abies.Conduit.Testing.E2E/Helpers/ApiSeeder.cs`
- `Picea.Abies.Conduit.Testing.E2E/Helpers/PageExtensions.cs`
