// =============================================================================
// Settings E2E Tests — Update profile, change password, logout
// =============================================================================

using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;
using Microsoft.Playwright;

namespace Picea.Abies.Conduit.Testing.E2E;

[Trait("Category", "E2E")]
[Collection("Conduit")]
public sealed class SettingsTests : IAsyncLifetime
{
    private readonly ConduitAppFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public SettingsTests(ConduitAppFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _seeder = new ApiSeeder(_fixture.ApiUrl);
    }

    public async Task DisposeAsync()
    {
        await _page.Context.DisposeAsync();
    }

    [Fact]
    public async Task Settings_WhenLoggedIn_ShouldShowCurrentUserInfo()
    {
        var username = $"settuser{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");
        await LoginViaUi(email, "password123");

        await _page.Locator(".navbar").GetByText("Settings").ClickAsync();
        await _page.WaitForSelectorAsync(".settings-page", new() { Timeout = 10000 });

        await Expect(_page.GetByPlaceholder("Email")).ToHaveValueAsync(email);
        await Expect(_page.GetByPlaceholder("Your Name")).ToHaveValueAsync(username);
    }

    [Fact]
    public async Task UpdateSettings_WithNewBioAndImage_ShouldPersistChanges()
    {
        var username = $"updsett{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");
        await LoginViaUi(email, "password123");

        await _page.Locator(".navbar").GetByText("Settings").ClickAsync();
        await _page.WaitForSelectorAsync(".settings-page", new() { Timeout = 10000 });

        const string newBio = "This is my updated bio from E2E tests";
        const string newImage = "https://example.com/avatar.png";

        await _page.GetByPlaceholder("Short bio about you").FillAsync(newBio);
        await _page.GetByPlaceholder("URL of profile picture").FillAsync(newImage);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Update Settings" }).ClickAsync();

        await _page.WaitForSelectorAsync("button:has-text('Update Settings'):not([disabled])", new() { Timeout = 15000 });

        await Expect(_page.GetByPlaceholder("Short bio about you")).ToHaveValueAsync(newBio, new() { Timeout = 10000 });
        await Expect(_page.GetByPlaceholder("URL of profile picture")).ToHaveValueAsync(newImage, new() { Timeout = 10000 });
    }

    private async Task LoginViaUi(string email, string password)
    {
        await _page.GotoAsync("/login");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync("h1:has-text('Sign in')");
        await _page.GetByPlaceholder("Email").FillAsync(email);
        await _page.GetByPlaceholder("Password").FillAsync(password);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
