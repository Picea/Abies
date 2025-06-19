using Microsoft.Playwright;
using Xunit;

namespace Abies.Conduit.E2E;

[Collection("Conduit collection")]
public class ProfileTests
{
    private readonly ConduitFixture _fixture;
    public ProfileTests(ConduitFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CanFollowAndUnfollowUser()
    {
        var browser = _fixture.Browser;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var userA = $"e2e{System.Guid.NewGuid():N}@example.com";
        var userB = $"e2e{System.Guid.NewGuid():N}@example.com";

        // register user B and create an article
        await page.GotoAsync("http://localhost:5209/register");
        await page.TypeAsync("input[placeholder=Username]", "userb");
        await page.TypeAsync("input[placeholder=Email]", userB);
        await page.TypeAsync("input[placeholder=Password]", "Password1!");
        await page.ClickAsync("button:has-text('Sign up')");
        await page.WaitForSelectorAsync("text=Your Feed");
        await page.ClickAsync("text=New Article");
        await page.TypeAsync("input[placeholder=Article Title]", "UserB Article");
        await page.TypeAsync("input[placeholder='What\'s this article about?']", "Following");
        await page.TypeAsync("textarea[placeholder='Write your article (in markdown)']", "Content");
        await page.ClickAsync("button:has-text('Publish Article')");
        await page.WaitForSelectorAsync("text=Edit Article");
        await page.ClickAsync("text=Sign out");

        // register user A
        await page.GotoAsync("http://localhost:5209/register");
        await page.TypeAsync("input[placeholder=Username]", "usera");
        await page.TypeAsync("input[placeholder=Email]", userA);
        await page.TypeAsync("input[placeholder=Password]", "Password1!");
        await page.ClickAsync("button:has-text('Sign up')");
        await page.WaitForSelectorAsync("text=Your Feed");

        await page.GotoAsync("http://localhost:5209/profile/userb");
        await page.ClickAsync("button:has-text('Follow')");
        await page.WaitForSelectorAsync("button:has-text('Unfollow')");
        await page.ClickAsync("button:has-text('Unfollow')");
        await page.WaitForSelectorAsync("button:has-text('Follow')");
    }
}
