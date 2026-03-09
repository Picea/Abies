// =============================================================================
// Endpoint Tests — Integration Tests for MapAbies
// =============================================================================
// Tests the full HTTP pipeline: request → Kestrel → MapAbies → response.
// Uses WebApplicationFactory to spin up a real Kestrel server in-process.
//
// Covers:
//   1. Static mode serves HTML with correct content-type
//   2. InteractiveServer mode serves HTML with WebSocket script
//   3. WebSocket endpoint rejects non-WebSocket requests
//   4. Page content includes rendered view output
//   5. URL routing is applied from the request path
//
// No external dependencies — pure in-process testing.
// =============================================================================

using System.Net;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using Picea;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Events;

namespace Picea.Abies.Server.Kestrel.Tests;

// ── Test Program: Counter ────────────────────────────────────────────────────────

public record TestModel(int Count, string CurrentPage);

public interface TestMessage : Message;
public record Increment : TestMessage;
public record Decrement : TestMessage;

public sealed class TestCounter : Program<TestModel, Unit>
{
    public static (TestModel, Command) Initialize(Unit argument) =>
        (new TestModel(0, "home"), Commands.None);

    public static (TestModel, Command) Transition(TestModel model, Message message) =>
        message switch
        {
            Increment => (model with { Count = model.Count + 1 }, Commands.None),
            Decrement => (model with { Count = model.Count - 1 }, Commands.None),
            UrlChanged url => (model with
            {
                CurrentPage = url.Url.Path.Count > 0 ? url.Url.Path[0] : "home"
            }, Commands.None),
            _ => (model, Commands.None)
        };

    public static Document View(TestModel model) =>
        new("Test Counter",
            div([class_("counter")],
            [
                h1([], [text($"Count: {model.Count}")]),
                button([class_("increment"), onclick(new Increment())], [text("+")]),
                button([class_("decrement"), onclick(new Decrement())], [text("−")]),
                p([], [text($"Page: {model.CurrentPage}")])
            ]),
            Head.meta("description", "A test counter app"),
            Head.stylesheet("/styles.css"));

    public static Subscription Subscriptions(TestModel model) =>
        new Subscription.None();
}

// ── Test Host Factory ────────────────────────────────────────────────────────

/// <summary>
/// Creates a test server with the Abies application mapped to specific endpoints.
/// </summary>
internal sealed class AbiesTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private AbiesTestHost(WebApplication app) => _app = app;

    /// <summary>
    /// Creates a test host with the given render mode.
    /// </summary>
    public static async Task<AbiesTestHost> Create(
        RenderMode mode,
        string path = "/")
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.UseWebSockets();
        app.UseAbiesStaticFiles();
        app.MapAbies<TestCounter, TestModel, Unit>(path, mode);

        await app.StartAsync();
        return new AbiesTestHost(app);
    }

    /// <summary>
    /// Gets an HttpClient connected to the test server.
    /// </summary>
    public HttpClient CreateClient() =>
        _app.GetTestClient();

    /// <summary>
    /// Gets the test server for WebSocket testing.
    /// </summary>
    public TestServer Server =>
        _app.Services.GetService(typeof(IServer)) as TestServer
        ?? throw new InvalidOperationException("TestServer not available");

    public async ValueTask DisposeAsync() =>
        await _app.DisposeAsync();
}

// ── Tests ────────────────────────────────────────────────────────────────────────

public class EndpointTests
{
    // =========================================================================
    // Static Mode — HTML Page Serving
    // =========================================================================

