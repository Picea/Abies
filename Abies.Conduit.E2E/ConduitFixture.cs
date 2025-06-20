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

    public async Task InitializeAsync()
    {
        Microsoft.Playwright.Program.Main(new[] { "install" });
        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });

        var projectDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "../../../../Abies.Conduit"));
        var startInfo = new ProcessStartInfo("dotnet", "run --no-build --project " + projectDir)
        {
            WorkingDirectory = projectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        _server = Process.Start(startInfo);
        await WaitForServerAsync();
    }

    private static async Task WaitForServerAsync()
    {
        using var client = new HttpClient();
        for (int i = 0; i < 30; i++)
        {
            try
            {
                await client.GetAsync("http://localhost:5209");
                return;
            }
            catch
            {
                await Task.Delay(1000);
            }
        }
        throw new InvalidOperationException("Server did not start.");
    }

    public async Task DisposeAsync()
    {
        if (_server != null && !_server.HasExited)
            _server.Kill(true);
        if (Browser != null)
            await Browser.CloseAsync();
        _playwright?.Dispose();
    }
}
