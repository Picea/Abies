using Microsoft.Playwright;

namespace Abies.Conduit.E2E;

/// <summary>
/// E2E tests for authentication user journeys.
/// Covers: Register, Login, Logout (via Settings page)
/// </summary>
public class AuthenticationTests : PlaywrightFixture
{
    [Fact]
    public async Task Register_NewUser_RedirectsToHome()
    {
        var username = GenerateTestUsername();
        var email = GenerateTestEmail();
        var password = "TestPassword123!";

        await Page.GotoAsync("/register");
        await WaitForAppReadyAsync();

        await Expect(Page.GetByPlaceholder("Username")).ToBeVisibleAsync(new() { Timeout = 10000 });

        await Page.GetByPlaceholder("Username").FillAsync(username);
        await Page.GetByPlaceholder("Email").FillAsync(email);
        await Page.GetByPlaceholder("Password").FillAsync(password);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign up" }).ClickAsync();

        // Should redirect to home page
        await Page.WaitForURLAsync("**/", new() { Timeout = 30000 });

        // Should see authenticated nav items
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "New Article" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Settings" })).ToBeVisibleAsync();
        
        // Should see username in nav
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = username })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Register_ShowsErrorForDuplicateEmail()
    {
        // First, register a user
        var (_, email, password) = await RegisterTestUserAsync();

        // Logout by going to settings and clicking logout
        await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await Page.WaitForURLAsync("**/settings", new() { Timeout = 10000 });
        await Page.GetByRole(AriaRole.Button, new() { Name = "logout", Exact = false }).ClickAsync();
        await Page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        // Navigate to register page
        await Page.GotoAsync("/register");
        await WaitForAppReadyAsync();

        // Try to register with the same email
        await Page.GetByPlaceholder("Username").FillAsync(GenerateTestUsername());
        await Page.GetByPlaceholder("Email").FillAsync(email);
        await Page.GetByPlaceholder("Password").FillAsync(password);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign up" }).ClickAsync();

        // Should show error message or stay on register page (not redirect to home)
        // Give it time to process and show error
        await Page.WaitForTimeoutAsync(2000);
        
        // Either error messages are shown, or we're still on register page (not redirected)
        var hasError = await Page.Locator(".error-messages").CountAsync() > 0;
        var stillOnRegister = Page.Url.Contains("/register");
        
        Assert.True(hasError || stillOnRegister, "Should either show error or stay on register page");
    }

    [Fact]
    public async Task Login_ExistingUser_RedirectsToHome()
    {
        // First register a user
        var (username, email, password) = await RegisterTestUserAsync();

        // Navigate to settings and logout
        await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await Page.WaitForURLAsync("**/settings", new() { Timeout = 10000 });
        
        await Page.GetByRole(AriaRole.Button, new() { Name = "logout", Exact = false }).ClickAsync();
        await Page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        // Now login
        await Page.GotoAsync("/login");
        await WaitForAppReadyAsync();

        await Expect(Page.GetByPlaceholder("Email")).ToBeVisibleAsync(new() { Timeout = 10000 });

        await Page.GetByPlaceholder("Email").FillAsync(email);
        await Page.GetByPlaceholder("Password").FillAsync(password);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        // Should redirect to home
        await Page.WaitForURLAsync("**/", new() { Timeout = 30000 });

        // Should see authenticated nav items
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "New Article" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = username })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShowsError()
    {
        await Page.GotoAsync("/login");
        await WaitForAppReadyAsync();

        await Page.GetByPlaceholder("Email").FillAsync("nonexistent@test.com");
        await Page.GetByPlaceholder("Password").FillAsync("wrongpassword");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        // Should show error message
        await Expect(Page.Locator(".error-messages")).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Fact]
    public async Task Logout_FromSettingsPage_RedirectsToHome()
    {
        // Register and login
        var (username, _, _) = await RegisterTestUserAsync();

        // Should be logged in
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = username })).ToBeVisibleAsync();

        // Navigate to settings
        await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await Page.WaitForURLAsync("**/settings", new() { Timeout = 10000 });

        // Click logout button
        await Page.GetByRole(AriaRole.Button, new() { Name = "logout", Exact = false }).ClickAsync();

        // Should redirect to home
        await Page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        // Should see unauthenticated nav items
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Sign in" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Sign up" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task UnauthenticatedUser_SeesSignInSignUpLinks()
    {
        await Page.GotoAsync("/");
        await WaitForAppReadyAsync();

        // Should see sign in and sign up links
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Sign in" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Sign up" })).ToBeVisibleAsync();

        // Should NOT see authenticated nav items
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "New Article" })).Not.ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Settings" })).Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task AuthenticatedUser_SeesCorrectNavItems()
    {
        var (username, _, _) = await RegisterTestUserAsync();

        // Should see authenticated nav items
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Home" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "New Article" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Settings" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = username })).ToBeVisibleAsync();

        // Should NOT see unauthenticated nav items
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Sign in" })).Not.ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Sign up" })).Not.ToBeVisibleAsync();
    }
}
