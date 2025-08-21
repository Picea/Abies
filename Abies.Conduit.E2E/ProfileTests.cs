using Microsoft.Playwright;
using Xunit;

namespace Abies.Conduit.E2E;

[Collection("Conduit collection")]
public class ProfileTests
{
    private readonly ConduitFixture _fixture;
    public ProfileTests(ConduitFixture fixture) => _fixture = fixture;

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
    public async Task CanFollowAndUnfollowUser()
    {
        var browser = _fixture.Browser;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

    var userA = $"e2e{System.Guid.NewGuid():N}@example.com";
    var userB = $"e2e{System.Guid.NewGuid():N}@example.com";
    var usernameA = $"usera{System.Guid.NewGuid():N}".Substring(0, 16);
    var usernameB = $"userb{System.Guid.NewGuid():N}".Substring(0, 16);

        // register user B and create an article
    await page.GotoAsync(_fixture.AppBaseUrl + "/register");
    await page.WaitForFunctionAsync("() => window.abiesReady === true");
    await page.GetByPlaceholder("Username").FillAsync(usernameB);
    await page.GetByPlaceholder("Username").DispatchEventAsync("input");
    await page.GetByPlaceholder("Username").DispatchEventAsync("change");
    await page.GetByPlaceholder("Email").FillAsync(userB);
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
    await page.WaitForSelectorAsync("textarea[placeholder='Write your article (in markdown)']");
    await page.GetByPlaceholder("Article Title").FillAsync("UserB Article");
    await page.GetByPlaceholder("Article Title").DispatchEventAsync("input");
    await page.GetByPlaceholder("Article Title").DispatchEventAsync("change");
    await page.WaitForFunctionAsync("() => { const el = document.querySelector(\"input[placeholder='Article Title']\"); return !!el && el.value === 'UserB Article'; }");
    await page.GetByPlaceholder("What's this article about?").FillAsync("Following");
    await page.GetByPlaceholder("What's this article about?").DispatchEventAsync("input");
    await page.GetByPlaceholder("What's this article about?").DispatchEventAsync("change");
    await page.WaitForFunctionAsync("() => { const el = document.querySelector(\"input[placeholder=\\\"What's this article about?\\\"]\"); return !!el && el.value === 'Following'; }");
    await page.GetByPlaceholder("Write your article (in markdown)").FillAsync("Content for follow/unfollow test.");
    await page.GetByPlaceholder("Write your article (in markdown)").DispatchEventAsync("input");
    await page.GetByPlaceholder("Write your article (in markdown)").DispatchEventAsync("change");
    await page.WaitForFunctionAsync("() => { const el = document.querySelector(\"textarea[placeholder='Write your article (in markdown)']\"); return !!el && el.value === 'Content for follow/unfollow test.'; }");
    await page.WaitForSelectorAsync("button:has-text('Publish Article'):not([disabled])", new() { Timeout = 60000 });
    await page.Locator("button:has-text('Publish Article')").ClickAsync();
    await page.WaitForSelectorAsync(".article-page .banner, .article-page .article-actions");
    // Logout via token clear + reload
    await page.EvaluateAsync("() => { localStorage.removeItem('jwt'); location.reload(); }");
    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    await WaitForLoggedOutAsync(page);

        // register user A
    await page.GotoAsync(_fixture.AppBaseUrl + "/register");
    await page.WaitForFunctionAsync("() => window.abiesReady === true");
    await page.GetByPlaceholder("Username").FillAsync(usernameA);
    await page.GetByPlaceholder("Username").DispatchEventAsync("input");
    await page.GetByPlaceholder("Username").DispatchEventAsync("change");
    await page.GetByPlaceholder("Email").FillAsync(userA);
    await page.GetByPlaceholder("Email").DispatchEventAsync("input");
    await page.GetByPlaceholder("Email").DispatchEventAsync("change");
    await page.GetByPlaceholder("Password").FillAsync("Password1!");
    await page.GetByPlaceholder("Password").DispatchEventAsync("input");
    await page.GetByPlaceholder("Password").DispatchEventAsync("change");
    await page.WaitForSelectorAsync("button:has-text('Sign up'):not([disabled])");
    await page.Locator("button:has-text('Sign up')").ClickAsync();
        await WaitForLoggedInAsync(page);

    await page.GotoAsync(_fixture.AppBaseUrl + $"/profile/{usernameB}");
    await page.WaitForFunctionAsync("() => window.abiesReady === true");
    await page.WaitForSelectorAsync(".profile-page");
    await page.WaitForSelectorAsync("button.action-btn");
        // Ensure server recognizes our Authorization before acting
        await page.WaitForFunctionAsync(
            "async () => { const t = localStorage.getItem('jwt'); if(!t) return false; try { const r = await fetch('http://localhost:5179/api/user', { headers: { 'Authorization': 'Token ' + t } }); return r.ok; } catch { return false; } }",
            null,
            new() { Timeout = 60000 }
        );
        // Click follow (outline -> secondary)
        await page.ClickAsync("button.btn.btn-sm.btn-outline-secondary.action-btn:has(.ion-plus-round)");
        // Confirm server reports following
        await page.WaitForFunctionAsync(
            $"async () => {{ try {{ const r = await fetch('http://localhost:5179/api/profiles/{usernameB}'); if(!r.ok) return false; const j = await r.json(); return !!j.profile && j.profile.following === true; }} catch {{ return false; }} }}",
            null,
            new() { Timeout = 60000 }
        );
        // Prefer UI reflection; if not yet reflected, reload
        try
        {
            await page.WaitForSelectorAsync("button.action-btn.btn.btn-sm.btn-secondary:has(.ion-plus-round)", new() { Timeout = 5000 });
        }
        catch
        {
            await page.ReloadAsync();
            await page.WaitForSelectorAsync(".profile-page");
            await page.WaitForSelectorAsync("button.action-btn.btn.btn-sm.btn-secondary:has(.ion-plus-round)", new() { Timeout = 10000 });
        }

        // Click unfollow (secondary -> outline)
        await page.ClickAsync("button.btn.btn-sm.btn-secondary.action-btn:has(.ion-plus-round)");
        // Confirm server reports not following
        await page.WaitForFunctionAsync(
            $"async () => {{ try {{ const r = await fetch('http://localhost:5179/api/profiles/{usernameB}'); if(!r.ok) return false; const j = await r.json(); return !!j.profile && j.profile.following === false; }} catch {{ return false; }} }}",
            null,
            new() { Timeout = 60000 }
        );
        try
        {
            await page.WaitForSelectorAsync("button.action-btn.btn.btn-sm.btn-outline-secondary:has(.ion-plus-round)", new() { Timeout = 5000 });
        }
        catch
        {
            await page.ReloadAsync();
            await page.WaitForSelectorAsync(".profile-page");
            await page.WaitForSelectorAsync("button.action-btn.btn.btn-sm.btn-outline-secondary:has(.ion-plus-round)", new() { Timeout = 10000 });
        }
    }
}
