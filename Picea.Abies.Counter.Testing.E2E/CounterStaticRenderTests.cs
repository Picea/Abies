// =============================================================================
// Counter Static Render E2E Tests — Static Mode
// =============================================================================
// Tests the Static render mode — server renders HTML once, no interactivity.
//
// In Static mode:
//   - The page is a one-shot server render of the initial model
//   - No WebSocket connection, no WASM download
//   - No bootstrap scripts are injected
//   - Buttons are rendered but clicking them does nothing
//
// These tests verify that the server-rendered HTML is structurally correct
// and that no interactive infrastructure (scripts, WebSocket) is present.
// =============================================================================

using Picea.Abies.Counter.Testing.E2E.Fixtures;
using Microsoft.Playwright;

namespace Picea.Abies.Counter.Testing.E2E;

[Trait("Category", "E2E")]
[Collection("CounterStatic")]
public sealed class CounterStaticRenderTests : IAsyncLifetime
{
    private readonly CounterStaticFixture _fixture;
    private IPage _page = null!;

    public CounterStaticRenderTests(CounterStaticFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
    }

    public async Task DisposeAsync()
    {
        await _page.Context.DisposeAsync();
    }

    [Fact]
    public async Task InitialLoad_ShouldShowServerRenderedCounter()
    {
        await _page.GotoAsync("/");

        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Abies Counter" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        await Expect(_page.Locator(".count"))
            .ToHaveTextAsync("0", new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task StaticMode_ShouldRenderAllButtons()
    {
        await _page.GotoAsync("/");

        // All buttons should be visible in the server-rendered HTML
        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "+" }))
            .ToBeVisibleAsync(new() { Timeout = 5_000 });

        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "\u2212" }))
            .ToBeVisibleAsync(new() { Timeout = 5_000 });

        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Reset" }))
            .ToBeVisibleAsync(new() { Timeout = 5_000 });
    }

    [Fact]
    public async Task StaticMode_ClickingShouldNotChangeCount()
    {
        await _page.GotoAsync("/");

        await Expect(_page.Locator(".count"))
            .ToHaveTextAsync("0", new() { Timeout = 5_000 });

        // Click the increment button — should have no effect in Static mode
        await _page.GetByRole(AriaRole.Button, new() { Name = "+" }).ClickAsync();
        await Task.Delay(500); // Small wait to ensure nothing changes

        await Expect(_page.Locator(".count"))
            .ToHaveTextAsync("0");
    }

    [Fact]
    public async Task StaticMode_ShouldNotIncludeInteractiveScripts()
    {
        await _page.GotoAsync("/");

        // Static mode should have no WebSocket or WASM bootstrap scripts
        var scriptCount = await _page.Locator("script[src*='abies-server']").CountAsync();
        Assert.Equal(0, scriptCount);

        var wasmScriptCount = await _page.Locator("script:has-text('dotnet')").CountAsync();
        Assert.Equal(0, wasmScriptCount);
    }

    [Fact]
    public async Task StaticMode_ShouldServeValidHtmlDocument()
    {
        await _page.GotoAsync("/");

        // Should be a valid HTML5 document with proper structure
        await Expect(_page.Locator("html")).ToBeAttachedAsync();
        await Expect(_page.Locator("head")).ToBeAttachedAsync();
        await Expect(_page.Locator("body")).ToBeAttachedAsync();

        // Should have the Counter title
        var title = await _page.TitleAsync();
        Assert.Equal("Abies Counter", title);
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
