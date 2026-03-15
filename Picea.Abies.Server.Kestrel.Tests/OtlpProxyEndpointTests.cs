// =============================================================================
// OtlpProxyEndpointTests — Tests for the OTLP Proxy Endpoint
// =============================================================================

using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Picea.Abies.Server.Kestrel.Tests;

[NotInParallel("rate-limit-state")]
public class OtlpProxyEndpointTests : IDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _client;

    public OtlpProxyEndpointTests()
    {
        // Reset rate limits between tests
        OtlpProxyEndpoint.ResetRateLimits();

        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddHttpClient();
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        // Map with no collector configured → should return 204
                        endpoints.MapOtlpProxy(new OtlpProxyOptions
                        {
                            MaxRequestSizeBytes = 1024, // 1KB for testing
                            RateLimitPerMinute = 5,
                        });
                    });
                });
            })
            .Start();

        _client = _host.GetTestClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
        OtlpProxyEndpoint.ResetRateLimits();
    }

    [Test]
    public async Task Traces_Endpoint_Returns_204_When_No_Collector_Configured()
    {
        // Arrange
        using var content = new ByteArrayContent([0x0A, 0x01, 0x02]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");

        // Act
        var response = await _client.PostAsync("/otlp/v1/traces", content);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Metrics_Endpoint_Returns_204_When_No_Collector_Configured()
    {
        using var content = new ByteArrayContent([0x0A, 0x01, 0x02]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");

        var response = await _client.PostAsync("/otlp/v1/metrics", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Logs_Endpoint_Returns_204_When_No_Collector_Configured()
    {
        using var content = new ByteArrayContent([0x0A, 0x01, 0x02]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");

        var response = await _client.PostAsync("/otlp/v1/logs", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Rejects_Unsupported_Content_Type()
    {
        using var content = new StringContent("not protobuf");
        content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        var response = await _client.PostAsync("/otlp/v1/traces", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnsupportedMediaType);
    }

    [Test]
    public async Task Accepts_Application_Json_Content_Type()
    {
        using var content = new StringContent("{}");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await _client.PostAsync("/otlp/v1/traces", content);

        // Should get 204 (no collector) not 415 (unsupported)
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Rejects_Oversized_Request()
    {
        // MaxRequestSizeBytes = 1024 in test setup
        var oversizedBody = new byte[2048];
        using var content = new ByteArrayContent(oversizedBody);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");

        var response = await _client.PostAsync("/otlp/v1/traces", content);

        // HttpStatusCode uses the HTTP/1.0 name RequestEntityTooLarge for 413
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.RequestEntityTooLarge);
    }

    [Test]
    public async Task Rate_Limiting_Blocks_Excess_Requests()
    {
        // RateLimitPerMinute = 5 in test setup

        // First 5 requests should succeed
        for (var i = 0; i < 5; i++)
        {
            using var c = new ByteArrayContent([0x0A]);
            c.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
            var response = await _client.PostAsync("/otlp/v1/traces", c);
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        }

        // 6th request should be rate limited
        using var rateLimitContent = new ByteArrayContent([0x0A]);
        rateLimitContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
        var rateLimitedResponse = await _client.PostAsync("/otlp/v1/traces", rateLimitContent);
        await Assert.That(rateLimitedResponse.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
        await Assert.That(rateLimitedResponse.Headers.GetValues("Retry-After").First()).IsEqualTo("60");
    }

    [Test]
    public async Task CheckRateLimit_Allows_Within_Limit()
    {
        for (var i = 0; i < 10; i++)
        {
            await Assert.That(OtlpProxyEndpoint.CheckRateLimit("test-client", 10)).IsTrue();
        }
    }

    [Test]
    public async Task CheckRateLimit_Blocks_Over_Limit()
    {
        for (var i = 0; i < 5; i++)
        {
            OtlpProxyEndpoint.CheckRateLimit("test-client-2", 5);
        }

        await Assert.That(OtlpProxyEndpoint.CheckRateLimit("test-client-2", 5)).IsFalse();
    }

    [Test]
    public async Task CheckRateLimit_Isolates_Clients()
    {
        // Fill up client A
        for (var i = 0; i < 3; i++)
        {
            OtlpProxyEndpoint.CheckRateLimit("client-a", 3);
        }

        // Client B should still be allowed
        await Assert.That(OtlpProxyEndpoint.CheckRateLimit("client-b", 3)).IsTrue();

        // Client A should be blocked
        await Assert.That(OtlpProxyEndpoint.CheckRateLimit("client-a", 3)).IsFalse();
    }
}

/// <summary>
/// Tests for the OTLP proxy with a mock collector backend.
/// </summary>
[NotInParallel("rate-limit-state")]
public class OtlpProxyForwardingTests : IDisposable
{
    private readonly IHost _collectorHost;
    private readonly IHost _proxyHost;
    private readonly HttpClient _client;
    private readonly List<byte[]> _receivedBodies = [];
    private readonly List<string?> _receivedContentTypes = [];

    public OtlpProxyForwardingTests()
    {
        OtlpProxyEndpoint.ResetRateLimits();

        // Start a mock collector
        _collectorHost = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services => services.AddRouting());
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapPost("/v1/traces", async context =>
                        {
                            using var ms = new MemoryStream();
                            await context.Request.Body.CopyToAsync(ms);
                            _receivedBodies.Add(ms.ToArray());
                            _receivedContentTypes.Add(context.Request.ContentType);
                            context.Response.StatusCode = 200;
                        });
                    });
                });
            })
            .Start();

        // Start the proxy, pointing at the mock collector
        _proxyHost = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    // Register a named HttpClient that uses the test collector
                    services.AddHttpClient("OtlpProxy")
                        .ConfigurePrimaryHttpMessageHandler(() => _collectorHost.GetTestServer().CreateHandler());
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapOtlpProxy(new OtlpProxyOptions
                        {
                            CollectorEndpoint = _collectorHost.GetTestServer().BaseAddress.ToString().TrimEnd('/'),
                        });
                    });
                });
            })
            .Start();

        _client = _proxyHost.GetTestClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _proxyHost.Dispose();
        _collectorHost.Dispose();
        OtlpProxyEndpoint.ResetRateLimits();
    }

    [Test]
    public async Task Forwards_Protobuf_Body_To_Collector()
    {
        // Arrange
        var traceData = new byte[] { 0x0A, 0x12, 0x34, 0x56, 0x78 };
        using var content = new ByteArrayContent(traceData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");

        // Act
        var response = await _client.PostAsync("/otlp/v1/traces", content);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(_receivedBodies).Count().IsEqualTo(1);
        await Assert.That(_receivedBodies[0]).IsEquivalentTo(traceData);
    }

    [Test]
    public async Task Preserves_Content_Type_When_Forwarding()
    {
        using var content = new ByteArrayContent([0x0A]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");

        await _client.PostAsync("/otlp/v1/traces", content);

        await Assert.That(_receivedContentTypes).Count().IsEqualTo(1);
        await Assert.That(_receivedContentTypes[0]).Contains("application/x-protobuf");
    }

    [Test]
    public async Task Forwards_Multiple_Requests()
    {
        for (var i = 0; i < 3; i++)
        {
            using var content = new ByteArrayContent([(byte)i]);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
            await _client.PostAsync("/otlp/v1/traces", content);
        }

        await Assert.That(_receivedBodies.Count).IsEqualTo(3);
        await Assert.That(_receivedBodies[0]).IsEquivalentTo(new byte[] { 0 });
        await Assert.That(_receivedBodies[1]).IsEquivalentTo(new byte[] { 1 });
        await Assert.That(_receivedBodies[2]).IsEquivalentTo(new byte[] { 2 });
    }
}
