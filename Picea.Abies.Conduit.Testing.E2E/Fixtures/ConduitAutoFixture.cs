// =============================================================================
// ConduitAutoFixture — E2E Test Infrastructure for InteractiveAuto Conduit
// =============================================================================

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Playwright;
using Picea.Abies.Conduit.App;
using Picea.Abies.Server;
using Picea.Abies.Server.Kestrel;

namespace Picea.Abies.Conduit.Testing.E2E.Fixtures;

/// <summary>
/// Shared fixture that starts the Conduit app in InteractiveAuto mode for E2E testing.
/// </summary>
public sealed class ConduitAutoFixture : IAsyncLifetime
{
    private ConduitInfraFixture? _infra;
    private WebApplication? _app;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    /// <summary>The base URL of the Kestrel server hosting the Conduit app.</summary>
    public string BaseUrl { get; private set; } = "";

    /// <summary>The Aspire-managed API URL (for ApiSeeder).</summary>
    public string ApiUrl => _infra?.ApiUrl ?? throw new InvalidOperationException("Fixture not initialized.");

    /// <summary>
    /// Creates a new Playwright browser context with an isolated page.
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
            Console.WriteLine($"[Browser:Auto {msg.Type}] {msg.Text}");

        page.PageError += (_, error) =>
            Console.WriteLine($"[Browser:Auto ERROR] {error}");

        return page;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _infra = await SharedInfra.GetAsync();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        _app = builder.Build();

        ConduitServerFixture.AddApiReverseProxy(_app, _infra.ApiUrl);

        _app.UseWebSockets();

        var solutionDir = FindSolutionDirectory();

        var configuration =
#if DEBUG
            "Debug";
#else
            "Release";
#endif

        var wasmAppBundlePath = Path.GetFullPath(Path.Combine(
            solutionDir,
            "Picea.Abies.Conduit.Wasm", "bin", configuration,
            "net10.0", "browser-wasm", "AppBundle"));

        _app.UseAbiesWasmFiles(wasmAppBundlePath);
        _app.UseAbiesStaticFiles();

        _app.MapAbies<ConduitProgram, Model, string>(
            "/{**catch-all}",
            new RenderMode.InteractiveAuto(),
            interpreter: ConduitInterpreter.Interpret,
            argument: _infra.ApiUrl);

        await _app.StartAsync();
        BaseUrl = _app.Urls.First();
        Console.WriteLine($"[ConduitAutoFixture] Kestrel started on {BaseUrl}");
        await WaitForServerReady(BaseUrl);

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("HEADED") != "1",
            SlowMo = Environment.GetEnvironmentVariable("HEADED") == "1" ? 300 : 0
        });
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_browser is not null)
            await _browser.DisposeAsync();

        _playwright?.Dispose();

        if (_app is not null)
            await _app.DisposeAsync();
    }

    private static async Task WaitForServerReady(string url, int timeoutSeconds = 60)
    {
        using var http = new HttpClient();
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await http.GetAsync(url);
                var status = (int)response.StatusCode;
                Console.WriteLine($"[ConduitAutoFixture] Health check {url} → {status}");
                if (status < 500)
                    return;
            }
            catch
            {
                // Server not ready yet
            }

            await Task.Delay(200);
        }

        throw new TimeoutException(
            $"Conduit server at {url} did not start within {timeoutSeconds} seconds.");
    }

    private static string FindSolutionDirectory()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "Picea.Abies.sln")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new DirectoryNotFoundException(
            "Could not find solution directory (containing Picea.Abies.sln).");
    }
}
