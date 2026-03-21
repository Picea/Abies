using Microsoft.Playwright;
using Picea.Abies.UI.Demo.Testing.E2E.Fixtures;

namespace Picea.Abies.UI.Demo.Testing.E2E;

[Category("E2E")]
[ClassDataSource<UiDemoFixture>(Shared = SharedType.Keyed, Key = "UiDemo")]
[NotInParallel("UiDemo")]
public sealed class UiDemoSmokeTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly UiDemoFixture _fixture;
    private IPage _page = null!;

    public UiDemoSmokeTests(UiDemoFixture fixture)
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
    public async Task PageLoad_ShouldRenderUiDemoHeading()
    {
        await _page.GotoAsync("/");

        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Phase 2 component kit" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        await Expect(_page.GetByText("Picea.Abies.UI.Demo"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task Modal_ShouldOpenAndCloseFromUserActions()
    {
        await _page.GotoAsync("/");

        var modalTrigger = _page.Locator("#modal-trigger-button");
        await Expect(modalTrigger).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await modalTrigger.ClickAsync();

        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Demo modal" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        await _page.GetByRole(AriaRole.Button, new() { Name = "Close" }).ClickAsync();

        await Expect(modalTrigger)
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task Modal_ShouldCloseFromFooterAction()
    {
        await _page.GotoAsync("/");

        var modalTrigger = _page.Locator("#modal-trigger-button");
        await Expect(modalTrigger).ToBeVisibleAsync(new() { Timeout = 10_000 });

        await modalTrigger.ClickAsync();

        var modalHeading = _page.GetByRole(AriaRole.Heading, new() { Name = "Demo modal" });
        await Expect(modalHeading).ToBeVisibleAsync(new() { Timeout = 10_000 });

        await _page.GetByRole(AriaRole.Button, new() { Name = "Done" }).ClickAsync();

        await Expect(modalHeading).ToBeHiddenAsync(new() { Timeout = 10_000 });
        await Expect(modalTrigger).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
