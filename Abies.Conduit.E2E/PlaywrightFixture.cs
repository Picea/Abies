using System.Diagnostics;

namespace Abies.Conduit.E2E;

/// <summary>
/// Base class for E2E tests that provides browser setup and teardown.
/// Starts the API and Frontend servers automatically if they're not already running.
/// </summary>
public class PlaywrightFixture : IAsyncLifetime
{
    private static readonly object _lock = new();
    private static Process? _apiProcess;
    private static Process? _frontendProcess;
    private static int _instanceCount;

    protected IPlaywright Playwright { get; private set; } = null!;
    protected IBrowser Browser { get; private set; } = null!;
    protected IBrowserContext Context { get; private set; } = null!;
    protected IPage Page { get; private set; } = null!;

    /// <summary>
    /// Base URL for the Conduit frontend (HTTPS)
    /// </summary>
    protected string BaseUrl => Environment.GetEnvironmentVariable("CONDUIT_URL") ?? "https://localhost:5209";

    /// <summary>
    /// Base URL for the Conduit API
    /// </summary>
    protected string ApiBaseUrl => Environment.GetEnvironmentVariable("CONDUIT_API_URL") ?? "http://localhost:5179/api";

    /// <summary>
    /// Path to the workspace root
    /// </summary>
    private static string WorkspaceRoot => Environment.GetEnvironmentVariable("WORKSPACE_ROOT")
        ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    public async Task InitializeAsync()
    {
        // Start servers if needed (thread-safe for parallel tests)
        await EnsureServersRunningAsync();

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        var headless = Environment.GetEnvironmentVariable("HEADED") != "1";
        var slowMo = int.TryParse(Environment.GetEnvironmentVariable("PW_SLOWMO_MS"), out var ms) ? ms : 0;

        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless,
            SlowMo = slowMo
        });

        Context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            IgnoreHTTPSErrors = true  // Accept self-signed certificates for local development
        });

        Page = await Context.NewPageAsync();

        lock (_lock)
        {
            _instanceCount++;
        }
    }

    public async Task DisposeAsync()
    {
        await Page.CloseAsync();
        await Context.CloseAsync();
        await Browser.CloseAsync();
        Playwright.Dispose();

        // Note: We don't stop the servers here because other tests may still be running.
        // Servers are reused across tests and stopped when all tests complete (or manually).
        lock (_lock)
        {
            _instanceCount--;
        }
    }

    private async Task EnsureServersRunningAsync()
    {
        // Create HttpClient that ignores SSL certificate errors for local dev servers
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(2) };

        var apiRunning = await IsServerRunningAsync(httpClient, "http://localhost:5179/api/tags");
        var frontendRunning = await IsServerRunningAsync(httpClient, "https://localhost:5209/");

        if (apiRunning && frontendRunning)
        {
            // Servers already running (e.g., started manually or by VS Code task)
            return;
        }

        lock (_lock)
        {
            // Start API server if not running
            if (!apiRunning && _apiProcess is null)
            {
                _apiProcess = StartServer(
                    projectPath: Path.Combine(WorkspaceRoot, "Abies.Conduit.Api"),
                    urls: "http://localhost:5179"
                );
            }

            // Start Frontend server if not running
            if (!frontendRunning && _frontendProcess is null)
            {
                _frontendProcess = StartServer(
                    projectPath: Path.Combine(WorkspaceRoot, "Abies.Conduit"),
                    urls: "https://localhost:5209"
                );
            }
        }

        // Wait for servers to be ready
        await WaitForServerAsync(httpClient, "http://localhost:5179/api/tags", TimeSpan.FromSeconds(30));
        await WaitForServerAsync(httpClient, "https://localhost:5209/", TimeSpan.FromSeconds(30));
    }

    private static async Task<bool> IsServerRunningAsync(HttpClient client, string url)
    {
        try
        {
            var response = await client.GetAsync(url);
            return response.IsSuccessStatusCode || (int)response.StatusCode < 500;
        }
        catch
        {
            return false;
        }
    }

    private static async Task WaitForServerAsync(HttpClient client, string url, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (await IsServerRunningAsync(client, url))
            {
                return;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Server at {url} did not become ready within {timeout.TotalSeconds}s");
    }

    private static Process StartServer(string projectPath, string urls)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" --urls {urls}",
            WorkingDirectory = WorkspaceRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var process = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start server for {projectPath}");

        // Don't wait for exit - it's a background server
        return process;
    }

    /// <summary>
    /// Generates a unique username for testing
    /// </summary>
    protected static string GenerateTestUsername() => $"testuser_{Guid.NewGuid():N}"[..20];

    /// <summary>
    /// Generates a unique email for testing
    /// </summary>
    protected static string GenerateTestEmail() => $"test_{Guid.NewGuid():N}@test.com";

    /// <summary>
    /// Waits for the Blazor WebAssembly app to be fully loaded and ready.
    /// Blazor WASM apps take time to download and initialize.
    /// </summary>
    protected async Task WaitForAppReadyAsync()
    {
        // Wait for Blazor to be loaded - look for the navbar specifically
        // The app shows the navbar when fully loaded (use .navbar to avoid matching pagination nav elements)
        await Expect(Page.Locator("nav.navbar")).ToBeVisibleAsync(new() { Timeout = 60000 });
    }

    /// <summary>
    /// Waits for the app initialization to complete (user check from localStorage finished).
    /// After initialization, either "Sign in" (unauthenticated) or "Settings" (authenticated) link appears.
    /// </summary>
    protected async Task WaitForInitializationCompleteAsync()
    {
        await WaitForAppReadyAsync();

        // Wait for either "Sign in" or "Settings" to appear - this indicates IsInitializing=false
        var signInLink = Page.GetByRole(AriaRole.Link, new() { Name = "Sign in" });
        var settingsLink = Page.GetByRole(AriaRole.Link, new() { Name = "Settings" });

        // Use a custom wait that checks for either condition
        await Expect(signInLink.Or(settingsLink)).ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    /// <summary>
    /// Waits for the authenticated user state to be loaded after a page navigation.
    /// This is needed because authentication state is loaded asynchronously from localStorage.
    /// </summary>
    protected async Task WaitForAuthenticatedStateAsync()
    {
        // Wait for the app to be ready and initialization complete
        await WaitForAppReadyAsync();

        // When authenticated, "Settings" link appears instead of "Sign in"
        // Wait for this to confirm auth state is loaded
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Settings" })).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    /// <summary>
    /// Registers a new user via the UI and returns their credentials
    /// </summary>
    protected async Task<(string Username, string Email, string Password)> RegisterTestUserAsync()
    {
        var username = GenerateTestUsername();
        var email = GenerateTestEmail();
        var password = "TestPassword123!";

        await Page.GotoAsync("/register");

        // Wait for initialization to complete before interacting with the page
        await WaitForInitializationCompleteAsync();

        // Wait for the registration form to be ready
        await Expect(Page.GetByPlaceholder("Username")).ToBeVisibleAsync(new() { Timeout = 10000 });

        await Page.GetByPlaceholder("Username").FillAsync(username);
        await Page.GetByPlaceholder("Email").FillAsync(email);
        await Page.GetByPlaceholder("Password").FillAsync(password);

        // Wait for form validation to complete before clicking
        await Page.WaitForTimeoutAsync(200);
        var signUpButton = Page.GetByRole(AriaRole.Button, new() { Name = "Sign up" });
        await Expect(signUpButton).ToBeEnabledAsync(new() { Timeout = 5000 });
        await signUpButton.ClickAsync();

        // Wait for navigation to home page
        await Page.WaitForURLAsync("**/", new() { Timeout = 30000 });

        return (username, email, password);
    }

    /// <summary>
    /// Creates an article via the UI and returns its slug.
    /// Uses in-app navigation to preserve authentication state.
    /// </summary>
    protected async Task<string> CreateTestArticleAsync(string title, string description, string body, params string[] tags)
    {
        // Navigate to editor via clicking the "New Article" link to preserve session
        // After login/register, the nav bar should show "New Article" link
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "New Article" })).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Page.GetByRole(AriaRole.Link, new() { Name = "New Article" }).ClickAsync();

        // Wait for the editor form to be ready
        await Expect(Page.GetByPlaceholder("Article Title")).ToBeVisibleAsync(new() { Timeout = 30000 });

        // Fill form fields using FillAsync and dispatch input events manually
        // FillAsync sets the value but may not trigger oninput reliably in headless mode
        var titleInput = Page.GetByPlaceholder("Article Title");
        var descInput = Page.GetByPlaceholder("What's this article about?");
        var bodyInput = Page.GetByPlaceholder("Write your article (in markdown)");

        await titleInput.FillAsync(title);
        await titleInput.DispatchEventAsync("input");

        await descInput.FillAsync(description);
        await descInput.DispatchEventAsync("input");

        await bodyInput.FillAsync(body);
        await bodyInput.DispatchEventAsync("input");

        foreach (var tag in tags)
        {
            await Page.GetByPlaceholder("Enter tags").FillAsync(tag);
            await Page.GetByPlaceholder("Enter tags").PressAsync("Enter");
        }

        // Wait for button to be enabled (form validation complete) - Playwright auto-retries
        var publishButton = Page.GetByRole(AriaRole.Button, new() { Name = "Publish Article" });
        await Expect(publishButton).ToBeEnabledAsync(new() { Timeout = 5000 });

        await publishButton.ClickAsync();

        // Wait for redirect to article page and extract slug from URL
        await Page.WaitForURLAsync("**/article/**", new() { Timeout = 30000 });
        var url = Page.Url;
        var slug = url.Split("/article/").Last().Split('?').First();

        return slug;
    }
}
