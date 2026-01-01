using System.Diagnostics;
using System.Net.Http;
using System.IO;
using Microsoft.Playwright;
using Xunit;

namespace Abies.Conduit.E2E;

public class ConduitFixture : IAsyncLifetime
{
public IBrowser Browser { get; private set; } = null!;
    private IPlaywright? _playwright;
    private Process? _server;
    private Process? _apiServer;
    public string AppBaseUrl { get; private set; } = string.Empty;

    private readonly string _logDir = System.IO.Path.Combine(AppContext.BaseDirectory, "e2e-logs");
    private string UiStdOutLogPath => System.IO.Path.Combine(_logDir, "ui-stdout.log");
    private string UiStdErrLogPath => System.IO.Path.Combine(_logDir, "ui-stderr.log");
    private string ApiLogPath => System.IO.Path.Combine(_logDir, "api.log");

    public async Task InitializeAsync()
    {
        Microsoft.Playwright.Program.Main(new[] { "install" });
        _playwright = await Playwright.CreateAsync();
        var headed = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HEADED"));
        var slowMoRaw = Environment.GetEnvironmentVariable("PW_SLOWMO_MS");
        _ = int.TryParse(slowMoRaw, out var slowMoMs);

    Directory.CreateDirectory(_logDir);
    TryDelete(UiStdOutLogPath);
    TryDelete(UiStdErrLogPath);
    TryDelete(ApiLogPath);

        Browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = !headed,
            SlowMo = slowMoMs > 0 ? slowMoMs : null,
        });

        var apiDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "../../../../Abies.Conduit.Api"));
        // If an API server is already running (e.g. started via VS Code task), reuse it to avoid port conflicts.
        if (!await IsServerUpAsync("http://localhost:5179/api/ping"))
        {
            var apiStart = new ProcessStartInfo("dotnet", $"run --project {apiDir} --no-launch-profile --urls http://localhost:5179")
            {
                WorkingDirectory = apiDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            _apiServer = Process.Start(apiStart);
            if (_apiServer == null)
                throw new InvalidOperationException("Failed to start API process.");
            _apiServer.EnableRaisingEvents = true;
            _apiServer.Exited += (_, __) => File.AppendAllText(ApiLogPath, $"{DateTime.UtcNow:o} API process exited code={_apiServer.ExitCode}\n");
            _apiServer.OutputDataReceived += (_, e) => { if (e.Data is not null) File.AppendAllText(ApiLogPath, e.Data + Environment.NewLine); };
            _apiServer.ErrorDataReceived += (_, e) => { if (e.Data is not null) File.AppendAllText(ApiLogPath, e.Data + Environment.NewLine); };
            _apiServer.BeginOutputReadLine();
            _apiServer.BeginErrorReadLine();
        }

        await WaitForServerAsync("http://localhost:5179/api/ping");

        var projectDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "../../../../Abies.Conduit"));
        // If UI server is already running, reuse it.
        if (await IsServerUpAsync("http://localhost:5209"))
        {
            AppBaseUrl = "http://localhost:5209";
            return;
        }

        var startInfo = new ProcessStartInfo("dotnet", $"run --project {projectDir} --no-launch-profile --urls http://localhost:5209")
        {
            WorkingDirectory = projectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        _server = Process.Start(startInfo);
        if (_server == null)
            throw new InvalidOperationException("Failed to start UI process.");
        _server.EnableRaisingEvents = true;
        _server.Exited += (_, __) => File.AppendAllText(UiStdErrLogPath, $"{DateTime.UtcNow:o} UI process exited code={_server.ExitCode}\n");

        _server.OutputDataReceived += (_, e) => { if (e.Data is not null) File.AppendAllText(UiStdOutLogPath, e.Data + Environment.NewLine); };
        _server.ErrorDataReceived += (_, e) => { if (e.Data is not null) File.AppendAllText(UiStdErrLogPath, e.Data + Environment.NewLine); };

    AppBaseUrl = await WaitForAppUrlAsync(_server);
        await WaitForServerAsync(AppBaseUrl);
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
        using var client = new HttpClient();
        for (int i = 0; i < 60; i++)
        {
            try
            {
                await client.GetAsync(url);
                return;
            }
            catch
            {
                await Task.Delay(1000);
            }
        }
        throw new InvalidOperationException($"Server at {url} did not start.");
    }

    public async Task DisposeAsync()
    {
        // Only kill processes we started. If we re-used externally started servers, _server/_apiServer stays null.
        if (_server != null && !_server.HasExited)
            _server.Kill(true);
        if (_apiServer != null && !_apiServer.HasExited)
            _apiServer.Kill(true);
        if (Browser != null)
            await Browser.CloseAsync();
        _playwright?.Dispose();
    }

    private static async Task<string> WaitForAppUrlAsync(Process serverProcess)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var urlRegex = new System.Text.RegularExpressions.Regex(@"https?://[^\s]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

    void TryParse(string? line)
        {
            if (string.IsNullOrEmpty(line)) return;
            // WebAssembly dev host prints: "App url: http://127.0.0.1:55009/?arg=..."
            var appIdx = line.IndexOf("App url:", StringComparison.OrdinalIgnoreCase);
            if (appIdx >= 0)
            {
                var match = urlRegex.Match(line, appIdx);
                if (match.Success)
                {
                    var raw = match.Value;
                    var q = raw.IndexOf('?');
            var baseUrl = q >= 0 ? raw.Substring(0, q) : raw;
            if (baseUrl.EndsWith('/')) baseUrl = baseUrl.TrimEnd('/');
                    tcs.TrySetResult(baseUrl);
                    return;
                }
            }

            // Kestrel-style output: "Now listening on: http://127.0.0.1:52901"
            var listenIdx = line.IndexOf("Now listening on:", StringComparison.OrdinalIgnoreCase);
            if (listenIdx >= 0)
            {
                var match = urlRegex.Match(line, listenIdx);
                if (match.Success)
                {
                    var url = match.Value;
                    if (url.EndsWith('/')) url = url.TrimEnd('/');
                    tcs.TrySetResult(url);
                    return;
                }
            }
            // Ignore other URLs (e.g., echoed --urls)
        }

        serverProcess.OutputDataReceived += (_, e) => TryParse(e.Data);
        serverProcess.ErrorDataReceived += (_, e) => TryParse(e.Data);
        serverProcess.BeginOutputReadLine();
        serverProcess.BeginErrorReadLine();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await using (cts.Token.Register(() => tcs.TrySetCanceled()))
        {
            try
            {
                return await tcs.Task.ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                throw new InvalidOperationException("Timed out waiting for UI app URL from dev server output.");
            }
        }
    }
}
