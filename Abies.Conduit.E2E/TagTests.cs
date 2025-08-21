using Microsoft.Playwright;
using Xunit;

namespace Abies.Conduit.E2E;

[Collection("Conduit collection")]
public class TagTests
{
    private readonly ConduitFixture _fixture;
    public TagTests(ConduitFixture fixture) => _fixture = fixture;

    private static async Task WaitForLoggedInAsync(IPage page)
    {
        await page.WaitForFunctionAsync("() => window.abiesReady === true");
        await page.WaitForFunctionAsync("() => !!localStorage.getItem('jwt')", null, new() { Timeout = 60000 });
    await page.WaitForSelectorAsync("text=Settings", new() { Timeout = 60000 });
    }

    [Fact]
    public async Task CanFilterArticlesByTag()
    {
        var browser = _fixture.Browser;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        // Capture browser console and request failures for debugging flakes
        page.Console += (_, msg) =>
        {
            try { Console.WriteLine($"[browser:{msg.Type}] {msg.Text}"); } catch { /* ignore */ }
        };
        page.RequestFailed += (_, req) =>
        {
            try { Console.WriteLine($"[request failed] {req.Method} {req.Url}"); } catch { /* ignore */ }
        };

        var email = $"e2e{System.Guid.NewGuid():N}@example.com";

    await page.GotoAsync(_fixture.AppBaseUrl + "/register");
    await page.WaitForFunctionAsync("() => window.abiesReady === true");
    var username = $"tag{System.Guid.NewGuid():N}".Substring(0, 16);
    await page.GetByPlaceholder("Username").FillAsync(username);
    await page.GetByPlaceholder("Username").DispatchEventAsync("input");
    await page.GetByPlaceholder("Username").DispatchEventAsync("change");
    await page.GetByPlaceholder("Email").FillAsync(email);
    await page.GetByPlaceholder("Email").DispatchEventAsync("input");
    await page.GetByPlaceholder("Email").DispatchEventAsync("change");
    await page.GetByPlaceholder("Password").FillAsync("Password1!");
    await page.GetByPlaceholder("Password").DispatchEventAsync("input");
    await page.GetByPlaceholder("Password").DispatchEventAsync("change");
    await page.WaitForSelectorAsync("button:has-text('Sign up'):not([disabled])");
    await page.Locator("button:has-text('Sign up')").ClickAsync();
        await WaitForLoggedInAsync(page);

    await page.ClickAsync("text=New Article");
    await page.WaitForURLAsync("**/editor");
    await page.WaitForSelectorAsync("input[placeholder='Article Title']");
    await page.GetByPlaceholder("Article Title").FillAsync("Tag Article");
    await page.GetByPlaceholder("Article Title").DispatchEventAsync("input");
    await page.GetByPlaceholder("Article Title").DispatchEventAsync("change");
    await page.GetByPlaceholder("What's this article about?").FillAsync("Tagging");
    await page.GetByPlaceholder("What's this article about?").DispatchEventAsync("input");
    await page.GetByPlaceholder("What's this article about?").DispatchEventAsync("change");
    await page.GetByPlaceholder("Write your article (in markdown)").FillAsync("Body for tag test.");
    await page.GetByPlaceholder("Write your article (in markdown)").DispatchEventAsync("input");
    await page.GetByPlaceholder("Write your article (in markdown)").DispatchEventAsync("change");
    await page.GetByPlaceholder("Enter tags").FillAsync("e2etag");
        await page.PressAsync("input[placeholder='Enter tags']", "Enter");
        await page.WaitForSelectorAsync(".tag-pill:has-text('e2etag')");
    await page.WaitForSelectorAsync("button:has-text('Publish Article'):not([disabled])", new() { Timeout = 60000 });
    await page.Locator("button:has-text('Publish Article')").ClickAsync();
        // Wait for SPA navigation to article page; the page renders a loading shell with this testid immediately
    await page.WaitForSelectorAsync(".article-page .banner, .article-page .article-actions");
        await page.WaitForURLAsync("**/article/**");
    await page.GotoAsync(_fixture.AppBaseUrl + "/");
    await page.WaitForURLAsync("**/");
    await page.WaitForFunctionAsync("() => window.abiesReady === true");
    await page.WaitForSelectorAsync(".home-page .feed-toggle");
    await page.WaitForSelectorAsync(".home-page .sidebar .tag-list");
    await page.WaitForSelectorAsync(".sidebar .tag-list a");
    var tagArticles = page.WaitForResponseAsync(r => r.Url.Contains("/api/articles?tag=") && r.Request.Method == "GET");
    await page.ClickAsync("text=e2etag");
    await tagArticles;
    await page.WaitForSelectorAsync(".home-page .article-preview");
    await page.WaitForSelectorAsync("text=Tag Article");
    }
}
