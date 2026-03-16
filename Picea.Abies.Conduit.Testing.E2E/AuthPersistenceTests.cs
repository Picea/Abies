// =============================================================================
// Auth Persistence E2E Tests — Hard reload should preserve authenticated state
// =============================================================================

using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Category("E2E")]
[ClassDataSource<ConduitAppFixture>(Shared = SharedType.Keyed, Key = "Conduit")]
[NotInParallel("Conduit")]
public sealed class AuthPersistenceTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitAppFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public AuthPersistenceTests(ConduitAppFixture fixture)
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
    public async Task ReloadingSettingsPage_ShouldPreserveSessionAndProtectedRoute()
    {
        var username = $"persist{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");

        await LoginViaUi(email, "password123");
        await _page.GotoAsync("/settings");
        await _page.WaitForWasmReady();
        await _page.ReloadAsync();
        await _page.WaitForWasmReady();

        await Expect(_page).ToHaveURLAsync(new Regex("/settings$"), new() { Timeout = 10000 });
        await Expect(_page.Locator(".settings-page")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.GetByPlaceholder("Email")).ToHaveValueAsync(email, new() { Timeout = 10000 });
        await Expect(_page.Locator(".navbar")).ToContainTextAsync(username, new() { Timeout = 10000 });
    }

    [Test]
    public async Task ReloadingFollowingFeed_ShouldPreserveSessionAndFeedRoute()
    {
        var author = $"peraut{Guid.NewGuid():N}"[..20];
        var authorEmail = $"{author}@test.com";
        var authorUser = await _seeder.RegisterUserAsync(author, authorEmail, "password123");
        var article = await _seeder.CreateArticleAsync(
            authorUser.Token,
            $"Persist Feed {Guid.NewGuid():N}"[..30],
            "Persisted feed description",
            "Persisted feed body.");
        await _seeder.WaitForArticleAsync(article.Slug);

        var reader = $"perrea{Guid.NewGuid():N}"[..20];
        var readerEmail = $"{reader}@test.com";
        var readerUser = await _seeder.RegisterUserAsync(reader, readerEmail, "password123");
        await _seeder.FollowUserAsync(readerUser.Token, author);

        await LoginViaUi(readerEmail, "password123");
        await _page.GotoAsync("/?feed=following");
        await _page.WaitForWasmReady();
        await _page.ReloadAsync();
        await _page.WaitForWasmReady();

        await Expect(_page).ToHaveURLAsync(new Regex(@"/\?feed=following$"), new() { Timeout = 10000 });
        await Expect(_page.Locator(".feed-toggle .nav-link.active"))
            .ToContainTextAsync("Your Feed", new() { Timeout = 10000 });
        await Expect(_page.Locator(".article-preview").Filter(new() { HasText = article.Title }))
            .ToHaveCountAsync(1, new() { Timeout = 10000 });
        await Expect(_page.Locator(".navbar")).ToContainTextAsync(reader, new() { Timeout = 10000 });
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
