using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Playwright;

namespace Picea.Abies.UI.Demo.Testing.E2E.Fixtures;

/// <summary>
/// Shared fixture that hosts the published UI demo AppBundle and provides
/// Playwright browser pages for end-to-end smoke tests.
/// </summary>
public sealed class UiDemoFixture : IAsyncInitializer, IAsyncDisposable
{
    private WebApplication? _app;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    /// <summary>The base URL of the hosted UI demo app.</summary>
    public string BaseUrl { get; private set; } = "";

    /// <summary>
    /// Creates a page in an isolated browser context for a single test.
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

        var appBundlePath = GetWasmAppBundlePath();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(BaseUrl);

        _app = builder.Build();

        var fileProvider = new PhysicalFileProvider(appBundlePath);

        _app.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = fileProvider
        });

        var contentTypeProvider = new FileExtensionContentTypeProvider();
        contentTypeProvider.Mappings[".dat"] = "application/octet-stream";

        _app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            ContentTypeProvider = contentTypeProvider,
            ServeUnknownFileTypes = true
        });

        await _app.StartAsync();
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
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    /// <summary>
    /// Resolves the UI demo browser-wasm AppBundle path from the solution root.
    /// </summary>
    private static string GetWasmAppBundlePath()
    {
        var solutionDir = FindSolutionDirectory();

        var configuration =
#if DEBUG
            "Debug";
#else
            "Release";
#endif

        return Path.GetFullPath(Path.Combine(
            solutionDir,
            "Picea.Abies.UI.Demo", "bin", configuration,
            "net10.0", "publish", "wwwroot"));
    }

    /// <summary>
    /// Waits until the local host responds with an HTTP success status.
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
                // Host not ready yet.
            }

            await Task.Delay(200);
        }

        throw new TimeoutException(
            $"UI demo host at {url} did not start within {timeoutSeconds} seconds.");
    }

    /// <summary>
    /// Finds an available local TCP port.
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
    /// Walks up directories to find the solution root.
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
