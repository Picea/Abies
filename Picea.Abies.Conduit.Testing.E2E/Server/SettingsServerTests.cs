// =============================================================================
// Settings E2E Tests — InteractiveServer Mode
// =============================================================================

using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Category("E2E")]
[ClassDataSource<ConduitServerFixture>(Shared = SharedType.Keyed, Key = "ConduitServer")]
[NotInParallel("ConduitServer")]
public sealed class SettingsServerTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitServerFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public SettingsServerTests(ConduitServerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _seeder = new ApiSeeder(_fixture.ApiUrl);
    }

    public async ValueTask DisposeAsync() => await _page.Context.DisposeAsync();

    [Test]
    public async Task Settings_WhenLoggedIn_ShouldShowCurrentUserInfo()
    {
        var username = $"srvsett{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");
        await LoginViaUi(email, "password123");

        await _page.Locator(".navbar").GetByText("Settings").ClickAsync();
        await _page.WaitForSelectorAsync(".settings-page", new() { Timeout = 10000 });

        await Expect(_page.GetByPlaceholder("Email")).ToHaveValueAsync(email);
        await Expect(_page.GetByPlaceholder("Your Name")).ToHaveValueAsync(username);
    }

    [Test]
    public async Task UpdateSettings_WithNewBio_ShouldPersistChanges()
    {
        var username = $"srvupd{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");
        await LoginViaUi(email, "password123");

        await _page.Locator(".navbar").GetByText("Settings").ClickAsync();
        await _page.WaitForSelectorAsync(".settings-page", new() { Timeout = 10000 });

        const string newBio = "Updated bio from server mode E2E test";
        await _page.GetByPlaceholder("Short bio about you").FillAndWaitForPatch(newBio);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Update Settings" }).ClickAsync();

        await _page.WaitForSelectorAsync("button:has-text('Update Settings'):not([disabled])",
            new() { Timeout = 15000 });

        await Expect(_page.GetByPlaceholder("Short bio about you")).ToHaveValueAsync(newBio,
            new() { Timeout = 10000 });
    }

    private async Task LoginViaUi(string email, string password)
    {
        await _page.GotoAsync("/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await _page.WaitForSelectorAsync("h1:has-text('Sign in')");
        await _page.GetByPlaceholder("Email").FillAndWaitForPatch(email);
        await _page.GetByPlaceholder("Password").FillAndWaitForPatch(password);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await _page.WaitForAuthenticatedShell();
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
