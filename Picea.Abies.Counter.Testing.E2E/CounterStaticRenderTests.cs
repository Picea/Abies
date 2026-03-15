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

using Microsoft.Playwright;
using Picea.Abies.Counter.Testing.E2E.Fixtures;

namespace Picea.Abies.Counter.Testing.E2E;

[Category("E2E")]
[ClassDataSource<CounterStaticFixture>(Shared = SharedType.Keyed, Key = "CounterStatic")]
[NotInParallel("CounterStatic")]
public sealed class CounterStaticRenderTests : IAsyncInitializer, IAsyncDisposable
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

    public async ValueTask DisposeAsync()
    {
        await _page.Context.DisposeAsync();
    }

    [Test]
    public async Task InitialLoad_ShouldShowServerRenderedCounter()
    {
        await _page.GotoAsync("/");

        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Abies Counter" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        await Expect(_page.Locator(".count"))
            .ToHaveTextAsync("0", new() { Timeout = 10_000 });
    }

    [Test]
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

    [Test]
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

    [Test]
    public async Task StaticMode_ShouldNotIncludeInteractiveScripts()
    {
        await _page.GotoAsync("/");

        // Static mode should have no WebSocket or WASM bootstrap scripts
        var scriptCount = await _page.Locator("script[src*='abies-server']").CountAsync();
        await Assert.That(scriptCount).IsEqualTo(0);

        var wasmScriptCount = await _page.Locator("script:has-text('dotnet')").CountAsync();
        await Assert.That(wasmScriptCount).IsEqualTo(0);
    }

    [Test]
    public async Task StaticMode_ShouldServeValidHtmlDocument()
    {
        await _page.GotoAsync("/");

        // Should be a valid HTML5 document with proper structure
        await Expect(_page.Locator("html")).ToBeAttachedAsync();
        await Expect(_page.Locator("head")).ToBeAttachedAsync();
        await Expect(_page.Locator("body")).ToBeAttachedAsync();

        // Should have the Counter title
        var title = await _page.TitleAsync();
        await Assert.That(title).IsEqualTo("Abies Counter");
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
