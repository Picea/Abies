using Microsoft.Playwright;
using Xunit;

namespace Abies.Conduit.E2E;

[Collection("Conduit collection")]
public class PaginationTests
{
    private readonly ConduitFixture _fixture;
    public PaginationTests(ConduitFixture fixture) => _fixture = fixture;

    private static async Task WaitForLoggedInAsync(IPage page)
    {
        await page.WaitForFunctionAsync("() => window.abiesReady === true");
        await page.WaitForFunctionAsync("() => !!localStorage.getItem('jwt')", null, new() { Timeout = 60000 });
    await page.WaitForSelectorAsync("text=Settings", new() { Timeout = 60000 });
    }

    [Fact]
    public async Task CanNavigateArticlePages()
    {
        var browser = _fixture.Browser;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

    var email = $"e2e{System.Guid.NewGuid():N}@example.com";
    var username = $"pageuser{System.Guid.NewGuid():N}".Substring(0, 16);

    await page.GotoAsync(_fixture.AppBaseUrl + "/register");
    await page.WaitForFunctionAsync("() => window.abiesReady === true");
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

        for (int i = 0; i < 11; i++)
        {
            await page.ClickAsync("text=New Article");
            await page.WaitForURLAsync("**/editor");
            await page.WaitForSelectorAsync("input[placeholder='Article Title']");
            await page.WaitForSelectorAsync("textarea[placeholder='Write your article (in markdown)']");
            await page.GetByPlaceholder("Article Title").FillAsync($"Page Article {i}");
            await page.GetByPlaceholder("Article Title").DispatchEventAsync("input");
            await page.GetByPlaceholder("Article Title").DispatchEventAsync("change");
            await page.WaitForFunctionAsync("() => { const el = document.querySelector(\"input[placeholder='Article Title']\"); return !!el && el.value && el.value.startsWith('Page Article '); }");
            await page.GetByPlaceholder("What's this article about?").FillAsync("Pages");
            await page.GetByPlaceholder("What's this article about?").DispatchEventAsync("input");
            await page.GetByPlaceholder("What's this article about?").DispatchEventAsync("change");
            await page.WaitForFunctionAsync("() => { const el = document.querySelector(\"input[placeholder=\\\"What's this article about?\\\"]\"); return !!el && el.value === 'Pages'; }");
            await page.GetByPlaceholder("Write your article (in markdown)").FillAsync("Body content for pagination test.");
            await page.GetByPlaceholder("Write your article (in markdown)").DispatchEventAsync("input");
            await page.GetByPlaceholder("Write your article (in markdown)").DispatchEventAsync("change");
            await page.WaitForFunctionAsync("() => { const el = document.querySelector(\"textarea[placeholder='Write your article (in markdown)']\"); return !!el && el.value === 'Body content for pagination test.'; }");
            await page.WaitForSelectorAsync("button:has-text('Publish Article'):not([disabled])", new() { Timeout = 60000 });
            await page.Locator("button:has-text('Publish Article')").ClickAsync();
            await page.WaitForSelectorAsync(".article-page .article-content, .article-page .banner");
            await page.ClickAsync("text=Home");
            await page.WaitForURLAsync("**/");
            await page.WaitForFunctionAsync("() => window.abiesReady === true");
            await page.WaitForSelectorAsync(".home-page .feed-toggle");
            await page.WaitForSelectorAsync(".home-page .article-preview, .home-page .sidebar");
        }
    await page.WaitForSelectorAsync(".home-page .feed-toggle");
    await page.WaitForSelectorAsync(".home-page .article-preview, .home-page .sidebar");
        // Ensure Global Feed is selected so all articles are counted
    await page.ClickAsync(".feed-toggle a.nav-link:has-text('Global Feed')");
    await page.WaitForSelectorAsync(".home-page .article-preview, .home-page .feed-toggle");
        // Wait until server reports at least 11 articles
        await page.WaitForFunctionAsync(
            "async () => { try { const r = await fetch('http://localhost:5179/api/articles?limit=1&offset=0'); if(!r.ok) return false; const j = await r.json(); return typeof j.articlesCount === 'number' && j.articlesCount >= 11; } catch { return false; } }",
            null,
            new() { Timeout = 60000 }
        );
        // Wait for pagination to appear and contain page 2
        await page.WaitForSelectorAsync("ul.pagination li a.page-link", new() { Timeout = 60000 });
        await page.WaitForFunctionAsync("() => Array.from(document.querySelectorAll('ul.pagination li a.page-link')).some(a => (a.textContent||'').trim() === '2')", null, new() { Timeout = 60000 });
        // Click on the link with text '2'
        try
        {
            await page.Locator("ul.pagination li a.page-link", new() { HasTextString = "2" }).First.ClickAsync();
        }
        catch
        {
            // If click didn't register due to render timing, reload and retry once
            await page.ReloadAsync();
            await page.WaitForSelectorAsync(".home-page .feed-toggle");
            await page.Locator(".feed-toggle a.nav-link:has-text('Global Feed')").ClickAsync();
            await page.WaitForSelectorAsync("ul.pagination li a.page-link");
            await page.Locator("ul.pagination li a.page-link", new() { HasTextString = "2" }).First.ClickAsync();
        }
        // Assert active page highlights '2'
        await page.WaitForSelectorAsync("ul.pagination li.active a.page-link", new() { Timeout = 60000 });
        await page.WaitForFunctionAsync("() => { const el = document.querySelector('ul.pagination li.active a.page-link'); if(!el) return false; return (el.textContent||'').trim() === '2'; }", null, new() { Timeout = 60000 });
    }
}
