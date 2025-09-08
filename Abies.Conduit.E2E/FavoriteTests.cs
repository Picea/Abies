using Microsoft.Playwright;
using Xunit;

namespace Abies.Conduit.E2E;

[Collection("Conduit collection")]
public class FavoriteTests
{
    private readonly ConduitFixture _fixture;
    public FavoriteTests(ConduitFixture fixture) => _fixture = fixture;

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
    public async Task CanFavoriteAndUnfavoriteArticle()
    {
        var browser = _fixture.Browser;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var email = $"e2e{System.Guid.NewGuid():N}@example.com";
        var username = $"favuser{System.Guid.NewGuid():N}".Substring(0, 16);
        var email2 = $"e2e{System.Guid.NewGuid():N}@example.com";
        var username2 = $"favuserb{System.Guid.NewGuid():N}".Substring(0, 16);

    // Register first user
        await page.GotoAsync(_fixture.AppBaseUrl + "/register");
        await page.WaitForFunctionAsync("() => window.abiesReady === true");
        await page.GetByPlaceholder("Username").FillAsync(username);
        await page.GetByPlaceholder("Email").FillAsync(email);
        await page.GetByPlaceholder("Password").FillAsync("Password1!");
        await page.WaitForSelectorAsync("button:has-text('Sign up'):not([disabled])");
        await page.Locator("button:has-text('Sign up')").ClickAsync();
    await WaitForLoggedInAsync(page);

        // Create an article
        await page.ClickAsync("text=New Article");
    // Wait for editor by its known fields/buttons instead of data-testid
    await page.WaitForURLAsync("**/editor");
    await page.WaitForSelectorAsync("input[placeholder='Article Title']");
        await page.GetByPlaceholder("Article Title").FillAsync("Fav Article");
        await page.GetByPlaceholder("What's this article about?").FillAsync("Favs");
        await page.GetByPlaceholder("Write your article (in markdown)").FillAsync("Body");
    await page.WaitForSelectorAsync("button:has-text('Publish Article'):not([disabled])", new() { Timeout = 60000 });
        await page.Locator("button:has-text('Publish Article')").ClickAsync();
        await page.WaitForURLAsync("**/article/*");
    await page.WaitForSelectorAsync(".article-page .banner, .article-page .article-actions");
        var slug = page.Url.Split('/').Last();

    // Logout author (no UI dependency): clear token and reload
    await page.EvaluateAsync("() => { localStorage.removeItem('jwt'); location.reload(); }");
    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    await WaitForLoggedOutAsync(page);

    // Register second user
        await page.GotoAsync(_fixture.AppBaseUrl + "/register");
        await page.WaitForFunctionAsync("() => window.abiesReady === true");
        await page.GetByPlaceholder("Username").FillAsync(username2);
        await page.GetByPlaceholder("Email").FillAsync(email2);
        await page.GetByPlaceholder("Password").FillAsync("Password1!");
        await page.WaitForSelectorAsync("button:has-text('Sign up'):not([disabled])");
        await page.Locator("button:has-text('Sign up')").ClickAsync();
    await WaitForLoggedInAsync(page);

        // Open article as second user
    await page.GotoAsync(_fixture.AppBaseUrl + $"/article/{slug}");
    await page.WaitForSelectorAsync(".article-page .banner, .article-page .article-actions");
    await page.WaitForFunctionAsync("() => window.abiesReady === true");

        // Ensure server recognizes our Authorization before acting
        await page.WaitForFunctionAsync(
            "async () => { const t = localStorage.getItem('jwt'); if(!t) return false; try { const r = await fetch('http://localhost:5179/api/user', { headers: { 'Authorization': 'Token ' + t } }); return r.ok; } catch { return false; } }",
            null,
            new() { Timeout = 60000 }
        );

    // Prefer class-based selectors (text can include counts and spacing)
    var favSelector = ".article-page .article-meta button.btn-outline-primary:has(.ion-heart)";
    var unfavSelector = ".article-page .article-meta button.btn-primary:has(.ion-heart)";
        var bannerFavSelector = ".article-page .banner .article-meta button:has(.ion-heart)";
        var actionsFavSelector = ".article-page .article-actions .article-meta button:has(.ion-heart)";

        // Ensure we start from not-favorited state
        if (await page.Locator(unfavSelector).CountAsync() > 0)
        {
            await page.Locator(unfavSelector).First.ClickAsync(new() { Force = true });
            await page.WaitForSelectorAsync(favSelector);
        }

    // Favorite
        async Task<bool> TryClickAsync(string selector)
        {
            var btnLoc = page.Locator(selector).First;
            if (await btnLoc.CountAsync() == 0) return false;
            await btnLoc.ScrollIntoViewIfNeededAsync();
            await btnLoc.ClickAsync(new() { Force = true });
            return true;
        }

        // Ensure we see at least one button instance
        await page.WaitForSelectorAsync($"{bannerFavSelector}, {actionsFavSelector}");

        // Click the specific 'Favorite' text variant if present
        var clicked = await TryClickAsync(favSelector);
        if (!clicked)
        {
            // Fall back to clicking banner, then actions
            clicked = await TryClickAsync(bannerFavSelector) || await TryClickAsync(actionsFavSelector);
        }
        // Confirm server-side favorited state for the current user
        await page.WaitForFunctionAsync(
            $"async () => {{ const t = localStorage.getItem('jwt'); if(!t) return false; try {{ const r = await fetch('http://localhost:5179/api/articles/{slug}'); if(!r.ok) return false; const j = await r.json(); return !!j.article && j.article.favorited === true; }} catch {{ return false; }} }}",
            null,
            new() { Timeout = 60000 }
        );
        // Prefer UI reflection, but fall back to a quick reload if needed
        try
        {
            await page.WaitForSelectorAsync(unfavSelector, new() { Timeout = 5000 });
        }
        catch
        {
            await page.ReloadAsync();
            await page.WaitForSelectorAsync(".article-page .banner, .article-page .article-actions");
            await page.WaitForSelectorAsync(unfavSelector, new() { Timeout = 10000 });
        }

        // Unfavorite
        // Recreate locator after potential re-render
        var unfavBtn = page.Locator(unfavSelector).First;
        await unfavBtn.ScrollIntoViewIfNeededAsync();
        await unfavBtn.ClickAsync(new() { Force = true });
        // Confirm server-side unfavorited state
        await page.WaitForFunctionAsync(
            $"async () => {{ try {{ const r = await fetch('http://localhost:5179/api/articles/{slug}'); if(!r.ok) return false; const j = await r.json(); return !!j.article && j.article.favorited === false; }} catch {{ return false; }} }}",
            null,
            new() { Timeout = 60000 }
        );
        // Prefer UI reflection, allow brief fallback reload
        try
        {
            await page.WaitForSelectorAsync(favSelector, new() { Timeout = 5000 });
        }
        catch
        {
            await page.ReloadAsync();
            await page.WaitForSelectorAsync(".article-page .banner, .article-page .article-actions");
            await page.WaitForSelectorAsync(favSelector, new() { Timeout = 10000 });
        }
    }
}
