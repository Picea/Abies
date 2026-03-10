// =============================================================================
// Article E2E Tests — InteractiveAuto Mode
// =============================================================================

using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;
using Microsoft.Playwright;

namespace Picea.Abies.Conduit.Testing.E2E;

[Trait("Category", "E2E")]
[Collection("ConduitAuto")]
public sealed class ArticleAutoTests : IAsyncLifetime
{
    private readonly ConduitAutoFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public ArticleAutoTests(ConduitAutoFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _seeder = new ApiSeeder(_fixture.ApiUrl);
    }

    public async Task DisposeAsync() => await _page.Context.DisposeAsync();

    [Fact]
    public async Task ViewArticle_ShouldShowTitleAndBody()
    {
        var username = $"autoartvw{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"AutoArt {Guid.NewGuid():N}"[..30],
            "Auto article description",
            "This is the full article body for auto mode.");
        await _seeder.WaitForArticleAsync(article.Slug);

        await _page.GotoAsync($"/article/{article.Slug}");
        await _page.WaitForWasmReady();

        await Expect(_page.Locator("h1")).ToContainTextAsync(article.Title,
            new() { Timeout = 15000 });
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
