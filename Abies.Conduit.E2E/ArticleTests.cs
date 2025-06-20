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

        // login as demo user
        await page.GotoAsync("http://localhost:5209/login");
        await page.FillAsync("input[placeholder=Email]", "demo@example.com");
        await page.FillAsync("input[placeholder=Password]", "demo");
        await page.ClickAsync("button[type=submit]");
        await page.WaitForSelectorAsync("text=Your Feed");

        await page.ClickAsync("text=New Article");
        await page.FillAsync("input[placeholder=Article Title]", "E2E Article");
        await page.FillAsync("input[placeholder='What\'s this article about?']", "Testing");
        await page.FillAsync("textarea[placeholder='Write your article (in markdown)']", "Hello World");
        await page.ClickAsync("button[type=submit]");
        await page.WaitForSelectorAsync("text=Edit Article");

        await page.FillAsync("textarea[placeholder='Write your article (in markdown)']", "Updated");
        await page.ClickAsync("text=Publish Article");
        await page.WaitForSelectorAsync("text=Updated");

        await page.ClickAsync("text=Delete Article");
        await page.WaitForSelectorAsync("text=Global Feed");
    }
}
