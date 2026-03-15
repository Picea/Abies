// =============================================================================
// Counter Interactivity E2E Tests — InteractiveWasm Mode
// =============================================================================
// Tests the full InteractiveWasm user journey through the browser:
//
//   1. Server renders initial HTML (fast first paint — count shows "0")
//   2. Browser downloads WASM bundle (_framework/dotnet.js, managed DLLs)
//   3. .NET runtime boots client-side
//   4. Abies MVU runtime takes over — replaces server-rendered DOM via OP_ADD_ROOT
//   5. Counter app is fully interactive (buttons respond to clicks)
//
// The WASM boot can take several seconds (download + JIT), so timeouts
// are set generously. The tests wait for interactivity by watching for
// the count to change after a button click.
// =============================================================================

using Microsoft.Playwright;
using Picea.Abies.Counter.Testing.E2E.Fixtures;
using Picea.Abies.Counter.Testing.E2E.Helpers;

namespace Picea.Abies.Counter.Testing.E2E;

[Category("E2E")]
[ClassDataSource<CounterWasmFixture>(Shared = SharedType.Keyed, Key = "CounterWasm")]
[NotInParallel("CounterWasm")]
public sealed class CounterWasmInteractivityTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly CounterWasmFixture _fixture;
    private IPage _page = null!;

    public CounterWasmInteractivityTests(CounterWasmFixture fixture)
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
        await InteractivityHelpers.WaitForWasmInteractivity(_page);

        await _page.GetByRole(AriaRole.Button, new() { Name = "+" }).ClickAsync();

        await Expect(_page.Locator(".count"))
            .ToHaveTextAsync("1", new() { Timeout = 5_000 });
    }

    [Test]
    public async Task Decrement_ShouldDecreaseCount()
    {
        await _page.GotoAsync("/");
        await InteractivityHelpers.WaitForWasmInteractivity(_page);

        await _page.GetByRole(AriaRole.Button, new() { Name = "\u2212" }).ClickAsync();

        await Expect(_page.Locator(".count"))
            .ToHaveTextAsync("-1", new() { Timeout = 5_000 });
    }

    [Test]
    public async Task Reset_ShouldReturnCountToZero()
    {
        await _page.GotoAsync("/");
        await InteractivityHelpers.WaitForWasmInteractivity(_page);

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
        await InteractivityHelpers.WaitForWasmInteractivity(_page);

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
