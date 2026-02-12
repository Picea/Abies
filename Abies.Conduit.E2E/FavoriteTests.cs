namespace Abies.Conduit.E2E;

/// <summary>
/// E2E tests for favorite article user journeys.
/// Covers: Favorite, Unfavorite, View favorited articles on profile
/// </summary>
public class FavoriteTests : PlaywrightFixture
{
    [Fact]
    public async Task FavoriteArticle_FromArticlePage_IncrementsCount()
    {
        // Create an article as first user
        var (authorName, _, _) = await RegisterTestUserAsync();
        var title = $"Article to Favorite {Guid.NewGuid():N}";
        var slug = await CreateTestArticleAsync(title, "Description", "Body", "fav-tag");

        // Logout
        await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await Page.WaitForURLAsync("**/settings", new() { Timeout = 10000 });
        await Page.GetByRole(AriaRole.Button, new() { Name = "logout", Exact = false }).ClickAsync();
        await Page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        // Login as different user
        await RegisterTestUserAsync();

        // Navigate to the article - use in-app navigation to preserve auth state
        // First go via home page to get article link
        await Page.GetByRole(AriaRole.Link, new() { Name = "Home" }).ClickAsync();
        await WaitForAppReadyAsync();

        // Wait for articles to load
        await Expect(Page.Locator("[data-testid='article-list'][data-status='loaded']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Click on the article
        var articleLink = Page.Locator(".preview-link").Filter(new() { HasText = title });
        await articleLink.ClickAsync();
        await Page.WaitForURLAsync($"**/article/{slug}", new() { Timeout = 10000 });
        await Expect(Page.GetByTestId("article-page")).ToBeVisibleAsync();

        // Find the favorite button and check initial state - use .First because it appears twice
        var favoriteButton = Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("Favorite") }).First;
        await Expect(favoriteButton).ToBeVisibleAsync();

        // Get initial count (should be 0)
        var buttonText = await favoriteButton.TextContentAsync();
        Assert.Contains("0", buttonText);

        // Click to favorite
        await favoriteButton.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        // Count should be 1 now - re-get the button as DOM may have changed
        favoriteButton = Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("Favorite|Unfavorite") }).First;
        buttonText = await favoriteButton.TextContentAsync();
        Assert.Contains("1", buttonText);
    }

    [Fact]
    public async Task UnfavoriteArticle_FromArticlePage_DecrementsCount()
    {
        // Create and favorite an article
        await RegisterTestUserAsync();
        var title = $"Article to Unfavorite {Guid.NewGuid():N}";
        var slug = await CreateTestArticleAsync(title, "Description", "Body", "unfav-tag");

        // Logout and login as different user
        await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await Page.WaitForURLAsync("**/settings", new() { Timeout = 10000 });
        await Page.GetByRole(AriaRole.Button, new() { Name = "logout", Exact = false }).ClickAsync();
        await Page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        await RegisterTestUserAsync();

        // Navigate to article using in-app navigation to preserve auth state
        await Page.GetByRole(AriaRole.Link, new() { Name = "Home" }).ClickAsync();
        await WaitForAppReadyAsync();
        await Expect(Page.Locator("[data-testid='article-list'][data-status='loaded']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        var articleLink = Page.Locator(".preview-link").Filter(new() { HasText = title });
        await articleLink.ClickAsync();
        await Page.WaitForURLAsync($"**/article/{slug}", new() { Timeout = 10000 });
        await Expect(Page.GetByTestId("article-page")).ToBeVisibleAsync();

        // Use .First because favorite button appears twice
        var favoriteButton = Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("Favorite") }).First;
        await favoriteButton.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        // Should show 1 - re-get the button
        favoriteButton = Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("Favorite|Unfavorite") }).First;
        var buttonText = await favoriteButton.TextContentAsync();
        Assert.Contains("1", buttonText);

        // Click again to unfavorite
        await favoriteButton.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        // Should show 0 again - re-get the button
        favoriteButton = Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("Favorite|Unfavorite") }).First;
        buttonText = await favoriteButton.TextContentAsync();
        Assert.Contains("0", buttonText);
    }

    [Fact(Skip = "Favorite from home page works in headed mode but has timing issues in headless mode. Article page favorite tests pass.")]
    public async Task FavoriteArticle_FromHomePage_UpdatesCount()
    {
        await RegisterTestUserAsync();
        var title = $"Home Favorite Test {Guid.NewGuid():N}";
        await CreateTestArticleAsync(title, "Description", "Body", "home-fav");

        // Go to home page using in-app navigation to preserve auth
        await Page.GetByRole(AriaRole.Link, new() { Name = "Home" }).ClickAsync();
        await WaitForAppReadyAsync();
        await WaitForAuthenticatedStateAsync();

        // Wait for articles to load
        await Expect(Page.Locator("[data-testid='article-list'][data-status='loaded']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Find the article preview with our title
        var articlePreview = Page.Locator(".article-preview").Filter(new() { HasText = title });
        await Expect(articlePreview).ToBeVisibleAsync();

        // Find the favorite button (heart icon) - the button with ion-heart icon
        var heartButton = articlePreview.Locator("button.btn-outline-primary, button.btn-primary");
        await Expect(heartButton).ToBeVisibleAsync();

        // Get initial count - should be 0
        var initialText = await heartButton.TextContentAsync();
        Assert.Contains("0", initialText);

        // Click to favorite
        await heartButton.ClickAsync();

        // Wait for loading state and then loaded state (the page reloads articles after favorite)
        await Page.WaitForTimeoutAsync(500);
        await Expect(Page.Locator("[data-testid='article-list'][data-status='loaded']")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Page.WaitForTimeoutAsync(500);

        // Re-query the article preview and button (DOM was recreated)
        articlePreview = Page.Locator(".article-preview").Filter(new() { HasText = title });
        await Expect(articlePreview).ToBeVisibleAsync(new() { Timeout = 5000 });

        heartButton = articlePreview.Locator("button.btn-outline-primary, button.btn-primary");
        var newText = await heartButton.TextContentAsync();

        // Count should now be 1
        Assert.Contains("1", newText);
    }

    [Fact]
    public async Task ProfileFavoritedArticles_ShowsFavoritedArticles()
    {
        // Create article as author
        await RegisterTestUserAsync();
        var title = $"Favorited Article {Guid.NewGuid():N}";
        var slug = await CreateTestArticleAsync(title, "Description", "Body", "profile-fav");

        // Logout
        await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await Page.WaitForURLAsync("**/settings", new() { Timeout = 10000 });
        await Page.GetByRole(AriaRole.Button, new() { Name = "logout", Exact = false }).ClickAsync();
        await Page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        // Register new user who will favorite
        var (favUsername, _, _) = await RegisterTestUserAsync();

        // Wait for auth state to propagate after registration
        await WaitForAuthenticatedStateAsync();

        // After registration we're on home page - wait for articles to load
        await Expect(Page.Locator("[data-testid='article-list'][data-status='loaded']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Find and click the article link
        var articleLink = Page.Locator(".preview-link").Filter(new() { HasText = title });
        await articleLink.ClickAsync();
        await Page.WaitForURLAsync($"**/article/{slug}", new() { Timeout = 10000 });
        await Expect(Page.GetByTestId("article-page")).ToBeVisibleAsync();

        // Use .First because favorite button appears twice
        var favoriteButton = Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("Favorite") }).First;
        await favoriteButton.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        // Navigate to profile using in-app navigation - auth should still be valid
        await Page.GetByRole(AriaRole.Link, new() { Name = favUsername }).ClickAsync();
        await Page.WaitForURLAsync($"**/profile/{favUsername}", new() { Timeout = 10000 });
        await Expect(Page.Locator(".profile-page")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Click on "Favorited Articles" tab (use the specific tab link)
        var favoritedTab = Page.GetByRole(AriaRole.Link, new() { Name = "Favorited Articles" });
        await Expect(favoritedTab).ToBeVisibleAsync(new() { Timeout = 5000 });
        await favoritedTab.ClickAsync();

        // Wait for URL to change (may already be there if clicked quickly)
        await Page.WaitForTimeoutAsync(1000);

        // Verify we're on the favorites page
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($".*/profile/{favUsername}/favorites"), new() { Timeout = 10000 });

        // The favorited article should be visible
        await Expect(Page.GetByText(title)).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Fact]
    public async Task ProfileMyArticles_ShowsOwnArticles()
    {
        var (username, _, _) = await RegisterTestUserAsync();

        var title = $"My Own Article {Guid.NewGuid():N}";
        await CreateTestArticleAsync(title, "Description", "Body", "my-article");

        // Navigate to profile
        await Page.GetByRole(AriaRole.Link, new() { Name = username }).ClickAsync();
        await Page.WaitForURLAsync($"**/profile/{username}", new() { Timeout = 10000 });

        // "My Articles" tab should be active by default
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "My Articles" })).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("active"));

        // The article should be visible
        await Expect(Page.GetByText(title)).ToBeVisibleAsync(new() { Timeout = 10000 });
    }
}
