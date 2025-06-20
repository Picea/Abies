using Microsoft.Playwright;
using Xunit;

namespace Abies.Conduit.E2E;

[Collection("Conduit collection")]
public class ArticleTests
{
    private readonly ConduitFixture _fixture;
    public ArticleTests(ConduitFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CanCreateEditAndDeleteArticle()
    {
        var browser = _fixture.Browser;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        // register and login
        await page.GotoAsync("http://localhost:5209/register");
        var email = $"e2e{System.Guid.NewGuid():N}@example.com";
        await page.TypeAsync("input[placeholder=Username]", "articleuser");
        await page.TypeAsync("input[placeholder=Email]", email);
        await page.TypeAsync("input[placeholder=Password]", "Password1!");
        await page.ClickAsync("button:has-text('Sign up')");
        await page.WaitForSelectorAsync("text=Your Feed");

        // logout to verify login as existing user works
        await page.ClickAsync("text=Settings");
        await page.ClickAsync("text=Or click here to logout");
        await page.GotoAsync("http://localhost:5209/login");
        await page.TypeAsync("input[placeholder=Email]", email);
        await page.TypeAsync("input[placeholder=Password]", "Password1!");
        await page.ClickAsync("button:has-text('Sign in')");
        await page.WaitForSelectorAsync("text=Your Feed");

        await page.ClickAsync("text=New Article");
        await page.TypeAsync("input[placeholder=Article Title]", "E2E Article");
        await page.TypeAsync("input[placeholder='What\'s this article about?']", "Testing");
        await page.TypeAsync("textarea[placeholder='Write your article (in markdown)']", "Hello World");
        await page.ClickAsync("button:has-text('Publish Article')");
        await page.WaitForSelectorAsync("text=Edit Article");

        await page.TypeAsync("textarea[placeholder='Write your article (in markdown)']", "Updated");
        await page.ClickAsync("text=Publish Article");
        await page.WaitForSelectorAsync("text=Updated");

        await page.ClickAsync("text=Delete Article");
        await page.WaitForSelectorAsync("text=Global Feed");
    }
}
