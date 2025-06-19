using Microsoft.Playwright;
using Xunit;

namespace Abies.Conduit.E2E;

[Collection("Conduit collection")]
public class FavoriteTests
{
    private readonly ConduitFixture _fixture;
    public FavoriteTests(ConduitFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CanFavoriteAndUnfavoriteArticle()
    {
        var browser = _fixture.Browser;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var email = $"e2e{System.Guid.NewGuid():N}@example.com";

        // register and login
        await page.GotoAsync("http://localhost:5209/register");
        await page.FillAsync("input[placeholder=Username]", "favuser");
        await page.FillAsync("input[placeholder=Email]", email);
        await page.FillAsync("input[placeholder=Password]", "Password1!");
        await page.ClickAsync("button[type=submit]");
        await page.WaitForSelectorAsync("text=Your Feed");

        // create article
        await page.ClickAsync("text=New Article");
        await page.FillAsync("input[placeholder=Article Title]", "Fav Article");
        await page.FillAsync("input[placeholder='What\'s this article about?']", "Favs");
        await page.FillAsync("textarea[placeholder='Write your article (in markdown)']", "Body");
        await page.ClickAsync("button[type=submit]");
        await page.WaitForSelectorAsync("text=Edit Article");
        var slug = page.Url.Split('/').Last();
        await page.GotoAsync("http://localhost:5209/");
        await page.WaitForSelectorAsync($"a[href='/article/{slug}']");

        // favorite from preview
        await page.ClickAsync($"a[href='/article/{slug}']");
        await page.WaitForSelectorAsync("button:has(.ion-heart)");
        await page.ClickAsync("button:has(.ion-heart)");
        await page.WaitForSelectorAsync("button.btn-primary");
        await page.ClickAsync("button.btn-primary");
        await page.WaitForSelectorAsync("button.btn-outline-primary");
    }
}
