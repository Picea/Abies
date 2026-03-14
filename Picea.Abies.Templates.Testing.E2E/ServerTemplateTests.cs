// =============================================================================
// ServerTemplateTests — E2E Tests for the "abies-server" Template
// =============================================================================
// Verifies that a project scaffolded from `dotnet new abies-server` actually
// works: builds, runs, serves the correct page, handles user interaction.
//
// Test pipeline:
//   dotnet pack → dotnet new install → dotnet new → patch csproj → build → run → Playwright
//
// The server template renders an InteractiveServer counter with:
//   - h1 "Counter"
//   - Badge "Server"
//   - +/−/Reset buttons
//   - abies-server.js served at /_abies/abies-server.js
// =============================================================================

using Microsoft.Playwright;
using Picea.Abies.Templates.Testing.E2E.Fixtures;
using Picea.Abies.Templates.Testing.E2E.Helpers;

namespace Picea.Abies.Templates.Testing.E2E;

/// <summary>
/// E2E tests verifying the abies-server template produces a working application.
/// </summary>
[Category("E2E")]
public class ServerTemplateTests : IAsyncDisposable
{
    [ClassDataSource<ServerTemplateFixture>(Shared = SharedType.PerTestSession)]
    public required ServerTemplateFixture Fixture { get; init; }

    private IPage _page = null!;

    [Before(Test)]
    public async Task SetUp()
    {
        _page = await Fixture.CreatePageAsync();
    }

    [After(Test)]
    public async Task TearDown()
    {
        if (_page is not null)
            await _page.Context.DisposeAsync();
    }

    public async ValueTask DisposeAsync() => GC.SuppressFinalize(this);

    // ─── Static Rendering Tests ───────────────────────────────────────

    [Test]
    public async Task InitialLoad_ShowsCounterHeading()
    {
        await _page.GotoAsync("/");

        await Assertions.Expect(
            _page.GetByRole(AriaRole.Heading, new() { Name = "Counter" }))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Test]
    public async Task InitialLoad_ShowsServerBadge()
    {
        await _page.GotoAsync("/");

        await Assertions.Expect(_page.Locator(".badge"))
            .ToHaveTextAsync("Server", new() { Timeout = 15_000 });
    }

    [Test]
    public async Task InitialLoad_ShowsZeroCount()
    {
        await _page.GotoAsync("/");

        await Assertions.Expect(_page.Locator(".count"))
            .ToHaveTextAsync("0", new() { Timeout = 15_000 });
    }

    [Test]
    public async Task AbiesServerJs_IsServed()
    {
        using var http = new HttpClient();
        var response = await http.GetAsync($"{Fixture.BaseUrl}/_abies/abies-server.js");

        await Assert.That(response.StatusCode)
            .IsEqualTo(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content.Length).IsGreaterThan(0);
    }

    // ─── Interactivity Tests ──────────────────────────────────────────

    [Test]
    public async Task Increment_UpdatesCount()
    {
        await _page.GotoAsync("/");
        await InteractivityHelpers.WaitForServerInteractivity(_page);

        await _page.GetByRole(AriaRole.Button, new() { Name = "+" }).ClickAsync();

        await Assertions.Expect(_page.Locator(".count"))
            .ToHaveTextAsync("1", new() { Timeout = 5_000 });
    }

    [Test]
    public async Task Decrement_UpdatesCount()
    {
        await _page.GotoAsync("/");
        await InteractivityHelpers.WaitForServerInteractivity(_page);

        await _page.GetByRole(AriaRole.Button, new() { Name = "\u2212" }).ClickAsync();

        await Assertions.Expect(_page.Locator(".count"))
            .ToHaveTextAsync("-1", new() { Timeout = 5_000 });
    }

    [Test]
    public async Task Reset_ClearsCount()
    {
        await _page.GotoAsync("/");
        await InteractivityHelpers.WaitForServerInteractivity(_page);

        var increment = _page.GetByRole(AriaRole.Button, new() { Name = "+" });
        await increment.ClickAsync();
        await increment.ClickAsync();
        await increment.ClickAsync();

        await Assertions.Expect(_page.Locator(".count"))
            .ToHaveTextAsync("3", new() { Timeout = 5_000 });

        await _page.GetByRole(AriaRole.Button, new() { Name = "Reset" }).ClickAsync();

        await Assertions.Expect(_page.Locator(".count"))
            .ToHaveTextAsync("0", new() { Timeout = 5_000 });
    }
}
