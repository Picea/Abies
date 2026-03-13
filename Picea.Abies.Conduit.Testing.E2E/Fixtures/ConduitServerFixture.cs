// =============================================================================
// ConduitServerFixture — E2E Test Infrastructure for InteractiveServer Conduit
// =============================================================================

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright;
using Picea.Abies.Conduit.App;
using Picea.Abies.Server;
using Picea.Abies.Server.Kestrel;

namespace Picea.Abies.Conduit.Testing.E2E.Fixtures;

/// <summary>
/// Shared fixture that starts the Conduit app in InteractiveServer mode
/// with API reverse proxying to the shared Aspire backend.
/// </summary>
public sealed class ConduitServerFixture : IAsyncLifetime
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
    /// No route interception needed — API calls go through the Kestrel proxy.
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
            Console.WriteLine($"[Browser:Server {msg.Type}] {msg.Text}");

        page.PageError += (_, error) =>
            Console.WriteLine($"[Browser:Server ERROR] {error}");

        return page;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _infra = await SharedInfra.GetAsync();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        _app = builder.Build();

        AddApiReverseProxy(_app, _infra.ApiUrl);

        _app.UseWebSockets();
        _app.UseAbiesStaticFiles();
        _app.MapAbies<ConduitProgram, Model, Unit>(
            "/{**catch-all}",
            new RenderMode.InteractiveServer(),
            interpreter: ConduitInterpreter.Interpret);

        await _app.StartAsync();
        BaseUrl = _app.Urls.First();
        Console.WriteLine($"[ServerFixture] Kestrel started on {BaseUrl}");
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

    /// <summary>
    /// Adds reverse proxy middleware that forwards /api/** to the Aspire backend.
    /// Used by server-hosted modes where the interpreter runs server-side.
    /// </summary>
    internal static void AddApiReverseProxy(WebApplication app, string targetApiUrl)
    {
        app.Map("/api/{**remainder}", async (HttpContext context) =>
        {
            var remainder = context.Request.RouteValues["remainder"]?.ToString() ?? "";
            using var proxyClient = new HttpClient();
            var targetUrl = $"{targetApiUrl}/api/{remainder}{context.Request.QueryString}";

            var proxyRequest = new HttpRequestMessage(
                new HttpMethod(context.Request.Method), targetUrl);

            byte[]? bodyBytes = null;
            if (context.Request.ContentLength > 0 || context.Request.ContentType is not null)
            {
                using var ms = new MemoryStream();
                await context.Request.Body.CopyToAsync(ms);
                bodyBytes = ms.ToArray();
            }

            foreach (var header in context.Request.Headers)
            {
                if (header.Key.StartsWith("Host", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                    continue;
                proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }

            if (bodyBytes is { Length: > 0 })
            {
                proxyRequest.Content = new ByteArrayContent(bodyBytes);
                if (context.Request.ContentType is not null)
                    proxyRequest.Content.Headers.ContentType =
                        System.Net.Http.Headers.MediaTypeHeaderValue.Parse(context.Request.ContentType);
            }

            Console.WriteLine($"[Proxy] {context.Request.Method} {targetUrl} (body: {bodyBytes?.Length ?? 0} bytes, ct: {context.Request.ContentType})");
            if (bodyBytes is { Length: > 0 })
            {
                // Redact body for auth-related endpoints to avoid logging credentials.
                var isAuthPath = remainder.StartsWith("users", StringComparison.OrdinalIgnoreCase);
                Console.WriteLine(isAuthPath
                    ? "[Proxy] Body: [REDACTED — auth endpoint]"
                    : $"[Proxy] Body: {System.Text.Encoding.UTF8.GetString(bodyBytes)}");
            }

            var proxyResponse = await proxyClient.SendAsync(proxyRequest);

            var statusCode = (int)proxyResponse.StatusCode;
            Console.WriteLine($"[Proxy] → {statusCode}");
            if (statusCode >= 400)
            {
                var body = await proxyResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"[Proxy] Error body: {body}");
            }

            context.Response.StatusCode = statusCode;

            foreach (var header in proxyResponse.Headers)
                context.Response.Headers[header.Key] = header.Value.ToArray();

            foreach (var header in proxyResponse.Content.Headers)
                context.Response.Headers[header.Key] = header.Value.ToArray();

            context.Response.Headers.Remove("transfer-encoding");

            await proxyResponse.Content.CopyToAsync(context.Response.Body);
        });
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
                Console.WriteLine($"[ServerFixture] Health check {url} → {(int)response.StatusCode} {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (HttpRequestException ex)
            {
                lastError = ex.Message;
                Console.WriteLine($"[ServerFixture] Health check {url} → {ex.GetType().Name}: {ex.Message}");
            }

            await Task.Delay(500);
        }

        throw new TimeoutException(
            $"Conduit server at {url} did not become healthy within {timeoutSeconds}s. Last error: {lastError}");
    }
}
