// =============================================================================
// OtlpProxyEndpoint — Browser → Server → Collector OTLP Forwarding
// =============================================================================
// Provides a reverse proxy for browser-originated OpenTelemetry trace data.
//
// Architecture:
//   Browser (abies-otel.js)         Server (this endpoint)         Collector
//   ┌───────────────────┐          ┌──────────────────────┐       ┌─────────┐
//   │ OTel Browser SDK  │          │ POST /otlp/v1/traces │       │ Aspire  │
//   │ - DOM event spans ├──────────► - Validate size      ├───────► Jaeger  │
//   │ - Fetch spans     │ protobuf │ - Rate limit         │ OTLP  │ Zipkin  │
//   │ - traceparent     │          │ - Forward bytes      │       │ etc.    │
//   └───────────────────┘          └──────────────────────┘       └─────────┘
//
// The proxy is a raw byte-forwarding relay — it does not deserialize the
// protobuf payload. This keeps the server dependency-free (no OTel protobuf
// packages needed) and minimizes latency.
//
// Security:
//   - Request size capped (default 1 MB) to prevent DoS
//   - Per-IP rate limiting to prevent abuse
//   - Content-Type validation (application/x-protobuf or application/json)
//   - No authentication by default — traces contain performance data, not secrets
//
// Graceful degradation:
//   - No collector configured → 204 No Content (silent drop)
//   - Collector unreachable → 502 Bad Gateway with body
//   - Rate limit exceeded → 429 Too Many Requests
//
// See also:
//   - OtlpProxyOptions.cs — configuration
//   - abies-otel.js — browser-side OTel instrumentation
//   - Endpoints.cs — MapAbies integration
// =============================================================================

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Picea.Abies.Server.Kestrel;

/// <summary>
/// Extension methods for mapping OTLP proxy endpoints to ASP.NET Core.
/// </summary>
public static class OtlpProxyEndpoint
{
    private static readonly ActivitySource _activitySource = new("Picea.Abies.Server.Kestrel.OtlpProxy");

    /// <summary>
    /// Rate limiter: tracks request timestamps per client IP.
    /// Uses a sliding window of 1 minute.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Queue<DateTime>> _rateLimitBuckets = new();

    /// <summary>
    /// Time-to-live for inactive rate limit buckets.
    /// Buckets with no requests newer than this TTL are removed to avoid unbounded growth.
    /// </summary>
    private static readonly TimeSpan _rateLimitBucketTtl = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Background timer that periodically scans and cleans up stale rate limit buckets.
    /// Stored in a static field to prevent garbage collection.
    /// </summary>
    private static readonly Timer _rateLimitCleanupTimer = new(
        _ => CleanupRateLimitBuckets(),
        state: null,
        dueTime: _rateLimitBucketTtl,
        period: _rateLimitBucketTtl);

