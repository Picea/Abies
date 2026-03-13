// =============================================================================
// ConduitInfraFixture — Shared Aspire Backend Infrastructure (Singleton)
// =============================================================================
// Starts the full Conduit backend stack via Aspire exactly once per test run:
//   - KurrentDB (container) — event store
//   - PostgreSQL (container) — read model
//   - Conduit API — REST backend
//
// Uses a thread-safe lazy singleton to ensure Aspire starts once regardless
// of how many render-mode fixtures request it. Each mode fixture calls
// SharedInfra.GetAsync() to obtain the shared API URL.
//
// Architecture:
//   Aspire starts the full backend stack with dynamic port assignment.
//   Each render-mode fixture (Server, WASM, Static, Auto) starts its own
//   Kestrel/frontend server and configures it to proxy API calls to the
//   shared Aspire-managed backend.
//
// Lifecycle:
//   - Created lazily on first access (any fixture's InitializeAsync)
//   - Disposed via AppDomain.ProcessExit hook (runs after all tests)
// =============================================================================

using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace Picea.Abies.Conduit.Testing.E2E.Fixtures;

/// <summary>
/// Provides a shared Aspire backend singleton across all test collections.
/// Thread-safe lazy initialization ensures Aspire starts exactly once.
/// </summary>
public static class SharedInfra
{
    private static readonly Lazy<Task<ConduitInfraFixture>> _instance = new(
        async () =>
        {
            var fixture = new ConduitInfraFixture();
            await fixture.InitializeAsync();

            // Register cleanup for when the test process exits
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
                fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();

            return fixture;
        },
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Gets the shared infrastructure fixture, starting Aspire on first call.
    /// </summary>
    public static Task<ConduitInfraFixture> GetAsync() => _instance.Value;
}

/// <summary>
/// Infrastructure fixture that starts the Conduit Aspire backend.
/// Use <see cref="SharedInfra.GetAsync"/> to access the singleton instance.
/// </summary>
public sealed class ConduitInfraFixture
{
    private DistributedApplication? _app;

    /// <summary>The base URL of the Conduit API (Aspire-managed).</summary>
    public string ApiUrl { get; private set; } = "";

    /// <summary>
    /// Starts the Aspire backend and waits for full health.
    /// </summary>
    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Picea_Abies_Conduit_AppHost>();

        _app = await builder.BuildAsync();
        await _app.StartAsync();

        var apiEndpoint = _app.GetEndpoint("conduit-api", "http");
        ApiUrl = apiEndpoint.ToString().TrimEnd('/');

        try
        {
            var kurrentDbEndpoint = _app.GetEndpoint("kurrentdb", "http");
            Console.WriteLine($"[Infra] KurrentDB endpoint: {kurrentDbEndpoint}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Infra] Could not get KurrentDB endpoint: {ex.Message}");
        }

        await WaitForApiHealthy(ApiUrl);
    }

    /// <summary>
    /// Tears down the Aspire application.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
            await _app.DisposeAsync();
    }

    /// <summary>
    /// Polls the API until it can handle both reads AND writes, or timeout expires.
    /// </summary>
    private static async Task WaitForApiHealthy(string apiUrl, int timeoutSeconds = 300)
    {
        using var http = new HttpClient();
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        var started = DateTime.UtcNow;

        Console.WriteLine($"[Infra] Phase 1: Waiting for API at {apiUrl} to respond...");
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await http.GetAsync($"{apiUrl}/api/tags");
                if (response.IsSuccessStatusCode)
                {
                    var elapsed = (DateTime.UtcNow - started).TotalSeconds;
                    Console.WriteLine($"[Infra] Phase 1 complete: API responding after {elapsed:F1}s");
                    break;
                }
            }
            catch
            {
                // API not ready yet
            }

            await Task.Delay(500);
        }

        Console.WriteLine("[Infra] Phase 2: Waiting for write path (KurrentDB) to be ready...");
        var probeId = Guid.NewGuid().ToString("N")[..10];
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await http.PostAsJsonAsync(
                    $"{apiUrl}/api/users",
                    new { user = new { username = $"probe{probeId}", email = $"probe{probeId}@test.com", password = "probe12345" } });

                if (response.IsSuccessStatusCode || (int)response.StatusCode == 422)
                {
                    var elapsed = (DateTime.UtcNow - started).TotalSeconds;
                    Console.WriteLine($"[Infra] Phase 2 complete: Write path ready after {elapsed:F1}s (status: {(int)response.StatusCode})");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Infra] Write probe error: {ex.GetType().Name}, retrying...");
            }

            await Task.Delay(2000);
        }

        throw new TimeoutException(
            $"Conduit API at {apiUrl} did not become fully healthy within {timeoutSeconds} seconds.");
    }
}
