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

    // Pipe browser console messages to test output for debugging
    page.Console += (_, msg) => Console.WriteLine($"[PW] {msg.Type}: {msg.Text}");

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
        // Wait for list to be in 'loaded' state, then for pagination to appear and contain page 2
        await page.WaitForSelectorAsync("[data-testid='article-list'][data-status='loaded']", new() { Timeout = 60000 });
        await page.WaitForSelectorAsync("ul.pagination li a.page-link", new() { Timeout = 60000 });
        await page.WaitForFunctionAsync("() => Array.from(document.querySelectorAll('ul.pagination li a.page-link')).some(a => (a.textContent||'').trim() === '2')", null, new() { Timeout = 60000 });
        // Capture the anchor we'll click (id, dataset, outerHTML) for diagnostics, then click it
    string? anchorBefore = null;
        try
        {
            anchorBefore = await page.EvaluateAsync<string>("() => { const a = Array.from(document.querySelectorAll('ul.pagination li a.page-link')).find(x => (x.textContent||'').trim() === '2'); if(!a) return JSON.stringify({found:false}); try { return JSON.stringify({found:true, id: a.id || null, dataset: Object.assign({}, a.dataset), outer: a.outerHTML}); } catch(e) { return JSON.stringify({found:true, id: a.id || null, outer: a.outerHTML, evalError: e.message}); } }");
            Console.WriteLine("[PW] clickedAnchor before click: " + anchorBefore);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[PW] failed to read clicked anchor before click: " + ex.Message);
        }

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
            await page.WaitForSelectorAsync("[data-testid='article-list'][data-status='loaded']");
            await page.WaitForSelectorAsync("ul.pagination li a.page-link");
            await page.Locator("ul.pagination li a.page-link", new() { HasTextString = "2" }).First.ClickAsync();
        }
        // Wait for list to load after clicking and assert current page via aria-current on the active link
        await page.WaitForSelectorAsync("[data-testid='article-list'][data-status='loaded']", new() { Timeout = 60000 });
        // Dump pagination outerHTML for diagnosis
        try
        {
            var outer = await page.EvaluateAsync<string>("() => document.querySelector('ul.pagination') ? document.querySelector('ul.pagination').outerHTML : ''");
            Console.WriteLine("[PW] pagination outerHTML after click: \n" + outer);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[PW] failed to read pagination outerHTML: " + ex.Message);
        }

        // Dump the runtime debug bridge if present
        try
        {
            // Filter only logs related to data-current-page updates to reduce noise
            var dbgFiltered = await page.EvaluateAsync<string>("() => { try { const d = window.__abiesDebug || {}; const logs = Array.isArray(d.logs) ? d.logs.filter(l => (l.type === 'updateAttribute' || l.type === 'addAttribute') && l.propertyName === 'data-current-page') : []; return JSON.stringify({ filteredLogs: logs, registeredEvents: d.registeredEvents || [] }); } catch(e) { return 'eval-failed:' + e.message; } }");
            Console.WriteLine("[PW] __abiesDebug (filtered): " + dbgFiltered);
            // Also output structured replacements/events/attributes for precise diagnosis
            var dbgStructured = await page.EvaluateAsync<string>(
                "() => { try { const d = window.__abiesDebug || {}; return JSON.stringify({ replacements: d.replacements || [], events: d.events || [], attributes: d.attributes || [] }); } catch(e) { return 'eval-failed:' + e.message; } }"
            );
            Console.WriteLine("[PW] __abiesDebug (structured): " + dbgStructured);
            // Also capture the anchor element after the click for comparison
            try
            {
                var anchorAfter = await page.EvaluateAsync<string>("() => { const a = Array.from(document.querySelectorAll('ul.pagination li a.page-link')).find(x => (x.textContent||'').trim() === '2'); if(!a) return JSON.stringify({found:false}); return JSON.stringify({found:true, id: a.id || null, dataset: Object.assign({}, a.dataset), outer: a.outerHTML, aria: a.getAttribute('aria-current')}); }");
                Console.WriteLine("[PW] clickedAnchor after click: " + anchorAfter);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[PW] failed to read clicked anchor after click: " + ex.Message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[PW] failed to read __abiesDebug: " + ex.Message);
        }

    // Prefer a robust wait: active link in pagination becomes '2'
    await page.WaitForFunctionAsync("() => { const a = document.querySelector('ul.pagination li a.page-link[aria-current=\"page\"]'); return !!a && (a.textContent||'').trim() === '2'; }", null, new() { Timeout = 60000 });
    }
}
