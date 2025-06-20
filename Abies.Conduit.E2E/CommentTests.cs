using Microsoft.Playwright;
using Xunit;

namespace Abies.Conduit.E2E;

[Collection("Conduit collection")]
public class CommentTests
{
    private readonly ConduitFixture _fixture;
    public CommentTests(ConduitFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CanAddAndDeleteComment()
    {
        var browser = _fixture.Browser;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var email = $"e2e{System.Guid.NewGuid():N}@example.com";

        // register and login
        await page.GotoAsync("http://localhost:5209/register");
        await page.TypeAsync("input[placeholder=Username]", "commenter");
        await page.TypeAsync("input[placeholder=Email]", email);
        await page.TypeAsync("input[placeholder=Password]", "Password1!");
        await page.ClickAsync("button:has-text('Sign up')");
        await page.WaitForSelectorAsync("text=Your Feed");

        // create article
        await page.ClickAsync("text=New Article");
        await page.TypeAsync("input[placeholder=Article Title]", "Comment Article");
        await page.TypeAsync("input[placeholder='What\'s this article about?']", "Comments");
        await page.TypeAsync("textarea[placeholder='Write your article (in markdown)']", "Body");
        await page.ClickAsync("button:has-text('Publish Article')");
        await page.WaitForSelectorAsync("text=Edit Article");

        // add comment
        await page.TypeAsync("textarea[placeholder='Write a comment...']", "First!");
        await page.ClickAsync("button:has-text('Post Comment')");
        await page.WaitForSelectorAsync("text=First!");

        // delete comment
        await page.ClickAsync(".mod-options i.ion-trash-a");
        await page.WaitForSelectorAsync("text=No comments yet", new() { Timeout = 5000 });
    }
}
