using Microsoft.Playwright;
using Picea.Abies.UI.Demo.Testing.E2E.Fixtures;

namespace Picea.Abies.UI.Demo.Testing.E2E;

[Category("E2E")]
[ClassDataSource<UiDemoFixture>(Shared = SharedType.Keyed, Key = "UiDemo")]
[NotInParallel("UiDemo")]
public sealed class UiDemoPhase2Tests : IAsyncInitializer, IAsyncDisposable
{
    private readonly UiDemoFixture _fixture;
    private IPage _page = null!;

    public UiDemoPhase2Tests(UiDemoFixture fixture)
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
    public async Task Buttons_ShouldRenderCoreVariants()
    {
        await _page.GotoAsync("/");

        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Primary action" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Secondary action" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Ghost action" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task TextInput_ShouldExposeLabelAndErrorText()
    {
        await _page.GotoAsync("/");

        await Expect(_page.GetByText("Display name"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(_page.GetByText("A token is required before saving."))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task Select_ShouldRenderConfiguredOptions()
    {
        await _page.GotoAsync("/");

        var variantSelect = _page.Locator("select[name='variant']");
        await Expect(variantSelect).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(variantSelect.Locator("option")).ToHaveCountAsync(3, new() { Timeout = 10_000 });
    }

    [Test]
    public async Task Spinner_ShouldExposeStatusRoleAndBusyState()
    {
        await _page.GotoAsync("/");

        var spinner = _page.Locator(".abies-ui-spinner").First;
        await Expect(spinner).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(spinner).ToHaveAttributeAsync("role", "status", new() { Timeout = 10_000 });
        await Expect(spinner).ToHaveAttributeAsync("aria-busy", "true", new() { Timeout = 10_000 });
    }

    [Test]
    public async Task Toast_ShouldExposeLiveRegionContracts()
    {
        await _page.GotoAsync("/");

        var politeToast = _page.Locator(".abies-ui-toast[aria-live='polite']").First;
        await Expect(politeToast).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(politeToast).ToContainTextAsync("Saved", new() { Timeout = 10_000 });

        var assertiveToast = _page.Locator(".abies-ui-toast[aria-live='assertive']").First;
        await Expect(assertiveToast).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(assertiveToast).ToContainTextAsync("Needs attention", new() { Timeout = 10_000 });
    }

    [Test]
    public async Task Table_ShouldRenderRows()
    {
        await _page.GotoAsync("/");

        var rows = _page.Locator("tbody tr[tabindex='0']");
        await Expect(rows).ToHaveCountAsync(3, new() { Timeout = 10_000 });
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
