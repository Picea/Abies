// =============================================================================
// Article E2E Tests — InteractiveServer Mode
// =============================================================================

using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Category("E2E")]
[ClassDataSource<ConduitServerFixture>(Shared = SharedType.Keyed, Key = "ConduitServer")]
[NotInParallel("ConduitServer")]
public sealed class ArticleServerTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitServerFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public ArticleServerTests(ConduitServerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _seeder = new ApiSeeder(_fixture.ApiUrl);
    }

    public async ValueTask DisposeAsync() => await _page.Context.DisposeAsync();

    [Test]
    public async Task ViewArticle_ShouldShowTitleAndBody()
    {
        var username = $"srvartvw{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"SrvArt {Guid.NewGuid():N}"[..30],
            "Article description",
            "This is the full article body for server mode.");
        await _seeder.WaitForArticleAsync(article.Slug);

        await _page.GotoAsync($"/article/{article.Slug}");

        await Expect(_page.Locator(".banner h1")).ToContainTextAsync(article.Title,
            new() { Timeout = 15000 });
    }

    [Test]
    public async Task FavoriteArticle_WhenLoggedIn_ShouldToggleFavoriteButton()
    {
        var author = $"srvfavau{Guid.NewGuid():N}"[..20];
        var authorEmail = $"{author}@test.com";
        var authorUser = await _seeder.RegisterUserAsync(author, authorEmail, "password123");
        var article = await _seeder.CreateArticleAsync(
            authorUser.Token,
            $"SrvFav {Guid.NewGuid():N}"[..30],
            "Favorite test",
            "Favoriting body.");
        await _seeder.WaitForArticleAsync(article.Slug);

        var reader = $"srvfavrd{Guid.NewGuid():N}"[..20];
        var readerEmail = $"{reader}@test.com";
        await _seeder.RegisterUserAsync(reader, readerEmail, "password123");

        await LoginViaUi(readerEmail, "password123");

        await _page.NavigateInApp($"/article/{article.Slug}");
        await Expect(_page.Locator(".banner h1")).ToContainTextAsync(article.Title,
            new() { Timeout = 15000 });

        var favBtn = _page.Locator("button:has-text('Favorite Article')").First;
        await favBtn.WaitForAsync(new() { Timeout = 10000 });
        await favBtn.ClickAsync();

        await Expect(_page.Locator("button:has-text('Unfavorite Article')").First)
            .ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    private async Task LoginViaUi(string email, string password)
    {
        await _page.GotoAsync("/login");
        await _page.WaitForSelectorAsync("h1:has-text('Sign in')");
        await _page.GetByPlaceholder("Email").FillAndWaitForPatch(email);
        await _page.GetByPlaceholder("Password").FillAndWaitForPatch(password);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        // CI can be slow to complete the post-login server patch cycle.
        // First wait until we have navigated away from /login, then wait for
        // a stable authenticated shell marker to appear.
        await _page.WaitForFunctionAsync(
            "() => !window.location.pathname.startsWith('/login')",
            null,
            new() { Timeout = 30000 });

        await _page.WaitForSelectorAsync(
            ".home-page, .feed-toggle, .article-preview",
            new() { Timeout = 30000 });
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
