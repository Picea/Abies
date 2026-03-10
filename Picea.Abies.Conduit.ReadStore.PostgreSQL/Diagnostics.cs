// =============================================================================
// Diagnostics — OpenTelemetry ActivitySource for PostgreSQL Read Store
// =============================================================================

using System.Diagnostics;

namespace Picea.Abies.Conduit.ReadStore.PostgreSQL;

/// <summary>
/// OpenTelemetry diagnostics for the PostgreSQL read store.
/// </summary>
internal static class ReadStoreDiagnostics
{
    /// <summary>
    /// The <see cref="ActivitySource"/> for read store operations.
    /// </summary>
    internal static readonly ActivitySource Source = new(
        "Picea.Abies.Conduit.ReadStore.PostgreSQL",
        "1.0.0");
}
