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
        await page.TypeAsync("input[placeholder=Username]", "taguser");
        await page.TypeAsync("input[placeholder=Email]", email);
        await page.TypeAsync("input[placeholder=Password]", "Password1!");
        await page.ClickAsync("button:has-text('Sign up')");
        await page.WaitForSelectorAsync("text=Your Feed");

        await page.ClickAsync("text=New Article");
        await page.TypeAsync("input[placeholder=Article Title]", "Tag Article");
        await page.TypeAsync("input[placeholder='What\'s this article about?']", "Tagging");
        await page.TypeAsync("textarea[placeholder='Write your article (in markdown)']", "Body");
        await page.TypeAsync("input[placeholder='Enter tags']", "e2etag");
        await page.PressAsync("input[placeholder='Enter tags']", "Enter");
        await page.ClickAsync("button:has-text('Publish Article')");
        await page.WaitForSelectorAsync("text=Edit Article");
        await page.GotoAsync("http://localhost:5209/");
        await page.ClickAsync("text=e2etag");
        await page.WaitForSelectorAsync("text=Tag Article");
    }
}
