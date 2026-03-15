// =============================================================================
// CounterAutoFixture — E2E Test Infrastructure for InteractiveAuto Counter
// =============================================================================
// Starts a real Kestrel server hosting the Counter in InteractiveAuto mode,
// then provides Playwright browser instances for E2E testing.
//
// InteractiveAuto mode: starts with server-side interactivity (WebSocket),
// then transitions to WASM once the client runtime is ready. This test
// fixture sets up both WebSocket and WASM file serving to support the
// full lifecycle.
//
// For E2E testing purposes, we verify that the app becomes interactive
// (initially via WebSocket) and processes user events correctly.
// =============================================================================

using System.Net.Sockets;
using Picea.Abies.Counter;
using Picea.Abies.Server;
using Picea.Abies.Server.Kestrel;
using Picea;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Playwright;

using TUnit.Core.Interfaces;
namespace Picea.Abies.Counter.Testing.E2E.Fixtures;

/// <summary>
/// Shared fixture that starts the Counter in InteractiveAuto mode for E2E testing.
/// </summary>
public sealed class CounterAutoFixture : IAsyncInitializer, IAsyncDisposable
{
    private WebApplication? _app;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    /// <summary>The base URL of the Counter app (Kestrel server).</summary>
    public string BaseUrl { get; private set; } = "";

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
            Console.WriteLine($"[Browser {msg.Type}] {msg.Text}");

        page.PageError += (_, error) =>
            Console.WriteLine($"[Browser ERROR] {error}");

        return page;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var port = GetAvailablePort();
        BaseUrl = $"http://localhost:{port}";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(BaseUrl);

        _app = builder.Build();

        // InteractiveAuto needs both WebSocket (for initial interactivity)
        // and WASM files (for eventual client-side takeover).
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
            "Picea.Abies.Counter.Wasm", "bin", configuration,
            "net10.0", "browser-wasm", "AppBundle"));

        _app.UseAbiesWasmFiles(wasmAppBundlePath);
        _app.UseAbiesStaticFiles();

        _app.MapAbies<CounterProgram, CounterModel, Unit>(
            "/{**catch-all}",
            new RenderMode.InteractiveAuto());

        _ = Task.Run(async () => await _app.RunAsync());
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

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
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
