namespace Abies.Conduit.E2E;

/// <summary>
/// E2E tests for DOM rendering correctness.
/// Tests DOM patching works correctly for dynamic list structures.
/// 
/// Related to:
/// - Issue #32: js-framework-benchmark table rendering
/// - ADR-016: ID-Based DOM Diffing for Dynamic Lists
/// </summary>
public class DomRenderingTests : PlaywrightFixture
{
    /// <summary>
    /// Verifies that article list items render correctly.
    /// Tests DOM patching works for list-like structures.
    /// </summary>
    [Fact]
    public async Task ArticleList_RendersDynamically()
    {
        // Register and create an article
        await RegisterTestUserAsync();
        var title = $"Table Test Article {Guid.NewGuid():N}";
        await CreateTestArticleAsync(title, "Description", "Body content", "test-tag");

        // Navigate to home page
        await Page.GotoAsync("/");
        await WaitForAppReadyAsync();

        // Wait for articles to load
        await Expect(Page.Locator("[data-testid='article-list'][data-status='loaded']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Verify the article preview is rendered with correct structure
        var articlePreview = Page.Locator(".article-preview").Filter(new() { HasText = title });
        await Expect(articlePreview).ToBeVisibleAsync();

        // Verify nested elements are present (tests DOM structure integrity)
        await Expect(articlePreview.Locator(".article-meta")).ToBeVisibleAsync();
        await Expect(articlePreview.Locator(".preview-link")).ToBeVisibleAsync();
        await Expect(articlePreview.Locator("h1")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies that tag lists render correctly after DOM updates.
    /// Tags use inline elements inside article previews.
    /// </summary>
    [Fact]
    public async Task TagList_RendersWithMultipleTags()
    {
        // Register and create an article with multiple tags
        await RegisterTestUserAsync();
        var tag1 = $"tag1-{Guid.NewGuid():N}"[..12];
        var tag2 = $"tag2-{Guid.NewGuid():N}"[..12];
        var title = $"Multi Tag Article {Guid.NewGuid():N}";
        await CreateTestArticleAsync(title, "Description", "Body", tag1, tag2);

        // Navigate to home page
        await Page.GotoAsync("/");
        await WaitForAppReadyAsync();

        // Wait for articles to load
        await Expect(Page.Locator("[data-testid='article-list'][data-status='loaded']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Find the article preview
        var articlePreview = Page.Locator(".article-preview").Filter(new() { HasText = title });
        await Expect(articlePreview).ToBeVisibleAsync();

        // Verify both tags are rendered in the tag list
        var tagList = articlePreview.Locator(".tag-list");
        await Expect(tagList).ToBeVisibleAsync();
        await Expect(tagList.Locator("li").Filter(new() { HasText = tag1 })).ToBeVisibleAsync();
        await Expect(tagList.Locator("li").Filter(new() { HasText = tag2 })).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies that nav bar links render correctly after authentication state change.
    /// This tests DOM patching of list-like structures (nav items).
    /// </summary>
    [Fact]
    public async Task NavBar_UpdatesAfterLogin()
    {
        // Go to home page first (unauthenticated)
        await Page.GotoAsync("/");
        await WaitForAppReadyAsync();

        // Verify unauthenticated nav items
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Sign in" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Sign up" })).ToBeVisibleAsync();

        // Register (which logs in and redirects)
        await RegisterTestUserAsync();
        await WaitForAppReadyAsync();

        // Verify authenticated nav items are now visible
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "New Article" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Settings" })).ToBeVisibleAsync();

        // Verify unauthenticated items are no longer visible
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Sign in" })).Not.ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Sign up" })).Not.ToBeVisibleAsync();
    }

}
