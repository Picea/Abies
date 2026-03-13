using Microsoft.Playwright;

namespace Picea.Abies.Templates.Testing.Fixtures;

/// <summary>
/// xUnit collection fixture that scaffolds the <c>abies-server</c> template,
/// builds it, runs it as an external process, and provides a Playwright browser
/// for E2E testing.
/// </summary>
/// <remarks>
/// <para>
/// The fixture lifecycle:
/// <list type="number">
///   <item><c>dotnet new install</c> — installs the template from the local directory</item>
///   <item><c>dotnet new abies-server</c> — scaffolds a new project in a temp directory</item>
///   <item>Patches csproj — replaces NuGet PackageReferences with local ProjectReferences</item>
///   <item><c>dotnet build</c> — builds the scaffolded project</item>
///   <item><c>dotnet run</c> — starts the server on a random port</item>
///   <item>Playwright connects and runs tests</item>
///   <item>Cleanup — kills process, deletes temp dir, uninstalls template</item>
/// </list>
/// </para>
/// </remarks>
public sealed class ServerTemplateFixture : IAsyncLifetime
{
    private TemplateProject? _project;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    /// <summary>Base URL of the running template server.</summary>
    public string BaseUrl => _project?.BaseUrl
        ?? throw new InvalidOperationException("Server template not started.");

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
            "abies-server", "TestServerApp");
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
