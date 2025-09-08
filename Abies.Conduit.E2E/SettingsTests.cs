using Microsoft.Playwright;
using Xunit;

namespace Abies.Conduit.E2E;

[Collection("Conduit collection")]
public class SettingsTests
{
    private readonly ConduitFixture _fixture;
    public SettingsTests(ConduitFixture fixture) => _fixture = fixture;

    private static async Task WaitForLoggedInAsync(IPage page)
    {
        await page.WaitForFunctionAsync("() => window.abiesReady === true");
        await page.WaitForFunctionAsync("() => !!localStorage.getItem('jwt')", null, new() { Timeout = 60000 });
        await page.WaitForSelectorAsync("text=Settings", new() { Timeout = 60000 });
    }

    [Fact]
    public async Task CanUpdateUserSettings()
    {
        var browser = _fixture.Browser;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var email = $"e2e{System.Guid.NewGuid():N}@example.com";

    await page.GotoAsync(_fixture.AppBaseUrl + "/register");
    await page.WaitForFunctionAsync("() => window.abiesReady === true");
    var username = $"settings{System.Guid.NewGuid():N}".Substring(0, 16);
    await page.WaitForSelectorAsync("input[placeholder='Username']");
    await page.WaitForSelectorAsync("input[placeholder='Email']");
    await page.WaitForSelectorAsync("input[placeholder='Password']");
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

    await page.ClickAsync("text=Settings");
    await page.WaitForSelectorAsync(".settings-page form");
    await page.WaitForSelectorAsync("input[placeholder='URL of profile picture']");
    await page.WaitForSelectorAsync("textarea[placeholder='Short bio about you']");
    await page.GetByPlaceholder("URL of profile picture").FillAsync("http://example.com/pic.png");
    await page.GetByPlaceholder("URL of profile picture").DispatchEventAsync("input");
    await page.GetByPlaceholder("URL of profile picture").DispatchEventAsync("change");
    await page.GetByPlaceholder("Short bio about you").FillAsync("Hello there");
    await page.GetByPlaceholder("Short bio about you").DispatchEventAsync("input");
    await page.GetByPlaceholder("Short bio about you").DispatchEventAsync("change");
    await page.WaitForSelectorAsync("button:has-text('Update Settings'):not([disabled])");
    await page.Locator("button:has-text('Update Settings')").ClickAsync();
        await page.WaitForSelectorAsync("text=Your Feed");
    await page.ClickAsync("text=Settings");
    await page.WaitForSelectorAsync(".settings-page form");
    var imgVal = await page.EvaluateAsync<string>(@"() => { const el = document.querySelector(""input[placeholder='URL of profile picture']""); return el ? `value=${el.value} attr=${el.getAttribute('value') ?? ''} defaultValue=${el.defaultValue ?? ''}` : 'not-found'; }");
    System.Console.WriteLine($"[e2e] settings image field state: {imgVal}");
    await page.WaitForFunctionAsync(@"() => { const el = document.querySelector(""input[placeholder='URL of profile picture']""); return !!el && el.value === 'http://example.com/pic.png'; }");
        await page.WaitForSelectorAsync("textarea:has-text('Hello there')");
    }
}
