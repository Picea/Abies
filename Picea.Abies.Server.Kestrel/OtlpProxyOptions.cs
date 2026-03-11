// =============================================================================
// OtlpProxyOptions — Configuration for the OTLP Proxy Endpoint
// =============================================================================
// Controls how browser-originated OTLP trace data is proxied to a backend
// collector. The proxy resolves the collector endpoint from (in priority order):
//
//   1. Explicit OtlpProxyOptions.CollectorEndpoint
//   2. OTEL_EXPORTER_OTLP_ENDPOINT environment variable (OTel standard)
//   3. DOTNET_DASHBOARD_OTLP_ENDPOINT_URL environment variable (Aspire)
//   4. IConfiguration "OpenTelemetry:Endpoint" key
//
// If no collector is configured, the proxy returns 204 No Content (graceful
// degradation — the browser OTel SDK silently drops the batch).
//
// See also:
//   - OtlpProxyEndpoint.cs — endpoint implementation
//   - abies-otel.js — browser-side OTel instrumentation
// =============================================================================

namespace Picea.Abies.Server.Kestrel;

/// <summary>
/// Configuration options for the OTLP proxy endpoint.
/// </summary>
public sealed record OtlpProxyOptions
{
    /// <summary>
    /// The OTLP collector endpoint URL to forward traces to.
    /// When null, the proxy auto-detects from environment variables and configuration.
    /// </summary>
    public string? CollectorEndpoint { get; init; }

    /// <summary>
    /// Maximum request body size in bytes. Requests exceeding this limit
    /// receive a 413 Payload Too Large response.
    /// </summary>
    /// <remarks>
    /// Default: 1 MB (1,048,576 bytes). This accommodates typical browser
    /// trace batches while preventing abuse from oversized payloads.
    /// </remarks>
    public int MaxRequestSizeBytes { get; init; } = 1_048_576;

    /// <summary>
    /// Maximum number of requests per client IP per minute.
    /// Requests exceeding this limit receive a 429 Too Many Requests response.
    /// </summary>
    /// <remarks>
    /// Default: 120 requests/minute (2 per second). The OTel browser SDK
    /// typically exports batches every 5–30 seconds, so this limit is
    /// generous for normal usage while preventing abuse.
    /// </remarks>
    public int RateLimitPerMinute { get; init; } = 120;

    /// <summary>
    /// Path prefix for OTLP proxy endpoints.
    /// </summary>
    /// <remarks>
    /// Default: "otlp/v1". This maps the proxy at:
    /// <list type="bullet">
    ///   <item><c>POST /otlp/v1/traces</c></item>
    ///   <item><c>POST /otlp/v1/metrics</c> (future)</item>
    ///   <item><c>POST /otlp/v1/logs</c> (future)</item>
    /// </list>
    /// </remarks>
    public string PathPrefix { get; init; } = "otlp/v1";
}
