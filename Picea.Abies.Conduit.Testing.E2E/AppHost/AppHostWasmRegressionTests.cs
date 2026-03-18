using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E.AppHost;

[Category("E2E")]
[ClassDataSource<ConduitAppHostWasmFixture>(Shared = SharedType.Keyed, Key = "ConduitAppHostWasm")]
[NotInParallel("ConduitAppHostWasm")]
public sealed class AppHostWasmRegressionTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitAppHostWasmFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public AppHostWasmRegressionTests(ConduitAppHostWasmFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _seeder = new ApiSeeder(_fixture.ApiUrl);
    }

    public async ValueTask DisposeAsync() => await _page.Context.DisposeAsync();

    [Test]
    public async Task NewArticle_ShouldAppearAfterReturningHomeAndSwitchingGlobalFeed()
    {
        var username = $"wasmhome{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");

        await LoginViaUi(email, "password123");
        await _page.NavigateInApp("/editor");
        await _page.WaitForSelectorAsync(".editor-page", new() { Timeout = 15000 });
        await _page.WaitForWasmReady();
        await _page.GetByPlaceholder("Article Title").FillAsync("Wasm apphost article");
        await _page.GetByPlaceholder("What's this article about?").FillAsync("Visible from home");
        await _page.GetByPlaceholder("Write your article (in markdown)").FillAsync("Content body");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Publish Article" }).ClickAsync();

        await _page.WaitForFunctionAsync("() => window.location.pathname.startsWith('/article/')", null, new() { Timeout = 15000 });
        await _page.Locator(".navbar-brand").ClickAsync();
        await _page.WaitForURLAsync("**/");
        await _page.WaitForWasmReady();
        await Expect(_page.Locator(".feed-toggle .nav-link.active")).ToContainTextAsync("Global Feed", new() { Timeout = 10000 });
        await Expect(_page.Locator(".article-preview").Filter(new() { HasText = "Wasm apphost article" }))
            .ToHaveCountAsync(1, new() { Timeout = 10000 });
    }

    [Test]
    public async Task Refresh_ShouldKeepUserLoggedIn()
    {
        var username = $"wasmkeep{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");

        await LoginViaUi(email, "password123");
        await _page.GotoAsync("/settings");
        await _page.WaitForWasmReady();
        await _page.ReloadAsync();
        await _page.WaitForWasmReady();

        await Expect(_page.Locator(".navbar")).ToContainTextAsync(username, new() { Timeout = 10000 });
        await Expect(_page.Locator(".settings-page")).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Test]
    public async Task Favoriting_OtherUsersArticle_ShouldShowInFavoritedArticles()
    {
        var author = $"wasmfa{Guid.NewGuid():N}"[..20];
        var authorEmail = $"{author}@test.com";
        var authorUser = await _seeder.RegisterUserAsync(author, authorEmail, "password123");
        var article = await _seeder.CreateArticleAsync(
            authorUser.Token,
            $"Wasm Fav {Guid.NewGuid():N}"[..30],
            "Favorite from AppHost wasm",
            "Favorite body.");
        await _seeder.WaitForArticleAsync(article.Slug);

        var reader = $"wasmfb{Guid.NewGuid():N}"[..20];
        var readerEmail = $"{reader}@test.com";
        await _seeder.RegisterUserAsync(reader, readerEmail, "password123");

        await LoginViaUi(readerEmail, "password123");
        await _page.WaitForFunctionAsync("() => window.sessionStorage.getItem('conduit.session') !== null", null, new() { Timeout = 10000 });
        await _page.GotoAsync($"/article/{article.Slug}");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync(".article-actions, .article-meta", new() { Timeout = 15000 });

        await _page.Locator(".article-actions button.btn-outline-primary, .article-meta button.btn-outline-primary").First.ClickAsync();
        await Expect(_page.Locator(".article-actions button.btn-primary, .article-meta button.btn-primary").First)
            .ToContainTextAsync("1", new() { Timeout = 10000 });

        await _page.GotoAsync($"/profile/{reader}/favorites");
        await _page.WaitForWasmReady();
        await Expect(_page.Locator(".article-preview").Filter(new() { HasText = article.Title }))
            .ToHaveCountAsync(1, new() { Timeout = 10000 });
    }

    [Test]
    public async Task OwnArticle_ShouldNotShowFavoriteActionOnArticlePage()
    {
        var username = $"wasmown{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"Wasm Own {Guid.NewGuid():N}"[..30],
            "Own article",
            "Body.");
        await _seeder.WaitForArticleAsync(article.Slug);

        await LoginViaUi(email, "password123");
        await _page.WaitForFunctionAsync("() => window.sessionStorage.getItem('conduit.session') !== null", null, new() { Timeout = 10000 });
        await _page.GotoAsync($"/article/{article.Slug}");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync(".article-actions, .article-meta", new() { Timeout = 15000 });

        await Expect(_page.GetByText("Delete Article").First).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.Locator(".article-actions button.btn-outline-primary, .article-meta button.btn-outline-primary"))
            .ToHaveCountAsync(0, new() { Timeout = 10000 });
    }

    [Test]
    public async Task FollowingAuthor_ShouldPopulateYourFeedAndExcludeUnfollowedArticles()
    {
        var followedAuthor = $"followa{Guid.NewGuid():N}"[..20];
        var followedAuthorEmail = $"{followedAuthor}@test.com";
        var followedAuthorUser = await _seeder.RegisterUserAsync(followedAuthor, followedAuthorEmail, "password123");
        var followedArticle = await _seeder.CreateArticleAsync(
            followedAuthorUser.Token,
            $"Followed AppHost {Guid.NewGuid():N}"[..30],
            "Should appear in your feed",
            "Body.");

        var stranger = $"strange{Guid.NewGuid():N}"[..20];
        var strangerEmail = $"{stranger}@test.com";
        var strangerUser = await _seeder.RegisterUserAsync(stranger, strangerEmail, "password123");
        var strangerArticle = await _seeder.CreateArticleAsync(
            strangerUser.Token,
            $"Unfollowed AppHost {Guid.NewGuid():N}"[..30],
            "Should not appear in your feed",
            "Body.");

        var reader = $"feedusr{Guid.NewGuid():N}"[..20];
        var readerEmail = $"{reader}@test.com";
        var readerUser = await _seeder.RegisterUserAsync(reader, readerEmail, "password123");
        await _seeder.FollowUserAsync(readerUser.Token, followedAuthor);
        await _seeder.WaitForArticleAsync(followedArticle.Slug);
        await _seeder.WaitForArticleAsync(strangerArticle.Slug);

        await LoginViaUi(readerEmail, "password123");
        await _page.GotoAsync("/?feed=following");
        await _page.WaitForWasmReady();

        var followedPreview = _page.Locator(".article-preview").Filter(new() { HasText = followedArticle.Title });
        var strangerPreview = _page.Locator(".article-preview").Filter(new() { HasText = strangerArticle.Title });
        await Expect(_page).ToHaveURLAsync(new Regex(@"/\?feed=following$"), new() { Timeout = 10000 });
        await Expect(followedPreview).ToHaveCountAsync(1, new() { Timeout = 10000 });
        await Expect(followedPreview).ToContainTextAsync(followedAuthor);
        await Expect(strangerPreview).ToHaveCountAsync(0, new() { Timeout = 10000 });
    }

    [Test]
    public async Task MyArticles_ShouldOnlyShowAuthoredArticles_NotFavoritedOnes()
    {
        var owner = $"myarts{Guid.NewGuid():N}"[..20];
        var ownerEmail = $"{owner}@test.com";
        var ownerUser = await _seeder.RegisterUserAsync(owner, ownerEmail, "password123");
        var ownArticle = await _seeder.CreateArticleAsync(
            ownerUser.Token,
            $"Own AppHost {Guid.NewGuid():N}"[..30],
            "This is mine",
            "Body.");

        var other = $"otharts{Guid.NewGuid():N}"[..20];
        var otherEmail = $"{other}@test.com";
        var otherUser = await _seeder.RegisterUserAsync(other, otherEmail, "password123");
        var otherArticle = await _seeder.CreateArticleAsync(
            otherUser.Token,
            $"Other AppHost {Guid.NewGuid():N}"[..30],
            "This is theirs",
            "Body.");

        await _seeder.FavoriteArticleAsync(ownerUser.Token, otherArticle.Slug);
        await _seeder.WaitForArticleAsync(ownArticle.Slug);
        await _seeder.WaitForArticleAsync(otherArticle.Slug);

        await LoginViaUi(ownerEmail, "password123");
        await _page.GotoAsync($"/profile/{owner}");
        await _page.WaitForWasmReady();

        var ownPreview = _page.Locator(".article-preview").Filter(new() { HasText = ownArticle.Title });
        var otherPreview = _page.Locator(".article-preview").Filter(new() { HasText = otherArticle.Title });
        await Expect(_page).ToHaveURLAsync(new Regex($@"/profile/{owner}$"), new() { Timeout = 10000 });
        await Expect(_page.Locator(".articles-toggle .nav-link.active")).ToContainTextAsync("My Articles", new() { Timeout = 10000 });
        await Expect(ownPreview).ToHaveCountAsync(1, new() { Timeout = 10000 });
        await Expect(otherPreview).ToHaveCountAsync(0, new() { Timeout = 10000 });

        await _page.Locator(".articles-toggle .nav-link").Filter(new() { HasText = "Favorited" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(new Regex($@"/profile/{owner}/favorites$"), new() { Timeout = 10000 });
        await Expect(otherPreview).ToHaveCountAsync(1, new() { Timeout = 10000 });
    }

    [Test]
    public async Task FeedTabs_ShouldShowExactlyOneActiveTab_WhenSwitchingBetweenYourAndGlobalFeed()
    {
        var author = $"tabauth{Guid.NewGuid():N}"[..20];
        var authorEmail = $"{author}@test.com";
        var authorUser = await _seeder.RegisterUserAsync(author, authorEmail, "password123");
        await _seeder.CreateArticleAsync(authorUser.Token, $"Tab Art {Guid.NewGuid():N}"[..30], "desc", "body");

        var reader = $"tabread{Guid.NewGuid():N}"[..20];
        var readerEmail = $"{reader}@test.com";
        var readerUser = await _seeder.RegisterUserAsync(reader, readerEmail, "password123");
        await _seeder.FollowUserAsync(readerUser.Token, author);

        await LoginViaUi(readerEmail, "password123");
        await _page.GotoAsync("/?feed=following");
        await _page.WaitForWasmReady();

        await Expect(_page.Locator(".feed-toggle .nav-link.active")).ToHaveCountAsync(1, new() { Timeout = 10000 });
        await Expect(_page.Locator(".feed-toggle .nav-link.active")).ToContainTextAsync("Your Feed", new() { Timeout = 10000 });

        await _page.Locator(".feed-toggle .nav-link").Filter(new() { HasText = "Global Feed" }).ClickAsync();
        await _page.WaitForWasmReady();

        if (!((await _page.Locator(".feed-toggle .nav-link.active").TextContentAsync())?.Contains("Global Feed") ?? false))
        {
            await _page.Locator(".feed-toggle .nav-link").Filter(new() { HasText = "Global Feed" }).ClickAsync();
            await _page.WaitForWasmReady();
        }

        await Expect(_page.Locator(".feed-toggle .nav-link.active")).ToHaveCountAsync(1, new() { Timeout = 10000 });
        await Expect(_page.Locator(".feed-toggle .nav-link.active")).ToContainTextAsync("Global Feed", new() { Timeout = 10000 });
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
        await _page.WaitForFunctionAsync("() => window.sessionStorage.getItem('conduit.session') !== null", null, new() { Timeout = 10000 });
    }

    private static ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);

    private static IPageAssertions Expect(IPage page) => Assertions.Expect(page);
}
