// =============================================================================
// BrowserTemplateTests — E2E Tests for the "abies-browser" Template
// =============================================================================
// Verifies that a project scaffolded from `dotnet new abies-browser` actually
// works: builds, runs via the WebAssembly SDK dev server, loads in a browser,
// and responds to user interaction.
//
// The browser template renders a standalone WASM counter with:
//   - h1 "Counter"
//   - Badge "Demo"
//   - +/− buttons (NO Reset)
//   - abies.js served from wwwroot
// =============================================================================

using Microsoft.Playwright;
using Picea.Abies.Templates.Testing.E2E.Fixtures;
using Picea.Abies.Templates.Testing.E2E.Helpers;

namespace Picea.Abies.Templates.Testing.E2E;

/// <summary>
/// E2E tests verifying the abies-browser template produces a working application.
/// </summary>
[Category("E2E")]
public class BrowserTemplateTests : IAsyncDisposable
{
    [ClassDataSource<BrowserTemplateFixture>(Shared = SharedType.PerTestSession)]
    public required BrowserTemplateFixture Fixture { get; init; }

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

    private static ILocator CounterValueLocator(IPage page) =>
        page.Locator(".count, .counter-value");

    private static async Task ClickFirstAvailableButton(IPage page, params string[] buttonNames)
    {
        foreach (var buttonName in buttonNames)
        {
            var button = page.GetByRole(AriaRole.Button, new() { Name = buttonName });
            if (await button.CountAsync() > 0)
            {
                await button.First.ClickAsync();
                return;
            }
        }

        throw new InvalidOperationException(
            $"Could not find any button with names: {string.Join(", ", buttonNames)}");
    }

    // ─── Static Rendering Tests ───────────────────────────────────────

    [Test]
    public async Task InitialLoad_ShowsCounterHeading()
    {
        await _page.GotoAsync("/");

        await Assertions.Expect(
            _page.GetByRole(AriaRole.Heading, new() { Name = "Counter" }))
            .ToBeVisibleAsync(new() { Timeout = 30_000 });
    }

    [Test]
    public async Task InitialLoad_ShowsDemoBadge()
    {
        await _page.GotoAsync("/");

        await Assertions.Expect(_page.Locator(".badge"))
            .ToHaveTextAsync("Demo", new() { Timeout = 30_000 });
    }

    [Test]
    public async Task InitialLoad_ShowsZeroCount()
    {
        await _page.GotoAsync("/");

        await Assertions.Expect(CounterValueLocator(_page))
            .ToHaveTextAsync("0", new() { Timeout = 30_000 });
    }

    [Test]
    public async Task AbiesJs_IsServed()
    {
        using var http = new HttpClient();
        var response = await http.GetAsync($"{Fixture.BaseUrl}/abies.js");

        await Assert.That(response.StatusCode)
            .IsEqualTo(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content.Length).IsGreaterThan(0);
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
        await InteractivityHelpers.WaitForWasmInteractivity(_page);

        await ClickFirstAvailableButton(_page, "+", "Increase");

        await Assertions.Expect(CounterValueLocator(_page))
            .ToHaveTextAsync("1", new() { Timeout = 5_000 });
    }

    [Test]
    public async Task Decrement_UpdatesCount()
    {
        await _page.GotoAsync("/");
        await InteractivityHelpers.WaitForWasmInteractivity(_page);

        await ClickFirstAvailableButton(_page, "-", "Decrease");

        await Assertions.Expect(CounterValueLocator(_page))
            .ToHaveTextAsync("-1", new() { Timeout = 5_000 });
    }
}
