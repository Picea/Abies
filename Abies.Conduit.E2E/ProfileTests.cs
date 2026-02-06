namespace Abies.Conduit.E2E;

/// <summary>
/// E2E tests for profile page user journeys.
/// Covers: View profile, My Articles, Favorited Articles, Profile info
/// </summary>
public class ProfileTests : PlaywrightFixture
{
    [Fact]
    public async Task ProfilePage_ShowsUserInfo()
    {
        var (username, _, _) = await RegisterTestUserAsync();

        // Update bio via settings
        await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await Page.WaitForURLAsync("**/settings", new() { Timeout = 10000 });

        var bio = "This is my test bio";
        await Page.GetByPlaceholder("Short bio about you").FillAsync(bio);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Update Settings" }).ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Navigate to profile
        await Page.Locator("nav").GetByRole(AriaRole.Link, new() { Name = username }).ClickAsync();
        await Page.WaitForURLAsync($"**/profile/{username}", new() { Timeout = 10000 });

        // Username and bio should be visible
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = username })).ToBeVisibleAsync();
        await Expect(Page.GetByText(bio)).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ProfilePage_MyArticles_ShowsAuthorArticles()
    {
        var (username, _, _) = await RegisterTestUserAsync();

        // Create an article
        var title = $"My Profile Article {Guid.NewGuid():N}";
        await CreateTestArticleAsync(title, "Description", "Body", "profile-article");

        // Navigate to profile
        await Page.Locator("nav").GetByRole(AriaRole.Link, new() { Name = username }).ClickAsync();
        await Page.WaitForURLAsync($"**/profile/{username}", new() { Timeout = 10000 });

        // "My Articles" tab should be active
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "My Articles" })).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("active"));

        // Article should be visible
        await Expect(Page.GetByText(title)).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Fact]
    public async Task ProfilePage_FavoritedArticles_ShowsFavorites()
    {
        // Create article as first user
        await RegisterTestUserAsync();
        var title = $"Article to Favorite {Guid.NewGuid():N}";
        var slug = await CreateTestArticleAsync(title, "Description", "Body", "fav-profile");

        // Logout
        await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await Page.WaitForURLAsync("**/settings", new() { Timeout = 10000 });
        await Page.GetByRole(AriaRole.Button, new() { Name = "logout", Exact = false }).ClickAsync();
        await Page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        // Login as second user who will favorite the article
        var (favUsername, _, _) = await RegisterTestUserAsync();

        // Wait for auth state to propagate after registration
        await WaitForAuthenticatedStateAsync();

        // After registration we're on home page - wait for articles to load
        await Expect(Page.Locator("[data-testid='article-list'][data-status='loaded']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Find and click the article
        var articleLink = Page.Locator(".preview-link").Filter(new() { HasText = title });
        await articleLink.ClickAsync();
        await Page.WaitForURLAsync($"**/article/{slug}", new() { Timeout = 10000 });
        await Expect(Page.GetByTestId("article-page")).ToBeVisibleAsync();

        // Use .First because favorite button appears twice (banner and article-actions)
        var favoriteButton = Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("Favorite") }).First;
        await favoriteButton.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        // Navigate to profile - auth should still be valid since we used in-app navigation
        await Page.Locator("nav").GetByRole(AriaRole.Link, new() { Name = favUsername }).ClickAsync();
        await Page.WaitForURLAsync($"**/profile/{favUsername}", new() { Timeout = 10000 });
        await Expect(Page.Locator(".profile-page")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Click "Favorited Articles" tab - wait for it to be visible first
        var favoritedTab = Page.GetByRole(AriaRole.Link, new() { Name = "Favorited Articles" });
        await Expect(favoritedTab).ToBeVisibleAsync(new() { Timeout = 5000 });
        await favoritedTab.ClickAsync();

        // Wait for URL to change
        await Page.WaitForTimeoutAsync(1000);
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($".*/profile/{favUsername}/favorites"), new() { Timeout = 10000 });

        // Favorited article should be visible
        await Expect(Page.GetByText(title)).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Fact]
    public async Task ProfilePage_OtherUser_ShowsFollowButton()
    {
        // Create profile for first user
        var (otherUsername, _, _) = await RegisterTestUserAsync();
        await CreateTestArticleAsync($"Other User Article {Guid.NewGuid():N}", "Description", "Body", "other-profile");

        // Logout
        await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await Page.WaitForURLAsync("**/settings", new() { Timeout = 10000 });
        await Page.GetByRole(AriaRole.Button, new() { Name = "logout", Exact = false }).ClickAsync();
        await Page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        // Login as different user
        await RegisterTestUserAsync();

        // Navigate to other user's profile
        await Page.GotoAsync($"/profile/{otherUsername}");
        await Expect(Page.Locator(".profile-page")).ToBeVisibleAsync();

        // Should see Follow button (not Edit Profile Settings)
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex($"Follow {otherUsername}") })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Edit Profile Settings" })).Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task ProfilePage_OwnProfile_ShowsEditSettingsButton()
    {
        var (username, _, _) = await RegisterTestUserAsync();

        // Navigate to own profile
        await Page.Locator("nav").GetByRole(AriaRole.Link, new() { Name = username }).ClickAsync();
        await Page.WaitForURLAsync($"**/profile/{username}", new() { Timeout = 10000 });

        // Should see Edit Profile Settings link (not Follow button)
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Edit Profile Settings" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("Follow") })).Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task ProfilePage_ClickArticle_NavigatesToArticle()
    {
        var (username, _, _) = await RegisterTestUserAsync();
        var title = $"Clickable Article {Guid.NewGuid():N}";
        await CreateTestArticleAsync(title, "Description", "Body", "click-profile");

        // Navigate to profile
        await Page.Locator("nav").GetByRole(AriaRole.Link, new() { Name = username }).ClickAsync();
        await Page.WaitForURLAsync($"**/profile/{username}", new() { Timeout = 10000 });

        // Click on the article
        var articleLink = Page.Locator(".preview-link").Filter(new() { HasText = title });
        await articleLink.ClickAsync();

        // Should navigate to article page
        await Page.WaitForURLAsync("**/article/**", new() { Timeout = 10000 });
        await Expect(Page.GetByTestId("article-page")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ProfilePage_NoArticles_ShowsEmptyMessage()
    {
        var (username, _, _) = await RegisterTestUserAsync();

        // Navigate directly to profile without creating articles
        await Page.GotoAsync($"/profile/{username}");
        await Expect(Page.Locator(".profile-page")).ToBeVisibleAsync();

        // Should show "No articles" message
        await Expect(Page.GetByText("No articles are here")).ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    [Fact(Skip = "Creating 12+ articles is too slow for E2E. Pagination covered by integration tests.")]
    public async Task ProfilePage_Pagination_Works()
    {
        // This test requires multiple articles to exist (12+) which is too slow for E2E
        // Pagination logic is covered by integration tests
        await Task.CompletedTask;
    }
}
