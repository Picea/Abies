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
    public async Task Stack_ShouldRenderChildren()
    {
        await _page.GotoAsync("/");

        await Expect(_page.GetByText("Item 1"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task Card_ShouldRenderContent()
    {
        await _page.GotoAsync("/");

        var card = _page.Locator("div.abies-ui-card").First;
        await Expect(card).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task Divider_ShouldRenderAsSeparator()
    {
        await _page.GotoAsync("/");

        var separator = _page.Locator("hr[role='separator']").First;
        await Expect(separator).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task DividerLabeled_ShouldRenderLabelText()
    {
        await _page.GotoAsync("/");

        var label = _page.Locator("span.abies-ui-divider__label").First;
        await Expect(label).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task ProgressBar_ShouldHaveAccessibleRole()
    {
        await _page.GotoAsync("/");

        var progressBar = _page.Locator("[role='progressbar']").First;
        await Expect(progressBar).ToBeVisibleAsync(new() { Timeout = 10_000 });

        await Expect(progressBar).ToHaveAttributeAsync("aria-valuemin", new System.Text.RegularExpressions.Regex(".*"), new() { Timeout = 10_000 });
        await Expect(progressBar).ToHaveAttributeAsync("aria-label", new System.Text.RegularExpressions.Regex("\\S+"), new() { Timeout = 10_000 });
    }

    [Test]
    public async Task ProgressBar_Indeterminate_ShouldNotHaveAriaValueNow()
    {
        await _page.GotoAsync("/");

        // The second [role="progressbar"] is the indeterminate one
        var indeterminate = _page.Locator("[role='progressbar']").Nth(1);
        await Expect(indeterminate).ToBeVisibleAsync(new() { Timeout = 10_000 });

        await Expect(indeterminate).Not.ToHaveAttributeAsync("aria-valuenow", new System.Text.RegularExpressions.Regex(".*"), new() { Timeout = 10_000 });
        await Expect(indeterminate).ToHaveAttributeAsync("aria-label", new System.Text.RegularExpressions.Regex("\\S+"), new() { Timeout = 10_000 });
    }

    [Test]
    public async Task Alert_ShouldHaveAssistiveTechnologyRoleAndText()
    {
        await _page.GotoAsync("/");

        var alert = _page.Locator("[role='alert'], [role='status']").First;
        await Expect(alert).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task Skeleton_ShouldHaveAriaBusy()
    {
        await _page.GotoAsync("/");

        var skeleton = _page.Locator("[aria-busy='true']").First;
        await Expect(skeleton).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task Modal_FocusShouldBeTrappedInsideWhenOpen()
    {
        await _page.GotoAsync("/");

        var modalTrigger = _page.Locator("#modal-trigger-button");
        await Expect(modalTrigger).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await modalTrigger.ClickAsync();

        var dialog = _page.GetByRole(AriaRole.Dialog);
        await Expect(dialog).ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Tab through a few focusable elements inside the modal
        for (var i = 0; i < 3; i++)
        {
            await _page.Keyboard.PressAsync("Tab");
        }

        // Verify active element is still inside the dialog
        var focusIsInsideDialog = await _page.EvaluateAsync<bool>(
            "() => document.querySelector('[role=\"dialog\"]')?.contains(document.activeElement) ?? false");

        await Assert.That(focusIsInsideDialog).IsTrue();

        // Close modal to leave clean state
        await _page.GetByRole(AriaRole.Button, new() { Name = "Close" }).ClickAsync();
    }

    [Test]
    public async Task Modal_EscapeShouldCloseAndRestoreFocusToTrigger()
    {
        await _page.GotoAsync("/");

        var modalTrigger = _page.Locator("#modal-trigger-button");
        await Expect(modalTrigger).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await modalTrigger.ClickAsync();

        var dialog = _page.GetByRole(AriaRole.Dialog);
        await Expect(dialog).ToBeVisibleAsync(new() { Timeout = 10_000 });

        await _page.Keyboard.PressAsync("Escape");
        await Expect(dialog).Not.ToBeVisibleAsync(new() { Timeout = 10_000 });

        var focusReturned = await _page.EvaluateAsync<bool>(
            "() => document.activeElement?.id === 'modal-trigger-button'");

        await Assert.That(focusReturned).IsTrue();
    }

    [Test]
    public async Task Table_ArrowKeysShouldNavigateRows()
    {
        await _page.GotoAsync("/");

        // Find first focusable table row
        var firstRow = _page.Locator("tbody tr[tabindex='0']").First;
        await Expect(firstRow).ToBeVisibleAsync(new() { Timeout = 10_000 });

        await firstRow.ClickAsync();
        await _page.Keyboard.PressAsync("ArrowDown");

        // Verify at least one focusable row exists in tbody (basic nav contract)
        var hasFocusableRows = await _page.EvaluateAsync<bool>(
            "() => { const rows = document.querySelectorAll('tbody tr[tabindex=\"0\"]'); return rows.length > 0 && rows[1] === document.activeElement; }");

        await Assert.That(hasFocusableRows).IsTrue();
    }

    [Test]
    public async Task Table_HomeAndEndKeysShouldRespectRowBoundaries()
    {
        await _page.GotoAsync("/");

        var firstRow = _page.Locator("tbody tr[tabindex='0']").First;
        await Expect(firstRow).ToBeVisibleAsync(new() { Timeout = 10_000 });

        await firstRow.ClickAsync();
        await _page.Keyboard.PressAsync("End");

        var atLastRow = await _page.EvaluateAsync<bool>(
            "() => { const rows = document.querySelectorAll('tbody tr[tabindex=\"0\"]'); return rows.length > 0 && document.activeElement === rows[rows.length - 1]; }");
        await Assert.That(atLastRow).IsTrue();

        await _page.Keyboard.PressAsync("Home");

        var atFirstRow = await _page.EvaluateAsync<bool>(
            "() => { const rows = document.querySelectorAll('tbody tr[tabindex=\"0\"]'); return rows.length > 0 && document.activeElement === rows[0]; }");
        await Assert.That(atFirstRow).IsTrue();
    }

    [Test]
    public async Task ProgressBar_DeterminateShouldExposeBoundedValueContract()
    {
        await _page.GotoAsync("/");

        var determinate = _page.Locator("[role='progressbar']").First;
        await Expect(determinate).ToBeVisibleAsync(new() { Timeout = 10_000 });

        await Expect(determinate).ToHaveAttributeAsync("aria-valuenow", "45", new() { Timeout = 10_000 });
        await Expect(determinate).ToHaveAttributeAsync("aria-valuemin", "0", new() { Timeout = 10_000 });
        await Expect(determinate).ToHaveAttributeAsync("aria-valuemax", "100", new() { Timeout = 10_000 });
        await Expect(_page.Locator(".abies-ui-progress-bar__value").First).ToHaveTextAsync("45%", new() { Timeout = 10_000 });
    }

    [Test]
    public async Task Alert_LiveRegionShouldExposeStatusAndAssertiveContracts()
    {
        await _page.GotoAsync("/");

        var infoStatus = _page.Locator(".abies-ui-alert[role='status']").First;
        await Expect(infoStatus).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(infoStatus).ToHaveAttributeAsync("aria-live", "polite", new() { Timeout = 10_000 });

        var dangerAlert = _page.Locator(".abies-ui-alert[role='alert']").First;
        await Expect(dangerAlert).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(dangerAlert).ToHaveAttributeAsync("aria-live", "assertive", new() { Timeout = 10_000 });
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
