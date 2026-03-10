// =============================================================================
// CounterWasmFixture — E2E Test Infrastructure for InteractiveWasm Counter
// =============================================================================
// Starts a real Kestrel server hosting the Counter in InteractiveWasm mode,
// then provides Playwright browser instances for E2E testing.
//
// Architecture:
//   1. Publishes Counter.Wasm (via MSBuild target in csproj)
//   2. Starts Kestrel on a random port serving:
//      - GET / → server-prerendered Counter HTML
//      - /_framework/* → WASM bundle files (dotnet.js, DLLs, etc.)
//      - /abies.js → Abies runtime JavaScript
//   3. Creates a Playwright Chromium browser instance
//   4. Each test gets an isolated browser context via CreatePageAsync()
//
// No Aspire needed — the Counter has no infrastructure dependencies.
// =============================================================================

using System.Net.Sockets;
using Picea.Abies.Counter;
using Picea.Abies.Server;
using Picea.Abies.Server.Kestrel;
using Picea;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Playwright;

namespace Picea.Abies.Counter.Testing.E2E.Fixtures;

/// <summary>
/// Shared fixture that starts the Counter in InteractiveWasm mode for E2E testing.
/// Implements <see cref="IAsyncLifetime"/> for xUnit lifecycle management.
/// </summary>
public sealed class CounterWasmFixture : IAsyncLifetime
{
    private WebApplication? _app;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    /// <summary>The base URL of the Counter app (Kestrel server).</summary>
    public string BaseUrl { get; private set; } = "";

    /// <summary>
    /// Creates a new Playwright browser context with an isolated page.
    /// Each test should call this to get an isolated browser context.
    /// </summary>
    public async Task<IPage> CreatePageAsync()
    {
        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();

        // Log browser console messages for diagnostics
        page.Console += (_, msg) =>
            Console.WriteLine($"[Browser {msg.Type}] {msg.Text}");

        page.PageError += (_, error) =>
            Console.WriteLine($"[Browser ERROR] {error}");

        return page;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        // ─── Start the Kestrel server ─────────────────────────────────
        var port = GetAvailablePort();
        BaseUrl = $"http://localhost:{port}";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(BaseUrl);

        _app = builder.Build();

        // Resolve the WASM AppBundle path relative to the solution root.
        // During test execution, the working directory varies, so we walk
        // up to find the solution root and compute the path from there.
        var solutionDir = FindSolutionDirectory();

        var configuration =
#if DEBUG
            "Debug";
#else
            "Release";
#endif

        var wasmAppBundlePath = Path.GetFullPath(Path.Combine(
            solutionDir,
            "Picea.Abies.Counter.Wasm", "bin", configuration,
            "net10.0", "browser-wasm", "AppBundle"));

        _app.UseAbiesWasmFiles(wasmAppBundlePath);

        _app.MapAbies<CounterProgram, CounterModel, Unit>(
            "/{**catch-all}",
            new RenderMode.InteractiveWasm());

        // Start the server in the background
        _ = Task.Run(async () => await _app.RunAsync());

        // Wait for the server to be ready
        await WaitForServerReady(BaseUrl);

        // ─── Set up Playwright ───────────────────────────────────
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

    /// <summary>
    /// Waits for the Kestrel server to respond to HTTP requests.
    /// </summary>
    private static async Task WaitForServerReady(string url, int timeoutSeconds = 30)
    {
        using var http = new HttpClient();
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await http.GetAsync(url);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch
            {
                // Server not ready yet
            }

            await Task.Delay(200);
        }

        throw new TimeoutException(
            $"Counter server at {url} did not start within {timeoutSeconds} seconds.");
    }

    /// <summary>
    /// Finds an available TCP port by binding to port 0.
    /// </summary>
    private static int GetAvailablePort()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>
    /// Walks up directories to find the solution root (containing Picea.Abies.sln).
    /// </summary>
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
