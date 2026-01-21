using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace Abies.Conduit.E2E;

[Collection("Conduit collection")]
public class UserJourneysTests
{
    private readonly ConduitFixture _fixture;
    public UserJourneysTests(ConduitFixture fixture) => _fixture = fixture;

    private async Task<IPage> NewPageAsync()
    {
        var context = await _fixture.CreateContextAsync();
        var page = await context.NewPageAsync();
        _fixture.AttachPageDiagnostics(page);
        return page;
    }

    private static async Task AuthWithTokenAsync(IPage page, string baseUrl, string token)
    {
        await page.GotoAsync(baseUrl);
        await page.EvaluateAsync($"() => localStorage.setItem('jwt', '{token}')");
        await page.ReloadAsync();
    }

    [Fact]
    public async Task PublishArticle_AndSeeItInGlobalFeed()
    {
        var page = await NewPageAsync();
        var user = await _fixture.Api.RegisterUserAsync();

        await AuthWithTokenAsync(page, _fixture.AppBaseUrl, user.Token);

        await page.GetByRole(AriaRole.Link, new() { Name = "New Article" }).ClickAsync();
        await page.GetByLabel("Article Title").FillAsync("E2E Journey Article");
        await page.GetByLabel("What's this article about?").FillAsync("Journey");
        await page.GetByLabel("Write your article (in markdown)").FillAsync("Body content for journey test.");
        await page.GetByPlaceholder("Enter tags").FillAsync("journey");
        await page.Keyboard.PressAsync("Enter");
        await page.GetByRole(AriaRole.Button, new() { Name = "Publish Article" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "E2E Journey Article" })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Link, new() { Name = "home" }).ClickAsync();
        await Expect(page.GetByText("Global Feed")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "E2E Journey Article" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Register_Login_Logout_Journey()
    {
        var page = await NewPageAsync();
        var email = $"e2e{Guid.NewGuid():N}@example.com";
        var username = $"user{Guid.NewGuid():N}".Substring(0, 12);

        await page.GotoAsync($"{_fixture.AppBaseUrl}/register");
        await page.GetByPlaceholder("Username").FillAsync(username);
        await page.GetByPlaceholder("Email").FillAsync(email);
        await page.GetByPlaceholder("Password").FillAsync("Password1!");
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign up" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Settings" })).ToBeVisibleAsync();

        await page.GetByText("Or click here to logout").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Sign in" })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Link, new() { Name = "Sign in" }).ClickAsync();
        await page.GetByPlaceholder("Email").FillAsync(email);
        await page.GetByPlaceholder("Password").FillAsync("Password1!");
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Settings" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task UpdateSettings_PersistsChanges()
    {
        var page = await NewPageAsync();
        var user = await _fixture.Api.RegisterUserAsync();

        await AuthWithTokenAsync(page, _fixture.AppBaseUrl, user.Token);
        await page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await page.GetByPlaceholder("URL of profile picture").FillAsync("http://example.com/pic.png");
        await page.GetByLabel("Short bio about you").FillAsync("Hello from E2E");
        await page.GetByRole(AriaRole.Button, new() { Name = "Update Settings" }).ClickAsync();

        await Expect(page.GetByText("Your Feed")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await Expect(page.GetByPlaceholder("URL of profile picture")).ToHaveValueAsync("http://example.com/pic.png");
        await Expect(page.GetByLabel("Short bio about you")).ToHaveValueAsync("Hello from E2E");
    }

    [Fact]
    public async Task FollowUser_AndUnfollow_FromProfile()
    {
        var author = await _fixture.Api.RegisterUserAsync();
        var follower = await _fixture.Api.RegisterUserAsync();

        var page = await NewPageAsync();
        await AuthWithTokenAsync(page, _fixture.AppBaseUrl, follower.Token);

        await page.GotoAsync($"{_fixture.AppBaseUrl}/profile/{author.Username}");
        await page.GetByRole(AriaRole.Button, new() { Name = $"Follow {author.Username}" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = $"Unfollow {author.Username}" })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = $"Unfollow {author.Username}" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = $"Follow {author.Username}" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Comment_AddAndDelete()
    {
        var author = await _fixture.Api.RegisterUserAsync();
        var reader = await _fixture.Api.RegisterUserAsync();
        var slug = await _fixture.Api.CreateArticleAsync(author.Token, "Comment Journey", "Comments", "Body", []);

        var page = await NewPageAsync();
        await AuthWithTokenAsync(page, _fixture.AppBaseUrl, reader.Token);

        await page.GotoAsync($"{_fixture.AppBaseUrl}/article/{slug}");
        await Expect(page.GetByLabel("Write a comment...")).ToBeVisibleAsync();
        await page.GetByLabel("Write a comment...").FillAsync("Nice article!");
        await page.GetByRole(AriaRole.Button, new() { Name = "Post Comment" }).ClickAsync();
        var comment = page.GetByText("Nice article!");
        await Expect(comment).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Delete Comment" }).ClickAsync();
        await Expect(comment).Not.ToBeVisibleAsync(new() { Timeout = 60000 });
    }

    [Fact]
    public async Task FavoriteArticle_ShowsInFavoritesTab()
    {
        var author = await _fixture.Api.RegisterUserAsync();
        var reader = await _fixture.Api.RegisterUserAsync();
        var slug = await _fixture.Api.CreateArticleAsync(author.Token, "Fav Journey", "Favorites", "Body", ["fav"]);

        var page = await NewPageAsync();
        await AuthWithTokenAsync(page, _fixture.AppBaseUrl, reader.Token);

        await page.GotoAsync($"{_fixture.AppBaseUrl}/article/{slug}");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Favorite Article" })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Favorite Article" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Unfavorite Article" })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Link, new() { Name = author.Username }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Favorited Articles" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Fav Journey" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Pagination_NavigatesToSecondPage()
    {
        var author = await _fixture.Api.RegisterUserAsync();
        var reader = await _fixture.Api.RegisterUserAsync();

        for (int i = 0; i < 12; i++)
        {
            await _fixture.Api.CreateArticleAsync(author.Token, $"Paged {i}", "Pages", $"Body {i}", ["page"]);
        }

        var page = await NewPageAsync();
        await AuthWithTokenAsync(page, _fixture.AppBaseUrl, reader.Token);

        await Expect(page.GetByText("Global Feed")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "2" }).ClickAsync();
        await Expect(page.Locator("ul.pagination li .page-link[aria-current=\"page\"]")).ToHaveTextAsync("2");
    }
}
