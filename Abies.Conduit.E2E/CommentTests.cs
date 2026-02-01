using Microsoft.Playwright;

namespace Abies.Conduit.E2E;

/// <summary>
/// E2E tests for comment functionality on articles.
/// Verifies that users can add multiple comments and they all remain visible.
/// Related to ADR-016: ID-Based DOM Diffing for Dynamic Lists
/// </summary>
public class CommentTests : PlaywrightFixture
{
    [Fact]
    public async Task AddMultipleComments_AllCommentsRemainVisible()
    {
        // Register a user
        await RegisterTestUserAsync();

        // Create an article
        var title = $"Test Article {Guid.NewGuid():N}";
        await CreateTestArticleAsync(title, "Test Description", "Test Body", "test-tag");

        // Now we should be on the article page
        await Expect(Page.GetByTestId("article-page")).ToBeVisibleAsync();

        // Add first comment
        var firstComment = "This is my first comment";
        await Page.GetByPlaceholder("Write a comment...").FillAsync(firstComment);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Post Comment" }).ClickAsync();

        // Wait for first comment to appear
        await Expect(Page.GetByText(firstComment)).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Add second comment
        var secondComment = "This is my second comment";
        await Page.GetByPlaceholder("Write a comment...").FillAsync(secondComment);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Post Comment" }).ClickAsync();

        // Wait for second comment to appear
        await Expect(Page.GetByText(secondComment)).ToBeVisibleAsync(new() { Timeout = 10000 });

        // CRITICAL: Both comments should still be visible (this was the bug)
        await Expect(Page.GetByText(firstComment)).ToBeVisibleAsync();
        await Expect(Page.GetByText(secondComment)).ToBeVisibleAsync();

        // Add a third comment
        var thirdComment = "And this is my third comment";
        await Page.GetByPlaceholder("Write a comment...").FillAsync(thirdComment);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Post Comment" }).ClickAsync();

        // Wait for third comment to appear
        await Expect(Page.GetByText(thirdComment)).ToBeVisibleAsync(new() { Timeout = 10000 });

        // All three comments should be visible
        await Expect(Page.GetByText(firstComment)).ToBeVisibleAsync();
        await Expect(Page.GetByText(secondComment)).ToBeVisibleAsync();
        await Expect(Page.GetByText(thirdComment)).ToBeVisibleAsync();

        // Verify comment cards have unique IDs (per ADR-016)
        var commentCards = await Page.Locator("[id^='comment-']").AllAsync();
        Assert.True(commentCards.Count >= 3, $"Expected at least 3 comment cards with id starting with 'comment-', found {commentCards.Count}");
    }

    [Fact]
    public async Task AddComment_CommentAppearsImmediately()
    {
        // Register a user
        await RegisterTestUserAsync();

        // Create an article
        var title = $"Test Article {Guid.NewGuid():N}";
        await CreateTestArticleAsync(title, "Test Description", "Test Body", "test-tag");

        // Add a comment
        var commentText = "This is a test comment";
        await Page.GetByPlaceholder("Write a comment...").FillAsync(commentText);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Post Comment" }).ClickAsync();

        // Comment should appear immediately (this is the critical functionality)
        await Expect(Page.GetByText(commentText)).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Fact]
    public async Task DeleteComment_CommentIsRemoved()
    {
        // Register a user
        await RegisterTestUserAsync();

        // Create an article
        var title = $"Test Article {Guid.NewGuid():N}";
        await CreateTestArticleAsync(title, "Test Description", "Test Body", "test-tag");

        // Helper to add a comment
        async Task AddCommentAsync(string commentText)
        {
            var textarea = Page.GetByPlaceholder("Write a comment...");
            await textarea.FillAsync(commentText);
            await Page.WaitForTimeoutAsync(200); // Wait for state update
            var button = Page.GetByRole(AriaRole.Button, new() { Name = "Post Comment" });
            await Expect(button).ToBeEnabledAsync(new() { Timeout = 5000 });
            await button.ClickAsync();
            await Expect(Page.GetByText(commentText)).ToBeVisibleAsync(new() { Timeout = 10000 });
        }

        // Add first comment
        var firstComment = "Comment to keep";
        await AddCommentAsync(firstComment);

        // Add second comment that we'll delete
        var secondComment = "Comment to delete";
        await AddCommentAsync(secondComment);

        // Count comment cards before delete
        var cardsBefore = await Page.Locator("[id^='comment-']").CountAsync();
        Assert.Equal(2, cardsBefore);

        // Find the second comment card
        var secondCommentCard = Page.Locator("[id^='comment-']").Filter(new() { HasText = secondComment });
        await Expect(secondCommentCard).ToBeVisibleAsync();
        
        var trashIcon = secondCommentCard.Locator("i.ion-trash-a");
        await Expect(trashIcon).ToBeVisibleAsync(new() { Timeout = 5000 });
        
        // Use JavaScript click which works more reliably for this element
        var trashId = await trashIcon.GetAttributeAsync("id");
        await Page.EvaluateAsync($"() => document.getElementById('{trashId}').click()");
        
        // Wait for the delete to complete
        await Page.WaitForTimeoutAsync(1000);

        // Second comment should be removed
        await Expect(Page.GetByText(secondComment)).Not.ToBeVisibleAsync(new() { Timeout = 10000 });

        // First comment should still be visible
        await Expect(Page.GetByText(firstComment)).ToBeVisibleAsync();
        
        // Should only have 1 comment card remaining
        var cardsAfter = await Page.Locator("[id^='comment-']").CountAsync();
        Assert.Equal(1, cardsAfter);
    }

    [Fact]
    public async Task CommentSection_NotLoggedIn_ShowsLoginPrompt()
    {
        // Go to home page without logging in
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click on an article (if any exist)
        var articleLink = Page.Locator(".preview-link").First;
        if (await articleLink.CountAsync() > 0)
        {
            await articleLink.ClickAsync();
            await Page.WaitForURLAsync("**/article/**");

            // Should show login prompt instead of comment form
            await Expect(Page.GetByText("Sign in")).ToBeVisibleAsync();
            await Expect(Page.GetByText("sign up")).ToBeVisibleAsync();
            
            // Comment textarea should not be visible
            await Expect(Page.GetByPlaceholder("Write a comment...")).Not.ToBeVisibleAsync();
        }
    }
}
