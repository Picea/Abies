using Microsoft.Playwright;

namespace Picea.Abies.Conduit.Testing.E2E.Fixtures;

public sealed class ConduitAppHostWasmFixture : IAsyncInitializer, IAsyncDisposable
{
    private ConduitInfraFixture? _infra;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public string BaseUrl => _infra?.WasmUrl ?? throw new InvalidOperationException("Fixture not initialized.");
    public string ApiUrl => _infra?.ApiUrl ?? throw new InvalidOperationException("Fixture not initialized.");

    public async Task<IPage> CreatePageAsync()
    {
        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();
        page.Console += (_, msg) => Console.WriteLine($"[Browser:AppHostWasm {msg.Type}] {msg.Text}");
        page.PageError += (_, error) => Console.WriteLine($"[Browser:AppHostWasm ERROR] {error}");
        return page;
    }

    public async Task InitializeAsync()
    {
        _infra = await SharedInfra.GetAsync();
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("HEADED") != "1",
            SlowMo = Environment.GetEnvironmentVariable("HEADED") == "1" ? 300 : 0
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
            await _browser.DisposeAsync();

        _playwright?.Dispose();
    }
}
