using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E.AppHost;

[Category("E2E")]
[ClassDataSource<ConduitAppHostServerFixture>(Shared = SharedType.Keyed, Key = "ConduitAppHostServer")]
[NotInParallel("ConduitAppHostServer")]
public sealed class AppHostServerRegressionTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitAppHostServerFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public AppHostServerRegressionTests(ConduitAppHostServerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _seeder = new ApiSeeder(_fixture.ApiUrl);
    }

    public async ValueTask DisposeAsync() => await _page.Context.DisposeAsync();

    [Test]
    public async Task NewArticle_ShouldAppearAfterReturningHomeAndSwitchingGlobalFeed()
    {
        var username = $"srvhome{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");

        await LoginViaUi(email, "password123");
        await _page.NavigateInApp("/editor");
        await _page.WaitForSelectorAsync(".editor-page", new() { Timeout = 15000 });
        await _page.GetByPlaceholder("Article Title").FillAsync("Server apphost article");
        await _page.GetByPlaceholder("What's this article about?").FillAsync("Visible from home");
        await _page.GetByPlaceholder("Write your article (in markdown)").FillAsync("Content body");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Publish Article" }).ClickAsync();

        await _page.WaitForFunctionAsync("() => window.location.pathname.startsWith('/article/')", null, new() { Timeout = 15000 });
        await _page.Locator(".navbar-brand").ClickAsync();
        await _page.WaitForURLAsync("**/");
        var activeFeed = _page.Locator(".feed-toggle .nav-link.active");
        await activeFeed.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        if (!((await activeFeed.TextContentAsync())?.Contains("Global Feed") ?? false))
        {
            await _page.Locator(".feed-toggle .nav-link").Filter(new() { HasText = "Global Feed" }).ClickAsync();
            await Expect(_page.Locator(".feed-toggle .nav-link.active")).ToContainTextAsync("Global Feed", new() { Timeout = 10000 });
        }

        await Expect(_page.Locator(".article-preview").Filter(new() { HasText = "Server apphost article" }))
            .ToHaveCountAsync(1, new() { Timeout = 10000 });
    }

    [Test]
    public async Task DeleteArticle_AsAuthor_ShouldRemoveItFromGlobalFeed()
    {
        var username = $"srvdel{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"Srv Delete {Guid.NewGuid():N}"[..30],
            "Delete from AppHost server",
            "Delete body.");
        await _seeder.WaitForArticleAsync(article.Slug);

        await LoginViaUi(email, "password123");
        await _page.NavigateInApp($"/article/{article.Slug}");
        await _page.WaitForSelectorAsync("text='Delete Article'", new() { Timeout = 15000 });
        await _page.GetByText("Delete Article").First.ClickAsync();

        await _page.WaitForURLAsync("**/");
        await _seeder.WaitForArticleDeletedAsync(article.Slug);
        await Expect(_page.Locator(".article-preview").Filter(new() { HasText = article.Title }))
            .ToHaveCountAsync(0, new() { Timeout = 10000 });
    }

    [Test]
    public async Task FeedTabs_ShouldShowExactlyOneActiveTab_WhenSwitchingBetweenYourAndGlobalFeed()
    {
        var author = $"srvtab{Guid.NewGuid():N}"[..20];
        var authorEmail = $"{author}@test.com";
        var authorUser = await _seeder.RegisterUserAsync(author, authorEmail, "password123");
        await _seeder.CreateArticleAsync(authorUser.Token, $"Srv Tab {Guid.NewGuid():N}"[..30], "desc", "body");

        var reader = $"srvrd{Guid.NewGuid():N}"[..20];
        var readerEmail = $"{reader}@test.com";
        var readerUser = await _seeder.RegisterUserAsync(reader, readerEmail, "password123");
        await _seeder.FollowUserAsync(readerUser.Token, author);

        await LoginViaUi(readerEmail, "password123");
        await _page.Locator(".feed-toggle .nav-link").Filter(new() { HasText = "Your Feed" }).ClickAsync();

        await Expect(_page.Locator(".feed-toggle .nav-link.active")).ToHaveCountAsync(1, new() { Timeout = 10000 });
        await Expect(_page.Locator(".feed-toggle .nav-link.active")).ToContainTextAsync("Your Feed", new() { Timeout = 10000 });

        await _page.Locator(".feed-toggle .nav-link").Filter(new() { HasText = "Global Feed" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(new Regex(@"/$"), new() { Timeout = 10000 });

        await Expect(_page.Locator(".feed-toggle .nav-link.active")).ToHaveCountAsync(1, new() { Timeout = 10000 });
        await Expect(_page.Locator(".feed-toggle .nav-link.active")).ToContainTextAsync("Global Feed", new() { Timeout = 10000 });
    }

    private async Task LoginViaUi(string email, string password)
    {
        await _page.GotoAsync("/login");
        await _page.WaitForSelectorAsync("h1:has-text('Sign in')");
        await _page.GetByPlaceholder("Email").FillAndWaitForPatch(email);
        await _page.GetByPlaceholder("Password").FillAndWaitForPatch(password);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await Expect(_page.Locator(".navbar")).ToContainTextAsync(email.Split('@')[0], new() { Timeout = 15000 });
    }

    private static ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);

    private static IPageAssertions Expect(IPage page) => Assertions.Expect(page);
}
