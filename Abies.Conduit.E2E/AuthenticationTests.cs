using Microsoft.Playwright;
using Xunit;

namespace Abies.Conduit.E2E;

[CollectionDefinition("Conduit collection")]
public class ConduitCollection : ICollectionFixture<ConduitFixture> { }

[Collection("Conduit collection")]
public class AuthenticationTests
{
    private readonly ConduitFixture _fixture;
    public AuthenticationTests(ConduitFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CanRegisterAndLogin()
    {
        var browser = _fixture.Browser;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("http://localhost:5209/register");
        await page.TypeAsync("input[placeholder=Username]", "e2euser" + System.Guid.NewGuid().ToString("N").Substring(0,6));
        var email = $"e2e{System.Guid.NewGuid():N}@example.com";
        await page.TypeAsync("input[placeholder=Email]", email);
        await page.TypeAsync("input[placeholder=Password]", "Password1!");
        await page.ClickAsync("button:has-text('Sign up')");
        await page.WaitForSelectorAsync("text=Your Feed");
        await page.ClickAsync("text=Settings");
        await page.ClickAsync("text=Or click here to logout");
        await page.GotoAsync("http://localhost:5209/login");
        await page.TypeAsync("input[placeholder=Email]", email);
        await page.TypeAsync("input[placeholder=Password]", "Password1!");
        await page.ClickAsync("button:has-text('Sign in')");
        await page.WaitForSelectorAsync("text=Your Feed");
    }
}
