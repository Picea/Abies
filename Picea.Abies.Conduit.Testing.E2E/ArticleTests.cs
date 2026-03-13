// =============================================================================
// Article E2E Tests — View, delete, favorite/unfavorite articles
// =============================================================================

using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;
using Microsoft.Playwright;

namespace Picea.Abies.Conduit.Testing.E2E;

[Trait("Category", "E2E")]
[Collection("Conduit")]
public sealed class ArticleTests : IAsyncLifetime
{
    private readonly ConduitAppFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public ArticleTests(ConduitAppFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _seeder = new ApiSeeder(_fixture.ApiUrl);
    }

    public async Task DisposeAsync()
    {
        await _page.Context.DisposeAsync();
    }

    [Fact]
    public async Task ViewArticle_WithContent_ShouldDisplayTitleAndBody()
    {
        var username = $"artview{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"View Test {Guid.NewGuid():N}"[..30],
            "Test description",
            "This is the article body for viewing.");

        await _seeder.WaitForArticleAsync(article.Slug);

        await _page.GotoAsync($"/article/{article.Slug}");
        await _page.WaitForWasmReady();

        await Expect(_page.Locator(".banner h1")).ToContainTextAsync(article.Title, new() { Timeout = 15000 });

        await Expect(_page.Locator(".article-meta").First).ToContainTextAsync(username);
    }

    [Fact]
    public async Task DeleteArticle_AsAuthor_ShouldNavigateToHome()
    {
        var username = $"artdel{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"Delete Test {Guid.NewGuid():N}"[..30],
            "To be deleted",
            "This article will be deleted.");

        await _seeder.WaitForArticleAsync(article.Slug);

        await LoginViaUi(email, "password123");

        await _page.NavigateInApp($"/article/{article.Slug}");

        await _page.WaitForSelectorAsync("text='Delete Article'", new() { Timeout = 15000 });

        await _page.GetByText("Delete Article").First.ClickAsync();

        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 10000 });
    }

    [Fact]
    public async Task FavoriteArticle_WhenLoggedIn_ShouldIncrementCounter()
    {
        var author = $"favauth{Guid.NewGuid():N}"[..20];
        var authorEmail = $"{author}@test.com";
        var authorUser = await _seeder.RegisterUserAsync(author, authorEmail, "password123");
        var article = await _seeder.CreateArticleAsync(
            authorUser.Token,
            $"Fav Test {Guid.NewGuid():N}"[..30],
            "Favorite me",
            "This article should be favorited.");

        var reader = $"favread{Guid.NewGuid():N}"[..20];
        var readerEmail = $"{reader}@test.com";
        await _seeder.RegisterUserAsync(reader, readerEmail, "password123");

        await _seeder.WaitForArticleAsync(article.Slug);
        await _seeder.WaitForProfileAsync(reader);

        await LoginViaUi(readerEmail, "password123");

        await _page.NavigateInApp($"/article/{article.Slug}");

        await _page.WaitForSelectorAsync(
            ".article-actions button.btn-outline-primary, .article-meta button.btn-outline-primary",
            new() { Timeout = 15000 });

        await _page.Locator(".article-actions button.btn-outline-primary, .article-meta button.btn-outline-primary")
            .First.ClickAsync();

        await Expect(
            _page.Locator(".article-actions button.btn-primary, .article-meta button.btn-primary")
                .First
        ).ToContainTextAsync("1", new() { Timeout = 10000 });
    }

    [Fact]
    public async Task ViewArticle_WithTags_ShouldDisplayTagList()
    {
        var username = $"arttag{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"Tag Display {Guid.NewGuid():N}"[..30],
            "With tags",
            "Article with tags.",
            ["testingtag", "e2etag"]);

        await _seeder.WaitForArticleAsync(article.Slug);

        await _page.GotoAsync($"/article/{article.Slug}");
        await _page.WaitForWasmReady();

        await Expect(_page.Locator(".banner h1")).ToContainTextAsync(article.Title, new() { Timeout = 15000 });

        await Expect(_page.Locator(".tag-list")).ToContainTextAsync("testingtag", new() { Timeout = 10000 });
        await Expect(_page.Locator(".tag-list")).ToContainTextAsync("e2etag", new() { Timeout = 10000 });
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
}
