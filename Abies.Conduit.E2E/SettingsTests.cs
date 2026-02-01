using Microsoft.Playwright;

namespace Abies.Conduit.E2E;

/// <summary>
/// E2E tests for user settings user journey.
/// Covers: Update profile settings (CRU - no delete required per spec)
/// </summary>
public class SettingsTests : PlaywrightFixture
{
    [Fact]
    public async Task UpdateSettings_ChangeBio_UpdatesProfile()
    {
        var (username, _, _) = await RegisterTestUserAsync();

        // Navigate to settings
        await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await Page.WaitForURLAsync("**/settings", new() { Timeout = 10000 });

        // Update bio
        var newBio = $"Updated bio at {DateTime.UtcNow:O}";
        var bioTextarea = Page.GetByPlaceholder("Short bio about you");
        await bioTextarea.FillAsync(newBio);

        // Submit
        await Page.GetByRole(AriaRole.Button, new() { Name = "Update Settings" }).ClickAsync();

        // Wait for update to complete
        await Page.WaitForTimeoutAsync(1000);

        // Navigate to profile and verify bio
        await Page.GetByRole(AriaRole.Link, new() { Name = username }).ClickAsync();
        await Page.WaitForURLAsync($"**/profile/{username}", new() { Timeout = 10000 });

        // Bio should be visible on profile page
        await Expect(Page.GetByText(newBio)).ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    [Fact]
    public async Task UpdateSettings_ChangeProfileImage_UpdatesAvatar()
    {
        var (username, _, _) = await RegisterTestUserAsync();

        // Navigate to settings
        await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await Page.WaitForURLAsync("**/settings", new() { Timeout = 10000 });

        // Update profile image URL
        var newImageUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=TestUser";
        var imageInput = Page.GetByPlaceholder("URL of profile picture");
        await imageInput.FillAsync(newImageUrl);

        // Submit
        await Page.GetByRole(AriaRole.Button, new() { Name = "Update Settings" }).ClickAsync();

        // Wait for update
        await Page.WaitForTimeoutAsync(1000);

        // Navigate to profile
        await Page.GetByRole(AriaRole.Link, new() { Name = username }).ClickAsync();
        await Page.WaitForURLAsync($"**/profile/{username}", new() { Timeout = 10000 });

        // Profile image should have the new URL
        var profileImg = Page.Locator(".user-img");
        await Expect(profileImg).ToHaveAttributeAsync("src", newImageUrl);
    }

    [Fact]
    public async Task SettingsPage_ShowsCurrentUserInfo()
    {
        var (username, email, _) = await RegisterTestUserAsync();

        // Navigate to settings
        await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await Page.WaitForURLAsync("**/settings", new() { Timeout = 10000 });

        // Username and email should be pre-filled
        var usernameInput = Page.GetByPlaceholder("Your Name");
        var emailInput = Page.GetByPlaceholder("Email");

        await Expect(usernameInput).ToHaveValueAsync(username);
        await Expect(emailInput).ToHaveValueAsync(email);
    }

    [Fact]
    public async Task SettingsPage_HasLogoutButton()
    {
        await RegisterTestUserAsync();

        // Navigate to settings
        await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await Page.WaitForURLAsync("**/settings", new() { Timeout = 10000 });

        // Logout button should be visible
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "logout", Exact = false })).ToBeVisibleAsync();
    }
}
