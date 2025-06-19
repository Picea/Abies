using Microsoft.Playwright;
using Xunit;

namespace Abies.Conduit.E2E;

[Collection("Conduit collection")]
public class PaginationTests
{
    private readonly ConduitFixture _fixture;
    public PaginationTests(ConduitFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CanNavigateArticlePages()
    {
        var browser = _fixture.Browser;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var email = $"e2e{System.Guid.NewGuid():N}@example.com";

        await page.GotoAsync("http://localhost:5209/register");
        await page.FillAsync("input[placeholder=Username]", "pageuser");
        await page.FillAsync("input[placeholder=Email]", email);
        await page.FillAsync("input[placeholder=Password]", "Password1!");
        await page.ClickAsync("button[type=submit]");
        await page.WaitForSelectorAsync("text=Your Feed");

        for (int i = 0; i < 11; i++)
        {
            await page.ClickAsync("text=New Article");
            await page.FillAsync("input[placeholder=Article Title]", $"Page Article {i}");
            await page.FillAsync("input[placeholder='What\'s this article about?']", "Pages");
            await page.FillAsync("textarea[placeholder='Write your article (in markdown)']", "Body");
            await page.ClickAsync("button[type=submit]");
            await page.WaitForSelectorAsync("text=Edit Article");
            await page.ClickAsync("text=Home");
        }

        await page.WaitForSelectorAsync("ul.pagination li:nth-child(2)");
        await page.ClickAsync("ul.pagination li:nth-child(2) a");
        await page.WaitForSelectorAsync("ul.pagination li.active:has-text('2')");
    }
}
