// =============================================================================
// AbiesServerOptions — Security & Hosting Options for MapAbies
// =============================================================================
// Optional configuration passed to MapAbies to harden the server-rendered
// HTML host and the interactive WebSocket endpoint.
//
// Two concerns are covered:
//
//   1. Security headers on the HTML page response. Low-risk headers
//      (X-Content-Type-Options, X-Frame-Options, Referrer-Policy) default ON
//      because they cannot break a typical app. A Content-Security-Policy is
//      opt-in because a too-strict default would block the bootstrap script.
//
//   2. Cross-Site WebSocket Hijacking protection. WebSocket upgrades are not
//      subject to the Same-Origin Policy or CORS, so any site can open a
//      connection to the interactive endpoint. AllowedWebSocketOrigins lets the
//      host restrict which Origin values are accepted. When no allowlist is
//      configured, same-origin connections are allowed (and a warning logged).
//
// Header names/values match the pattern used by the Conduit API
// (Conduit.Api/Program.cs).
//
// See also:
//   - Endpoints.cs — MapAbies wiring
//   - Picea.Abies.Server/Page.cs — bootstrap script emission
// =============================================================================

namespace Picea.Abies.Server.Kestrel;

/// <summary>
/// Optional security and hosting options for <see cref="Endpoints.MapAbies"/>.
/// </summary>
public sealed record AbiesServerOptions
{
    // ── Security Headers (HTML page response) ────────────────────────────────

    /// <summary>
    /// Emit <c>X-Content-Type-Options: nosniff</c> on the HTML page response.
    /// Defaults to <c>true</c> — it is safe for virtually all apps.
    /// </summary>
    public bool ContentTypeOptionsNoSniff { get; init; } = true;

    /// <summary>
    /// Value for the <c>X-Frame-Options</c> header on the HTML page response.
    /// Defaults to <c>"DENY"</c>. Set to <c>null</c> to omit the header (for
    /// apps that intentionally allow framing).
    /// </summary>
    public string? FrameOptions { get; init; } = "DENY";

    /// <summary>
    /// Value for the <c>Referrer-Policy</c> header on the HTML page response.
    /// Defaults to <c>"strict-origin-when-cross-origin"</c>. Set to <c>null</c>
    /// to omit the header.
    /// </summary>
    public string? ReferrerPolicy { get; init; } = "strict-origin-when-cross-origin";

    /// <summary>
    /// Value for the <c>Content-Security-Policy</c> header on the HTML page
    /// response. Opt-in (defaults to <c>null</c>) because the bootstrap uses an
    /// external/inline script and a too-strict default would break apps. When
    /// set, the value is emitted verbatim.
    /// </summary>
    public string? ContentSecurityPolicy { get; init; }

    // ── WebSocket Origin Allowlist ───────────────────────────────────────────

    /// <summary>
    /// Allowlist of <c>Origin</c> header values permitted to open an interactive
    /// WebSocket connection (e.g. <c>"https://app.example.com"</c>). Matching is
    /// case-insensitive on the full origin string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// WebSocket upgrades are not protected by the Same-Origin Policy or CORS, so
    /// without this check any web page could connect to the interactive endpoint
    /// (Cross-Site WebSocket Hijacking).
    /// </para>
    /// <para>
    /// When this allowlist is empty (the default), connections are accepted only
    /// when the <c>Origin</c> host matches the request host (same-origin), and a
    /// warning is logged that no explicit allowlist is configured. Configure this
    /// for production deployments behind proxies or with multiple origins.
    /// </para>
    /// </remarks>
    public IReadOnlyList<string> AllowedWebSocketOrigins { get; init; } = [];
}
