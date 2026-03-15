// =============================================================================
// Counter Interactivity E2E Tests — InteractiveAuto Mode
// =============================================================================
// Tests the full InteractiveAuto user journey through the browser:
//
//   1. Server renders initial HTML (fast first paint — count shows "0")
//   2. Browser connects via WebSocket for immediate server-side interactivity
//   3. WASM runtime downloads in the background
//   4. Once WASM is ready, the MVU loop transitions from server to client
//   5. Counter remains fully interactive throughout the transition
//
// InteractiveAuto combines the best of both modes:
//   - Near-instant interactivity (like InteractiveServer)
//   - Eventually client-side (like InteractiveWasm, offloading the server)
//
// Tests use the ServerInteractivity wait helper since the app is interactive
// from the WebSocket side immediately — no need to wait for WASM boot.
// =============================================================================

using Picea.Abies.Counter.Testing.E2E.Fixtures;
using Picea.Abies.Counter.Testing.E2E.Helpers;
using Microsoft.Playwright;

namespace Picea.Abies.Counter.Testing.E2E;

[Category("E2E")]
[ClassDataSource<CounterAutoFixture>(Shared = SharedType.Keyed, Key = "CounterAuto")]
[NotInParallel("CounterAuto")]
public sealed class CounterAutoInteractivityTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly CounterAutoFixture _fixture;
    private IPage _page = null!;

    public CounterAutoInteractivityTests(CounterAutoFixture fixture)
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
    public async Task Increment_ShouldIncreaseCount()
    {
        await _page.GotoAsync("/");
        await InteractivityHelpers.WaitForServerInteractivity(_page);

        await _page.GetByRole(AriaRole.Button, new() { Name = "+" }).ClickAsync();

        await Expect(_page.Locator(".count"))
            .ToHaveTextAsync("1", new() { Timeout = 5_000 });
    }

    [Test]
    public async Task Decrement_ShouldDecreaseCount()
    {
        await _page.GotoAsync("/");
        await InteractivityHelpers.WaitForServerInteractivity(_page);

        await _page.GetByRole(AriaRole.Button, new() { Name = "\u2212" }).ClickAsync();

        await Expect(_page.Locator(".count"))
            .ToHaveTextAsync("-1", new() { Timeout = 5_000 });
    }

    [Test]
    public async Task Reset_ShouldReturnCountToZero()
    {
        await _page.GotoAsync("/");
        await InteractivityHelpers.WaitForServerInteractivity(_page);

        var incrementButton = _page.GetByRole(AriaRole.Button, new() { Name = "+" });
        await incrementButton.ClickAsync();
        await incrementButton.ClickAsync();
        await incrementButton.ClickAsync();

        await Expect(_page.Locator(".count"))
            .ToHaveTextAsync("3", new() { Timeout = 5_000 });

        await _page.GetByRole(AriaRole.Button, new() { Name = "Reset" }).ClickAsync();

        await Expect(_page.Locator(".count"))
            .ToHaveTextAsync("0", new() { Timeout = 5_000 });
    }

    [Test]
    public async Task MultipleClicks_ShouldTrackCountAccurately()
    {
        await _page.GotoAsync("/");
        await InteractivityHelpers.WaitForServerInteractivity(_page);

        var incrementButton = _page.GetByRole(AriaRole.Button, new() { Name = "+" });
        var decrementButton = _page.GetByRole(AriaRole.Button, new() { Name = "\u2212" });

        for (var i = 0; i < 5; i++)
            await incrementButton.ClickAsync();

        await Expect(_page.Locator(".count"))
            .ToHaveTextAsync("5", new() { Timeout = 5_000 });

        await decrementButton.ClickAsync();
        await decrementButton.ClickAsync();

        await Expect(_page.Locator(".count"))
            .ToHaveTextAsync("3", new() { Timeout = 5_000 });
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
