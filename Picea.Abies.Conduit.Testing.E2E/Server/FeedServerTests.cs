// =============================================================================
// Feed E2E Tests — InteractiveServer Mode
// =============================================================================

using System.Text.Json;
using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Category("E2E")]
[ClassDataSource<ConduitServerFixture>(Shared = SharedType.Keyed, Key = "ConduitServer")]
[NotInParallel("ConduitServer")]
public sealed class FeedServerTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitServerFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public FeedServerTests(ConduitServerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _seeder = new ApiSeeder(_fixture.ApiUrl);
    }

    public async ValueTask DisposeAsync() => await _page.Context.DisposeAsync();

    [Test]
    public async Task GlobalFeed_WithArticles_ShouldShowArticlePreviews()
    {
        var username = $"srvfeed{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"SrvFeed {Guid.NewGuid():N}"[..30],
            "Description for global feed",
            "Body of global feed article.");
        await _seeder.WaitForArticleAsync(article.Slug);

        await _page.GotoAsync("/");
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        await _page.Locator(".feed-toggle").GetByText("Global Feed").ClickAsync();

        await Expect(_page.Locator(".article-preview").First).ToBeVisibleAsync(
            new() { Timeout = 10000 });
    }

    [Test]
    public async Task HomeBanner_ShouldShowConduitBranding()
    {
        await _page.GotoAsync("/");
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        await Expect(_page.Locator(".banner h1")).ToContainTextAsync("conduit");
        await Expect(_page.Locator(".banner p")).ToContainTextAsync(
            "A place to share your knowledge.");
    }

    [Test]
    public async Task TagSidebar_ShouldShowPopularTags()
    {
        var username = $"srvtag{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var uniqueTag = $"stag{Guid.NewGuid():N}"[..15];
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"Tagged {Guid.NewGuid():N}"[..30],
            "Tagged description",
            "Body with tags.",
            [uniqueTag]);
        await _seeder.WaitForArticleAsync(article.Slug);

        await _page.GotoAsync("/");
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        await Expect(_page.Locator(".sidebar .tag-list")).ToContainTextAsync(uniqueTag,
            new() { Timeout = 15000 });
    }

    [Test]
    public async Task ServerRuntime_DebuggerBadgeClick_ShouldTogglePanel()
    {
        await _page.GotoAsync("/");
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        var shell = _page.Locator("[data-abies-debugger-shell='1']");
        var panel = _page.Locator("[data-abies-debugger-panel='1']");

        await Expect(shell).ToBeVisibleAsync(new() { Timeout = 15000 });
        await shell.ClickAsync();
        await Expect(panel).ToBeVisibleAsync(new() { Timeout = 10000 });

        await shell.ClickAsync();
        await Expect(panel).Not.ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Test]
    public async Task ServerRuntime_ImportedTimelineReplay_ShouldApplySnapshots()
    {
        await _page.GotoAsync("/register?abies-debugger=1");
        await _page.WaitForSelectorAsync(".auth-page", new() { Timeout = 15000 });

        var shell = _page.Locator("[data-abies-debugger-shell='1']");
        var panel = _page.Locator("[data-abies-debugger-panel='1']");
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

        var exportedPath = Path.Combine(Path.GetTempPath(), $"abies-replay-server-{Guid.NewGuid():N}.json");
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

        await _page.Locator("button:has-text('Back')").ClickAsync();
        await Expect(_page.Locator("h1")).ToContainTextAsync("Sign up", new() { Timeout = 10000 });

        await _page.Locator("button:has-text('Step')").ClickAsync();
        await Expect(_page.Locator("h1")).ToContainTextAsync("Sign in", new() { Timeout = 10000 });
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
