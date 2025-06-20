using Microsoft.Playwright;
using Xunit;

namespace Abies.Conduit.E2E;

[Collection("Conduit collection")]
public class TagTests
{
    private readonly ConduitFixture _fixture;
    public TagTests(ConduitFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CanFilterArticlesByTag()
    {
        var browser = _fixture.Browser;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var email = $"e2e{System.Guid.NewGuid():N}@example.com";

        await page.GotoAsync("http://localhost:5209/register");
        await page.FillAsync("input[placeholder=Username]", "taguser");
        await page.FillAsync("input[placeholder=Email]", email);
        await page.FillAsync("input[placeholder=Password]", "Password1!");
        await page.ClickAsync("button[type=submit]");
        await page.WaitForSelectorAsync("text=Your Feed");

        await page.ClickAsync("text=New Article");
        await page.FillAsync("input[placeholder=Article Title]", "Tag Article");
        await page.FillAsync("input[placeholder='What\'s this article about?']", "Tagging");
        await page.FillAsync("textarea[placeholder='Write your article (in markdown)']", "Body");
        await page.FillAsync("input[placeholder='Enter tags']", "e2etag");
        await page.PressAsync("input[placeholder='Enter tags']", "Enter");
        await page.ClickAsync("button[type=submit]");
        await page.WaitForSelectorAsync("text=Edit Article");
        await page.GotoAsync("http://localhost:5209/");
        await page.ClickAsync("text=e2etag");
        await page.WaitForSelectorAsync("text=Tag Article");
    }
}
