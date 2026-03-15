// =============================================================================
// ConduitAppFixture — E2E Test Infrastructure for InteractiveWasm Conduit
// =============================================================================
// Hosts the Conduit app in InteractiveWasm mode via Kestrel, with a reverse
// proxy forwarding /api/** to the shared Aspire backend.
//
// Uses SharedInfra to share the Aspire backend across all render-mode fixtures.
// The server renders initial HTML, then the WASM runtime takes over client-side.
// =============================================================================

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Playwright;
using Picea.Abies.Conduit.App;
using Picea.Abies.Server;
using Picea.Abies.Server.Kestrel;
namespace Picea.Abies.Conduit.Testing.E2E.Fixtures;

/// <summary>
/// Shared fixture that starts the Conduit app in InteractiveWasm mode for E2E testing.
/// The server renders initial HTML; the client-side WASM runtime takes over after load.
/// </summary>
public sealed class ConduitAppFixture : IAsyncInitializer, IAsyncDisposable
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
    /// No route interception needed — API calls go through the Kestrel reverse proxy.
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
            Console.WriteLine($"[Browser:WASM {msg.Type}] {msg.Text}");

        page.PageError += (_, error) =>
            Console.WriteLine($"[Browser:WASM ERROR] {error}");

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

        var solutionDir = FindSolutionDirectory();

        var configuration =
#if DEBUG
            "Debug";
#else
            "Release";
#endif

        // Path.Join never drops earlier segments (unlike Path.Combine which
        // silently discards everything before a rooted argument).
        var wasmAppBundlePath = Path.GetFullPath(Path.Join(
            solutionDir,
            "Picea.Abies.Conduit.Wasm", "bin", configuration,
            "net10.0", "browser-wasm", "AppBundle"));

        _app.UseAbiesWasmFiles(wasmAppBundlePath);
        _app.UseAbiesStaticFiles();

        _app.MapAbies<ConduitProgram, Model, string>(
            "/{**catch-all}",
            new RenderMode.InteractiveWasm(),
            interpreter: ConduitInterpreter.Interpret,
            argument: _infra.ApiUrl);

        await _app.StartAsync();
        BaseUrl = _app.Urls.First();
        Console.WriteLine($"[ConduitAppFixture] Kestrel started on {BaseUrl}");
        await WaitForServerReady(BaseUrl);

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
        string lastError = "no attempts made";

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await http.GetAsync(url);
                Console.WriteLine(
                    $"[ConduitAppFixture] Health check {url} → {(int)response.StatusCode} {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (HttpRequestException ex)
            {
                lastError = ex.Message;
            }

            await Task.Delay(200);
        }

        throw new TimeoutException(
            $"Conduit server at {url} did not become healthy within {timeoutSeconds}s. Last error: {lastError}");
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
