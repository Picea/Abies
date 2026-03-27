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

    [Test]
    public async Task InitialLoad_DefaultDebuggerStartup_ImportsAndMountsDebugger()
    {
        var debuggerResponseTask = _page.WaitForResponseAsync(response =>
            response.Url.Contains("/_abies/debugger.js", StringComparison.Ordinal)
            && response.Status == 200);

        await _page.GotoAsync("/");

        var debuggerResponse = await debuggerResponseTask;
        await Assert.That(debuggerResponse.Ok).IsTrue();

        await Assertions.Expect(_page.Locator("#abies-debugger-timeline"))
            .ToHaveAttributeAsync(
                "data-abies-debugger-adapter-initialized",
                "1",
                new() { Timeout = 15_000 });

        var debuggerEnabled = await _page.EvaluateAsync<bool>(
            "() => Boolean(window.__abiesDebugger && window.__abiesDebugger.enabled)");

        await Assert.That(debuggerEnabled).IsTrue();
    }

    [Test]
    public async Task InitialLoad_DebuggerBadgeClick_TogglesDebuggerPanel()
    {
        await _page.GotoAsync("/");

        var shell = _page.Locator("[data-abies-debugger-shell='1']");
        var panel = _page.Locator("[data-abies-debugger-panel='1']");

        await Assertions.Expect(shell).ToBeVisibleAsync(new() { Timeout = 30_000 });

        await shell.ClickAsync();
        await Assertions.Expect(panel).ToBeVisibleAsync(new() { Timeout = 10_000 });

        await shell.ClickAsync();
        await Assertions.Expect(panel).Not.ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    // ─── Interactivity Tests ──────────────────────────────────────────

    [Test]
    public async Task Increment_UpdatesCount()
    {
        await _page.GotoAsync("/");
        await InteractivityHelpers.WaitForServerInteractivity(_page);

        await _page.GetByRole(AriaRole.Button, new() { Name = "Increase" }).ClickAsync();

        await Assertions.Expect(_page.Locator(".count"))
            .ToHaveTextAsync("1", new() { Timeout = 5_000 });
    }

    [Test]
    public async Task Decrement_UpdatesCount()
    {
        await _page.GotoAsync("/");
        await InteractivityHelpers.WaitForServerInteractivity(_page);

        await _page.GetByRole(AriaRole.Button, new() { Name = "Decrease" }).ClickAsync();

        await Assertions.Expect(_page.Locator(".count"))
            .ToHaveTextAsync("-1", new() { Timeout = 5_000 });
    }

    [Test]
    public async Task Reset_ClearsCount()
    {
        await _page.GotoAsync("/");
        await InteractivityHelpers.WaitForServerInteractivity(_page);

        var increment = _page.GetByRole(AriaRole.Button, new() { Name = "Increase" });
        await increment.ClickAsync();
        await increment.ClickAsync();
        await increment.ClickAsync();

        await Assertions.Expect(_page.Locator(".count"))
            .ToHaveTextAsync("3", new() { Timeout = 5_000 });

        await _page.GetByRole(AriaRole.Button, new() { Name = "Reset" }).ClickAsync();

        await Assertions.Expect(_page.Locator(".count"))
            .ToHaveTextAsync("0", new() { Timeout = 5_000 });
    }

    [Test]
    public async Task DebuggerStepBackAndForward_ReplaysVisibleCounterState()
    {
        await _page.GotoAsync("/");
        await InteractivityHelpers.WaitForServerInteractivity(_page);

        var increment = _page.GetByRole(AriaRole.Button, new() { Name = "Increase" });
        await increment.ClickAsync();
        await increment.ClickAsync();
        await Assertions.Expect(_page.Locator(".count"))
            .ToHaveTextAsync("2", new() { Timeout = 5_000 });

        var shell = _page.Locator("[data-abies-debugger-shell='1']");
        await shell.ClickAsync();

        await _page.GetByRole(AriaRole.Button, new() { Name = "Back" }).ClickAsync();
        await Assertions.Expect(_page.Locator(".count"))
            .ToHaveTextAsync("1", new() { Timeout = 5_000 });

        await _page.GetByRole(AriaRole.Button, new() { Name = "Step" }).ClickAsync();
        await Assertions.Expect(_page.Locator(".count"))
            .ToHaveTextAsync("2", new() { Timeout = 5_000 });
    }
}
