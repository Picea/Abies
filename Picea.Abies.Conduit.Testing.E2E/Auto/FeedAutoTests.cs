// =============================================================================
// Feed E2E Tests — InteractiveAuto Mode
// =============================================================================

using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Category("E2E")]
[ClassDataSource<ConduitAutoFixture>(Shared = SharedType.Keyed, Key = "ConduitAuto")]
[NotInParallel("ConduitAuto")]
public sealed class FeedAutoTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitAutoFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public FeedAutoTests(ConduitAutoFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _seeder = new ApiSeeder(_fixture.ApiUrl);
    }

    public async ValueTask DisposeAsync() => await _page.Context.DisposeAsync();

    [Test]
    public async Task GlobalFeed_WithArticles_ShouldShowArticlePreviews()
    {
        var username = $"autofeed{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"AutoFeed {Guid.NewGuid():N}"[..30],
            "Description for auto feed",
            "Body of auto feed article.");
        await _seeder.WaitForArticleAsync(article.Slug);

        await _page.GotoAsync("/");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        await _page.Locator(".feed-toggle").GetByText("Global Feed").ClickAsync();

        await Expect(_page.Locator(".article-preview").First).ToBeVisibleAsync(
            new() { Timeout = 10000 });
    }

    [Test]
    public async Task HomeBanner_ShouldShowConduitBranding()
    {
        await _page.GotoAsync("/");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        await Expect(_page.Locator(".banner h1")).ToContainTextAsync("conduit");
        await Expect(_page.Locator(".banner p")).ToContainTextAsync(
            "A place to share your knowledge.");
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
