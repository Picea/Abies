using Microsoft.Playwright;

namespace Abies.Conduit.E2E;

/// <summary>
/// E2E tests for follow user journeys.
/// Covers: Follow user, Unfollow user, Your Feed with followed users' articles
/// </summary>
public class FollowTests : PlaywrightFixture
{
    [Fact]
    public async Task FollowUser_FromArticlePage_UpdatesButton()
    {
        // Create article as first user
        var (authorName, _, _) = await RegisterTestUserAsync();
        var title = $"Follow Test Article {Guid.NewGuid():N}";
        var slug = await CreateTestArticleAsync(title, "Description", "Body", "follow-tag");

        // Logout
        await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await Page.WaitForURLAsync("**/settings", new() { Timeout = 10000 });
        await Page.GetByRole(AriaRole.Button, new() { Name = "logout", Exact = false }).ClickAsync();
        await Page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        // Login as different user
        await RegisterTestUserAsync();

        // Navigate to the article
        await Page.GotoAsync($"/article/{slug}");
        await Expect(Page.GetByTestId("article-page")).ToBeVisibleAsync();

        // Find the follow button - use .First because it appears twice (banner and article-actions)
        var followButton = Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex($"Follow {authorName}") }).First;
        await Expect(followButton).ToBeVisibleAsync();

        // Click to follow
        await followButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Button should now say "Unfollow" - use .First
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex($"Unfollow {authorName}") }).First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task UnfollowUser_FromArticlePage_UpdatesButton()
    {
        // Create article as first user
        var (authorName, _, _) = await RegisterTestUserAsync();
        var title = $"Unfollow Test Article {Guid.NewGuid():N}";
        var slug = await CreateTestArticleAsync(title, "Description", "Body", "unfollow-tag");

        // Logout
        await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await Page.WaitForURLAsync("**/settings", new() { Timeout = 10000 });
        await Page.GetByRole(AriaRole.Button, new() { Name = "logout", Exact = false }).ClickAsync();
        await Page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        // Login as different user
        await RegisterTestUserAsync();

        // Navigate to the article and follow - use .First because buttons appear twice
        await Page.GotoAsync($"/article/{slug}");
        await Expect(Page.GetByTestId("article-page")).ToBeVisibleAsync();

        var followButton = Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex($"Follow {authorName}") }).First;
        await followButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Now unfollow - use .First
        var unfollowButton = Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex($"Unfollow {authorName}") }).First;
        await unfollowButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Button should now say "Follow" again - use .First
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex($"Follow {authorName}") }).First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task FollowUser_FromProfilePage_UpdatesButton()
    {
        // Create article as first user to have something on their profile
        var (authorName, _, _) = await RegisterTestUserAsync();
        await CreateTestArticleAsync($"Profile Follow Test {Guid.NewGuid():N}", "Description", "Body", "profile-follow");

        // Logout
        await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await Page.WaitForURLAsync("**/settings", new() { Timeout = 10000 });
        await Page.GetByRole(AriaRole.Button, new() { Name = "logout", Exact = false }).ClickAsync();
        await Page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        // Login as different user
        await RegisterTestUserAsync();

        // Navigate to author's profile
        await Page.GotoAsync($"/profile/{authorName}");
        await Expect(Page.Locator(".profile-page")).ToBeVisibleAsync();

        // Find the follow button on profile page
        var followButton = Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex($"Follow {authorName}") });
        await Expect(followButton).ToBeVisibleAsync();

        // Click to follow
        await followButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Button should now say "Unfollow"
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex($"Unfollow {authorName}") })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task YourFeed_ShowsFollowedUsersArticles()
    {
        // Create article as first user
        var (authorName, _, _) = await RegisterTestUserAsync();
        var title = $"Feed Test Article {Guid.NewGuid():N}";
        var slug = await CreateTestArticleAsync(title, "Description", "Body", "feed-tag");

        // Logout
        await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await Page.WaitForURLAsync("**/settings", new() { Timeout = 10000 });
        await Page.GetByRole(AriaRole.Button, new() { Name = "logout", Exact = false }).ClickAsync();
        await Page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        // Login as different user
        await RegisterTestUserAsync();
        
        // Wait for auth state to be fully propagated before proceeding
        await WaitForAuthenticatedStateAsync();

        // Navigate to article using in-app navigation to preserve auth state
        await Page.GetByRole(AriaRole.Link, new() { Name = "Home" }).ClickAsync();
        await WaitForAppReadyAsync();
        await Expect(Page.Locator("[data-testid='article-list'][data-status='loaded']")).ToBeVisibleAsync(new() { Timeout = 10000 });
        
        // Find and click the article
        var articleLink = Page.Locator(".preview-link").Filter(new() { HasText = title });
        await articleLink.ClickAsync();
        await Page.WaitForURLAsync($"**/article/{slug}", new() { Timeout = 10000 });
        await Expect(Page.GetByTestId("article-page")).ToBeVisibleAsync();

        // Follow the author - use .First because button appears twice
        var followButton = Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex($"Follow {authorName}") }).First;
        await followButton.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        // Go to home page - need to use navigation link to preserve auth state
        await Page.GetByRole(AriaRole.Link, new() { Name = "Home" }).ClickAsync();
        await WaitForAppReadyAsync();
        await WaitForAuthenticatedStateAsync();

        // Wait for articles to load first
        await Expect(Page.Locator("[data-testid='article-list'][data-status='loaded']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Click on "Your Feed" tab - should be visible since we're authenticated
        var yourFeedLink = Page.GetByRole(AriaRole.Link, new() { Name = "Your Feed" });
        await Expect(yourFeedLink).ToBeVisibleAsync(new() { Timeout = 10000 });
        await yourFeedLink.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        // The followed user's article should be visible
        await Expect(Page.GetByText(title)).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Fact]
    public async Task YourFeed_NotVisibleWhenLoggedOut()
    {
        await Page.GotoAsync("/");
        await WaitForAppReadyAsync();

        // "Your Feed" tab should NOT be visible when logged out
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Your Feed" })).Not.ToBeVisibleAsync();

        // Only "Global Feed" should be visible
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Global Feed" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task YourFeed_VisibleWhenLoggedIn()
    {
        await RegisterTestUserAsync();

        // After registration, user is redirected to home page
        // Wait for the home page to be fully loaded with articles
        await WaitForAppReadyAsync();
        await Expect(Page.Locator("[data-testid='article-list'][data-status='loaded']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // "Your Feed" tab should be visible when logged in
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Your Feed" })).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Global Feed" })).ToBeVisibleAsync();
    }
}
