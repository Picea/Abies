// =============================================================================
// URL Navigation E2E Tests — Query/path driven feed and profile routes
// =============================================================================

using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Category("E2E")]
[ClassDataSource<ConduitAppFixture>(Shared = SharedType.Keyed, Key = "Conduit")]
[NotInParallel("Conduit")]
public sealed class UrlNavigationTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitAppFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public UrlNavigationTests(ConduitAppFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _seeder = new ApiSeeder(_fixture.ApiUrl);
    }

    public async ValueTask DisposeAsync()
    {
        await _page.Context.DisposeAsync();
    }

    [Test]
    public async Task FeedTabs_ShouldExposeRouteBasedHrefs()
    {
        var username = $"urltab{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");
        await LoginViaUi(email, "password123");

        await Expect(_page.Locator(".feed-toggle .nav-link").Filter(new() { HasText = "Your Feed" }))
            .ToHaveAttributeAsync("href", "/?feed=following");
        await Expect(_page.Locator(".feed-toggle .nav-link").Filter(new() { HasText = "Global Feed" }))
            .ToHaveAttributeAsync("href", "/");
    }

    [Test]
    public async Task ClickingYourFeed_ShouldUpdateUrlAndActivateFollowingFeed()
    {
        var author = $"urlaut{Guid.NewGuid():N}"[..20];
        var authorEmail = $"{author}@test.com";
        var authorUser = await _seeder.RegisterUserAsync(author, authorEmail, "password123");
        var article = await _seeder.CreateArticleAsync(
            authorUser.Token,
            $"FollowingRoute {Guid.NewGuid():N}"[..30],
            "Route-following description",
            "Route-following body.");
        await _seeder.WaitForArticleAsync(article.Slug);

        var follower = $"urlflw{Guid.NewGuid():N}"[..20];
        var followerEmail = $"{follower}@test.com";
        var followerUser = await _seeder.RegisterUserAsync(follower, followerEmail, "password123");
        await _seeder.FollowUserAsync(followerUser.Token, author);

        await LoginViaUi(followerEmail, "password123");
        await _page.Locator(".feed-toggle .nav-link").Filter(new() { HasText = "Your Feed" }).ClickAsync();

        await Expect(_page).ToHaveURLAsync(new Regex(@"/\?feed=following$"), new() { Timeout = 10000 });
        await Expect(_page.Locator(".feed-toggle .nav-link.active"))
            .ToContainTextAsync("Your Feed", new() { Timeout = 10000 });
        await Expect(_page.Locator(".article-preview").Filter(new() { HasText = article.Title }))
            .ToHaveCountAsync(1, new() { Timeout = 10000 });
    }

    [Test]
    public async Task TagRoute_ShouldLoadSelectedTagAndPreservePageQuery()
    {
        var username = $"urltag{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var uniqueTag = $"urltag{Guid.NewGuid():N}"[..15];

        ArticleSeedResult? lastArticle = null;
        for (var i = 1; i <= 11; i++)
        {
            lastArticle = await _seeder.CreateArticleAsync(
                user.Token,
                $"TagRoute {i} {Guid.NewGuid():N}"[..30],
                "Tagged route article",
                "Body.",
                [uniqueTag]);
        }

        await _seeder.WaitForArticleAsync(lastArticle!.Slug);

        await _page.GotoAsync($"/tag/{uniqueTag}?page=2");
        await _page.WaitForWasmReady();

        await Expect(_page).ToHaveURLAsync(new Regex($@"/tag/{uniqueTag}\?page=2$"), new() { Timeout = 10000 });
        await Expect(_page.Locator(".feed-toggle .nav-link.active"))
            .ToContainTextAsync(uniqueTag, new() { Timeout = 10000 });
        await Expect(_page.Locator("ul.pagination .page-item.active"))
            .ToContainTextAsync("2", new() { Timeout = 10000 });
    }

    [Test]
    public async Task ProfileFavoritesRoute_ShouldLoadFavoritedTab()
    {
        var author = $"urlprf{Guid.NewGuid():N}"[..20];
        var authorEmail = $"{author}@test.com";
        var authorUser = await _seeder.RegisterUserAsync(author, authorEmail, "password123");

        var other = $"urloth{Guid.NewGuid():N}"[..20];
        var otherEmail = $"{other}@test.com";
        var otherUser = await _seeder.RegisterUserAsync(other, otherEmail, "password123");
        var otherArticle = await _seeder.CreateArticleAsync(
            otherUser.Token,
            $"FavRoute {Guid.NewGuid():N}"[..30],
            "Favorited route article",
            "Body.");

        await _seeder.FavoriteArticleAsync(authorUser.Token, otherArticle.Slug);
        await _seeder.WaitForProfileAsync(author);
        await _seeder.WaitForArticleAsync(otherArticle.Slug);

        await LoginViaUi(authorEmail, "password123");
        await _page.GotoAsync($"/profile/{author}/favorites");
        await _page.WaitForWasmReady();

        await Expect(_page).ToHaveURLAsync(new Regex($@"/profile/{author}/favorites$"), new() { Timeout = 10000 });
        await Expect(_page.Locator(".articles-toggle .nav-link.active"))
            .ToContainTextAsync("Favorited", new() { Timeout = 10000 });
        await Expect(_page.Locator(".article-preview").Filter(new() { HasText = otherArticle.Title }))
            .ToHaveCountAsync(1, new() { Timeout = 10000 });
    }

    private async Task LoginViaUi(string email, string password)
    {
        await _page.GotoAsync("/login");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync("h1:has-text('Sign in')");
        await _page.GetByPlaceholder("Email").FillAsync(email);
        await _page.GetByPlaceholder("Password").FillAsync(password);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);

    private static IPageAssertions Expect(IPage page) =>
        Assertions.Expect(page);
}
