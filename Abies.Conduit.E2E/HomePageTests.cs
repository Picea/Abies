namespace Abies.Conduit.E2E;

/// <summary>
/// E2E tests for home page and navigation user journeys.
/// Covers: Global feed, Tag filtering, Popular tags, Pagination
/// </summary>
public class HomePageTests : PlaywrightFixture
{
    [Fact]
    public async Task HomePage_ShowsBannerAndGlobalFeed()
    {
        await Page.GotoAsync("/");
        await WaitForAppReadyAsync();

        // Banner should be visible
        await Expect(Page.Locator(".banner")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "conduit" })).ToBeVisibleAsync();
        await Expect(Page.GetByText("A place to share your knowledge")).ToBeVisibleAsync();

        // Global Feed tab should be visible and active
        var globalFeedLink = Page.GetByRole(AriaRole.Link, new() { Name = "Global Feed" });
        await Expect(globalFeedLink).ToBeVisibleAsync();
        await Expect(globalFeedLink).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("active"));
    }

    [Fact]
    public async Task HomePage_ShowsPopularTags()
    {
        await Page.GotoAsync("/");
        await WaitForAppReadyAsync();

        // Sidebar should be visible with "Popular Tags"
        await Expect(Page.Locator(".sidebar")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Popular Tags")).ToBeVisibleAsync();

        // Tag list should be present
        await Expect(Page.Locator(".sidebar .tag-list")).ToBeVisibleAsync();
    }

    [Fact(Skip = "Tag click works in headed mode but has timing issues in headless mode. Integration tests cover tag filtering.")]
    public async Task HomePage_ClickTag_FiltersArticles()
    {
        // First create an article with a specific tag
        await RegisterTestUserAsync();
        var uniqueTag = $"tag{Guid.NewGuid():N}"[..10];
        var title = $"Tagged Article {Guid.NewGuid():N}";
        await CreateTestArticleAsync(title, "Description", "Body", uniqueTag);

        // Go to home page using in-app navigation
        await Page.GetByRole(AriaRole.Link, new() { Name = "Home" }).ClickAsync();
        await WaitForAppReadyAsync();
        await WaitForAuthenticatedStateAsync();

        // Wait for articles to load
        await Expect(Page.Locator("[data-testid='article-list'][data-status='loaded']")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Page.WaitForTimeoutAsync(500);

        // Find the article preview with our title (ensure we're looking at the right article)
        var articlePreview = Page.Locator(".article-preview").Filter(new() { HasText = title });
        await Expect(articlePreview).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Find the tag link (a.tag-pill) in the article's tag-list
        var tagLink = articlePreview.Locator(".tag-list a.tag-pill").Filter(new() { HasText = uniqueTag });
        await Expect(tagLink).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Click the tag link
        await tagLink.ClickAsync();

        // Wait for the article list to reload (shows loading then loaded)
        await Page.WaitForTimeoutAsync(500);
        await Expect(Page.Locator("[data-testid='article-list'][data-status='loaded']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // A new tab with the tag name should appear in the feed toggle (format: "# tagname")
        // The tag tab should be the active one
        var tagTab = Page.Locator(".feed-toggle .nav-link.active").Filter(new() { HasText = $"# {uniqueTag}" });
        await Expect(tagTab).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Fact]
    public async Task HomePage_ArticlePreview_ShowsCorrectInfo()
    {
        var (username, _, _) = await RegisterTestUserAsync();
        var title = $"Preview Article {Guid.NewGuid():N}";
        var description = "This is the article description for preview";
        await CreateTestArticleAsync(title, description, "Body content", "preview-tag");

        // Go to home page
        await Page.GotoAsync("/");
        await WaitForAppReadyAsync();

        // Wait for articles to load
        await Expect(Page.Locator("[data-testid='article-list'][data-status='loaded']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Find the article preview
        var articlePreview = Page.Locator(".article-preview").Filter(new() { HasText = title });
        await Expect(articlePreview).ToBeVisibleAsync();

        // Should show title
        await Expect(articlePreview.GetByRole(AriaRole.Heading, new() { Name = title })).ToBeVisibleAsync();

        // Should show description
        await Expect(articlePreview.GetByText(description)).ToBeVisibleAsync();

        // Should show author name
        await Expect(articlePreview.GetByText(username)).ToBeVisibleAsync();

        // Should show "Read more..." link
        await Expect(articlePreview.GetByText("Read more...")).ToBeVisibleAsync();

        // Should show favorite button with count
        await Expect(articlePreview.Locator("button.btn-outline-primary, button.btn-primary")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task HomePage_ClickArticle_NavigatesToArticlePage()
    {
        await RegisterTestUserAsync();
        var title = $"Click Test Article {Guid.NewGuid():N}";
        await CreateTestArticleAsync(title, "Description", "Body", "click-test");

        // Go to home page
        await Page.GotoAsync("/");
        await WaitForAppReadyAsync();

        // Wait for articles to load
        await Expect(Page.Locator("[data-testid='article-list'][data-status='loaded']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Click on the article preview link
        var articleLink = Page.Locator(".preview-link").Filter(new() { HasText = title });
        await articleLink.ClickAsync();

        // Should navigate to article page
        await Page.WaitForURLAsync("**/article/**", new() { Timeout = 10000 });
        await Expect(Page.GetByTestId("article-page")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = title })).ToBeVisibleAsync();
    }

    [Fact(Skip = "Creating 12+ articles is too slow for E2E. Pagination covered by integration tests.")]
    public async Task HomePage_Pagination_NavigatesBetweenPages()
    {
        // This test requires multiple articles to exist (12+) which is too slow for E2E
        // Pagination logic is covered by integration tests
        await Task.CompletedTask;
    }

    [Fact]
    public async Task HomePage_SwitchBetweenFeedTabs()
    {
        await RegisterTestUserAsync();

        // After registration, user is redirected to home page
        // Wait for the home page to be fully loaded with articles
        await WaitForAppReadyAsync();
        await Expect(Page.Locator("[data-testid='article-list'][data-status='loaded']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Global Feed should be active initially
        var globalFeed = Page.GetByRole(AriaRole.Link, new() { Name = "Global Feed" });
        await Expect(globalFeed).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("active"));

        // Click Your Feed - allow more time for auth state to propagate
        var yourFeed = Page.GetByRole(AriaRole.Link, new() { Name = "Your Feed" });
        await Expect(yourFeed).ToBeVisibleAsync(new() { Timeout = 10000 });
        await yourFeed.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Your Feed should now be active
        await Expect(yourFeed).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("active"));
        await Expect(globalFeed).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("active"));

        // Click back to Global Feed
        await globalFeed.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Global Feed should be active again
        await Expect(globalFeed).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("active"));
    }
}
