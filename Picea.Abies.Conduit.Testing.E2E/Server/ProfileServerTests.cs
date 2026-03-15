// =============================================================================
// Profile E2E Tests — InteractiveServer Mode
// =============================================================================

using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;
using Microsoft.Playwright;

namespace Picea.Abies.Conduit.Testing.E2E;

[Category("E2E")]
[ClassDataSource<ConduitServerFixture>(Shared = SharedType.Keyed, Key = "ConduitServer")]
[NotInParallel("ConduitServer")]
public sealed class ProfileServerTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitServerFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public ProfileServerTests(ConduitServerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _seeder = new ApiSeeder(_fixture.ApiUrl);
    }

    public async ValueTask DisposeAsync() => await _page.Context.DisposeAsync();

    [Test]
    public async Task ViewProfile_ShouldShowUsername()
    {
        var username = $"srvprf{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        await _seeder.CreateArticleAsync(
            user.Token,
            $"SrvProfile {Guid.NewGuid():N}"[..30],
            "Profile test",
            "Article body.");
        await _seeder.WaitForProfileAsync(username);

        await _page.GotoAsync($"/profile/{username}");

        await Expect(_page.Locator(".user-info h4")).ToContainTextAsync(username,
            new() { Timeout = 15000 });
    }

    [Test]
    public async Task FollowUser_WhenLoggedIn_ShouldToggleFollowButton()
    {
        var target = $"srvtgt{Guid.NewGuid():N}"[..20];
        var targetEmail = $"{target}@test.com";
        await _seeder.RegisterUserAsync(target, targetEmail, "password123");

        var follower = $"srvflw{Guid.NewGuid():N}"[..20];
        var followerEmail = $"{follower}@test.com";
        await _seeder.RegisterUserAsync(follower, followerEmail, "password123");

        await _seeder.WaitForProfileAsync(target);
        await _seeder.WaitForProfileAsync(follower);

        await LoginViaUi(followerEmail, "password123");

        await _page.NavigateInApp($"/profile/{target}");
        await Expect(_page.Locator(".user-info h4")).ToContainTextAsync(target,
            new() { Timeout = 15000 });

        var followBtn = _page.Locator("button:has-text('Follow')").First;
        await followBtn.ClickAsync();

        await Expect(_page.Locator($"button:has-text('Unfollow {target}')").First)
            .ToBeVisibleAsync(new() { Timeout = 10000 });
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
