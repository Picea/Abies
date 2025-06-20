using Microsoft.Playwright;
using Xunit;

namespace Abies.Conduit.E2E;

[Collection("Conduit collection")]
public class SettingsTests
{
    private readonly ConduitFixture _fixture;
    public SettingsTests(ConduitFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CanUpdateUserSettings()
    {
        var browser = _fixture.Browser;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var email = $"e2e{System.Guid.NewGuid():N}@example.com";

        await page.GotoAsync("http://localhost:5209/register");
        await page.FillAsync("input[placeholder=Username]", "settingsuser");
        await page.FillAsync("input[placeholder=Email]", email);
        await page.FillAsync("input[placeholder=Password]", "Password1!");
        await page.ClickAsync("button[type=submit]");
        await page.WaitForSelectorAsync("text=Your Feed");

        await page.ClickAsync("text=Settings");
        await page.FillAsync("input[placeholder='URL of profile picture']", "http://example.com/pic.png");
        await page.FillAsync("textarea[placeholder='Short bio about you']", "Hello there");
        await page.ClickAsync("button[type=submit]");
        await page.WaitForSelectorAsync("text=Your Feed");
        await page.ClickAsync("text=Settings");
        await page.WaitForSelectorAsync("input[value='http://example.com/pic.png']");
        await page.WaitForSelectorAsync("textarea:has-text('Hello there')");
    }
}
