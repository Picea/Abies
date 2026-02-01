using Microsoft.Playwright;

namespace Abies.Conduit.E2E;

/// <summary>
/// E2E tests for article CRUD user journeys.
/// Covers: Create, Read, Update, Delete articles
/// </summary>
public class ArticleTests : PlaywrightFixture
{
    [Fact]
    public async Task CreateArticle_PublishesAndRedirectsToArticlePage()
    {
        await RegisterTestUserAsync();

        var title = $"Test Article {Guid.NewGuid():N}";
        var description = "This is a test article description";
        var body = "# Heading\n\nThis is the article body with **markdown**.";
        var tag = "test-tag";

        // Navigate to editor
        await Page.GetByRole(AriaRole.Link, new() { Name = "New Article" }).ClickAsync();
        await Page.WaitForURLAsync("**/editor", new() { Timeout = 10000 });

        // Fill in article details
        await Page.GetByPlaceholder("Article Title").FillAsync(title);
        await Page.GetByPlaceholder("What's this article about?").FillAsync(description);
        await Page.GetByPlaceholder("Write your article (in markdown)").FillAsync(body);
        await Page.GetByPlaceholder("Enter tags").FillAsync(tag);
        await Page.GetByPlaceholder("Enter tags").PressAsync("Enter");

        // Publish
        await Page.GetByRole(AriaRole.Button, new() { Name = "Publish Article" }).ClickAsync();

        // Should redirect to article page
        await Page.WaitForURLAsync("**/article/**", new() { Timeout = 30000 });

        // Article content should be visible
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = title })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Heading")).ToBeVisibleAsync(); // Markdown heading rendered
        await Expect(Page.GetByText(tag)).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ViewArticle_ShowsArticleContent()
    {
        var (username, _, _) = await RegisterTestUserAsync();

        var title = $"View Test Article {Guid.NewGuid():N}";
        var description = "Test description for viewing";
        var body = "This is the full article body content.";

        await CreateTestArticleAsync(title, description, body, "view-tag");

        // Should be on article page
        await Expect(Page.GetByTestId("article-page")).ToBeVisibleAsync();
        
        // Article content should be displayed
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = title })).ToBeVisibleAsync();
        await Expect(Page.GetByText(body)).ToBeVisibleAsync();
        // Author name appears multiple times (banner + article-actions), use .First
        await Expect(Page.GetByText(username).First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task EditArticle_AuthorCanModify()
    {
        await RegisterTestUserAsync();

        var originalTitle = $"Original Title {Guid.NewGuid():N}";
        await CreateTestArticleAsync(originalTitle, "Original description", "Original body", "edit-tag");

        // Should see Edit Article button (as author) - use .First because button appears in banner AND article-actions
        var editButton = Page.GetByRole(AriaRole.Link, new() { Name = "Edit Article" }).First;
        await Expect(editButton).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Click edit
        await editButton.ClickAsync();
        await Page.WaitForURLAsync("**/editor/**", new() { Timeout = 10000 });

        // Wait for form to load with existing values - wait for the title to be populated
        var titleInput = Page.GetByPlaceholder("Article Title");
        await Expect(titleInput).ToBeVisibleAsync(new() { Timeout = 10000 });
        
        // Wait for the title input to have the original title (meaning article data loaded)
        await Expect(titleInput).ToHaveValueAsync(originalTitle, new() { Timeout = 10000 });

        // Modify the title
        var newTitle = $"Updated Title {Guid.NewGuid():N}";
        await titleInput.FillAsync(newTitle);

        // When editing (Slug is set), the button says "Update Article"
        var updateButton = Page.GetByRole(AriaRole.Button, new() { Name = "Update Article" });
        await Expect(updateButton).ToBeVisibleAsync(new() { Timeout = 5000 });
        await updateButton.ClickAsync();

        // Should redirect back to article page with new title
        await Page.WaitForURLAsync("**/article/**", new() { Timeout = 30000 });
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = newTitle })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task DeleteArticle_AuthorCanDelete()
    {
        await RegisterTestUserAsync();

        var title = $"Article to Delete {Guid.NewGuid():N}";
        await CreateTestArticleAsync(title, "Will be deleted", "Delete me", "delete-tag");

        // Should see Delete Article button (as author) - use .First because button appears in banner AND article-actions
        var deleteButton = Page.GetByRole(AriaRole.Button, new() { Name = "Delete Article" }).First;
        await Expect(deleteButton).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Click delete
        await deleteButton.ClickAsync();

        // Should redirect to home page
        await Page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        // Article should no longer exist (navigating to it should show error or redirect)
        // We verify by checking we're on home page
        await Expect(Page.GetByTestId("home-page")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ArticlePage_ShowsTagList()
    {
        await RegisterTestUserAsync();

        var tags = new[] { "tag-one", "tag-two", "tag-three" };
        var title = $"Tagged Article {Guid.NewGuid():N}";
        await CreateTestArticleAsync(title, "Description", "Body with tags", tags);

        // All tags should be visible
        foreach (var tag in tags)
        {
            await Expect(Page.GetByText(tag)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task ArticlePage_RendersMarkdown()
    {
        await RegisterTestUserAsync();

        var title = $"Markdown Article {Guid.NewGuid():N}";
        var markdownBody = @"# Main Heading

This is a paragraph with **bold** and *italic* text.

## Subheading

- List item 1
- List item 2

```code block```";

        await CreateTestArticleAsync(title, "Markdown test", markdownBody, "markdown");

        // Headings should be rendered as HTML headings
        await Expect(Page.Locator("h1:has-text('Main Heading')")).ToBeVisibleAsync();
        await Expect(Page.Locator("h2:has-text('Subheading')")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ArticlePage_NonAuthor_CannotSeeEditDeleteButtons()
    {
        // Create article as first user
        await RegisterTestUserAsync();
        var title = $"Other User Article {Guid.NewGuid():N}";
        var slug = await CreateTestArticleAsync(title, "Description", "Body", "other-tag");

        // Logout
        await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await Page.WaitForURLAsync("**/settings", new() { Timeout = 10000 });
        await Page.GetByRole(AriaRole.Button, new() { Name = "logout", Exact = false }).ClickAsync();
        await Page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        // Register as different user
        await RegisterTestUserAsync();

        // Navigate to the article
        await Page.GotoAsync($"/article/{slug}");
        await Expect(Page.GetByTestId("article-page")).ToBeVisibleAsync();

        // Should NOT see Edit/Delete buttons
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Edit Article" }).First).Not.ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Delete Article" }).First).Not.ToBeVisibleAsync();

        // Should see Follow/Favorite buttons instead - use .First because they appear twice
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("Follow") }).First).ToBeVisibleAsync();
    }
}
