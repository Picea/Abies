// =============================================================================
// Health E2E Tests — App shell and API availability smoke coverage
// =============================================================================

using System.Text.Json;
using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Category("E2E")]
[ClassDataSource<ConduitAppFixture>(Shared = SharedType.Keyed, Key = "Conduit")]
[NotInParallel("Conduit")]
public sealed class HealthTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitAppFixture _fixture;
    private IPage _page = null!;

    public HealthTests(ConduitAppFixture fixture)
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
    public async Task AppRoot_ShouldLoadNavbarAndBranding()
    {
        await _page.GotoAsync("/");
        await _page.WaitForWasmReady();

        await Expect(_page.Locator("a.navbar-brand")).ToContainTextAsync("conduit", new() { Timeout = 10000 });
        await Expect(_page.Locator("nav.navbar")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.Locator(".banner h1")).ToContainTextAsync("conduit", new() { Timeout = 10000 });
    }

    [Test]
    public async Task ApiTags_ShouldBeReachable()
    {
        using var http = new HttpClient();
        var response = await http.GetAsync($"{_fixture.ApiUrl}/api/tags");

        await Assert.That((int)response.StatusCode).IsEqualTo(200);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body.Contains("tags", StringComparison.OrdinalIgnoreCase)).IsTrue();
    }

    [Test]
    public async Task LoginPage_ShouldLoadCoreFields()
    {
        await _page.GotoAsync("/login");
        await _page.WaitForWasmReady();

        await Expect(_page.Locator("h1")).ToContainTextAsync("Sign in", new() { Timeout = 10000 });
        await Expect(_page.GetByPlaceholder("Email")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.GetByPlaceholder("Password")).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Test]
    public async Task RegisterPage_ShouldLoadCoreFields()
    {
        await _page.GotoAsync("/register");
        await _page.WaitForWasmReady();

        await Expect(_page.Locator("h1")).ToContainTextAsync("Sign up", new() { Timeout = 10000 });
        await Expect(_page.GetByPlaceholder("Your Name")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.GetByPlaceholder("Email")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.GetByPlaceholder("Password")).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Test]
    public async Task BrowserRuntime_DebuggerBadgeClick_ShouldTogglePanel()
    {
        await _page.GotoAsync("/?abies-debugger=1");
        await _page.WaitForWasmReady();

        var shell = _page.Locator("[data-abies-debugger-shell='1']");
        var panel = _page.Locator("[data-abies-debugger-panel='1']");

        if (await shell.CountAsync() == 0)
        {
            return;
        }

        await Expect(shell).ToBeVisibleAsync(new() { Timeout = 15000 });
        await shell.ClickAsync();
        await Expect(panel).ToBeVisibleAsync(new() { Timeout = 10000 });

        await shell.ClickAsync();
        await Expect(panel).Not.ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Test]
    public async Task BrowserRuntime_ImportedTimelineReplay_ShouldApplySnapshots()
    {
        await _page.GotoAsync("/register?abies-debugger=1");
        await _page.WaitForWasmReady();

        var shell = _page.Locator("[data-abies-debugger-shell='1']");
        var panel = _page.Locator("[data-abies-debugger-panel='1']");
        if (await shell.CountAsync() == 0)
        {
            return;
        }
        await Expect(shell).ToBeVisibleAsync(new() { Timeout = 15000 });
        if (!await panel.IsVisibleAsync())
        {
            await shell.ClickAsync();
            await Expect(panel).ToBeVisibleAsync(new() { Timeout = 10000 });
        }

        await Expect(_page.Locator("h1")).ToContainTextAsync("Sign up", new() { Timeout = 10000 });
        await _page.GetByRole(AriaRole.Link, new() { Name = "Have an account?" }).ClickAsync();
        await Expect(_page.Locator("h1")).ToContainTextAsync("Sign in", new() { Timeout = 10000 });

        var exportDownloadTask = _page.WaitForDownloadAsync();
        await _page.GetByRole(AriaRole.Button, new() { Name = "Export" }).ClickAsync();
        var exportDownload = await exportDownloadTask;

        var exportedPath = Path.Combine(Path.GetTempPath(), $"abies-replay-wasm-{Guid.NewGuid():N}.json");
        await exportDownload.SaveAsAsync(exportedPath);

        var exportedJson = await File.ReadAllTextAsync(exportedPath);
        using var payload = JsonDocument.Parse(exportedJson);

        var timelineEntries = payload.RootElement.GetProperty("timelineEntries");
        await Assert.That(timelineEntries.GetArrayLength()).IsGreaterThan(0);

        var firstSnapshot = timelineEntries[0].GetProperty("modelSnapshotPreview").GetString() ?? "";
        await Assert.That(firstSnapshot.StartsWith("{", StringComparison.Ordinal)).IsTrue();

        await _page.GetByRole(AriaRole.Button, new() { Name = "Clear" }).ClickAsync();

        var chooser = await _page.RunAndWaitForFileChooserAsync(async () =>
            await _page.GetByRole(AriaRole.Button, new() { Name = "Import" }).ClickAsync());
        await chooser.SetFilesAsync(exportedPath);

        var timelineItems = _page.Locator("[data-sequence]");
        await Expect(timelineItems.First).ToBeVisibleAsync(new() { Timeout = 15000 });
        await Expect(timelineItems.Nth(1)).ToBeVisibleAsync(new() { Timeout = 15000 });

        await timelineItems.First.ClickAsync();
        await Expect(_page.Locator("h1")).ToContainTextAsync("Sign up", new() { Timeout = 10000 });

        await timelineItems.Nth(1).ClickAsync();
        await Expect(_page.Locator("h1")).ToContainTextAsync("Sign in", new() { Timeout = 10000 });
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
