// =============================================================================
// Settings E2E Tests — Update profile, change password, logout
// =============================================================================

using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Category("E2E")]
[ClassDataSource<ConduitAppFixture>(Shared = SharedType.Keyed, Key = "Conduit")]
[NotInParallel("Conduit")]
public sealed class SettingsTests : IAsyncInitializer, IAsyncDisposable
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

    public async ValueTask DisposeAsync()
    {
        await _page.Context.DisposeAsync();
    }

    [Test]
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

    [Test]
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

        await _page.Locator(".navbar").GetByText(username).ClickAsync();
        await Expect(_page.Locator(".user-info p")).ToContainTextAsync(newBio, new() { Timeout = 10000 });
        await Expect(_page.Locator($".user-img[src='{newImage}']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        await _page.Locator(".user-info").GetByText("Edit Profile Settings").ClickAsync();
        await _page.WaitForSelectorAsync(".settings-page", new() { Timeout = 10000 });
        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Update Settings" }))
            .ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Test]
    public async Task UpdateSettings_WithBioOnly_ShouldPersistBio()
    {
        var username = $"bioonly{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");
        await LoginViaUi(email, "password123");

        await _page.Locator(".navbar").GetByText("Settings").ClickAsync();
        await _page.WaitForSelectorAsync(".settings-page", new() { Timeout = 10000 });
        await _page.GetByPlaceholder("Short bio about you").FillAsync("Bio only update");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Update Settings" }).ClickAsync();
        await _page.WaitForSelectorAsync("button:has-text('Update Settings'):not([disabled])", new() { Timeout = 15000 });

        await _page.Locator(".navbar").GetByText(username).ClickAsync();
        await Expect(_page.Locator(".user-info p")).ToContainTextAsync("Bio only update", new() { Timeout = 10000 });
    }

    [Test]
    public async Task UpdateSettings_WithImageOnly_ShouldPersistImage()
    {
        var username = $"imgonly{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");
        await LoginViaUi(email, "password123");

        await _page.Locator(".navbar").GetByText("Settings").ClickAsync();
        await _page.WaitForSelectorAsync(".settings-page", new() { Timeout = 10000 });
        const string newImage = "https://example.com/image-only.png";
        await _page.GetByPlaceholder("URL of profile picture").FillAsync(newImage);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Update Settings" }).ClickAsync();
        await _page.WaitForSelectorAsync("button:has-text('Update Settings'):not([disabled])", new() { Timeout = 15000 });

        await _page.Locator(".navbar").GetByText(username).ClickAsync();
        await Expect(_page.Locator($".user-img[src='{newImage}']")).ToBeVisibleAsync(new() { Timeout = 10000 });
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

    private static IPageAssertions Expect(IPage page) =>
        Assertions.Expect(page);

}