    /// <summary>
    /// Static fallback HttpClient used when IHttpClientFactory is not registered.
    /// Avoids creating a new HttpClient per request (socket exhaustion).
    /// </summary>
    private static readonly HttpClient _fallbackHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10),
    };

    /// <summary>
    /// Periodically invoked to trim old timestamps from each bucket and remove buckets
    /// that have been inactive longer than <see cref="_rateLimitBucketTtl"/>.
    /// This prevents the static <see cref="_rateLimitBuckets"/> dictionary from growing
    /// without bound in long-running processes with many unique client IPs.
    /// </summary>
    private static void CleanupRateLimitBuckets()
    {
        var cutoff = DateTime.UtcNow - _rateLimitBucketTtl;

        foreach (var (key, timestamps) in _rateLimitBuckets)
        {
            lock (timestamps)
            {
                while (timestamps.Count > 0 && timestamps.Peek() < cutoff)
                {
                    timestamps.Dequeue();
                }

                if (timestamps.Count == 0)
                {
                    _rateLimitBuckets.TryRemove(key, out _);
                }
            }
        }
    }

    /// <summary>
    /// Maps OTLP proxy endpoints for forwarding browser traces to a collector.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The collector endpoint is resolved in priority order:
    /// </para>
    /// <list type="number">
    ///   <item><see cref="OtlpProxyOptions.CollectorEndpoint"/> (explicit)</item>
    ///   <item><c>OTEL_EXPORTER_OTLP_TRACES_ENDPOINT</c> environment variable</item>
    ///   <item><c>ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL</c> environment variable (Aspire, preferred)</item>
    ///   <item><c>DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL</c> environment variable (Aspire alternative)</item>
    ///   <item><c>OTEL_EXPORTER_OTLP_ENDPOINT</c> environment variable (only when OTEL_EXPORTER_OTLP_PROTOCOL is HTTP)</item>
    ///   <item><c>DOTNET_DASHBOARD_OTLP_ENDPOINT_URL</c> environment variable (Aspire, fallback)</item>
    ///   <item><c>OpenTelemetry:Endpoint</c> configuration key</item>
    /// </list>
    /// <para>
    /// When no collector is configured, the endpoint returns <c>204 No Content</c>
    /// for graceful degradation — the browser OTel SDK silently drops the batch.
    /// </para>
    /// <example>
    /// <code>
    /// // Auto-detect from environment variables:
    /// app.MapOtlpProxy();
    ///
    /// // Explicit configuration:
    /// app.MapOtlpProxy(new OtlpProxyOptions
    /// {
    ///     CollectorEndpoint = "http://localhost:4318",
    ///     MaxRequestSizeBytes = 2 * 1024 * 1024
    /// });
    /// </code>
    /// </example>
    /// </remarks>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="options">Optional configuration. Defaults are used when null.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapOtlpProxy(
        this IEndpointRouteBuilder endpoints,
        OtlpProxyOptions? options = null)
    {
        options ??= new OtlpProxyOptions();

        // Resolve collector endpoint from options → env vars → configuration
        var collectorUrl = ResolveCollectorEndpoint(options, endpoints);

        // Create a shared HttpClient for forwarding (pooled connections)
        var httpClientFactory = endpoints.ServiceProvider.GetService<IHttpClientFactory>();
        var loggerFactory = endpoints.ServiceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("Picea.Abies.OtlpProxy");

        if (collectorUrl is not null)
        {
            logger?.LogInformation("OTLP proxy enabled — forwarding to {CollectorUrl}", collectorUrl);
        }
        else
        {
            logger?.LogInformation(
                "OTLP proxy enabled — no collector configured. " +
                "Set OTEL_EXPORTER_OTLP_TRACES_ENDPOINT or configure OpenTelemetry:Endpoint to enable forwarding.");
        }

        // Map POST /otlp/v1/traces
        endpoints.MapPost($"{options.PathPrefix}/traces", async (HttpContext context) =>
        {
            await HandleOtlpRequest(context, collectorUrl, "traces", options, httpClientFactory, logger);
        });

        // Map POST /otlp/v1/metrics (future — same forwarding pattern)
        endpoints.MapPost($"{options.PathPrefix}/metrics", async (HttpContext context) =>
        {
            await HandleOtlpRequest(context, collectorUrl, "metrics", options, httpClientFactory, logger);
        });

        // Map POST /otlp/v1/logs (future — same forwarding pattern)
        endpoints.MapPost($"{options.PathPrefix}/logs", async (HttpContext context) =>
        {
            await HandleOtlpRequest(context, collectorUrl, "logs", options, httpClientFactory, logger);
        });

        return endpoints;
    }

    /// <summary>
    /// Handles an incoming OTLP request: validates, rate-limits, and forwards.
    /// </summary>
    private static async Task HandleOtlpRequest(
        HttpContext context,
        string? collectorUrl,
        string signalType,
        OtlpProxyOptions options,
        IHttpClientFactory? httpClientFactory,
        ILogger? logger)
    {
        using var activity = _activitySource.StartActivity($"Picea.Abies.OtlpProxy.{signalType}");
        activity?.SetTag("otlp.signal_type", signalType);

        // --- Validate Content-Type ---
        var contentType = context.Request.ContentType;
        if (contentType is null ||
            (!contentType.Contains("application/x-protobuf") &&
             !contentType.Contains("application/json")))
        {
            context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
            await context.Response.WriteAsync(
                "Unsupported Content-Type. Expected application/x-protobuf or application/json.");
            activity?.SetStatus(ActivityStatusCode.Error, "UnsupportedMediaType");
            return;
        }

        // --- Validate Request Size ---
        if (context.Request.ContentLength > options.MaxRequestSizeBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await context.Response.WriteAsync(
                $"Request body exceeds maximum size of {options.MaxRequestSizeBytes} bytes.");
            activity?.SetStatus(ActivityStatusCode.Error, "PayloadTooLarge");
            return;
        }

        // --- Rate Limiting ---
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!CheckRateLimit(clientIp, options.RateLimitPerMinute))
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = "60";
            await context.Response.WriteAsync("Rate limit exceeded. Try again later.");
            activity?.SetStatus(ActivityStatusCode.Error, "RateLimitExceeded");
            logger?.LogWarning("OTLP proxy rate limit exceeded for {ClientIp}", clientIp);
            return;
        }

        // --- No Collector → Graceful Degradation ---
        if (collectorUrl is null)
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            activity?.SetTag("otlp.forwarded", false);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        // --- Read Request Body ---
        using var memoryStream = new MemoryStream();
        await context.Request.Body.CopyToAsync(memoryStream, context.RequestAborted);

        // Double-check actual size (Content-Length can be absent for chunked transfer)
        if (memoryStream.Length > options.MaxRequestSizeBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await context.Response.WriteAsync(
                $"Request body exceeds maximum size of {options.MaxRequestSizeBytes} bytes.");
            activity?.SetStatus(ActivityStatusCode.Error, "PayloadTooLarge");
            return;
        }

        // --- Forward to Collector ---
        var targetUrl = BuildTargetUrl(collectorUrl, signalType);
        activity?.SetTag("otlp.target_url", targetUrl);

        try
        {
            var client = httpClientFactory?.CreateClient("OtlpProxy") ?? _fallbackHttpClient;

            using var forwardRequest = new HttpRequestMessage(HttpMethod.Post, targetUrl);
            memoryStream.Position = 0;
            using var forwardContent = new ByteArrayContent(memoryStream.ToArray());
            if (!forwardContent.Headers.TryAddWithoutValidation("Content-Type", contentType))
            {
                forwardContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
            }
            forwardRequest.Content = forwardContent;

            // Forward timeout: 10 seconds max
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            using var response = await client.SendAsync(forwardRequest, cts.Token);

            context.Response.StatusCode = (int)response.StatusCode;
            activity?.SetTag("otlp.collector_status", (int)response.StatusCode);
            activity?.SetStatus(ActivityStatusCode.Ok);

            logger?.LogDebug(
                "OTLP proxy forwarded {SignalType} ({BodySize} bytes) → {StatusCode}",
                signalType, memoryStream.Length, (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            await context.Response.WriteAsync($"Failed to forward to collector: {ex.Message}");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger?.LogWarning(ex, "OTLP proxy failed to forward {SignalType} to {TargetUrl}", signalType, targetUrl);
        }
        catch (OperationCanceledException) when (!context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
            await context.Response.WriteAsync("Collector did not respond within timeout.");
            activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
            logger?.LogWarning("OTLP proxy timeout forwarding {SignalType} to {TargetUrl}", signalType, targetUrl);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync($"Unexpected proxy error: {ex.GetType().Name}: {ex.Message}");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger?.LogError(ex, "OTLP proxy unexpected failure forwarding {SignalType} to {TargetUrl}", signalType, targetUrl);
        }
    }

    /// <summary>
    /// Resolves the OTLP collector endpoint URL from multiple sources.
    /// </summary>
    private static string? ResolveCollectorEndpoint(
        OtlpProxyOptions options,
        IEndpointRouteBuilder endpoints)
    {
        // 1. Explicit option
        if (!string.IsNullOrEmpty(options.CollectorEndpoint))
            return options.CollectorEndpoint;

        // 2. Signal-specific OTel endpoint (already points to /v1/traces)
        var otelTracesEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT");
        if (!string.IsNullOrEmpty(otelTracesEndpoint))
            return otelTracesEndpoint;

        // 3. Aspire HTTP endpoint (preferred for browser OTLP proxying)
        var aspireHttpEndpoint = Environment.GetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL");
        if (!string.IsNullOrEmpty(aspireHttpEndpoint))
            return aspireHttpEndpoint;

        // 4. Aspire HTTP endpoint (alternative name)
        var dotnetDashboardHttpEndpoint = Environment.GetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL");
        if (!string.IsNullOrEmpty(dotnetDashboardHttpEndpoint))
            return dotnetDashboardHttpEndpoint;

        // 5. Standard OTel environment variable (only if explicitly HTTP protocol)
        var otelEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (!string.IsNullOrEmpty(otelEndpoint) && IsHttpOtlpProtocol())
            return otelEndpoint;

        // 6. Aspire Dashboard environment variable (fallback, often gRPC)
        var aspireEndpoint = Environment.GetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL");
        if (!string.IsNullOrEmpty(aspireEndpoint))
            return aspireEndpoint;

        // 7. IConfiguration
        var config = endpoints.ServiceProvider.GetService<IConfiguration>();
        var configEndpoint = config?["OpenTelemetry:Endpoint"];
        if (!string.IsNullOrEmpty(configEndpoint))
            return configEndpoint;

        return null;
    }

    private static bool IsHttpOtlpProtocol()
    {
        var protocol = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL");
        return string.Equals(protocol, "http/protobuf", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(protocol, "http/json", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildTargetUrl(string collectorUrl, string signalType)
    {
        var trimmed = collectorUrl.TrimEnd('/');

        // Support both base collector endpoints (http://host:4318) and
        // signal-specific endpoints (http://host:4318/v1/traces).
        if (trimmed.EndsWith("/v1/traces", StringComparison.OrdinalIgnoreCase) ||
            trimmed.EndsWith("/v1/metrics", StringComparison.OrdinalIgnoreCase) ||
            trimmed.EndsWith("/v1/logs", StringComparison.OrdinalIgnoreCase))
        {
            var v1Index = trimmed.LastIndexOf("/v1/", StringComparison.OrdinalIgnoreCase);
            var baseEndpoint = v1Index >= 0 ? trimmed[..v1Index] : trimmed;
            return $"{baseEndpoint}/v1/{signalType}";
        }

        return $"{trimmed}/v1/{signalType}";
    }

    /// <summary>
    /// Sliding window rate limiter. Returns true if the request is allowed.
    /// </summary>
    internal static bool CheckRateLimit(string clientId, int maxPerMinute)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-1);

        var bucket = _rateLimitBuckets.GetOrAdd(clientId, _ => new Queue<DateTime>());

        lock (bucket)
        {
            // Evict expired entries
            while (bucket.Count > 0 && bucket.Peek() < windowStart)
            {
                bucket.Dequeue();
            }

            if (bucket.Count >= maxPerMinute)
                return false;

            bucket.Enqueue(now);
            return true;
        }
    }

    /// <summary>
    /// Resets rate limit state. Intended for testing only.
    /// </summary>
    internal static void ResetRateLimits() => _rateLimitBuckets.Clear();
}
