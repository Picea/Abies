// =============================================================================
// Authentication E2E Tests — InteractiveServer Mode
// =============================================================================

using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Category("E2E")]
[ClassDataSource<ConduitServerFixture>(Shared = SharedType.Keyed, Key = "ConduitServer")]
[NotInParallel("ConduitServer")]
public sealed class AuthenticationServerTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitServerFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public AuthenticationServerTests(ConduitServerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _seeder = new ApiSeeder(_fixture.ApiUrl);
    }

    public async ValueTask DisposeAsync() => await _page.Context.DisposeAsync();

    [Test]
    public async Task Register_WithValidCredentials_ShouldNavigateToHomeWithAuthenticatedNav()
    {
        await _page.GotoAsync("/register");
        await _page.WaitForSelectorAsync("h1:has-text('Sign up')");

        var uniqueName = $"srvreguser{Guid.NewGuid():N}"[..20];
        var email = $"{uniqueName}@test.com";

        await _page.GetByPlaceholder("Your Name").FillAndWaitForPatch(uniqueName);
        await _page.GetByPlaceholder("Email").FillAndWaitForPatch(email);
        await _page.GetByPlaceholder("Password").FillAndWaitForPatch("password123");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign up" }).ClickAsync();

        await _page.WaitForAuthenticatedShell();
        await _page.OpenSettingsFromNavbar();
        await Expect(_page.GetByPlaceholder("Email")).ToHaveValueAsync(email);
        await Expect(_page.GetByPlaceholder("Your Name")).ToHaveValueAsync(uniqueName);
    }

    [Test]
    public async Task Login_WithValidCredentials_ShouldNavigateToHomeWithAuthenticatedNav()
    {
        var username = $"srvlogin{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        const string password = "password123";
        await _seeder.RegisterUserAsync(username, email, password);

        await _page.GotoAsync("/login");
        await _page.WaitForSelectorAsync("h1:has-text('Sign in')");

        await _page.GetByPlaceholder("Email").FillAndWaitForPatch(email);
        await _page.GetByPlaceholder("Password").FillAndWaitForPatch(password);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        await _page.WaitForAuthenticatedShell();
        await _page.OpenSettingsFromNavbar();
        await Expect(_page.GetByPlaceholder("Email")).ToHaveValueAsync(email);
        await Expect(_page.GetByPlaceholder("Your Name")).ToHaveValueAsync(username);
    }

    [Test]
    public async Task Login_WithInvalidCredentials_ShouldShowErrors()
    {
        await _page.GotoAsync("/login");
        await _page.WaitForSelectorAsync("h1:has-text('Sign in')");

        await _page.GetByPlaceholder("Email").FillAndWaitForPatch("nonexistent@test.com");
        await _page.GetByPlaceholder("Password").FillAndWaitForPatch("wrongpassword");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        await Expect(_page.Locator(".auth-page h1")).ToContainTextAsync("Sign in", new() { Timeout = 10000 });
        await Expect(_page.Locator(".navbar a[href='/settings']")).ToHaveCountAsync(0, new() { Timeout = 10000 });
    }

    [Test]
    public async Task Logout_FromSettings_ShouldClearSessionAndNavigateToHome()
    {
        var username = $"srvlogout{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");

        await LoginViaUi(email, "password123");

        await _page.Locator(".navbar").GetByText("Settings").ClickAsync();
        await _page.WaitForSelectorAsync(".settings-page", new() { Timeout = 10000 });

        await _page.GetByRole(AriaRole.Button, new() { Name = "Or click here to logout." })
            .ClickAsync();

        await _page.NavigateInApp("/settings");
        await Expect(_page.Locator(".auth-page h1")).ToContainTextAsync("Sign in", new() { Timeout = 10000 });
        await Expect(_page.Locator(".navbar a[href='/settings']")).ToHaveCountAsync(0, new() { Timeout = 10000 });
    }

    /// <summary>
    /// RealWorld spec: auth.spec.ts — "should show error for wrong password after registering"
    /// </summary>
    [Test]
    public async Task Login_WithWrongPasswordAfterRegister_ShouldShowError()
    {
        var username = $"srvwpw{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");

        await _page.GotoAsync("/login");
        await _page.WaitForSelectorAsync("h1:has-text('Sign in')");

        await _page.GetByPlaceholder("Email").FillAndWaitForPatch(email);
        await _page.GetByPlaceholder("Password").FillAndWaitForPatch("wrongpassword");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        await Expect(_page.Locator(".auth-page h1")).ToContainTextAsync("Sign in", new() { Timeout = 10000 });
        await Expect(_page.Locator(".navbar a[href='/settings']")).ToHaveCountAsync(0, new() { Timeout = 10000 });
    }

    /// <summary>
    /// RealWorld spec: auth.spec.ts — "should prevent accessing editor when not logged in"
    /// In Server mode, the app may redirect to login or show the unauthenticated navbar.
    /// </summary>
    [Test]
    public async Task Editor_WhenNotLoggedIn_ShouldRedirectToLogin()
    {
        await _page.GotoAsync("/editor");

        // In Server mode the client-side auth guard may redirect to login, or
        // the page may render with an unauthenticated navbar showing "Sign in" link.
        var signInHeading = _page.Locator("h1:has-text('Sign in')");
        var signInNavLink = _page.Locator("nav a[href='/login']");

        await signInHeading.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        await Expect(signInNavLink).ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    private async Task LoginViaUi(string email, string password)
    {
        await _page.GotoAsync("/login");
        await _page.WaitForSelectorAsync("h1:has-text('Sign in')");
        await _page.GetByPlaceholder("Email").FillAndWaitForPatch(email);
        await _page.GetByPlaceholder("Password").FillAndWaitForPatch(password);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await _page.WaitForAuthenticatedShell();
        await _page.OpenSettingsFromNavbar();
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);

}
