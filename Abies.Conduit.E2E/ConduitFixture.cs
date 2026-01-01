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

    public async Task InitializeAsync()
    {
        Microsoft.Playwright.Program.Main(new[] { "install" });
        _playwright = await Playwright.CreateAsync();
        var headed = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HEADED"));
        var slowMoRaw = Environment.GetEnvironmentVariable("PW_SLOWMO_MS");
        _ = int.TryParse(slowMoRaw, out var slowMoMs);

        Browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = !headed,
            SlowMo = slowMoMs > 0 ? slowMoMs : null,
        });

        var apiDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "../../../../Abies.Conduit.Api"));
    var apiStart = new ProcessStartInfo("dotnet", $"run --project {apiDir} --no-launch-profile --urls http://localhost:5179")
        {
            WorkingDirectory = apiDir,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
        };
    _apiServer = Process.Start(apiStart);
    await WaitForServerAsync("http://localhost:5179/api/ping");

        var projectDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "../../../../Abies.Conduit"));
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

    AppBaseUrl = await WaitForAppUrlAsync(_server);
        await WaitForServerAsync(AppBaseUrl);
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
