// =============================================================================
// TemplateFixture — Abstract Base for Template E2E Test Fixtures
// =============================================================================
// Implements the full template E2E pipeline:
//
//   1. dotnet new {template}    → scaffold a project from the installed template
//   2. Patch csproj             → replace Version="1.0.*-*" with actual NBGV version
//   3. Patch launchSettings     → overwrite applicationUrl with our test port
//   4. Write nuget.config       → point at the local feed + nuget.org
//   5. dotnet build             → compile the generated project
//   6. dotnet run               → start the app on a random port
//   7. Playwright               → launch Chromium for browser-based verification
//
// Each concrete fixture (ServerTemplateFixture, BrowserTemplateFixture, etc.)
// overrides TemplateName and ProjectName to target a specific template.
//
// Lifecycle (TUnit ClassDataSource):
//   InitializeAsync  → full pipeline above
//   DisposeAsync      → kill process, dispose Playwright, delete temp dir
// =============================================================================

using System.Diagnostics;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using TUnit.Core.Interfaces;

namespace Picea.Abies.Templates.Testing.E2E.Infrastructure;

/// <summary>
/// Base fixture for template E2E tests. Scaffolds a project from a dotnet template,
/// builds and runs it, then provides Playwright pages for browser-based verification.
/// </summary>
public abstract class TemplateFixture : IAsyncInitializer, IAsyncDisposable
{
    private Process? _appProcess;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string _projectDir = "";

    /// <summary>The <c>dotnet new</c> short name (e.g. "abies-server").</summary>
    protected abstract string TemplateName { get; }

    /// <summary>The project name passed to <c>-n</c> (e.g. "TestServerApp").</summary>
    protected abstract string ProjectName { get; }

    /// <summary>
    /// The local NuGet feed containing packed Abies packages and installed templates.
    /// TUnit injects this via <see cref="ClassDataSourceAttribute{T}"/> with
    /// <c>SharedType.PerTestSession</c>, so it is initialized once and shared.
    /// </summary>
    [ClassDataSource<LocalNuGetFeed>(Shared = SharedType.PerTestSession)]
    public required LocalNuGetFeed Feed { get; init; }

    /// <summary>The base URL of the running app (e.g. "http://localhost:12345").</summary>
    public string BaseUrl { get; private set; } = "";

