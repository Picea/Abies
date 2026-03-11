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
using Picea.Abies.Server.Kestrel;

namespace Picea.Abies.Server.Kestrel.Tests;

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

    [Fact]
    public async Task Traces_Endpoint_Returns_204_When_No_Collector_Configured()
    {
        // Arrange
        var content = new ByteArrayContent([0x0A, 0x01, 0x02]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");

        // Act
        var response = await _client.PostAsync("/otlp/v1/traces", content);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Metrics_Endpoint_Returns_204_When_No_Collector_Configured()
    {
        var content = new ByteArrayContent([0x0A, 0x01, 0x02]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");

        var response = await _client.PostAsync("/otlp/v1/metrics", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logs_Endpoint_Returns_204_When_No_Collector_Configured()
    {
        var content = new ByteArrayContent([0x0A, 0x01, 0x02]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");

        var response = await _client.PostAsync("/otlp/v1/logs", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_Unsupported_Content_Type()
    {
        var content = new StringContent("not protobuf");
        content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        var response = await _client.PostAsync("/otlp/v1/traces", content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Accepts_Application_Json_Content_Type()
    {
        var content = new StringContent("{}");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await _client.PostAsync("/otlp/v1/traces", content);

        // Should get 204 (no collector) not 415 (unsupported)
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_Oversized_Request()
    {
        // MaxRequestSizeBytes = 1024 in test setup
        var oversizedBody = new byte[2048];
        var content = new ByteArrayContent(oversizedBody);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");

        var response = await _client.PostAsync("/otlp/v1/traces", content);

        Assert.Equal(HttpStatusCode.PayloadTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task Rate_Limiting_Blocks_Excess_Requests()
    {
        // RateLimitPerMinute = 5 in test setup
        var content = () =>
        {
            var c = new ByteArrayContent([0x0A]);
            c.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
            return c;
        };

        // First 5 requests should succeed
        for (var i = 0; i < 5; i++)
        {
            var response = await _client.PostAsync("/otlp/v1/traces", content());
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        // 6th request should be rate limited
        var rateLimitedResponse = await _client.PostAsync("/otlp/v1/traces", content());
        Assert.Equal(HttpStatusCode.TooManyRequests, rateLimitedResponse.StatusCode);
        Assert.Equal("60", rateLimitedResponse.Headers.GetValues("Retry-After").First());
    }

    [Fact]
    public void CheckRateLimit_Allows_Within_Limit()
    {
        for (var i = 0; i < 10; i++)
        {
            Assert.True(OtlpProxyEndpoint.CheckRateLimit("test-client", 10));
        }
    }

    [Fact]
    public void CheckRateLimit_Blocks_Over_Limit()
    {
        for (var i = 0; i < 5; i++)
        {
            OtlpProxyEndpoint.CheckRateLimit("test-client-2", 5);
        }

        Assert.False(OtlpProxyEndpoint.CheckRateLimit("test-client-2", 5));
    }

    [Fact]
    public void CheckRateLimit_Isolates_Clients()
    {
        // Fill up client A
        for (var i = 0; i < 3; i++)
        {
            OtlpProxyEndpoint.CheckRateLimit("client-a", 3);
        }

        // Client B should still be allowed
        Assert.True(OtlpProxyEndpoint.CheckRateLimit("client-b", 3));

        // Client A should be blocked
        Assert.False(OtlpProxyEndpoint.CheckRateLimit("client-a", 3));
    }
}

/// <summary>
/// Tests for the OTLP proxy with a mock collector backend.
/// </summary>
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

        var collectorClient = _collectorHost.GetTestClient();

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

    [Fact]
    public async Task Forwards_Protobuf_Body_To_Collector()
    {
        // Arrange
        var traceData = new byte[] { 0x0A, 0x12, 0x34, 0x56, 0x78 };
        var content = new ByteArrayContent(traceData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");

        // Act
        var response = await _client.PostAsync("/otlp/v1/traces", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(_receivedBodies);
        Assert.Equal(traceData, _receivedBodies[0]);
    }

    [Fact]
    public async Task Preserves_Content_Type_When_Forwarding()
    {
        var content = new ByteArrayContent([0x0A]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");

        await _client.PostAsync("/otlp/v1/traces", content);

        Assert.Single(_receivedContentTypes);
        Assert.Contains("application/x-protobuf", _receivedContentTypes[0]);
    }

    [Fact]
    public async Task Forwards_Multiple_Requests()
    {
        for (var i = 0; i < 3; i++)
        {
            var content = new ByteArrayContent([(byte)i]);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
            await _client.PostAsync("/otlp/v1/traces", content);
        }

        Assert.Equal(3, _receivedBodies.Count);
        Assert.Equal([0], _receivedBodies[0]);
        Assert.Equal([1], _receivedBodies[1]);
        Assert.Equal([2], _receivedBodies[2]);
    }
}
