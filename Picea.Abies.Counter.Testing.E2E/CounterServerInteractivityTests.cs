// =============================================================================
// Counter Interactivity E2E Tests — InteractiveServer Mode
// =============================================================================
// Tests the full InteractiveServer user journey through the browser:
//
//   1. Server renders initial HTML (fast first paint — count shows "0")
//   2. Browser connects via WebSocket to /_abies/ws
//   3. abies-server.js applies binary DOM patches from the server
//   4. User events are sent to the server MVU runtime via WebSocket
//   5. Counter app is fully interactive (buttons respond to clicks)
//
// InteractiveServer has near-instant interactivity — no WASM download needed.
// The MVU loop runs server-side, so each interaction incurs a round-trip.
// =============================================================================

using Picea.Abies.Counter.Testing.E2E.Fixtures;
using Picea.Abies.Counter.Testing.E2E.Helpers;
using Microsoft.Playwright;

namespace Picea.Abies.Counter.Testing.E2E;

[Trait("Category", "E2E")]
[Collection("CounterServer")]
public sealed class CounterServerInteractivityTests : IAsyncLifetime
{
    private readonly CounterServerFixture _fixture;
    private IPage _page = null!;

    public CounterServerInteractivityTests(CounterServerFixture fixture)
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
    public async Task Increment_ShouldIncreaseCount()
    {
        await _page.GotoAsync("/");
        await InteractivityHelpers.WaitForServerInteractivity(_page);

        await _page.GetByRole(AriaRole.Button, new() { Name = "+" }).ClickAsync();

        await Expect(_page.Locator(".count"))
            .ToHaveTextAsync("1", new() { Timeout = 5_000 });
    }

    [Fact]
    public async Task Decrement_ShouldDecreaseCount()
    {
        await _page.GotoAsync("/");
        await InteractivityHelpers.WaitForServerInteractivity(_page);

        await _page.GetByRole(AriaRole.Button, new() { Name = "\u2212" }).ClickAsync();

        await Expect(_page.Locator(".count"))
            .ToHaveTextAsync("-1", new() { Timeout = 5_000 });
    }

    [Fact]
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

    [Fact]
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
