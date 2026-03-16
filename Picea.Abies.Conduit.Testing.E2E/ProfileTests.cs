// =============================================================================
// Profile E2E Tests — View profile, follow/unfollow, article tabs
// =============================================================================

using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Category("E2E")]
[ClassDataSource<ConduitAppFixture>(Shared = SharedType.Keyed, Key = "Conduit")]
[NotInParallel("Conduit")]
public sealed class ProfileTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitAppFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public ProfileTests(ConduitAppFixture fixture)
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
    public async Task ViewProfile_ShouldShowUsernameAndArticles()
    {
        var username = $"profvw{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"Profile Article {Guid.NewGuid():N}"[..30],
            "For profile test",
            "Article body.");

        await _seeder.WaitForProfileAsync(username);
        await _seeder.WaitForArticleAsync(article.Slug);

        await _page.GotoAsync($"/profile/{username}");
        await _page.WaitForWasmReady();
        await Expect(_page.Locator(".user-info h4")).ToContainTextAsync(username, new() { Timeout = 15000 });
    }

    [Test]
    public async Task FollowUser_WhenLoggedIn_ShouldToggleFollowButton()
    {
        var target = $"proftgt{Guid.NewGuid():N}"[..20];
        var targetEmail = $"{target}@test.com";
        await _seeder.RegisterUserAsync(target, targetEmail, "password123");

        var follower = $"profflw{Guid.NewGuid():N}"[..20];
        var followerEmail = $"{follower}@test.com";
        await _seeder.RegisterUserAsync(follower, followerEmail, "password123");

        await _seeder.WaitForProfileAsync(target);
        await _seeder.WaitForProfileAsync(follower);

        await LoginViaUi(followerEmail, "password123");

        await _page.NavigateInApp($"/profile/{target}");

        await Expect(_page.Locator(".user-info h4")).ToContainTextAsync(target, new() { Timeout = 15000 });

        var followBtn = _page.Locator("button:has-text('Follow')").First;
        await followBtn.ClickAsync();

        await Expect(_page.Locator($"button:has-text('Unfollow {target}')").First)
            .ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Test]
    public async Task UnfollowUser_WhenFollowing_ShouldToggleBackToFollow()
    {
        var target = $"profunf{Guid.NewGuid():N}"[..20];
        var targetEmail = $"{target}@test.com";
        await _seeder.RegisterUserAsync(target, targetEmail, "password123");

        var follower = $"profuf2{Guid.NewGuid():N}"[..20];
        var followerEmail = $"{follower}@test.com";
        var followerUser = await _seeder.RegisterUserAsync(follower, followerEmail, "password123");
        await _seeder.FollowUserAsync(followerUser.Token, target);

        await _seeder.WaitForProfileAsync(target);
        await _seeder.WaitForProfileAsync(follower);

        await LoginViaUi(followerEmail, "password123");

        await _page.NavigateInApp($"/profile/{target}");

        await Expect(_page.Locator($"button:has-text('Unfollow {target}')").First)
            .ToBeVisibleAsync(new() { Timeout = 15000 });

        var unfollowBtn = _page.Locator($"button:has-text('Unfollow {target}')").First;
        await unfollowBtn.ClickAsync();

        await Expect(_page.Locator($"button:has-text('Follow {target}')").First)
            .ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Test]
    public async Task ProfileTabs_ShouldSwitchBetweenMyArticlesAndFavorited()
    {
        var author = $"prftab{Guid.NewGuid():N}"[..20];
        var authorEmail = $"{author}@test.com";
        var authorUser = await _seeder.RegisterUserAsync(author, authorEmail, "password123");
        await _seeder.CreateArticleAsync(
            authorUser.Token,
            $"My Article {Guid.NewGuid():N}"[..30],
            "Author's own",
            "Body of author's article.");

        var other = $"prfoth{Guid.NewGuid():N}"[..20];
        var otherEmail = $"{other}@test.com";
        var otherUser = await _seeder.RegisterUserAsync(other, otherEmail, "password123");
        var otherArticle = await _seeder.CreateArticleAsync(
            otherUser.Token,
            $"Other Article {Guid.NewGuid():N}"[..30],
            "Other's article",
            "Body of other's article.");
        await _seeder.FavoriteArticleAsync(authorUser.Token, otherArticle.Slug);

        await _seeder.WaitForProfileAsync(author);
        await _seeder.WaitForProfileAsync(other);

        await LoginViaUi(authorEmail, "password123");

        await _page.NavigateInApp($"/profile/{author}");

        await Expect(_page.Locator(".user-info h4")).ToContainTextAsync(author, new() { Timeout = 15000 });
        await Expect(_page.Locator(".article-preview").Filter(new() { HasText = "My Article" }))
            .ToHaveCountAsync(1, new() { Timeout = 10000 });

        await Expect(_page.Locator(".articles-toggle .nav-link.active"))
            .ToContainTextAsync("My Articles");

        await _page.Locator(".articles-toggle .nav-link").Filter(new() { HasText = "Favorited" }).ClickAsync();

        await Expect(_page.Locator(".articles-toggle .nav-link.active"))
            .ToContainTextAsync("Favorited", new() { Timeout = 10000 });
        await Expect(_page.Locator(".article-preview").Filter(new() { HasText = otherArticle.Title }))
            .ToHaveCountAsync(1, new() { Timeout = 10000 });
        await Expect(_page.Locator(".article-preview").Filter(new() { HasText = "My Article" }))
            .ToHaveCountAsync(0, new() { Timeout = 10000 });
    }

    [Test]
    public async Task OwnProfile_ShouldShowEditSettingsLink()
    {
        var username = $"prfown{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");

        await _seeder.WaitForProfileAsync(username);

        await LoginViaUi(email, "password123");

        await _page.NavigateInApp($"/profile/{username}");

        await Expect(_page.Locator(".user-info h4")).ToContainTextAsync(username, new() { Timeout = 15000 });

        await Expect(_page.GetByText("Edit Profile Settings"))
            .ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Test]
    public async Task ViewingAnotherUsersProfile_ShouldNotShowEditSettingsLink()
    {
        var target = $"prfoth2{Guid.NewGuid():N}"[..20];
        var targetEmail = $"{target}@test.com";
        await _seeder.RegisterUserAsync(target, targetEmail, "password123");

        var viewer = $"prfview{Guid.NewGuid():N}"[..20];
        var viewerEmail = $"{viewer}@test.com";
        await _seeder.RegisterUserAsync(viewer, viewerEmail, "password123");

        await _seeder.WaitForProfileAsync(target);
        await LoginViaUi(viewerEmail, "password123");
        await _page.NavigateInApp($"/profile/{target}");

        await Expect(_page.Locator(".user-info a[href='/settings']")).ToHaveCountAsync(0, new() { Timeout = 10000 });
        await Expect(_page.Locator($"button:has-text('Follow {target}')").First)
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
