using Microsoft.Playwright;

namespace Picea.Abies.Templates.Testing.Fixtures;

/// <summary>
/// xUnit collection fixture that scaffolds the <c>abies-browser</c> template,
/// builds it, runs it via the WebAssembly SDK dev server, and provides a
/// Playwright browser for E2E testing.
/// </summary>
public sealed class BrowserTemplateFixture : IAsyncLifetime
{
    private TemplateProject? _project;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    /// <summary>Base URL of the running template dev server.</summary>
    public string BaseUrl => _project?.BaseUrl
        ?? throw new InvalidOperationException("Browser template not started.");

    /// <summary>Playwright browser instance for creating pages.</summary>
    public IBrowser Browser => _browser
        ?? throw new InvalidOperationException("Browser not launched.");

    /// <summary>
    /// Creates a new Playwright page with console and error logging.
    /// </summary>
    public async Task<IPage> CreatePageAsync()
    {
        var page = await Browser.NewPageAsync();
        page.Console += (_, msg) =>
            Console.WriteLine($"[browser:console] {msg.Type}: {msg.Text}");
        page.PageError += (_, err) =>
            Console.Error.WriteLine($"[browser:error] {err}");
        return page;
    }

    public async Task InitializeAsync()
    {
        _project = await TemplateProject.CreateAsync(
            "abies-browser", "TestBrowserApp");
        await _project.RunAsync();

        _playwright = await Playwright.CreateAsync();

        var headless = string.IsNullOrEmpty(
            Environment.GetEnvironmentVariable("HEADED"));
        _browser = await _playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = headless });
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null)
            await _browser.DisposeAsync();
        _playwright?.Dispose();

        if (_project is not null)
            await _project.DisposeAsync();
    }
}
