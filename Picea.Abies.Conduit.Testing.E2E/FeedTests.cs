// =============================================================================
// Feed E2E Tests — Global feed, your feed, tag filter, pagination
// =============================================================================

using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Category("E2E")]
[ClassDataSource<ConduitAppFixture>(Shared = SharedType.Keyed, Key = "Conduit")]
[NotInParallel("Conduit")]
public sealed class FeedTests : IAsyncInitializer, IAsyncDisposable
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

    public async ValueTask DisposeAsync()
    {
        await _page.Context.DisposeAsync();
    }

    [Test]
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

        await Expect(_page.Locator(".article-preview").Filter(new() { HasText = article.Title }))
            .ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.Locator(".article-preview").First).ToBeVisibleAsync(
            new() { Timeout = 10000 });
    }

    [Test]
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

    [Test]
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

        var taggedPreview = _page.Locator(".article-preview").Filter(new() { HasText = taggedArticle.Title });
        await Expect(taggedPreview).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.Locator(".article-preview").Filter(new() { HasText = "Untagged" }))
            .ToHaveCountAsync(0, new() { Timeout = 10000 });
    }

    [Test]
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
        await _seeder.CreateArticleAsync(
            readerUser.Token,
            $"OwnPost {Guid.NewGuid():N}"[..30],
            "Reader article for feed context",
            "This is the reader's own article.");
        await _seeder.FollowUserAsync(readerUser.Token, author);

        await LoginViaUi(readerEmail, "password123");

        await _page.NavigateInApp("/");
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });
        await _page.Locator(".feed-toggle").GetByText("Your Feed").ClickAsync();
        await Expect(_page).ToHaveURLAsync(new Regex(@"/\?feed=following$"), new() { Timeout = 10000 });
        await _page.WaitForWasmReady();

        var followedPreview = _page.Locator(".article-preview").Filter(new() { HasText = article.Title });
        await Expect(followedPreview).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(followedPreview).ToContainTextAsync(author);
    }

    [Test]
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

        var preview = _page.Locator(".article-preview").Filter(
            new() { HasText = article.Title });
        await Expect(preview).ToBeVisibleAsync(new() { Timeout = 15000 });
        await Expect(preview).ToContainTextAsync(username);
        await Expect(preview).ToContainTextAsync(article.Description);
        await Expect(preview).ToContainTextAsync("metatag");
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

    [Test]
    public async Task HomeSidebar_ShouldRenderUiToastWrapper()
    {
        await _page.GotoAsync("/");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        await Expect(_page.Locator(".sidebar .conduit-sidebar-ui-proof")).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Test]
    public async Task GlobalFeed_ShouldBeActiveByDefault_WhenAnonymous()
    {
        await _page.GotoAsync("/");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        // Anonymous users land on Global Feed by default (no click needed)
        await Expect(_page.Locator(".feed-toggle .nav-link.active"))
            .ToContainTextAsync("Global Feed", new() { Timeout = 10000 });

        // "Your Feed" tab is not present for anonymous users
        await Expect(_page.Locator(".feed-toggle").GetByText("Your Feed"))
            .Not.ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    [Test]
    public async Task YourFeed_WhenNotFollowingAnyone_ShouldBeDefaultAndShowEmptyState()
    {
        var username = $"feedempty{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");
        await LoginViaUi(email, "password123");

        // After login the app navigates home; "Your Feed" is the default tab for logged-in users
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });
        await Expect(_page.Locator(".feed-toggle .nav-link.active"))
            .ToContainTextAsync("Your Feed", new() { Timeout = 10000 });

        // No articles because this user follows nobody
        await Expect(_page.Locator(".article-preview"))
            .ToContainTextAsync("No articles are here... yet.", new() { Timeout = 15000 });
    }

    [Test]
    public async Task GlobalFeed_Pagination_ShouldShowPaginationAndNavigateToPage2()
    {
        var username = $"feedpage{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");

        // Create 11 articles so the global feed is guaranteed to span at least 2 pages (page size = 10)
        ArticleSeedResult? lastArticle = null;
        for (var i = 1; i <= 11; i++)
        {
            lastArticle = await _seeder.CreateArticleAsync(
                user.Token,
                $"PagTest {i} {username}",
                "Pagination test description",
                "Pagination test body.");
        }
        // Ensure the read model has picked up our articles before navigating
        await _seeder.WaitForArticleAsync(lastArticle!.Slug);

        await _page.GotoAsync("/");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        // Pagination widget must now be visible (anonymous → Global Feed, ≥11 articles)
        await Expect(_page.Locator("ul.pagination")).ToBeVisibleAsync(new() { Timeout = 15000 });

        // Navigate to page 2
        await _page.Locator("ul.pagination .page-link", new() { HasText = "2" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(new Regex(@"/\?page=2$"), new() { Timeout = 10000 });

        // Page 2 should now be the active page
        await Expect(_page.Locator("ul.pagination .page-item.active"))
            .ToContainTextAsync("2", new() { Timeout = 10000 });

        // Articles are still rendered on page 2
        await Expect(_page.Locator(".article-preview").First)
            .ToBeVisibleAsync(new() { Timeout = 10000 });
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
