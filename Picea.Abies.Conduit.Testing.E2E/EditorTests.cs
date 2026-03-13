// =============================================================================
// Editor E2E Tests — Create and edit articles
// =============================================================================

using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Trait("Category", "E2E")]
[Collection("Conduit")]
public sealed class EditorTests : IAsyncLifetime
{
    private readonly ConduitAppFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public EditorTests(ConduitAppFixture fixture)
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
    public async Task CreateArticle_WithAllFields_ShouldNavigateToArticlePage()
    {
        var username = $"editor{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");
        await LoginViaUi(email, "password123");

        var apiRequests = new System.Collections.Concurrent.ConcurrentBag<(string Method, string Url, int Status, string Body)>();
        _page.Response += async (_, response) =>
        {
            if (response.Url.Contains("/api/"))
            {
                var body = "";
                try
                { body = await response.TextAsync(); }
                catch { body = "(unreadable)"; }
                apiRequests.Add((response.Request.Method, response.Url, response.Status, body.Length > 500 ? body[..500] : body));
            }
        };

        await _page.NavigateInApp("/editor");
        await _page.WaitForSelectorAsync(".editor-page", new() { Timeout = 10000 });
        await _page.GetByPlaceholder("Article Title").WaitForAsync(new() { Timeout = 10000 });

        var title = $"E2E Test Article {Guid.NewGuid():N}"[..40];
        const string description = "A description for E2E testing";
        const string body = "This article was created by an E2E test.";

        await _page.GetByPlaceholder("Article Title").FillAsync(title);
        await _page.GetByPlaceholder("What's this article about?").FillAsync(description);
        await _page.GetByPlaceholder("Write your article (in markdown)").FillAsync(body);

        var tagInput = _page.GetByPlaceholder("Enter tags");
        await tagInput.FillAsync("e2e");
        await tagInput.PressAsync("Enter");
        await tagInput.FillAsync("testing");
        await tagInput.PressAsync("Enter");

        var publishBtn = _page.GetByRole(AriaRole.Button, new() { Name = "Publish Article" });
        await publishBtn.WaitForAsync(new() { Timeout = 10000 });
        await publishBtn.ClickAsync();

        await _page.WaitForTimeoutAsync(8000);
        var currentUrl = _page.Url;
        var currentPath = new Uri(currentUrl).AbsolutePath;

        var requestLog = string.Join("\n", apiRequests.Select(r => $"  {r.Method} {r.Url} => {r.Status}: {r.Body}"));

        if (!currentPath.StartsWith("/article/"))
        {
            var bodyText = await _page.EvaluateAsync<string>(
                "() => document.body?.innerText?.substring(0, 800) || 'empty'");
            throw new Exception(
                $"After clicking Publish, expected /article/*, got: {currentPath}\n" +
                $"Full URL: {currentUrl}\n" +
                $"API traffic:\n{requestLog}\n" +
                $"Page body (first 800 chars): {bodyText}");
        }

        var slug = currentPath.Split('/').Last();
        await _seeder.WaitForArticleAsync(slug);

        await _page.NavigateInApp($"/article/{slug}");

        await Expect(_page.Locator(".banner h1")).ToContainTextAsync(title, new() { Timeout = 15000 });
    }

    [Fact]
    public async Task EditArticle_ChangeTitle_ShouldReflectUpdatedTitle()
    {
        var username = $"editart{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"Original Title {Guid.NewGuid():N}"[..30],
            "Original description",
            "Original body content");
        await _seeder.WaitForArticleAsync(article.Slug);

        await LoginViaUi(email, "password123");

        await _page.NavigateInApp($"/editor/{article.Slug}");
        await _page.WaitForSelectorAsync(".editor-page", new() { Timeout = 10000 });

        await _page.WaitForFunctionAsync(
            "() => document.querySelector('[placeholder=\"Article Title\"]')?.value?.length > 0",
            null, new() { Timeout = 15000 });

        var newTitle = $"Updated Title {Guid.NewGuid():N}"[..30];
        await _page.GetByPlaceholder("Article Title").FillAsync(newTitle);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Publish Article" }).ClickAsync();

        await _page.WaitForFunctionAsync(
            "() => window.location.pathname.startsWith('/article/')",
            null, new() { Timeout = 15000 });

        var updatedSlug = new Uri(_page.Url).AbsolutePath.Split('/').Last();
        await _seeder.WaitForArticleWithTitleAsync(updatedSlug, newTitle);

        await _page.NavigateInApp($"/article/{updatedSlug}");

        await Expect(_page.Locator(".banner h1")).ToContainTextAsync(newTitle, new() { Timeout = 15000 });
    }

    [Fact]
    public async Task CreateArticle_WithTags_ShouldShowTagPillsBeforePublish()
    {
        var username = $"tagart{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");
        await LoginViaUi(email, "password123");

        await _page.NavigateInApp("/editor");
        await _page.WaitForSelectorAsync(".editor-page", new() { Timeout = 10000 });

        var tagInput = _page.GetByPlaceholder("Enter tags");
        await tagInput.FillAsync("alpha");
        await tagInput.PressAsync("Enter");
        await tagInput.FillAsync("beta");
        await tagInput.PressAsync("Enter");

        await Expect(_page.Locator(".tag-list .tag-default").Nth(0)).ToContainTextAsync("alpha");
        await Expect(_page.Locator(".tag-list .tag-default").Nth(1)).ToContainTextAsync("beta");
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
