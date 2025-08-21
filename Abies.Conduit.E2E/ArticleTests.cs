using Microsoft.Playwright;
using Xunit;

namespace Abies.Conduit.E2E;

[Collection("Conduit collection")]
public class ArticleTests
{
    private readonly ConduitFixture _fixture;
    public ArticleTests(ConduitFixture fixture) => _fixture = fixture;

    private static async Task WaitForLoggedInAsync(IPage page)
    {
        await page.WaitForFunctionAsync("() => window.abiesReady === true");
        await page.WaitForFunctionAsync("() => !!localStorage.getItem('jwt')", null, new() { Timeout = 60000 });
    await page.WaitForSelectorAsync("text=Settings", new() { Timeout = 60000 });
    }

    private static async Task WaitForLoggedOutAsync(IPage page)
    {
        await page.WaitForFunctionAsync("() => window.abiesReady === true");
        await page.WaitForFunctionAsync("() => !localStorage.getItem('jwt')", null, new() { Timeout = 60000 });
        await page.WaitForSelectorAsync("text=Sign in", new() { Timeout = 60000 });
    }

    [Fact]
    public async Task CanCreateEditAndDeleteArticle()
    {
        var browser = _fixture.Browser;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        // register and login
    await page.GotoAsync(_fixture.AppBaseUrl + "/register");
    await page.WaitForFunctionAsync("() => window.abiesReady === true");
    var email = $"e2e{System.Guid.NewGuid():N}@example.com";
    var username = $"article{System.Guid.NewGuid():N}".Substring(0, 16);
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

    // logout to verify login as existing user works (token clear + reload)
    await page.EvaluateAsync("() => { localStorage.removeItem('jwt'); location.reload(); }");
    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    await WaitForLoggedOutAsync(page);
    await page.GotoAsync(_fixture.AppBaseUrl + "/login");
    await page.WaitForFunctionAsync("() => window.abiesReady === true");
    await page.GetByPlaceholder("Email").FillAsync(email);
    await page.GetByPlaceholder("Email").DispatchEventAsync("input");
    await page.GetByPlaceholder("Email").DispatchEventAsync("change");
    await page.GetByPlaceholder("Password").FillAsync("Password1!");
    await page.GetByPlaceholder("Password").DispatchEventAsync("input");
    await page.GetByPlaceholder("Password").DispatchEventAsync("change");
    await page.WaitForSelectorAsync("button:has-text('Sign in'):not([disabled])");
    await page.Locator("button:has-text('Sign in')").ClickAsync();
        await WaitForLoggedInAsync(page);

    await page.ClickAsync("text=New Article");
    await page.WaitForURLAsync("**/editor");
    await page.WaitForSelectorAsync("input[placeholder='Article Title']");
    await page.WaitForSelectorAsync("textarea[placeholder='Write your article (in markdown)']");
    await page.GetByPlaceholder("Article Title").FillAsync("E2E Article");
    await page.GetByPlaceholder("Article Title").DispatchEventAsync("input");
    await page.GetByPlaceholder("Article Title").DispatchEventAsync("change");
    await page.GetByPlaceholder("What's this article about?").FillAsync("Testing");
    await page.GetByPlaceholder("What's this article about?").DispatchEventAsync("input");
    await page.GetByPlaceholder("What's this article about?").DispatchEventAsync("change");
    await page.GetByPlaceholder("Write your article (in markdown)").FillAsync("Hello World from E2E.");
    await page.GetByPlaceholder("Write your article (in markdown)").DispatchEventAsync("input");
    await page.GetByPlaceholder("Write your article (in markdown)").DispatchEventAsync("change");
    await page.WaitForFunctionAsync("() => { const t = document.querySelector(\"input[placeholder='Article Title']\"); const d = document.querySelector(\"input[placeholder=\\\"What's this article about?\\\"]\"); const b = document.querySelector(\"textarea[placeholder='Write your article (in markdown)']\"); return !!t && t.value === 'E2E Article' && !!d && d.value === 'Testing' && !!b && b.value === 'Hello World from E2E.'; }", null, new() { Timeout = 60000 });
    var prePublishDiag = await page.EvaluateAsync<string>("() => JSON.stringify(Array.from(document.querySelectorAll('button')).map(b => ({ text: (b.textContent||'').trim(), disabled: !!b.disabled })))");
    System.Console.WriteLine($"Buttons before first publish: {prePublishDiag}");
    await page.WaitForSelectorAsync("button:has-text('Publish Article'):not([disabled])", new() { Timeout = 60000 });
    await page.Locator("button:has-text('Publish Article')").ClickAsync();
    await page.WaitForSelectorAsync(".article-page .banner, .article-page .article-actions");
    await page.WaitForSelectorAsync("h1");
        // Go to editor to update the article
    await page.ClickAsync("text=Edit Article");
    await page.WaitForSelectorAsync("textarea[placeholder='Write your article (in markdown)']");
    await page.WaitForSelectorAsync("textarea[placeholder='Write your article (in markdown)']");
    var onEditDiag = await page.EvaluateAsync<string>("() => JSON.stringify({ " +
        "btns: Array.from(document.querySelectorAll('button')).map(b => ({ text: (b.textContent||'').trim(), disabled: !!b.disabled })), " +
        "title: document.querySelector(\"input[placeholder='Article Title']\")?.value, " +
        "desc: document.querySelector(\"input[placeholder=\\\"What's this article about?\\\"]\")?.value, " +
        "body: document.querySelector(\"textarea[placeholder='Write your article (in markdown)']\")?.value " +
    "})");
    System.Console.WriteLine($"Editor after navigating to Edit: {onEditDiag}");

    await page.GetByPlaceholder("Article Title").FillAsync("E2E Article Updated");
    await page.GetByPlaceholder("Article Title").DispatchEventAsync("input");
    await page.GetByPlaceholder("Article Title").DispatchEventAsync("change");
    await page.WaitForSelectorAsync("button:has-text('Update Article'):not([disabled])");
    var waitUpdate = page.WaitForResponseAsync(r => r.Url.Contains("/api/articles/") && r.Request.Method == "PUT");
    await page.Locator("button:has-text('Update Article')").ClickAsync();
    await waitUpdate;
    await page.WaitForSelectorAsync(".article-page .banner, .article-page .article-actions");
    await page.WaitForFunctionAsync("() => { const h = document.querySelector('h1'); return !!h && h.textContent.trim() === 'E2E Article Updated'; }", null, new() { Timeout = 60000 });

    await page.ClickAsync("text=Delete Article");
    await page.WaitForSelectorAsync("text=Global Feed");
    }
}
