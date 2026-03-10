// =============================================================================
// Feed E2E Tests — InteractiveServer Mode
// =============================================================================

using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;
using Microsoft.Playwright;

namespace Picea.Abies.Conduit.Testing.E2E;

[Trait("Category", "E2E")]
[Collection("ConduitServer")]
public sealed class FeedServerTests : IAsyncLifetime
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

    public async Task DisposeAsync() => await _page.Context.DisposeAsync();

    [Fact]
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

    [Fact]
    public async Task HomeBanner_ShouldShowConduitBranding()
    {
        await _page.GotoAsync("/");
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        await Expect(_page.Locator(".banner h1")).ToContainTextAsync("conduit");
        await Expect(_page.Locator(".banner p")).ToContainTextAsync(
            "A place to share your knowledge.");
    }

    [Fact]
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

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