    [Fact]
    public async Task Static_ServesHtmlWithCorrectContentType()
    {
        await using var host = await AbiesTestHost.Create(new RenderMode.Static());
        using var client = host.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html; charset=utf-8",
            response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task Static_ServesCompleteHtmlDocument()
    {
        await using var host = await AbiesTestHost.Create(new RenderMode.Static());
        using var client = host.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Contains("<title>Test Counter</title>", html);
        Assert.Contains("Count: 0", html);
    }

    [Fact]
    public async Task Static_NoScriptsInOutput()
    {
        await using var host = await AbiesTestHost.Create(new RenderMode.Static());
        using var client = host.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.DoesNotContain("<script", html);
    }

    [Fact]
    public async Task Static_RoutesFromRequestPath()
    {
        await using var host = await AbiesTestHost.Create(
            new RenderMode.Static(), path: "/{**catch-all}");
        using var client = host.CreateClient();

        var html = await client.GetStringAsync("/articles");

        Assert.Contains("Page: articles", html);
    }

    // =========================================================================
    // InteractiveServer Mode — HTML + WebSocket
    // =========================================================================

    [Fact]
    public async Task InteractiveServer_ServesHtmlWithWebSocketScript()
    {
        await using var host = await AbiesTestHost.Create(
            new RenderMode.InteractiveServer());
        using var client = host.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("abies-server.js", html);
        Assert.Contains("data-ws-path", html);
    }

    [Fact]
    public async Task InteractiveServer_WebSocketEndpoint_RejectsNonWebSocket()
    {
        await using var host = await AbiesTestHost.Create(
            new RenderMode.InteractiveServer());
        using var client = host.CreateClient();

        var response = await client.GetAsync("/_abies/ws");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InteractiveServer_CustomWebSocketPath()
    {
        await using var host = await AbiesTestHost.Create(
            new RenderMode.InteractiveServer(WebSocketPath: "/custom/ws"));
        using var client = host.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("""data-ws-path="/custom/ws""", html);
    }

    // =========================================================================
    // InteractiveWasm Mode — HTML + WASM Script, No WebSocket
    // =========================================================================

    [Fact]
    public async Task InteractiveWasm_ServesHtmlWithWasmScript()
    {
        await using var host = await AbiesTestHost.Create(
            new RenderMode.InteractiveWasm());
        using var client = host.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("import { dotnet } from '/_framework/dotnet.js'", html);
        Assert.Contains("await dotnet.run()", html);
        Assert.DoesNotContain("abies-server.js", html);
    }

    // =========================================================================
    // InteractiveAuto Mode — Both Scripts
    // =========================================================================

    [Fact]
    public async Task InteractiveAuto_ServesBothScripts()
    {
        await using var host = await AbiesTestHost.Create(
            new RenderMode.InteractiveAuto());
        using var client = host.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("abies-server.js", html);
        Assert.Contains("import { dotnet } from '/_framework/dotnet.js'", html);
        Assert.Contains("await dotnet.run()", html);
        Assert.Contains("data-auto", html);
    }

    [Fact]
    public async Task InteractiveAuto_WebSocketEndpoint_RejectsNonWebSocket()
    {
        await using var host = await AbiesTestHost.Create(
            new RenderMode.InteractiveAuto());
        using var client = host.CreateClient();

        var response = await client.GetAsync("/_abies/ws");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // =========================================================================
    // Content Tests — Body and Head
    // =========================================================================

    [Fact]
    public async Task Render_IncludesBodyContent()
    {
        await using var host = await AbiesTestHost.Create(new RenderMode.Static());
        using var client = host.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("""class="counter""", html);
        Assert.Contains("Count: 0", html);
        Assert.Contains("Page: home", html);
    }

    [Fact]
    public async Task Render_IncludesHeadElements()
    {
        await using var host = await AbiesTestHost.Create(new RenderMode.Static());
        using var client = host.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("meta", html);
        Assert.Contains("description", html);
        Assert.Contains("stylesheet", html);
    }

    // =========================================================================
    // Static File Serving — abies-server.js
    // =========================================================================

    [Fact]
    public async Task StaticFiles_ServesAbiesServerJs()
    {
        await using var host = await AbiesTestHost.Create(new RenderMode.Static());
        using var client = host.CreateClient();

        var response = await client.GetAsync("/_abies/abies-server.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("abies-server.js", content);
        Assert.Contains("WebSocket", content);
    }

    [Fact]
    public async Task StaticFiles_ServesWithCorrectContentType()
    {
        await using var host = await AbiesTestHost.Create(new RenderMode.Static());
        using var client = host.CreateClient();

        var response = await client.GetAsync("/_abies/abies-server.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.Equal("text/javascript", contentType);
    }

    [Fact]
    public async Task StaticFiles_Returns404ForNonexistentFile()
    {
        await using var host = await AbiesTestHost.Create(new RenderMode.Static());
        using var client = host.CreateClient();

        var response = await client.GetAsync("/_abies/nonexistent.js");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // InteractiveWasm Mode — No WebSocket Endpoint
    // =========================================================================

    [Fact]
    public async Task InteractiveWasm_NoWebSocketEndpoint()
    {
        await using var host = await AbiesTestHost.Create(
            new RenderMode.InteractiveWasm());
        using var client = host.CreateClient();

        // The /_abies/ws endpoint should not exist in InteractiveWasm mode
        var response = await client.GetAsync("/_abies/ws");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task InteractiveWasm_RoutesFromRequestPath()
    {
        await using var host = await AbiesTestHost.Create(
            new RenderMode.InteractiveWasm(), path: "/{**catch-all}");
        using var client = host.CreateClient();

        var html = await client.GetStringAsync("/articles");

        Assert.Contains("Page: articles", html);
        Assert.Contains("import { dotnet } from '/_framework/dotnet.js'", html);
    }

    // =========================================================================
    // UseAbiesWasmFiles — WASM Static File Serving
    // =========================================================================

    [Fact]
    public void UseAbiesWasmFiles_ThrowsForMissingDirectory()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        Assert.Throws<DirectoryNotFoundException>(() =>
            app.UseAbiesWasmFiles("/nonexistent/path/to/wasm"));
    }

    [Fact]
    public async Task UseAbiesWasmFiles_ServesFilesFromAppBundle()
    {
        // Create a temporary directory simulating an AppBundle
        var tempDir = Path.Combine(Path.GetTempPath(), $"abies-wasm-test-{Guid.NewGuid():N}");
        var frameworkDir = Path.Combine(tempDir, "_framework");
        Directory.CreateDirectory(frameworkDir);

        try
        {
            // Write a fake dotnet.js
            await File.WriteAllTextAsync(
                Path.Combine(frameworkDir, "dotnet.js"),
                "// mock dotnet.js for testing\nexport const dotnet = {};");

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();

            var app = builder.Build();
            app.UseAbiesWasmFiles(tempDir);
            app.MapAbies<TestCounter, TestModel, Unit>("/", new RenderMode.InteractiveWasm());

            await app.StartAsync();

            using var client = app.GetTestClient();
            var response = await client.GetAsync("/_framework/dotnet.js");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("mock dotnet.js", content);

            await app.DisposeAsync();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
