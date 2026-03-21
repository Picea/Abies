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

    [Test]
    public async Task Phase2LayoutSections_ShouldRenderCoreElements()
    {
        await _page.GotoAsync("/");

        await Expect(_page.GetByText("Item 1"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        await Expect(_page.Locator(".abies-ui-card").First)
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        await Expect(_page.Locator("hr[role='separator']").First)
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        await Expect(_page.GetByText("Section break"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task Phase2FeedbackSections_ShouldExposeAccessibilityContracts()
    {
        await _page.GotoAsync("/");

        var progressBars = _page.Locator("[role='progressbar']");
        await Expect(progressBars).ToHaveCountAsync(2, new() { Timeout = 10_000 });

        var determinate = progressBars.First;
        await Expect(determinate).ToHaveAttributeAsync("aria-valuenow", new System.Text.RegularExpressions.Regex(".*"), new() { Timeout = 10_000 });
        await Expect(determinate).ToHaveAttributeAsync("aria-label", new System.Text.RegularExpressions.Regex("\\S+"), new() { Timeout = 10_000 });

        var indeterminate = progressBars.Nth(1);
        await Expect(indeterminate).Not.ToHaveAttributeAsync("aria-valuenow", new System.Text.RegularExpressions.Regex(".*"), new() { Timeout = 10_000 });

        await Expect(_page.Locator(".abies-ui-alert[role='alert'][aria-live='assertive']").First)
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        var skeletonCount = await _page.Locator(".abies-ui-skeleton[aria-busy='true']").CountAsync();
        await Assert.That(skeletonCount).IsGreaterThan(0);
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