    /// <summary>
    /// Creates a new Playwright browser context with an isolated page.
    /// Each test should call this for isolation.
    /// </summary>
    public async Task<IPage> CreatePageAsync()
    {
        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();

        page.Console += (_, msg) =>
            Console.WriteLine($"[{TemplateName} Browser {msg.Type}] {msg.Text}");

        page.PageError += (_, error) =>
            Console.WriteLine($"[{TemplateName} Browser ERROR] {error}");

        return page;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var port = GetAvailablePort();
        BaseUrl = $"http://localhost:{port}";

        // 1. Scaffold the project from the template
        _projectDir = Path.Join(
            Path.GetTempPath(), $"abies-e2e-{TemplateName}-{Guid.NewGuid():N}");

        Console.WriteLine($"[{TemplateName}] Scaffolding to {_projectDir}");
        await DotNetCli.NewAsync(TemplateName, ProjectName, _projectDir);

        // 2. Patch the csproj to use the exact local package version
        PatchCsprojVersion();

        // 3. Patch launchSettings.json to bind to our test port.
        //    WasmAppHost (used by Microsoft.NET.Sdk.WebAssembly projects) ignores
        //    ASPNETCORE_URLS and --urls — it reads applicationUrl from the launch
        //    profile. We overwrite it so the dev server binds to our chosen port.
        PatchLaunchSettings(port);

        // 4. Write a nuget.config that uses the local feed + nuget.org
        WriteNuGetConfig();

        // 5. Build the project
        Console.WriteLine($"[{TemplateName}] Building...");
        await DotNetCli.BuildAsync(_projectDir, timeoutSeconds: 600);

        // 6. Start the app
        Console.WriteLine($"[{TemplateName}] Starting on {BaseUrl}...");
        _appProcess = DotNetCli.StartRun(_projectDir, port);

        // Give the process a moment to start, then check if it crashed immediately
        await Task.Delay(2000);
        if (_appProcess.HasExited)
        {
            throw new InvalidOperationException(
                $"App process exited immediately with code {_appProcess.ExitCode}. " +
                $"Check [app stdout] and [app stderr] console output above for details.");
        }

        await WaitForServerReady(BaseUrl, _appProcess);
        Console.WriteLine($"[{TemplateName}] App is ready.");

        // 7. Launch Playwright
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("HEADED") != "1",
            SlowMo = Environment.GetEnvironmentVariable("HEADED") == "1" ? 300 : 0
        });
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Console.WriteLine($"[{TemplateName}] Disposing...");

        if (_browser is not null)
            await _browser.DisposeAsync();

        _playwright?.Dispose();

        if (_appProcess is not null)
        {
            try
            {
                if (!_appProcess.HasExited)
                {
                    _appProcess.Kill(entireProcessTree: true);
                    await _appProcess.WaitForExitAsync()
                        .WaitAsync(TimeSpan.FromSeconds(10));
                }
            }
            catch (InvalidOperationException ex)
            {
                // Process already exited between HasExited check and Kill() (TOCTOU race)
                Console.WriteLine(
                    $"[{TemplateName}] Warning: Could not kill app process: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                // WaitForExitAsync timed out — process is stuck
                Console.WriteLine(
                    $"[{TemplateName}] Warning: Could not kill app process: {ex.Message}");
            }

            _appProcess.Dispose();
        }

        try
        {
            if (Directory.Exists(_projectDir))
                Directory.Delete(_projectDir, recursive: true);
        }
        catch (IOException ex)
        {
            Console.WriteLine(
                $"[{TemplateName}] Warning: Could not delete {_projectDir}: {ex.Message}");
        }
    }

    /// <summary>
    /// Replaces floating version specifiers (e.g. <c>Version="1.0.*-*"</c>) in the
    /// generated csproj with the actual NBGV version from the local feed, so NuGet
    /// restore resolves our local packages.
    /// </summary>
    private void PatchCsprojVersion()
    {
        var csprojFiles = Directory.GetFiles(_projectDir, "*.csproj");
        foreach (var csproj in csprojFiles)
        {
            var content = File.ReadAllText(csproj);

            // Match any floating version specifier containing wildcards
            // e.g. Version="1.0.*-*", Version="1.0.0-*", Version="*-*"
            var patched = Regex.Replace(
                content,
                @"Version=""[^""]*\*[^""]*""",
                $"Version=\"{Feed.PackageVersion}\"");

            if (patched != content)
            {
                File.WriteAllText(csproj, patched);
                Console.WriteLine(
                    $"[{TemplateName}] Patched {Path.GetFileName(csproj)}: " +
                    $"floating version → Version=\"{Feed.PackageVersion}\"");
            }
        }
    }

    /// <summary>
    /// Writes a <c>nuget.config</c> that adds the local feed directory as the
    /// highest-priority package source, with nuget.org as fallback.
    /// </summary>
    private void WriteNuGetConfig()
    {
        var config = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
                            <config>
                                <add key="globalPackagesFolder" value=".nuget/packages" />
                            </config>
              <packageSources>
                <clear />
                <add key="local-e2e-feed" value="{Feed.FeedDir}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
            </configuration>
            """;

        File.WriteAllText(Path.Join(_projectDir, "nuget.config"), config);
    }

    /// <summary>
    /// Overwrites the <c>Properties/launchSettings.json</c> in the generated project
    /// so the app binds to our test port. This is critical for WasmAppHost-based
    /// projects (Microsoft.NET.Sdk.WebAssembly) which ignore <c>ASPNETCORE_URLS</c>
    /// and <c>--urls</c>, reading the port exclusively from the launch profile.
    /// </summary>
    private void PatchLaunchSettings(int port)
    {
        var launchSettingsDir = Path.Join(_projectDir, "Properties");
        var launchSettingsPath = Path.Join(launchSettingsDir, "launchSettings.json");

        if (!File.Exists(launchSettingsPath))
        {
            Console.WriteLine(
                $"[{TemplateName}] No launchSettings.json found — skipping patch.");
            return;
        }

        // Overwrite with a minimal profile that binds to our test port.
        // We use HTTP only — no HTTPS — to avoid certificate setup in CI.
        var patched = $$"""
            {
              "profiles": {
                "E2ETest": {
                  "commandName": "Project",
                  "launchBrowser": false,
                  "environmentVariables": {
                    "ASPNETCORE_ENVIRONMENT": "Development"
                  },
                  "applicationUrl": "http://localhost:{{port}}"
                }
              }
            }
            """;

        File.WriteAllText(launchSettingsPath, patched);
        Console.WriteLine(
            $"[{TemplateName}] Patched launchSettings.json → http://localhost:{port}");
    }

    private static async Task WaitForServerReady(
        string url, Process process, int timeoutSeconds = 120)
    {
        using var http = new HttpClient();
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            // Check if the process has crashed
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"App process exited with code {process.ExitCode} while waiting for {url} to become ready. " +
                    "Check [app stdout] and [app stderr] console output above for details.");
            }

            try
            {
                var response = await http.GetAsync(url);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (HttpRequestException)
            {
                // Server not ready yet — connection refused or network error
            }
            catch (TaskCanceledException)
            {
                // Request timed out — server not ready yet
            }

            await Task.Delay(500);
        }

        throw new TimeoutException(
            $"App at {url} did not become ready within {timeoutSeconds} seconds.");
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
