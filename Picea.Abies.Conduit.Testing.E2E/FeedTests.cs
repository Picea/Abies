// =============================================================================
// Feed E2E Tests — Global feed, your feed, tag filter, pagination
// =============================================================================

using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;
using Microsoft.Playwright;

namespace Picea.Abies.Conduit.Testing.E2E;

[Trait("Category", "E2E")]
[Collection("Conduit")]
public sealed class FeedTests : IAsyncLifetime
{
    private readonly ConduitAppFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public FeedTests(ConduitAppFixture fixture)
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
    public async Task GlobalFeed_WithArticles_ShouldShowArticlePreviews()
    {
        var username = $"feedglo{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"GloFeed {Guid.NewGuid():N}"[..30],
            "Description for global feed",
            "Body of global feed article.");
        await _seeder.WaitForArticleAsync(article.Slug);

        await _page.GotoAsync("/");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        await _page.Locator(".feed-toggle").GetByText("Global Feed").ClickAsync();
        await _page.WaitForTimeoutAsync(2000);

        await Expect(_page.Locator(".article-preview").First).ToBeVisibleAsync(
            new() { Timeout = 10000 });
    }

    [Fact]
    public async Task TagSidebar_ShouldShowPopularTags()
    {
        var username = $"feedtag{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var uniqueTag = $"tag{Guid.NewGuid():N}"[..15];
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"Tagged Article {Guid.NewGuid():N}"[..30],
            "Tagged description",
            "Body with tags.",
            [uniqueTag]);
        await _seeder.WaitForArticleAsync(article.Slug);

        await _page.GotoAsync("/");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        await Expect(_page.Locator(".sidebar .tag-list")).ToContainTextAsync(uniqueTag,
            new() { Timeout = 15000 });
    }

    [Fact]
    public async Task ClickTag_ShouldFilterFeedByTag()
    {
        var username = $"feedflt{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var uniqueTag = $"flt{Guid.NewGuid():N}"[..15];
        var taggedArticle = await _seeder.CreateArticleAsync(
            user.Token,
            $"Tagged {Guid.NewGuid():N}"[..30],
            "Has the filter tag",
            "Body.",
            [uniqueTag]);
        await _seeder.CreateArticleAsync(
            user.Token,
            $"Untagged {Guid.NewGuid():N}"[..30],
            "No special tag",
            "Body without tag.");
        await _seeder.WaitForArticleAsync(taggedArticle.Slug);

        await _page.GotoAsync("/");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        var tagLocator = _page.Locator($".sidebar .tag-list >> text={uniqueTag}");
        await tagLocator.WaitForAsync(new() { Timeout = 15000 });
        await tagLocator.ClickAsync();

        await Expect(_page.Locator(".feed-toggle .nav-link.active"))
            .ToContainTextAsync(uniqueTag, new() { Timeout = 10000 });
    }

    [Fact]
    public async Task YourFeed_WhenFollowingUser_ShouldShowTheirArticles()
    {
        var author = $"feedaut{Guid.NewGuid():N}"[..20];
        var authorEmail = $"{author}@test.com";
        var authorUser = await _seeder.RegisterUserAsync(author, authorEmail, "password123");
        var article = await _seeder.CreateArticleAsync(
            authorUser.Token,
            $"FollowedPost {Guid.NewGuid():N}"[..30],
            "For your feed",
            "This should appear in your feed.");
        await _seeder.WaitForArticleAsync(article.Slug);

        var reader = $"feedrd{Guid.NewGuid():N}"[..20];
        var readerEmail = $"{reader}@test.com";
        var readerUser = await _seeder.RegisterUserAsync(reader, readerEmail, "password123");
        await _seeder.FollowUserAsync(readerUser.Token, author);

        await LoginViaUi(readerEmail, "password123");

        await _page.NavigateInApp("/");
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });
        await _page.Locator(".feed-toggle").GetByText("Your Feed").ClickAsync();
        await _page.WaitForTimeoutAsync(2000);

        await Expect(_page.Locator(".article-preview").First).ToBeVisibleAsync(
            new() { Timeout = 10000 });
    }

    [Fact]
    public async Task ArticlePreview_ShouldShowMetadata()
    {
        var username = $"feedmta{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"Preview Meta {Guid.NewGuid():N}"[..30],
            "Preview description text",
            "Body of preview article.",
            ["metatag"]);
        await _seeder.WaitForArticleAsync(article.Slug);

        await _page.GotoAsync("/");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });
        await _page.Locator(".feed-toggle").GetByText("Global Feed").ClickAsync();
        await _page.WaitForTimeoutAsync(2000);

        var preview = _page.Locator(".article-preview").Filter(
            new() { HasText = article.Title });
        await Expect(preview).ToBeVisibleAsync(new() { Timeout = 15000 });
        await Expect(preview).ToContainTextAsync(username);
        await Expect(preview).ToContainTextAsync(article.Description);
    }

    [Fact]
    public async Task HomeBanner_ShouldShowConduitBranding()
    {
        await _page.GotoAsync("/");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        await Expect(_page.Locator(".banner h1")).ToContainTextAsync("conduit");
        await Expect(_page.Locator(".banner p")).ToContainTextAsync(
            "A place to share your knowledge.");
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
