// =============================================================================
// Diagnostics — OpenTelemetry ActivitySource for Conduit API
// =============================================================================

using System.Diagnostics;

namespace Picea.Abies.Conduit.Api;

/// <summary>
/// OpenTelemetry diagnostics for the Conduit REST API.
/// </summary>
internal static class ApiDiagnostics
{
    /// <summary>
    /// The <see cref="ActivitySource"/> for API operations.
    /// </summary>
    internal static readonly ActivitySource Source = new(
        "Picea.Abies.Conduit.Api",
        "1.0.0");
}
