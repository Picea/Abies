// =============================================================================
// Null Fields and Security E2E Tests — Avatar fallbacks and XSS smoke checks
// =============================================================================

using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Category("E2E")]
[ClassDataSource<ConduitAppFixture>(Shared = SharedType.Keyed, Key = "Conduit")]
[NotInParallel("Conduit")]
public sealed class NullFieldsAndSecurityTests : IAsyncInitializer, IAsyncDisposable
{
    private const string DefaultAvatarUrl = "https://static.productionready.io/images/smiley-cyrus.jpg";

    private readonly ConduitAppFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public NullFieldsAndSecurityTests(ConduitAppFixture fixture)
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
    public async Task NewUser_ShouldShowDefaultAvatarInProfileAndNavbar()
    {
        var username = $"nullav{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");
        await LoginViaUi(email, "password123");

        await Expect(_page.Locator("nav .user-pic")).ToHaveAttributeAsync("src", DefaultAvatarUrl);

        await _page.GotoAsync($"/profile/{username}");
        await _page.WaitForWasmReady();

        await Expect(_page.Locator(".user-img")).ToHaveAttributeAsync("src", DefaultAvatarUrl);
        await Expect(_page.Locator(".user-info p")).ToHaveTextAsync(string.Empty);
    }

    [Test]
    public async Task ClearingImageAndBio_ShouldRestoreDefaultAvatarAndBlankBio()
    {
        var username = $"nullclr{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");

        await _seeder.UpdateUserAsync(
            user.Token,
            image: "https://example.com/avatar.png",
            username: username,
            bio: "Temporary bio",
            email: email);

        await _seeder.UpdateUserAsync(
            user.Token,
            image: string.Empty,
            username: username,
            bio: string.Empty,
            email: email);

        await LoginViaUi(email, "password123");
        await _page.GotoAsync($"/profile/{username}");
        await _page.WaitForWasmReady();

        await Expect(_page.Locator(".user-img")).ToHaveAttributeAsync("src", DefaultAvatarUrl);
        await Expect(_page.Locator(".user-info p")).ToHaveTextAsync(string.Empty);

    }

    [Test]
    public async Task NewUser_ShouldShowDefaultAvatarInArticleMeta()
    {
        var username = $"nullcmt{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"AvatarPost {Guid.NewGuid():N}"[..30],
            "Avatar fallback article",
            "Avatar fallback body.");
        await _seeder.WaitForArticleAsync(article.Slug);

        await LoginViaUi(email, "password123");
        await _page.GotoAsync($"/article/{article.Slug}");
        await _page.WaitForWasmReady();

        await Expect(_page.Locator(".article-meta img").First).ToHaveAttributeAsync("src", DefaultAvatarUrl);
    }

    [Test]
    public async Task MaliciousImagePayload_ShouldNotCreateExecutableAttributes()
    {
        var username = $"xssimg{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        await _seeder.UpdateUserAsync(
            user.Token,
            image: "https://example.com/pic.jpg\" onerror=\"alert(1)",
            username: username,
            bio: user.Bio,
            email: email);

        var dialogTriggered = false;
        _page.Dialog += (_, dialog) =>
        {
            dialogTriggered = true;
            _ = dialog.DismissAsync();
        };

        await LoginViaUi(email, "password123");
        await _page.GotoAsync($"/profile/{username}");
        await _page.WaitForWasmReady();
        await _page.WaitForTimeoutAsync(1000);

        var hasOnError = await _page.Locator(".user-img").EvaluateAsync<bool>("img => img.hasAttribute('onerror')");
        await Assert.That(hasOnError).IsFalse();
        await Assert.That(dialogTriggered).IsFalse();
    }

    [Test]
    public async Task MaliciousArticleContent_ShouldRenderAsTextNotHtml()
    {
        var username = $"xssart{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"SafeBody {Guid.NewGuid():N}"[..30],
            "Before <img src=x onerror=alert(1)> After",
            "Before <script>alert(1)</script> After");
        await _seeder.WaitForArticleAsync(article.Slug);

        var dialogTriggered = false;
        _page.Dialog += (_, dialog) =>
        {
            dialogTriggered = true;
            _ = dialog.DismissAsync();
        };

        await LoginViaUi(email, "password123");
        await _page.GotoAsync($"/article/{article.Slug}");
        await _page.WaitForWasmReady();
        await _page.WaitForTimeoutAsync(1000);

        await Expect(_page.Locator(".article-content")).ToContainTextAsync("<script>alert(1)</script>");
        await Expect(_page.Locator(".article-content p").First).ToContainTextAsync("Before <script>alert(1)</script> After");
        await Expect(_page.Locator(".article-content script")).ToHaveCountAsync(0);
        await Expect(_page.Locator(".article-content [onerror], .article-content [onload]"))
            .ToHaveCountAsync(0);
        await Assert.That(dialogTriggered).IsFalse();
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
