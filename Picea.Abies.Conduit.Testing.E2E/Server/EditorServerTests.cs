// =============================================================================
// Editor E2E Tests — InteractiveServer Mode
// =============================================================================

using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Trait("Category", "E2E")]
[Collection("ConduitServer")]
public sealed class EditorServerTests : IAsyncLifetime
{
    private readonly ConduitServerFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public EditorServerTests(ConduitServerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _seeder = new ApiSeeder(_fixture.ApiUrl);
    }

    public async Task DisposeAsync() => await _page.Context.DisposeAsync();

    [Fact]
    public async Task CreateArticle_WithAllFields_ShouldNavigateToArticlePage()
    {
        var username = $"srvedit{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");
        await LoginViaUi(email, "password123");

        await _page.NavigateInApp("/editor");
        await _page.WaitForSelectorAsync(".editor-page", new() { Timeout = 10000 });
        await _page.GetByPlaceholder("Article Title").WaitForAsync(new() { Timeout = 10000 });

        var title = $"SrvArticle {Guid.NewGuid():N}"[..40];
        const string description = "A description for server E2E testing";
        const string body = "This article was created by a server E2E test.";

        await _page.GetByPlaceholder("Article Title").FillAndWaitForPatch(title);
        await _page.GetByPlaceholder("What's this article about?").FillAndWaitForPatch(description);
        await _page.GetByPlaceholder("Write your article (in markdown)").FillAndWaitForPatch(body);

        var tagInput = _page.GetByPlaceholder("Enter tags");
        await tagInput.FillAndWaitForPatch("server");
        await tagInput.PressAsync("Enter");

        var publishBtn = _page.GetByRole(AriaRole.Button, new() { Name = "Publish Article" });
        await publishBtn.WaitForAsync(new() { Timeout = 10000 });
        await publishBtn.ClickAsync();

        await _page.WaitForURLAsync("**/article/**", new() { Timeout = 15000 });
        var currentPath = new Uri(_page.Url).AbsolutePath;

        if (!currentPath.StartsWith("/article/"))
        {
            var bodyText = await _page.EvaluateAsync<string>(
                "() => document.body?.innerText?.substring(0, 800) || 'empty'");
            throw new Exception(
                $"After clicking Publish, expected /article/*, got: {currentPath}\n" +
                $"Page body (first 800 chars): {bodyText}");
        }

        var slug = currentPath.Split('/').Last();
        await _seeder.WaitForArticleAsync(slug);
        await _page.NavigateInApp($"/article/{slug}");

        await Expect(_page.Locator(".banner h1")).ToContainTextAsync(title, new() { Timeout = 15000 });
    }

    [Fact]
    public async Task CreateArticle_WithTags_ShouldShowTagPillsBeforePublish()
    {
        var username = $"srvtgart{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");
        await LoginViaUi(email, "password123");

        await _page.NavigateInApp("/editor");
        await _page.WaitForSelectorAsync(".editor-page", new() { Timeout = 10000 });

        var tagInput = _page.GetByPlaceholder("Enter tags");
        await tagInput.FillAndWaitForPatch("alpha");
        await tagInput.PressAsync("Enter");
        await Expect(_page.Locator(".tag-list .tag-default").Nth(0)).ToContainTextAsync("alpha", new() { Timeout = 5000 });
        await Expect(tagInput).ToHaveValueAsync("", new() { Timeout = 5000 });

        await tagInput.FillAndWaitForPatch("beta");
        await tagInput.PressAsync("Enter");
        await Expect(_page.Locator(".tag-list .tag-default").Nth(1)).ToContainTextAsync("beta", new() { Timeout = 5000 });

        await Expect(_page.Locator(".tag-list .tag-default").Nth(0)).ToContainTextAsync("alpha");
        await Expect(_page.Locator(".tag-list .tag-default").Nth(1)).ToContainTextAsync("beta");
    }

    private async Task LoginViaUi(string email, string password)
    {
        await _page.GotoAsync("/login");
        await _page.WaitForSelectorAsync("h1:has-text('Sign in')");
        await _page.GetByPlaceholder("Email").FillAndWaitForPatch(email);
        await _page.GetByPlaceholder("Password").FillAndWaitForPatch(password);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
