using System.Diagnostics;
using System.Net.Http;
using System.IO;
using Microsoft.Playwright;
using Xunit;

namespace Abies.Conduit.E2E;

public class ConduitFixture : IAsyncLifetime
{
    private const string DefaultUiUrl = "http://localhost:5209";
    private const string DefaultApiPingUrl = "http://localhost:5179/api/ping";

public IBrowser Browser { get; private set; } = null!;
    public IBrowserContext SharedContext { get; private set; } = null!;
public ApiClient Api { get; private set; } = null!;
    private IPlaywright? _playwright;
    private Process? _appHost;
    public string AppBaseUrl { get; private set; } = string.Empty;
    public string ApiBaseUrl { get; private set; } = string.Empty;

    private readonly string _logDir = System.IO.Path.Combine(AppContext.BaseDirectory, "e2e-logs");
    private string UiStdOutLogPath => System.IO.Path.Combine(_logDir, "ui-stdout.log");
    private string UiStdErrLogPath => System.IO.Path.Combine(_logDir, "ui-stderr.log");
    private string ApiLogPath => System.IO.Path.Combine(_logDir, "api.log");
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Diagnostics.Stopwatch> _reqTimers = new();

    public async Task InitializeAsync()
    {
        var skipPwInstall = string.Equals(Environment.GetEnvironmentVariable("SKIP_PW_INSTALL"), "1", StringComparison.OrdinalIgnoreCase);
        if (!skipPwInstall)
        {
            Microsoft.Playwright.Program.Main(new[] { "install" });
        }
        _playwright = await Playwright.CreateAsync();
        var headed = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HEADED"));
        var slowMoRaw = Environment.GetEnvironmentVariable("PW_SLOWMO_MS");
        _ = int.TryParse(slowMoRaw, out var slowMoMs);
        var defaultTimeoutRaw = Environment.GetEnvironmentVariable("PW_DEFAULT_TIMEOUT_MS");
        _ = int.TryParse(defaultTimeoutRaw, out var defaultTimeoutMs);

    Directory.CreateDirectory(_logDir);
    TryDelete(UiStdOutLogPath);
    TryDelete(UiStdErrLogPath);
    TryDelete(ApiLogPath);

        Browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = !headed,
            SlowMo = slowMoMs > 0 ? slowMoMs : null,
        });

        // If caller supplies base URLs, reuse them and skip Aspire startup.
        var uiBase = Environment.GetEnvironmentVariable("E2E_UI_BASE_URL");
        var apiBase = Environment.GetEnvironmentVariable("E2E_API_BASE_URL");
        if (!string.IsNullOrWhiteSpace(uiBase) && !string.IsNullOrWhiteSpace(apiBase))
        {
            AppBaseUrl = uiBase.TrimEnd('/');
            ApiBaseUrl = apiBase.TrimEnd('/');
            await WaitForServerAsync(AppBaseUrl);
            await WaitForServerAsync($"{ApiBaseUrl}/api/ping");
            Api = new ApiClient(ApiBaseUrl);
            SharedContext = await CreateContextInternalAsync(defaultTimeoutMs > 0 ? defaultTimeoutMs : 30000);
            await WarmUpAsync(SharedContext);
            return;
        }

        // Otherwise start the full distributed app via Aspire AppHost.
    var appHostDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "../../../../Abies.Conduit.Apphost"));
    var appHostProject = System.IO.Path.Combine(appHostDir, "Abies.Conduit.AppHost.csproj");

        var psi = new ProcessStartInfo("dotnet", $"run --project \"{appHostProject}\" --no-launch-profile")
        {
            WorkingDirectory = appHostDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        // We configure the dashboard port to avoid collisions across runs.
        psi.Environment["DOTNET_DASHBOARD_PORT"] = Environment.GetEnvironmentVariable("DOTNET_DASHBOARD_PORT") ?? "18888";
        // Aspire Hosting validates dashboard configuration even when you don't actively open the dashboard.
        // When launched via the Aspire CLI these are usually set automatically, but from tests we need to set them.
        psi.Environment["ASPNETCORE_URLS"] = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://127.0.0.1:0";
        psi.Environment["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] =
            Environment.GetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL") ?? "http://127.0.0.1:0";
        // AppHost projects often assume local dev launch profiles (which may default to https). For E2E we use http.
        psi.Environment["ASPIRE_ALLOW_UNSECURED_TRANSPORT"] =
            Environment.GetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT") ?? "true";

        _appHost = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start Aspire AppHost process.");
        _appHost.EnableRaisingEvents = true;
        _appHost.Exited += (_, __) => File.AppendAllText(UiStdErrLogPath, $"{DateTime.UtcNow:o} AppHost process exited code={_appHost.ExitCode}\n");
        _appHost.OutputDataReceived += (_, e) => { if (e.Data is not null) File.AppendAllText(UiStdOutLogPath, e.Data + Environment.NewLine); };
        _appHost.ErrorDataReceived += (_, e) => { if (e.Data is not null) File.AppendAllText(UiStdErrLogPath, e.Data + Environment.NewLine); };
        _appHost.BeginOutputReadLine();
        _appHost.BeginErrorReadLine();

    var (uiUrl, apiUrl) = await WaitForAspireUrlsAsync(_appHost, UiStdErrLogPath);
        AppBaseUrl = uiUrl;
        ApiBaseUrl = apiUrl;

        await WaitForServerAsync(AppBaseUrl);
        await WaitForServerAsync($"{ApiBaseUrl}/api/ping");

        Api = new ApiClient(ApiBaseUrl);
        SharedContext = await CreateContextInternalAsync(defaultTimeoutMs > 0 ? defaultTimeoutMs : 30000);
        await WarmUpAsync(SharedContext);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // ignore
        }
    }

    private static async Task<bool> IsServerUpAsync(string url)
    {
        try
        {
            using var client = new HttpClient();
            using var resp = await client.GetAsync(url);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task WaitForServerAsync(string url)
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
        for (int i = 0; i < 60; i++)
        {
            try
            {
                using var resp = await client.GetAsync(url);
                if (resp.IsSuccessStatusCode)
                    return;
            }
            catch
            {
            }
            await Task.Delay(1000);
        }
        throw new InvalidOperationException($"Server at {url} did not start.");
    }

    public async Task DisposeAsync()
    {
        if (_appHost is not null && !_appHost.HasExited)
        {
            _appHost.Kill(true);
            await _appHost.WaitForExitAsync();
        }
        if (SharedContext is not null)
            await SharedContext.CloseAsync();
        if (Browser is not null)
            await Browser.CloseAsync();
        _playwright?.Dispose();
    }

    public async Task<IBrowserContext> CreateContextAsync()
    {
        if (SharedContext is not null)
        {
            return SharedContext;
        }

        var ms = ParseDefaultTimeoutMs();
        return await CreateContextInternalAsync(ms);
    }

    private int ParseDefaultTimeoutMs()
    {
        if (!int.TryParse(Environment.GetEnvironmentVariable("PW_DEFAULT_TIMEOUT_MS"), out var ms) || ms <= 0)
            ms = 30000;
        return ms;
    }

    private async Task<IBrowserContext> CreateContextInternalAsync(int timeoutMs)
    {
        var context = await Browser.NewContextAsync();
        context.SetDefaultTimeout(timeoutMs);
        return context;
    }

    private async Task WarmUpAsync(IBrowserContext context)
    {
        var page = await context.NewPageAsync();
        try
        {
            await page.GotoAsync(AppBaseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        catch
        {
            // ignore warm-up failures; main tests will surface issues
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    public void AttachPageDiagnostics(IPage page)
    {
        page.Console += (_, msg) => System.Console.WriteLine($"[console:{msg.Type}] {msg.Text}");
        page.RequestFailed += (_, req) => System.Console.WriteLine($"[requestfailed] {req.Method} {req.Url} - {req.Failure}");
        page.Request += (_, req) =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _reqTimers[req.Url] = sw;
        };
        page.Response += async (_, resp) =>
        {
            if (_reqTimers.TryRemove(resp.Url, out var sw))
            {
                sw.Stop();
                if (!resp.Ok || sw.Elapsed > TimeSpan.FromSeconds(2))
                {
                    string body = string.Empty;
                    try { body = await resp.TextAsync(); } catch { }
                    System.Console.WriteLine($"[response] {resp.Request.Method} {resp.Url} -> {(int)resp.Status} in {sw.ElapsedMilliseconds}ms :: {body}");
                }
            }
        };
    }

    private static async Task<(string UiUrl, string ApiUrl)> WaitForAspireUrlsAsync(Process appHostProcess, string diagnosticsLogPath)
    {
        // Aspire prints lines like:
        //   Resource "conduit-app" ... bindings: http://localhost:5209
        //   Resource "conduit-api" ... bindings: http://localhost:5179
        // The exact format can vary. We do a best-effort parse for the two project names used by the AppHost.
        var uiTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var apiTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        var urlRegex = new System.Text.RegularExpressions.Regex(@"https?://[^\s]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);
        var uiNameRegex = new System.Text.RegularExpressions.Regex(@"\bconduit-app\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);
        var apiNameRegex = new System.Text.RegularExpressions.Regex(@"\bconduit-api\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        void TryParse(string? line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            var match = urlRegex.Match(line);
            if (!match.Success) return;

            var url = match.Value;
            if (url.EndsWith('/')) url = url.TrimEnd('/');

            if (uiNameRegex.IsMatch(line))
                uiTcs.TrySetResult(url);
            if (apiNameRegex.IsMatch(line))
                apiTcs.TrySetResult(url);
        }

        appHostProcess.OutputDataReceived += (_, e) => TryParse(e.Data);
        appHostProcess.ErrorDataReceived += (_, e) => TryParse(e.Data);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        await using (cts.Token.Register(() =>
        {
            uiTcs.TrySetCanceled();
            apiTcs.TrySetCanceled();
        }))
        {
            try
            {
                var ui = await uiTcs.Task.ConfigureAwait(false);
                var api = await apiTcs.Task.ConfigureAwait(false);
                return (ui, api);
            }
            catch (TaskCanceledException)
            {
                var msg = "Timed out discovering Aspire service URLs from AppHost output. " +
                          $"See logs: {diagnosticsLogPath}.";
                throw new InvalidOperationException(msg);
            }
        }
    }
}
